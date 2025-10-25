# Deploying Xams MCP Server to Digital Ocean

Complete guide for deploying and managing the Xams MCP Server on Digital Ocean App Platform.

## Prerequisites

- GitHub account
- Digital Ocean account ([sign up](https://cloud.digitalocean.com/registrations/new))
- Git repository with your xams-mcp-server code

---

## Initial Deployment

### Step 1: Prepare Your Repository

1. **Update `app.yaml`** with your GitHub details:

```yaml
github:
  repo: YOUR_USERNAME/xams-mcp-server
  branch: main
```

2. **Commit and push:**

```bash
git add app.yaml
git commit -m "Configure for Digital Ocean deployment"
git push origin main
```

### Step 2: Create Digital Ocean App

1. **Login to Digital Ocean:**
   - Visit https://cloud.digitalocean.com/apps
   - Click "Create App"

2. **Connect GitHub:**
   - Choose "GitHub" as source
   - Click "Manage Access" to authorize Digital Ocean
   - Select your `xams-mcp-server` repository
   - Branch: `main`
   - Autodeploy: **Enabled** ✓

3. **Configure Resources:**
   Digital Ocean will detect your `app.yaml` and auto-configure:
   - **Name:** xams-mcp-server
   - **Type:** Web Service
   - **Build Command:** `npm run build`
   - **Run Command:** `npm start`
   - **HTTP Port:** 8080
   - **Health Check:** `/health`

4. **Choose Plan:**
   - Recommended: **Basic Plan** (smallest instance)
   - $5/month (or free trial credits)
   - Sufficient for documentation serving

5. **Environment Variables** (already set in app.yaml):
   - `NODE_ENV=production`
   - `PORT=8080`

6. **Review and Create:**
   - Click "Next" through review
   - Click "Create Resources"
   - Deployment starts (3-5 minutes)

### Step 3: Get Your URL

After deployment completes:

1. **Find your app URL:**
   - Digital Ocean provides: `https://xams-mcp-server-xxxxx.ondigitalocean.app`
   - Copy this URL!

2. **Test the deployment:**

```bash
curl https://your-url.ondigitalocean.app/health

# Expected response:
# {
#   "status": "healthy",
#   "resources": 26,
#   "timestamp": "2024-..."
# }
```

3. **Share the URL** with your team for their Claude Code configuration.

---

## Updating Documentation

When Xams docs are updated, follow this process:

### Quick Update Process

```bash
# 1. Navigate to local repository
cd xams-mcp-server

# 2. Copy updated documentation
rm -rf docs/xams-docs-v1
cp -r /path/to/xams/.claude/xams-docs-v1 ./docs/

# Update refs if changed
cp /path/to/xams/.claude/xams_ref/*.md ./refs/

# Update guides if changed
cp /path/to/xams/.claude/SECURITY_AUDIT_GUIDE.md ./guides/
cp /path/to/xams/.claude/you-might-not-need-an-effect.md ./guides/

# 3. Commit and push
git add .
git commit -m "Update Xams documentation"
git push origin main

# 4. Digital Ocean auto-deploys!
# Wait 2-3 minutes for deployment
```

### Verify Update

```bash
# Check deployment status in Digital Ocean dashboard
# Or test the health endpoint:
curl https://your-url.ondigitalocean.app/health

# Resources count should reflect any changes
```

**Everyone gets updates automatically!** No action needed from end users.

---

## Monitoring & Maintenance

### View Logs

1. **Digital Ocean Dashboard:**
   - Apps → xams-mcp-server → Runtime Logs
   - See: "Discovered 26 resources"
   - See: MCP connections

2. **Via CLI (doctl):**

```bash
# Install Digital Ocean CLI
brew install doctl  # macOS
# or: https://docs.digitalocean.com/reference/doctl/how-to/install/

# Authenticate
doctl auth init

# View logs
doctl apps logs YOUR_APP_ID --type run
```

### Health Monitoring

**Automated health checks:**
- Digital Ocean pings `/health` every 10 seconds
- Restarts service if unhealthy
- Configure in `app.yaml`

**Manual health check:**

```bash
curl https://your-url.ondigitalocean.app/health
```

**Expected response:**
```json
{
  "status": "healthy",
  "resources": 26,
  "timestamp": "2024-10-24T..."
}
```

### View Server Info

```bash
curl https://your-url.ondigitalocean.app/
```

Returns:
```json
{
  "name": "Xams MCP Server",
  "description": "Public MCP server providing Xams Framework documentation",
  "version": "1.0.0",
  "resources": 26,
  "mcp_endpoint": "/sse",
  "usage": {
    "claude_code_config": {
      "mcpServers": {
        "xams": {
          "url": "https://your-url.ondigitalocean.app"
        }
      }
    }
  }
}
```

---

## Custom Domain (Optional)

### Add Custom Domain

1. **In Digital Ocean App Settings:**
   - Settings → Domains
   - Add Domain: `mcp.yourdomain.com`
   - Follow DNS configuration instructions

2. **Update `app.yaml`:**

```yaml
domains:
  - domain: mcp.yourdomain.com
    type: PRIMARY
```

3. **Commit and push:**

```bash
git add app.yaml
git commit -m "Add custom domain"
git push
```

4. **Update user configurations:**
   - Users update their `mcp.json` with new domain
   - Old domain still works during transition

---

## Scaling & Performance

### Current Configuration

- **Instance:** basic-xxs (512MB RAM, 1 vCPU)
- **Cost:** ~$5/month
- **Capacity:** Hundreds of concurrent Claude Code connections

### If You Need More

**Upgrade instance size in `app.yaml`:**

```yaml
instance_size_slug: basic-xs  # 1GB RAM, 1 vCPU ($12/month)
# or: basic-s, basic-m, professional-xs, etc.
```

**Scale horizontally:**

```yaml
instance_count: 2  # Run multiple instances
```

Digital Ocean load-balances automatically.

---

## Troubleshooting

### Deployment Fails

**Check build logs:**
1. Digital Ocean dashboard → Apps → xams-mcp-server
2. Click on failed deployment
3. View build logs

**Common issues:**
- Missing dependencies in package.json
- TypeScript compilation errors
- Wrong build/run commands in app.yaml

**Fix:**
1. Fix the issue locally
2. Test: `npm install && npm run build && npm start`
3. Commit and push
4. Digital Ocean retries deployment

### Server Not Responding

**Check runtime logs:**
```bash
doctl apps logs YOUR_APP_ID --type run
```

**Look for:**
- "Discovered X resources" (should be 26)
- Port binding errors
- Crash logs

**Common issues:**
- Wrong PORT environment variable
- Missing documentation files
- Crashed on startup

### Health Check Failing

**Test locally:**
```bash
npm run build
npm start
curl http://localhost:8080/health
```

**If local works but deployment fails:**
- Check Digital Ocean environment variables
- Verify health check path in app.yaml
- Check logs for errors

---

## Rollback

If deployment breaks something:

1. **Via Dashboard:**
   - Apps → xams-mcp-server → Deployments
   - Find previous working deployment
   - Click "..." → Redeploy

2. **Via Git:**

```bash
# Revert to previous commit
git revert HEAD
git push

# Or reset to specific commit
git reset --hard COMMIT_HASH
git push --force  # Caution!
```

Digital Ocean auto-deploys the reverted code.

---

## Cost Optimization

### Current Cost Estimate

- Basic instance: **$5/month**
- Bandwidth: **Included** (1TB free)
- Build minutes: **Included**

**Total:** ~$5/month

### Free Tier Option

Digital Ocean offers:
- $200 credit for new accounts (60 days)
- Free static sites (if you don't need SSE)

**Static alternative:**
- Serve docs as static JSON
- Use DigitalOcean Spaces (S3-like)
- Ultra-cheap but less dynamic

---

## Backup & Recovery

### Code Backup

- Code is on GitHub (already backed up)
- Documentation is copied from main Xams repo
- No persistent data to back up

### Disaster Recovery

If app is completely deleted:

1. Recreate app on Digital Ocean
2. Connect to same GitHub repo
3. Digital Ocean rebuilds and deploys
4. Share new URL with team (if changed)

**Recovery time:** ~5 minutes

---

## Security Considerations

### Current Setup (Public, No Auth)

- ✅ Read-only content (documentation)
- ✅ No sensitive data
- ✅ CORS enabled for Claude Code access
- ✅ No API keys required

**Safe for public use!**

### If You Need Auth (Future)

Add authentication for private/internal use:

1. **API Key in headers:**

```typescript
app.use((req, res, next) => {
  const apiKey = req.headers['x-api-key'];
  if (apiKey !== process.env.API_KEY) {
    return res.status(401).json({ error: 'Unauthorized' });
  }
  next();
});
```

2. **Update user configurations:**

```json
{
  "mcpServers": {
    "xams": {
      "url": "https://your-url.ondigitalocean.app",
      "headers": {
        "x-api-key": "your-secret-key"
      }
    }
  }
}
```

---

## CI/CD Pipeline

### Current Setup (Automatic)

- Push to `main` branch → Auto-deploy
- No additional setup needed
- Digital Ocean handles everything

### Add Testing (Optional)

Create `.github/workflows/test.yml`:

```yaml
name: Test
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '18'
      - run: npm ci
      - run: npm run build
      - run: npm start &
      - run: sleep 5
      - run: curl http://localhost:8080/health
```

This tests builds before deploying.

---

## Quick Reference Commands

```bash
# Update documentation
cp -r /path/to/docs ./docs/ && git add . && git commit -m "Update docs" && git push

# View logs
doctl apps logs YOUR_APP_ID --type run --follow

# Check health
curl https://your-url.ondigitalocean.app/health

# View server info
curl https://your-url.ondigitalocean.app/

# Restart app
# (Via dashboard: Apps → xams-mcp-server → Settings → Restart)
```

---

## Support

- **Digital Ocean Docs:** https://docs.digitalocean.com/products/app-platform/
- **GitHub Issues:** https://github.com/YOUR_USERNAME/xams-mcp-server/issues
- **Digital Ocean Support:** https://www.digitalocean.com/support/

---

## Summary

**Initial Setup:** 10 minutes (create app, connect GitHub, deploy)

**Update Process:** 2 minutes (copy docs, commit, push)

**Cost:** ~$5/month (basic instance)

**Maintenance:** Minimal (auto-deploys, auto-scales, auto-restarts)

**Result:** Publicly accessible MCP server with zero installation for end users!
