/** Error body returned by the backend for every 4xx/5xx response. */
export interface ApiErrorBody {
  error: string
  message: string
  errors?: Record<string, string[]>
}

export type HoldStatus = 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED'
