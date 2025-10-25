/**
 * Processes MDX content to plain Markdown by stripping MDX-specific syntax
 */

/**
 * Converts MDX content to plain Markdown
 * Removes imports, JSX components, and other MDX-specific syntax
 * while preserving standard Markdown
 */
export function processMdxToMarkdown(content: string): string {
  let processed = content;

  // Remove import statements
  processed = processed.replace(/^import\s+.*?;?\s*$/gm, "");

  // Remove export statements
  processed = processed.replace(/^export\s+.*?;?\s*$/gm, "");

  // Remove JSX components (simple approach - remove self-closing tags)
  // e.g., <Callout />, <Steps />, etc.
  processed = processed.replace(/<[A-Z]\w*\s*\/>/g, "");

  // Remove opening JSX tags (e.g., <Callout>, <Steps>)
  processed = processed.replace(/<[A-Z]\w*(\s+[^>]*)?>$/gm, "");

  // Remove closing JSX tags (e.g., </Callout>, </Steps>)
  processed = processed.replace(/<\/[A-Z]\w*>/g, "");

  // Clean up excessive blank lines (more than 2 consecutive)
  processed = processed.replace(/\n{3,}/g, "\n\n");

  // Trim leading and trailing whitespace
  processed = processed.trim();

  return processed;
}

/**
 * Determines if a file should be processed as MDX
 */
export function isMdxFile(filePath: string): boolean {
  return filePath.endsWith(".mdx");
}

/**
 * Reads and processes a file, converting MDX to Markdown if needed
 */
export async function readAndProcessFile(filePath: string): Promise<string> {
  const fs = await import("fs/promises");
  const content = await fs.readFile(filePath, "utf-8");

  if (isMdxFile(filePath)) {
    return processMdxToMarkdown(content);
  }

  return content;
}
