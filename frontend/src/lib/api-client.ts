import axios from 'axios'
import { config } from '@/lib/config'

export const STUDENT_ID_HEADER = 'X-Student-Id'

export const apiClient = axios.create({
  baseURL: config.apiBaseUrl,
  timeout: 15_000,
  headers: { Accept: 'application/json' },
})

export function studentHeaders(studentId: string) {
  return { [STUDENT_ID_HEADER]: studentId }
}
