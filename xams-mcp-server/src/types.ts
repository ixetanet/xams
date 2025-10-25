/**
 * Represents an MCP resource that can be served
 */
export interface XamsResource {
  /** MCP URI (e.g., "xams://docs/quickstart") */
  uri: string;
  /** Human-readable name */
  name: string;
  /** Description of the resource */
  description: string;
  /** MIME type */
  mimeType: string;
  /** Absolute file path */
  filePath: string;
}

/**
 * Resource category for organization
 */
export enum ResourceCategory {
  Docs = "docs",
  Ref = "ref",
  Guides = "guides",
}

/**
 * Result of resource discovery
 */
export interface DiscoveryResult {
  resources: XamsResource[];
  errors: string[];
}
