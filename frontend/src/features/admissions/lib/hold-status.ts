import type { Hold } from '@/features/admissions/types'
import type { HoldStatus } from '@/types/api'

/**
 * The status to show right now: a HELD hold whose time has run out is shown as EXPIRED immediately,
 * before the next server refresh confirms it.
 */
export function getDisplayStatus(hold: Pick<Hold, 'status' | 'expiresAt'>, now = Date.now()): HoldStatus {
  if (hold.status === 'HELD' && Date.parse(hold.expiresAt) <= now) return 'EXPIRED'
  return hold.status
}

/** HELD (and not yet expired) or CONFIRMED - the student already has this class covered. */
export function isActiveHold(hold: Pick<Hold, 'status' | 'expiresAt'>, now = Date.now()): boolean {
  const status = getDisplayStatus(hold, now)
  return status === 'HELD' || status === 'CONFIRMED'
}

/** The total reservation window, used to draw the countdown progress. */
export function getHoldDurationMs(hold: Pick<Hold, 'createdAt' | 'expiresAt'>): number {
  return Math.max(0, Date.parse(hold.expiresAt) - Date.parse(hold.createdAt))
}
