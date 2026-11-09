import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import type { HoldStatus } from '@/types/api'

const styles: Record<HoldStatus, { label: string; className: string }> = {
  HELD: { label: 'On hold - awaiting payment', className: 'bg-amber-100 text-amber-900' },
  CONFIRMED: { label: 'Confirmed', className: 'bg-emerald-100 text-emerald-800' },
  EXPIRED: { label: 'Expired', className: 'bg-zinc-200 text-zinc-700' },
  CANCELLED: { label: 'Cancelled', className: 'bg-zinc-200 text-zinc-700' },
}

export function HoldStatusBadge({ status, className }: { status: HoldStatus; className?: string }) {
  const style = styles[status]
  return (
    <Badge className={cn('border-transparent', style.className, className)} data-status={status}>
      {style.label}
    </Badge>
  )
}
