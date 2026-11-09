declare global {
  interface Window {
    /** Written by public/config.js; the Docker image regenerates it from VITE_API_BASE_URL at startup. */
    __APP_CONFIG__?: { apiBaseUrl?: string }
  }
}

const DEFAULT_API_BASE_URL = 'http://localhost:8080'

function resolveApiBaseUrl(): string {
  const runtime = typeof window !== 'undefined' ? window.__APP_CONFIG__?.apiBaseUrl : undefined
  const url = runtime?.trim() || import.meta.env.VITE_API_BASE_URL?.trim() || DEFAULT_API_BASE_URL
  return url.replace(/\/+$/, '')
}

export const config = {
  apiBaseUrl: resolveApiBaseUrl(),
  /** How often seat counts and application statuses are refreshed. */
  pollIntervalMs: 4000,
} as const
