import { Badge } from '@/components/ui/badge'
import { getSeatStatus } from '@/features/classes/lib/seat-status'
import type { SchoolClass } from '@/features/classes/types'
import { cn } from '@/lib/utils'

const styles = {
  full: { label: 'Full', className: 'bg-red-100 text-red-800' },
  few: { label: 'Few seats left', className: 'bg-amber-100 text-amber-900' },
  open: { label: 'Open', className: 'bg-emerald-100 text-emerald-800' },
} as const

export function SeatStatusBadge({ schoolClass }: { schoolClass: Pick<SchoolClass, 'totalSeats' | 'availableSeats'> }) {
  const style = styles[getSeatStatus(schoolClass)]
  return <Badge className={cn('border-transparent', style.className)}>{style.label}</Badge>
}
