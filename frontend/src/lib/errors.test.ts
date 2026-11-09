import { AxiosError } from 'axios'
import { describe, expect, it } from 'vitest'
import { getErrorCode, getErrorMessage, getFieldErrors } from '@/lib/errors'
import { apiError } from '@/test/utils'

describe('getErrorMessage', () => {
  it('turns known backend error codes into plain language', () => {
    const error = apiError(409, { error: 'NO_SEATS_AVAILABLE', message: 'No seats available.' })
    expect(getErrorMessage(error)).toMatch(/all seats in this class are taken/i)
    expect(getErrorCode(error)).toBe('NO_SEATS_AVAILABLE')
  })

  it('explains expired holds and ownership problems', () => {
    expect(getErrorMessage(apiError(409, { error: 'HOLD_EXPIRED', message: 'x' }))).toMatch(/expired/i)
    expect(getErrorMessage(apiError(403, { error: 'NOT_YOUR_APPLICATION', message: 'x' }))).toMatch(
      /different student/i,
    )
  })

  it('falls back to the server message for codes it does not know', () => {
    const error = apiError(400, { error: 'VALIDATION_FAILED', message: 'Total seats must be at least 1.' })
    expect(getErrorMessage(error)).toBe('Total seats must be at least 1.')
  })

  it('explains network failures and timeouts', () => {
    expect(getErrorMessage(new AxiosError('Network Error', 'ERR_NETWORK'))).toMatch(/cannot reach/i)
    expect(getErrorMessage(new AxiosError('timeout', 'ECONNABORTED'))).toMatch(/too long/i)
  })

  it('never shows technical details for unexpected errors', () => {
    expect(getErrorMessage(new Error('TypeError: x is undefined'))).toBe('Something went wrong. Please try again.')
  })
})

describe('getFieldErrors', () => {
  it('returns the first message per field', () => {
    const error = apiError(400, {
      error: 'VALIDATION_FAILED',
      message: 'Invalid',
      errors: { name: ['Name is required.', 'Second'], totalSeats: ['Too many seats.'] },
    })
    expect(getFieldErrors(error)).toEqual({ name: 'Name is required.', totalSeats: 'Too many seats.' })
  })
})
