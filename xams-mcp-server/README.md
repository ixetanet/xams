# Xams MCP Server

Public Model Context Protocol (MCP) server providing Xams Framework documentation for Claude Code.

## What is This?

A publicly hosted MCP server that gives Claude Code instant access to all Xams Framework documentation.

**✨ No installation required!** Just add a URL to your Claude Code configuration.

## Quick Setup (30 Seconds)

### 1. Find Your Claude Code MCP Configuration

**Location depends on your system:**
- **macOS:** `~/Library/Application Support/Claude/config/mcp.json`
- **Linux:** `~/.config/claude/mcp.json`
- **Windows:** `%APPDATA%\Claude\config\mcp.json`

If the file doesn't exist, create it.

### 2. Add This Configuration

```json
{
  "mcpServers": {
    "xams": {
      "url": "https://your-mcp-server-url.ondigitalocean.app"
    }
  }
}
```

Replace `your-mcp-server-url.ondigitalocean.app` with your actual deployment URL.

### 3. Restart Claude Code

Completely quit and restart Claude Code for the configuration to take effect.

### 4. Done!

Claude Code now has access to all Xams Framework documentation via MCP URIs.

## Available Resources

Once configured, Claude Code can access these resources:

### Documentation

- `xams://docs/quickstart` - Getting started with Xams
- `xams://docs/entities` - Entity creation and management
- `xams://docs/servicelogic` - Service logic best practices
- `xams://docs/security` - Security patterns and owner field rules
- `xams://docs/attributes` - Field and entity attributes
- `xams://docs/actions` - Custom API endpoints
- `xams://docs/queries` - Query building and filtering
- `xams://docs/firebaseauth` - Firebase authentication setup
- `xams://docs/realtime` - WebSocket/SignalR integration
- `xams://docs/scheduledjobs` - Background job scheduling
- `xams://docs/servicestartup` - Application initialization

### Reference Documentation

- `xams://ref/core-essentials` - Core API essentials
- `xams://ref/core-api` - Complete Core API reference
- `xams://ref/react-essentials` - React API essentials
- `xams://ref/react-api` - Complete React API reference

### Guides

- `xams://guides/security-audit` - Security audit methodology
- `xams://guides/you-might-not-need-an-effect` - React best practices
- `xams://guides/generate-types` - TypeScript type generation script

## Using with Claude Code

### In Your Project's CLAUDE.md

Replace local file paths with MCP URIs:

```markdown
## ⚠️ MANDATORY FIRST ACTIONS REQUIRED

Read these MCP resources in parallel before any other work:

1. `xams://docs/quickstart`
2. `xams://docs/entities`
3. `xams://docs/servicelogic`
4. `xams://ref/core-essentials`
5. `xams://ref/react-essentials`
6. `xams://guides/you-might-not-need-an-effect`
7. `xams://docs/firebaseauth`
8. `xams://docs/security`

Load when relevant:
- Attributes: `xams://docs/attributes`
- Actions: `xams://docs/actions`
- Queries: `xams://docs/queries`
```

### During Conversations

Claude Code will automatically access these resources when needed. You can also explicitly reference them:

```
User: "Check the service logic best practices"
Claude: [Reads xams://docs/servicelogic automatically]
```

## Benefits

✅ **Zero installation** - Just add a URL, no npm, no dependencies
✅ **Always up-to-date** - Server updates automatically, everyone benefits
✅ **No local .claude/ folder** - All docs served from public server
✅ **Cross-platform** - Works on any OS with Claude Code
✅ **Smaller projects** - No documentation bloat in repositories
✅ **Instant onboarding** - Add URL, restart Claude Code, done

## Troubleshooting

### Claude Code Can't Find Resources

1. Verify the URL in your `mcp.json` is correct
2. Restart Claude Code after configuration changes
3. Check server health: Visit `https://your-url.ondigitalocean.app/health` in browser

### Resources Not Loading

1. Test server connectivity:
   ```bash
   curl https://your-url.ondigitalocean.app/health
   # Should return: {"status":"healthy","resources":26,...}
   ```
2. Check Claude Code logs for MCP connection errors
3. Verify you have internet connectivity

### Server URL Changed

If the deployment URL changes, simply update your `mcp.json` with the new URL and restart Claude Code.

## Development & Deployment

### Local Development

```bash
git clone <repository-url>
cd xams-mcp-server
npm install
npm run build
npm start
# Server runs at http://localhost:8080
```

Test the server:
```bash
curl http://localhost:8080/health
```

### Deploying to Digital Ocean

See `DEPLOYMENT.md` for complete deployment instructions.

Quick version:
1. Push code to GitHub
2. Connect repository to Digital Ocean App Platform
3. Digital Ocean auto-builds and deploys
4. Share the deployment URL with your team

### Updating Documentation

1. Copy updated docs to this repository:
   ```bash
   cp -r /path/to/xams/.claude/xams-docs-v1 ./docs/
   cp -r /path/to/xams/.claude/xams_ref ./refs/
   ```
2. Commit and push to GitHub
3. Digital Ocean auto-deploys
4. Everyone gets updates instantly (no action needed)

## Technical Details

- **Protocol:** MCP (Model Context Protocol)
- **Transport:** HTTP with Server-Sent Events (SSE)
- **Language:** TypeScript
- **Runtime:** Node.js ≥18
- **Hosting:** Digital Ocean App Platform
- **Web Framework:** Express
- **Discovery:** Automatic filesystem scanning at startup
- **Processing:** MDX → Markdown conversion at runtime
- **Endpoints:**
  - `GET /` - Server info
  - `GET /health` - Health check
  - `GET /sse` - MCP SSE endpoint
  - `POST /messages` - MCP message handler

## License

MIT

## Support

For issues and questions:
- GitHub Issues: [repository-url]/issues
- Xams Documentation: https://xams.ixeta.com
