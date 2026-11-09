import type { SchoolClass } from '@/features/classes/types'

type SeatCounts = Pick<SchoolClass, 'totalSeats' | 'heldSeats' | 'confirmedSeats'>

function percent(part: number, total: number) {
  return total > 0 ? Math.min(100, (part / total) * 100) : 0
}

/** Stacked bar: confirmed (dark), held awaiting payment (amber), free (empty). */
export function SeatUsageBar({ schoolClass }: { schoolClass: SeatCounts }) {
  const { totalSeats, heldSeats, confirmedSeats } = schoolClass
  const confirmed = percent(confirmedSeats, totalSeats)
  const held = Math.min(100 - confirmed, percent(heldSeats, totalSeats))

  return (
    <div
      className="flex h-2 w-full overflow-hidden rounded-full bg-muted"
      role="img"
      aria-label={`${confirmedSeats} confirmed and ${heldSeats} on hold out of ${totalSeats} seats`}
    >
      <div className="h-full bg-primary transition-[width] duration-500" style={{ width: `${confirmed}%` }} />
      <div className="h-full bg-amber-400 transition-[width] duration-500" style={{ width: `${held}%` }} />
    </div>
  )
}
