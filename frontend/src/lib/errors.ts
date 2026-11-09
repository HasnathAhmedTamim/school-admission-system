import { isAxiosError } from 'axios'
import type { ApiErrorBody } from '@/types/api'

const FRIENDLY_MESSAGES: Record<string, string> = {
  NO_SEATS_AVAILABLE: 'Sorry, all seats in this class are taken right now. Seats on hold are released if they are not paid in time, so please check again shortly.',
  ALREADY_APPLIED: 'This student already has an active application for this class.',
  ALREADY_ADMITTED: 'This student is already admitted to this class.',
  HOLD_EXPIRED: 'The reserved seat has expired because payment was not completed in time. You can apply again if seats are available.',
  HOLD_CANCELLED: 'This application was cancelled, so it can no longer be paid.',
  ADMISSION_ALREADY_CONFIRMED: 'This admission is already paid and confirmed, so it cannot be cancelled.',
  NOT_YOUR_APPLICATION: 'This application belongs to a different student. Check the birth registration number.',
  CLASS_NOT_FOUND: 'This class could not be found. It may have been removed.',
  HOLD_NOT_FOUND: 'This application could not be found.',
  CLASS_NAME_TAKEN: 'A class with this name already exists. Please use a different name or shift.',
  STUDENT_ID_REQUIRED: 'Please enter the student’s 17-digit birth registration number first.',
  INVALID_STUDENT_ID: 'The birth registration number must be exactly 17 digits.',
  SERVICE_BUSY: 'The admission system is very busy right now. Please try again in a moment.',
}

const GENERIC_MESSAGE = 'Something went wrong. Please try again.'

function isApiErrorBody(value: unknown): value is ApiErrorBody {
  return typeof value === 'object' && value !== null && typeof (value as ApiErrorBody).error === 'string'
}

/** The backend error code (e.g. NO_SEATS_AVAILABLE), if the failure came from the API. */
export function getErrorCode(error: unknown): string | undefined {
  if (isAxiosError(error) && isApiErrorBody(error.response?.data)) {
    return error.response.data.error
  }
  return undefined
}

/** Per-field validation messages from a 400 response, keyed by camelCase field name. */
export function getFieldErrors(error: unknown): Record<string, string> {
  if (!isAxiosError(error) || !isApiErrorBody(error.response?.data)) {
    return {}
  }
  const fieldErrors: Record<string, string> = {}
  for (const [field, messages] of Object.entries(error.response.data.errors ?? {})) {
    if (messages.length > 0) fieldErrors[field] = messages[0]
  }
  return fieldErrors
}

/** Turns any failure into a sentence a parent or administrator can act on. */
export function getErrorMessage(error: unknown): string {
  if (!isAxiosError(error)) {
    return GENERIC_MESSAGE
  }

  if (!error.response) {
    return error.code === 'ECONNABORTED'
      ? 'The server took too long to respond. Please try again.'
      : 'Cannot reach the admission server. Check your internet connection and try again.'
  }

  const body = error.response.data
  if (isApiErrorBody(body)) {
    return FRIENDLY_MESSAGES[body.error] ?? (body.message || GENERIC_MESSAGE)
  }

  return error.response.status >= 500
    ? 'The admission server had a problem. Please try again in a moment.'
    : GENERIC_MESSAGE
}
