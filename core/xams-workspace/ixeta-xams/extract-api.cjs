#!/usr/bin/env node

/**
 * Extracts minimal API signatures from TypeScript declaration files
 * Outputs a concise, Claude-friendly API reference
 */

const fs = require("fs");
const path = require("path");

const DIST_TYPES = path.join(__dirname, "dist", "types");
const OUTPUT_FILE = path.join(__dirname, "REACT_API_SIGNATURES.md");

// Read and parse index.d.ts to get all exports
const indexPath = path.join(DIST_TYPES, "index.d.ts");
const indexContent = fs.readFileSync(indexPath, "utf-8");

// Extract exported items
const exportedItems = new Map();
const lines = indexContent.split("\n");

for (const line of lines) {
  // Match: export { default as Something } from "./path"
  const match1 = line.match(
    /export\s+\{\s*default\s+as\s+(\w+)\s*\}\s+from\s+["']([^"']+)["']/
  );
  if (match1) {
    exportedItems.set(match1[1], {
      name: match1[1],
      path: match1[2],
      type: "default",
    });
    continue;
  }

  // Match: export { Something } from "./path"
  const match2 = line.match(
    /export\s+\{\s*([^}]+)\s*\}\s+from\s+["']([^"']+)["']/
  );
  if (match2) {
    const names = match2[1]
      .split(",")
      .map((n) => n.trim().replace(/\s+as\s+\w+/, ""));
    names.forEach((name) => {
      if (name && !name.startsWith("//")) {
        exportedItems.set(name, { name, path: match2[2], type: "named" });
      }
    });
    continue;
  }

  // Match: export * from "./path"
  const match3 = line.match(/export\s+\*\s+from\s+["']([^"']+)["']/);
  if (match3) {
    exportedItems.set(`*:${match3[1]}`, {
      name: "*",
      path: match3[1],
      type: "all",
    });
    continue;
  }

  // Match: export type { Something } from "./path"
  const match4 = line.match(
    /export\s+type\s+\{\s*([^}]+)\s*\}\s+from\s+["']([^"']+)["']/
  );
  if (match4) {
    const names = match4[1]
      .split(",")
      .map((n) => n.trim().replace(/\s+as\s+\w+/, ""));
    names.forEach((name) => {
      if (name && !name.startsWith("//")) {
        exportedItems.set(name, { name, path: match4[2], type: "type" });
      }
    });
  }
}

// Helper to read file content
function readTypeFile(relativePath) {
  const fullPath = path.join(DIST_TYPES, relativePath + ".d.ts");
  if (fs.existsSync(fullPath)) {
    return fs.readFileSync(fullPath, "utf-8");
  }
  return null;
}

// Helper to extract interface/type definition
function extractDefinition(content, name) {
  const lines = content.split("\n");
  let inDefinition = false;
  let braceCount = 0;
  let definition = [];

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    // Start of interface/type
    if (
      !inDefinition &&
      (line.includes(`interface ${name}`) ||
        line.includes(`type ${name}`) ||
        line.includes(`export interface ${name}`) ||
        line.includes(`export type ${name}`))
    ) {
      inDefinition = true;
      definition.push(line);
      braceCount += (line.match(/\{/g) || []).length;
      braceCount -= (line.match(/\}/g) || []).length;
      if (braceCount === 0 && line.includes(";")) {
        break;
      }
      continue;
    }

    if (inDefinition) {
      definition.push(line);
      braceCount += (line.match(/\{/g) || []).length;
      braceCount -= (line.match(/\}/g) || []).length;
      if (braceCount === 0) {
        break;
      }
    }
  }

  return definition.length > 0 ? definition.join("\n") : null;
}

// Build output
let output = `# @ixeta/xams - API Signatures

**Version**: 1.0.16
**Auto-generated**: ${new Date().toISOString().split("T")[0]}

This document contains concise TypeScript signatures for all exported items from @ixeta/xams.

---

`;

// Group exports by category
const hooks = [];
const components = [];
const contexts = [];
const types = [];
const utilities = [];
const other = [];

for (const [key, exp] of exportedItems.entries()) {
  if (exp.path.includes("/hooks/")) hooks.push(exp);
  else if (exp.path.includes("/components/")) components.push(exp);
  else if (exp.path.includes("/contexts/")) contexts.push(exp);
  else if (exp.path.includes("/api/")) types.push(exp);
  else if (exp.path.includes("/utils/")) utilities.push(exp);
  else if (exp.type === "all") {
    // For export *, read the file and extract exports
    const content = readTypeFile(exp.path);
    if (content) {
      types.push({
        name: `[All exports from ${exp.path}]`,
        path: exp.path,
        type: "all",
      });
    }
  } else {
    other.push(exp);
  }
}

// Output each category
function outputCategory(title, items) {
  if (items.length === 0) return;

  output += `## ${title}\n\n`;

  for (const item of items) {
    if (item.type === "all") {
      output += `### ${item.name}\n\n`;
      const content = readTypeFile(item.path);
      if (content) {
        // Extract all export statements
        const exportLines = content
          .split("\n")
          .filter(
            (line) => line.trim().startsWith("export") && !line.includes("from")
          );
        output += "```typescript\n" + exportLines.join("\n") + "\n```\n\n";
      }
      continue;
    }

    const content = readTypeFile(item.path);
    if (!content) continue;

    const def = extractDefinition(content, item.name);
    if (def) {
      output += `### ${item.name}\n\n`;
      output += "```typescript\n" + def + "\n```\n\n";
    } else {
      // Try to find the declaration another way
      const lines = content.split("\n");
      const relevantLines = lines.filter(
        (line) =>
          line.includes(item.name) &&
          (line.includes("declare") || line.includes("export"))
      );
      if (relevantLines.length > 0) {
        output += `### ${item.name}\n\n`;
        output +=
          "```typescript\n" +
          relevantLines.slice(0, 5).join("\n") +
          "\n```\n\n";
      }
    }
  }
}

outputCategory("Hooks", hooks);
outputCategory("Components", components);
outputCategory("Contexts", contexts);
outputCategory("API Types", types);
outputCategory("Utilities", utilities);
outputCategory("Other Exports", other);

// Write output
fs.writeFileSync(OUTPUT_FILE, output);
console.log(`✅ Generated ${OUTPUT_FILE}`);
console.log(`📄 Size: ${(output.length / 1024).toFixed(1)}KB`);
console.log(`📝 Lines: ${output.split("\n").length}`);
