import express from "express";
import cors from "cors";
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { SSEServerTransport } from "@modelcontextprotocol/sdk/server/sse.js";
import {
  ListResourcesRequestSchema,
  ReadResourceRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { discoverAllResources } from "./resourceDiscovery.js";
import { readAndProcessFile } from "./mdxProcessor.js";
import type { XamsResource } from "./types.js";

// Resource cache
let resourceCache: XamsResource[] | null = null;

/**
 * Get all available resources (cached)
 */
async function getResources(): Promise<XamsResource[]> {
  if (resourceCache) {
    return resourceCache;
  }

  const result = await discoverAllResources();

  if (result.errors.length > 0) {
    console.error("Resource discovery errors:");
    result.errors.forEach((error) => console.error(`  - ${error}`));
  }

  resourceCache = result.resources;
  console.log(`Discovered ${resourceCache.length} resources`);

  return resourceCache;
}

/**
 * Find a resource by URI
 */
async function findResource(uri: string): Promise<XamsResource | undefined> {
  const resources = await getResources();
  return resources.find((r) => r.uri === uri);
}

/**
 * Create and configure MCP server
 */
function createMCPServer(): Server {
  const server = new Server(
    {
      name: "xams-mcp-server",
      version: "1.0.0",
    },
    {
      capabilities: {
        resources: {},
      },
    }
  );

  // Handle resource listing
  server.setRequestHandler(ListResourcesRequestSchema, async () => {
    const resources = await getResources();

    return {
      resources: resources.map((r) => ({
        uri: r.uri,
        name: r.name,
        description: r.description,
        mimeType: r.mimeType,
      })),
    };
  });

  // Handle resource reading
  server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
    const resource = await findResource(request.params.uri);

    if (!resource) {
      throw new Error(`Resource not found: ${request.params.uri}`);
    }

    try {
      const content = await readAndProcessFile(resource.filePath);

      return {
        contents: [
          {
            uri: resource.uri,
            mimeType: resource.mimeType,
            text: content,
          },
        ],
      };
    } catch (error) {
      throw new Error(
        `Failed to read resource ${request.params.uri}: ${error}`
      );
    }
  });

  return server;
}

/**
 * Main server setup
 */
async function main() {
  console.log("Starting Xams MCP Server (HTTP)...");

  // Pre-load resources
  await getResources();

  const app = express();
  const PORT = parseInt(process.env.PORT || "8080", 10);

  // Enable CORS for public access
  app.use(cors());

  // Health check endpoint for Digital Ocean
  app.get("/health", (req, res) => {
    res.status(200).json({
      status: "healthy",
      resources: resourceCache?.length || 0,
      timestamp: new Date().toISOString(),
    });
  });

  // Info endpoint
  app.get("/", async (req, res) => {
    const resources = await getResources();
    res.json({
      name: "Xams MCP Server",
      description: "Public MCP server providing Xams Framework documentation",
      version: "1.0.0",
      resources: resources.length,
      mcp_endpoint: "/sse",
      usage: {
        claude_code_config: {
          mcpServers: {
            xams: {
              url: `${req.protocol}://${req.get("host")}`,
            },
          },
        },
      },
    });
  });

  // SSE endpoint for MCP protocol
  app.get("/sse", async (req, res) => {
    console.log("New MCP connection via SSE");

    const transport = new SSEServerTransport("/messages", res);
    const server = createMCPServer();

    await server.connect(transport);

    // Keep connection alive
    req.on("close", () => {
      console.log("MCP connection closed");
    });
  });

  // POST endpoint for messages
  app.post("/messages", express.json(), async (req, res) => {
    // This is handled by SSE transport, but we need the route
    res.status(200).end();
  });

  // Debug/Testing endpoint - fetch resource directly via REST
  app.get("/api/resource", async (req, res) => {
    const uri = req.query.uri as string;

    if (!uri) {
      return res.status(400).json({
        error: "Missing uri parameter",
        usage: "GET /api/resource?uri=xams://docs/quickstart",
      });
    }

    const resource = await findResource(uri);

    if (!resource) {
      return res.status(404).json({
        error: "Resource not found",
        uri,
        available: "GET /api/resources for list of available URIs",
      });
    }

    try {
      const content = await readAndProcessFile(resource.filePath);

      res.json({
        uri: resource.uri,
        name: resource.name,
        description: resource.description,
        mimeType: resource.mimeType,
        content,
      });
    } catch (error) {
      res.status(500).json({
        error: "Failed to read resource",
        uri,
        details: String(error),
      });
    }
  });

  // Debug/Testing endpoint - list all resources
  app.get("/api/resources", async (req, res) => {
    const resources = await getResources();

    res.json({
      total: resources.length,
      resources: resources.map((r) => ({
        uri: r.uri,
        name: r.name,
        description: r.description,
      })),
    });
  });

  // Start server
  app.listen(PORT, () => {
    console.log(`Xams MCP Server listening on port ${PORT}`);
    console.log(`Health check: http://localhost:${PORT}/health`);
    console.log(`MCP endpoint: http://localhost:${PORT}/sse`);
    console.log(`Ready to serve ${resourceCache?.length || 0} resources`);
  });
}

// Start the server
main().catch((error) => {
  console.error("Fatal server error:", error);
  process.exit(1);
});
