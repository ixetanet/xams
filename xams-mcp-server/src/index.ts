import express from "express";
import cors from "cors";
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

  // MCP Protocol Endpoint - POST for JSON-RPC messages
  app.post("/", express.json(), async (req, res) => {
    console.log("MCP POST request received");

    // Validate headers
    const accept = req.get("accept") || "";
    const protocolVersion = req.get("mcp-protocol-version");

    console.log("Headers:", { accept, protocolVersion });
    console.log("Body:", JSON.stringify(req.body, null, 2));

    // Handle JSON-RPC message
    const message = req.body;

    if (!message || !message.jsonrpc) {
      return res.status(400).json({
        jsonrpc: "2.0",
        error: {
          code: -32600,
          message: "Invalid Request: missing jsonrpc field",
        },
        id: null,
      });
    }

    try {
      // Route message based on method
      if (message.method === "resources/list") {
        const resources = await getResources();
        return res.json({
          jsonrpc: "2.0",
          id: message.id,
          result: {
            resources: resources.map((r) => ({
              uri: r.uri,
              name: r.name,
              description: r.description,
              mimeType: r.mimeType,
            })),
          },
        });
      } else if (message.method === "resources/read") {
        const uri = message.params?.uri;
        if (!uri) {
          return res.status(400).json({
            jsonrpc: "2.0",
            error: {
              code: -32602,
              message: "Invalid params: missing uri",
            },
            id: message.id,
          });
        }

        const resource = await findResource(uri);
        if (!resource) {
          return res.status(404).json({
            jsonrpc: "2.0",
            error: {
              code: -32001,
              message: `Resource not found: ${uri}`,
            },
            id: message.id,
          });
        }

        const content = await readAndProcessFile(resource.filePath);
        return res.json({
          jsonrpc: "2.0",
          id: message.id,
          result: {
            contents: [
              {
                uri: resource.uri,
                mimeType: resource.mimeType,
                text: content,
              },
            ],
          },
        });
      } else if (message.method === "initialize") {
        // Handle MCP initialize handshake
        return res.json({
          jsonrpc: "2.0",
          id: message.id,
          result: {
            protocolVersion: "2024-11-05",
            capabilities: {
              resources: {},
            },
            serverInfo: {
              name: "xams-mcp-server",
              version: "1.0.0",
            },
          },
        });
      } else {
        // Unknown method
        return res.status(400).json({
          jsonrpc: "2.0",
          error: {
            code: -32601,
            message: `Method not found: ${message.method}`,
          },
          id: message.id,
        });
      }
    } catch (error) {
      console.error("Error handling MCP request:", error);
      return res.status(500).json({
        jsonrpc: "2.0",
        error: {
          code: -32603,
          message: "Internal error",
          data: String(error),
        },
        id: message.id,
      });
    }
  });

  // GET endpoint - Info page when accessed via browser
  app.get("/", async (req, res) => {
    // If client accepts SSE, could potentially start SSE stream
    // For now, just return info page
    const resources = await getResources();
    res.json({
      name: "Xams MCP Server",
      description: "Public MCP server providing Xams Framework documentation",
      version: "1.0.0",
      resources: resources.length,
      mcp_endpoint: "/",
      transport: "HTTP with JSON-RPC",
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
