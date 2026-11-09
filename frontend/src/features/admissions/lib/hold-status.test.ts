import { describe, expect, it } from 'vitest'
import { getDisplayStatus, getHoldDurationMs, isActiveHold } from '@/features/admissions/lib/hold-status'

const now = Date.parse('2026-11-09T10:00:00Z')
const future = '2026-11-09T10:05:00Z'
const past = '2026-11-09T09:59:59Z'

describe('getDisplayStatus', () => {
  it('shows a HELD hold whose time has run out as EXPIRED before the server catches up', () => {
    expect(getDisplayStatus({ status: 'HELD', expiresAt: past }, now)).toBe('EXPIRED')
    expect(getDisplayStatus({ status: 'HELD', expiresAt: future }, now)).toBe('HELD')
  })

  it('keeps final statuses as they are', () => {
    expect(getDisplayStatus({ status: 'CONFIRMED', expiresAt: past }, now)).toBe('CONFIRMED')
    expect(getDisplayStatus({ status: 'CANCELLED', expiresAt: future }, now)).toBe('CANCELLED')
  })
})

describe('isActiveHold', () => {
  it('counts unexpired holds and confirmed admissions only', () => {
    expect(isActiveHold({ status: 'HELD', expiresAt: future }, now)).toBe(true)
    expect(isActiveHold({ status: 'CONFIRMED', expiresAt: past }, now)).toBe(true)
    expect(isActiveHold({ status: 'HELD', expiresAt: past }, now)).toBe(false)
    expect(isActiveHold({ status: 'EXPIRED', expiresAt: past }, now)).toBe(false)
    expect(isActiveHold({ status: 'CANCELLED', expiresAt: future }, now)).toBe(false)
  })
})

describe('getHoldDurationMs', () => {
  it('is the time between creation and expiry', () => {
    expect(getHoldDurationMs({ createdAt: '2026-11-09T10:00:00Z', expiresAt: '2026-11-09T10:10:00Z' })).toBe(600_000)
  })
})
