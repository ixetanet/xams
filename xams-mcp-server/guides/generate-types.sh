#!/bin/bash

# Generate TypeScript Types Script (Xams Framework)
#
# This script calls the ADMIN_GetTypes API endpoint and saves the generated
# TypeScript types to the specified output file.
#
# Usage:
#   ./scripts/generate-types.sh [OUTPUT_FILE] [API_URL] [USER_ID]
#
# Parameters:
#   OUTPUT_FILE - Path to output TypeScript file (default: types.ts)
#   API_URL     - Backend API URL (default: http://localhost:5000)
#   USER_ID     - User ID for authentication (default: system user)
#
# Environment Variables:
#   OUTPUT_FILE - Path to output TypeScript file
#   API_URL     - Backend API URL
#   USER_ID     - User ID for authentication
#
# Examples:
#   # Generate to current directory
#   ./scripts/generate-types.sh
#
#   # Specify output file path
#   ./scripts/generate-types.sh src/types/entities.ts
#
#   # With custom API URL
#   ./scripts/generate-types.sh src/types/entities.ts http://localhost:5001
#
#   # With all parameters
#   ./scripts/generate-types.sh src/api/types.ts http://localhost:5001 YOUR_USER_ID
#
#   # Use environment variables
#   OUTPUT_FILE=src/types/entities.ts API_URL=http://localhost:5001 ./scripts/generate-types.sh

set -e  # Exit on error

# Configuration - parameters can be positional args or environment variables
OUTPUT_FILE="${1:-${OUTPUT_FILE:-types.ts}}"
API_URL="${2:-${API_URL:-http://localhost:5000}}"
USER_ID="${3:-${USER_ID:-f8a43b04-4752-4fda-a89f-62bebcd8240c}}"
TEMP_FILE=$(mktemp)

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Cleanup temp file on exit
trap "rm -f $TEMP_FILE" EXIT

# Validate output file path
OUTPUT_DIR=$(dirname "$OUTPUT_FILE")
if [ ! -d "$OUTPUT_DIR" ]; then
    echo -e "${YELLOW}Warning: Output directory does not exist: $OUTPUT_DIR${NC}"
    echo "Creating directory..."
    mkdir -p "$OUTPUT_DIR"
fi

echo "📝 Generating TypeScript types..."
echo "   API URL: $API_URL"
echo "   Output:  $OUTPUT_FILE"
echo ""

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo -e "${RED}Error: jq is not installed${NC}"
    echo "Please install jq: brew install jq (on macOS)"
    exit 1
fi

# Check if server is reachable
if ! curl -s -f -o /dev/null -w "%{http_code}" "$API_URL/health" > /dev/null 2>&1; then
    echo -e "${YELLOW}Warning: Server may not be running at $API_URL${NC}"
    echo "Attempting to call API anyway..."
fi

# Call the API
HTTP_CODE=$(curl -s -w "%{http_code}" -X POST "$API_URL/xams/action" \
  -H "Content-Type: application/json" \
  -H "UserId: $USER_ID" \
  -d '{"name": "ADMIN_GetTypes"}' \
  -o "$TEMP_FILE")

# Check HTTP response code
if [ "$HTTP_CODE" -ne 200 ]; then
    echo -e "${RED}Error: API returned HTTP $HTTP_CODE${NC}"
    echo "Response:"
    cat "$TEMP_FILE"
    exit 1
fi

# Extract data from response
if ! TYPES_DATA=$(jq -r '.data' "$TEMP_FILE" 2>/dev/null); then
    echo -e "${RED}Error: Failed to parse JSON response${NC}"
    echo "Response:"
    cat "$TEMP_FILE"
    exit 1
fi

# Check if data is empty or null
if [ "$TYPES_DATA" == "null" ] || [ -z "$TYPES_DATA" ]; then
    echo -e "${RED}Error: No type data returned from API${NC}"
    exit 1
fi

# Write to output file
echo "$TYPES_DATA" > "$OUTPUT_FILE"

# Verify file was written
if [ ! -f "$OUTPUT_FILE" ]; then
    echo -e "${RED}Error: Failed to write output file${NC}"
    exit 1
fi

# Count types generated
TYPE_COUNT=$(echo "$TYPES_DATA" | grep -c "^export type" || echo "0")

echo -e "${GREEN}✅ Success!${NC}"
echo "   Generated $TYPE_COUNT TypeScript types"
echo "   Saved to: $OUTPUT_FILE"
echo ""
echo "You can now import these types in your TypeScript code:"
echo "   import { YourType } from '<relative-path-to-types>';"
