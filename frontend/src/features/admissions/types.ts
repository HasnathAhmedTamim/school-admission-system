import type { HoldStatus } from '@/types/api'

/** An application (seat hold) as returned by GET /holds/{id} and GET /me/holds. */
export interface Hold {
  holdId: string
  classId: string
  className: string
  status: HoldStatus
  expiresAt: string
  createdAt: string
  confirmedAt?: string | null
  cancelledAt?: string | null
}

/** Returned by POST /classes/{classId}/holds. */
export interface HoldCreated {
  holdId: string
  classId: string
  className: string
  studentId: string
  status: HoldStatus
  expiresAt: string
  createdAt: string
}

/** Returned by POST /holds/{holdId}/confirm. */
export interface ConfirmHoldResult {
  holdId: string
  classId: string
  status: HoldStatus
  confirmedAt?: string | null
}
