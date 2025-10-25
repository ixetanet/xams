# Xams MCP Server - Quick Start Guide

## For Server Administrators (Deployment)

### 1. Create GitHub Repository

```bash
# Navigate to the xams-mcp-server directory
cd /path/to/xams-mcp-server

# Initialize git
git init
git add .
git commit -m "Initial commit: Xams MCP Server v1.0.0"

# Create repository on GitHub (via web interface):
# - Name: xams-mcp-server
# - Description: Public MCP server for Xams Framework documentation
# - Public or Private (your choice)

# Push to GitHub
git remote add origin https://github.com/YOUR_USERNAME/xams-mcp-server.git
git branch -M main
git push -u origin main
```

### 2. Update app.yaml

Edit `app.yaml` and replace `YOUR_USERNAME` with your GitHub username:

```yaml
github:
  repo: YOUR_USERNAME/xams-mcp-server
  branch: main
```

Commit and push the change:

```bash
git add app.yaml
git commit -m "Update GitHub repository in app.yaml"
git push
```

### 3. Deploy to Digital Ocean

1. **Login to Digital Ocean:**
   - Visit https://cloud.digitalocean.com/apps
   - Click "Create App"

2. **Connect GitHub:**
   - Select "GitHub" as source
   - Authorize Digital Ocean to access your repositories
   - Select your `xams-mcp-server` repository
   - Branch: `main`
   - Auto-deploy: Enabled

3. **Configure App (Digital Ocean will auto-detect from app.yaml):**
   - Resource type: Web Service
   - Build command: `npm run build`
   - Run command: `npm start`
   - HTTP port: 8080
   - Health check: `/health`

4. **Review and Deploy:**
   - Plan: Basic (smallest instance is fine)
   - Click "Create Resources"
   - Wait 3-5 minutes for deployment

5. **Get Your URL:**
   - Digital Ocean will provide a URL like:
   - `https://xams-mcp-server-xxxxx.ondigitalocean.app`
   - Save this URL!

### 4. Test the Deployment

```bash
# Replace with your actual URL
curl https://your-url.ondigitalocean.app/health

# Should return:
# {"status":"healthy","resources":26,"timestamp":"..."}
```

Done! Your MCP server is now publicly accessible.

---

## For End Users (Zero Installation!)

### 1. Get the Server URL

Ask your administrator for the MCP server URL. It will look like:
```
https://xams-mcp-server-xxxxx.ondigitalocean.app
```

### 2. Configure Claude Code

Find your Claude Code MCP configuration file:
- **macOS:** `~/Library/Application Support/Claude/config/mcp.json`
- **Linux:** `~/.config/claude/mcp.json`
- **Windows:** `%APPDATA%\Claude\config\mcp.json`

If the file doesn't exist, create it:

```json
{
  "mcpServers": {
    "xams": {
      "url": "https://xams-mcp-server-xxxxx.ondigitalocean.app"
    }
  }
}
```

Replace the URL with your actual server URL.

If the file already exists, add the `xams` entry to the `mcpServers` object.

### 3. Restart Claude Code

Completely quit and restart Claude Code for the MCP configuration to take effect.

### 4. Test It

Start a conversation with Claude Code and try:

```
User: "List all available xams resources"
```

Claude should be able to list all 26 Xams documentation resources.

```
User: "Read xams://docs/quickstart"
```

Claude should display the Xams quickstart guide.

### 5. Update Your Project's CLAUDE.md

Copy the `CLAUDE.md.template` from this repository and customize it for your project.

Key changes from old CLAUDE.md:
- Replace file paths like `./.claude/xams-docs-v1/src/app/quickstart/page.mdx`
- With MCP URIs like `xams://docs/quickstart`

Done! No installation, no dependencies, just works.

---

## Updating Documentation (Server Administrator)

When Xams documentation updates:

```bash
# 1. Navigate to MCP server repository
cd xams-mcp-server

# 2. Copy updated documentation
rm -rf docs/xams-docs-v1
cp -r /path/to/xams/.claude/xams-docs-v1 ./docs/

# Optional: Update refs and guides if changed
cp /path/to/xams/.claude/xams_ref/*.md ./refs/
cp /path/to/xams/.claude/SECURITY_AUDIT_GUIDE.md ./guides/
cp /path/to/xams/.claude/you-might-not-need-an-effect.md ./guides/

# 3. Commit and push
git add .
git commit -m "Update Xams documentation to latest version"
git push

# 4. Digital Ocean auto-deploys (wait 2-3 minutes)

# 5. Verify deployment
curl https://your-url.ondigitalocean.app/health
```

**Time required:** ~2 minutes

**Everyone gets updates automatically!** No action needed from end users.

---

## Troubleshooting

### Claude Code Can't Access Resources

1. **Verify the URL in your mcp.json is correct**
2. **Check server health:**
   ```bash
   curl https://your-url.ondigitalocean.app/health
   # Should return: {"status":"healthy","resources":26,...}
   ```
3. **Verify JSON syntax** in mcp.json (use a JSON validator)
4. **Restart Claude Code completely** (quit and relaunch)

### Resources Not Loading

1. **Test connectivity:**
   ```bash
   curl https://your-url.ondigitalocean.app/
   # Should return server info
   ```
2. **Check Claude Code logs** for MCP connection errors
3. **Verify internet connectivity**

---

## What You Get

After setup, Claude Code can access:

**19 Documentation Pages:**
- Quickstart, Entities, Service Logic, Security, Attributes, Actions, Queries, Firebase Auth, Realtime, Scheduled Jobs, Service Startup, API, Architecture, Auditing, Logging, Performance, React Field, React Guide, etc.

**4 Reference Guides:**
- Core Essentials, Core API, React Essentials, React API

**3 Helper Guides:**
- Security Audit Guide, You Might Not Need an Effect, Generate Types Script

All accessible via `xams://` URIs in any conversation!

---

## Next Steps

1. ✅ Get server URL from administrator
2. ✅ Configure: Add URL to Claude Code `mcp.json`
3. ✅ Restart: Quit and relaunch Claude Code
4. ✅ Test: Ask Claude to "list xams resources"
5. ✅ Update projects: Use `CLAUDE.md.template` as reference
6. ✅ Remove old `.claude/` folders from projects
7. ✅ Enjoy simplified Xams development!

---

## Support

- **Repository:** https://github.com/YOUR_USERNAME/xams-mcp-server
- **Issues:** https://github.com/YOUR_USERNAME/xams-mcp-server/issues

---

## Summary

**One-time setup:** Add URL to config (~30 seconds)

**Per-project:** Small CLAUDE.md file instead of large .claude/ folder

**Updates:** Automatic from server (zero user action)

**Requirements:** Just internet + Claude Code

**Result:** Streamlined Xams development with Claude Code!
