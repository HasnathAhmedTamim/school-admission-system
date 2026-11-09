#!/bin/sh
# Lets the backend URL change per environment without rebuilding the image.
set -eu

api_base_url="${VITE_API_BASE_URL:-http://localhost:8080}"
escaped=$(printf '%s' "$api_base_url" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g')

printf 'window.__APP_CONFIG__ = { apiBaseUrl: "%s" }\n' "$escaped" > /usr/share/nginx/html/config.js
echo "Frontend will call the API at $api_base_url"
