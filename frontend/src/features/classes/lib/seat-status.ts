import type { SchoolClass } from '@/features/classes/types'

export type SeatStatus = 'full' | 'few' | 'open'

/** "few" once 10% or fewer of the seats (at least one) are still free. */
export function getSeatStatus(schoolClass: Pick<SchoolClass, 'totalSeats' | 'availableSeats'>): SeatStatus {
  if (schoolClass.availableSeats <= 0) return 'full'
  const fewThreshold = Math.max(1, Math.ceil(schoolClass.totalSeats * 0.1))
  return schoolClass.availableSeats <= fewThreshold ? 'few' : 'open'
}

export interface SeatTotals {
  classes: number
  totalSeats: number
  heldSeats: number
  confirmedSeats: number
  availableSeats: number
}

export function sumSeats(classes: SchoolClass[]): SeatTotals {
  return classes.reduce<SeatTotals>(
    (totals, c) => ({
      classes: totals.classes + 1,
      totalSeats: totals.totalSeats + c.totalSeats,
      heldSeats: totals.heldSeats + c.heldSeats,
      confirmedSeats: totals.confirmedSeats + c.confirmedSeats,
      availableSeats: totals.availableSeats + c.availableSeats,
    }),
    { classes: 0, totalSeats: 0, heldSeats: 0, confirmedSeats: 0, availableSeats: 0 },
  )
}
