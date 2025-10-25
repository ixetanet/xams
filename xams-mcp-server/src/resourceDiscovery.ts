import { readdir, stat, readFile } from "fs/promises";
import { join, dirname, basename } from "path";
import { fileURLToPath } from "url";
import type { XamsResource, DiscoveryResult } from "./types.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

/**
 * Recursively find all page.mdx files in a directory
 */
async function findPageMdxFiles(
  dir: string,
  baseDir: string
): Promise<string[]> {
  const results: string[] = [];

  try {
    const entries = await readdir(dir, { withFileTypes: true });

    for (const entry of entries) {
      const fullPath = join(dir, entry.name);

      if (entry.isDirectory()) {
        // Recursively search subdirectories
        const subResults = await findPageMdxFiles(fullPath, baseDir);
        results.push(...subResults);
      } else if (entry.name === "page.mdx") {
        // Found a page.mdx file
        results.push(fullPath);
      }
    }
  } catch (error) {
    // Directory doesn't exist or can't be read - silently skip
  }

  return results;
}

/**
 * Extract the first heading from markdown content as the title
 */
function extractTitle(content: string, fallback: string): string {
  const match = content.match(/^#\s+(.+)$/m);
  return match ? match[1].trim() : fallback;
}

/**
 * Extract description from content (first paragraph or sentence)
 */
function extractDescription(content: string): string {
  // Remove headings and get first meaningful paragraph
  const withoutHeadings = content.replace(/^#+\s+.+$/gm, "").trim();
  const firstParagraph = withoutHeadings.split("\n\n")[0];

  if (firstParagraph && firstParagraph.length > 0) {
    // Limit to ~150 characters
    return firstParagraph.length > 150
      ? firstParagraph.substring(0, 147) + "..."
      : firstParagraph;
  }

  return "Xams Framework documentation";
}

/**
 * Convert file path to URI segment
 * e.g., "quickstart/page.mdx" -> "quickstart"
 * e.g., "firebaseauth/page.mdx" -> "firebaseauth"
 */
function pathToUriSegment(filePath: string, appDir: string): string {
  // Get relative path from app directory
  const relativePath = filePath.replace(appDir, "").replace(/^\//, "");

  // Remove /page.mdx suffix and clean up
  return relativePath.replace(/\/page\.mdx$/, "").replace(/\//g, "/");
}

/**
 * Discover all documentation resources from xams-docs-v1
 */
export async function discoverDocs(): Promise<DiscoveryResult> {
  const resources: XamsResource[] = [];
  const errors: string[] = [];

  try {
    // Path to docs folder (from build directory)
    const docsBase = join(__dirname, "..", "docs", "xams-docs-v1", "src", "app");

    // Find all page.mdx files
    const mdxFiles = await findPageMdxFiles(docsBase, docsBase);

    for (const filePath of mdxFiles) {
      try {
        const content = await readFile(filePath, "utf-8");
        const uriSegment = pathToUriSegment(filePath, docsBase);

        // Skip empty segments
        if (!uriSegment) continue;

        const title = extractTitle(content, uriSegment);
        const description = extractDescription(content);

        resources.push({
          uri: `xams://docs/${uriSegment}`,
          name: `Xams: ${title}`,
          description,
          mimeType: "text/markdown",
          filePath,
        });
      } catch (error) {
        errors.push(`Failed to process ${filePath}: ${error}`);
      }
    }
  } catch (error) {
    errors.push(`Failed to discover docs: ${error}`);
  }

  return { resources, errors };
}

/**
 * Discover reference documentation
 */
export async function discoverRefs(): Promise<DiscoveryResult> {
  const resources: XamsResource[] = [];
  const errors: string[] = [];

  try {
    const refsBase = join(__dirname, "..", "refs");
    const entries = await readdir(refsBase, { withFileTypes: true });

    for (const entry of entries) {
      if (entry.isFile() && entry.name.endsWith(".md")) {
        try {
          const filePath = join(refsBase, entry.name);
          const content = await readFile(filePath, "utf-8");

          // Convert filename to URI segment
          // e.g., "XAMS_CORE_ESSENTIALS.md" -> "core-essentials"
          const baseName = basename(entry.name, ".md");
          const uriSegment = baseName
            .replace(/^XAMS_/, "")
            .replace(/_/g, "-")
            .toLowerCase();

          const title = extractTitle(content, baseName);
          const description = extractDescription(content);

          resources.push({
            uri: `xams://ref/${uriSegment}`,
            name: `Xams Reference: ${title}`,
            description,
            mimeType: "text/markdown",
            filePath,
          });
        } catch (error) {
          errors.push(`Failed to process ${entry.name}: ${error}`);
        }
      }
    }
  } catch (error) {
    errors.push(`Failed to discover refs: ${error}`);
  }

  return { resources, errors };
}

/**
 * Discover guides
 */
export async function discoverGuides(): Promise<DiscoveryResult> {
  const resources: XamsResource[] = [];
  const errors: string[] = [];

  try {
    const guidesBase = join(__dirname, "..", "guides");
    const entries = await readdir(guidesBase, { withFileTypes: true });

    for (const entry of entries) {
      if (entry.isFile() && (entry.name.endsWith(".md") || entry.name.endsWith(".sh"))) {
        try {
          const filePath = join(guidesBase, entry.name);
          const content = await readFile(filePath, "utf-8");

          // Convert filename to URI segment
          const ext = entry.name.endsWith(".sh") ? ".sh" : ".md";
          const baseName = basename(entry.name, ext);
          const uriSegment = baseName.replace(/_/g, "-").toLowerCase();

          const title = extractTitle(content, baseName);
          const description = extractDescription(content);

          // Determine MIME type
          const mimeType = entry.name.endsWith(".sh")
            ? "text/x-shellscript"
            : "text/markdown";

          resources.push({
            uri: `xams://guides/${uriSegment}`,
            name: `Xams Guide: ${title}`,
            description,
            mimeType,
            filePath,
          });
        } catch (error) {
          errors.push(`Failed to process ${entry.name}: ${error}`);
        }
      }
    }
  } catch (error) {
    errors.push(`Failed to discover guides: ${error}`);
  }

  return { resources, errors };
}

/**
 * Discover all resources
 */
export async function discoverAllResources(): Promise<DiscoveryResult> {
  const [docsResult, refsResult, guidesResult] = await Promise.all([
    discoverDocs(),
    discoverRefs(),
    discoverGuides(),
  ]);

  return {
    resources: [
      ...docsResult.resources,
      ...refsResult.resources,
      ...guidesResult.resources,
    ],
    errors: [
      ...docsResult.errors,
      ...refsResult.errors,
      ...guidesResult.errors,
    ],
  };
}
