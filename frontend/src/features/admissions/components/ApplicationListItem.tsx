import { ChevronRight } from 'lucide-react'
import { Link } from 'react-router'
import { HoldStatusBadge } from '@/features/admissions/components/HoldStatusBadge'
import type { Hold } from '@/features/admissions/types'
import { useCountdown } from '@/hooks/use-countdown'
import { formatCountdown, formatDateTime } from '@/lib/format'
import { paths } from '@/lib/paths'
import { cn } from '@/lib/utils'

export function ApplicationListItem({ hold }: { hold: Hold }) {
  const isHeld = hold.status === 'HELD'
  const { msLeft, isExpired } = useCountdown(isHeld ? hold.expiresAt : undefined)
  const status = isHeld && isExpired ? 'EXPIRED' : hold.status

  return (
    <Link
      to={paths.application(hold.holdId)}
      className="flex items-center gap-4 rounded-xl border bg-background p-4 transition-colors hover:bg-muted/50 focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
      data-testid="application-item"
    >
      <div className="min-w-0 flex-1 space-y-1">
        <div className="flex flex-wrap items-center gap-2">
          <span className="font-medium">{hold.className}</span>
          <HoldStatusBadge status={status} />
        </div>
        <p className="text-sm text-muted-foreground">
          Applied {formatDateTime(hold.createdAt)}
          {status === 'CONFIRMED' && hold.confirmedAt && ` · Confirmed ${formatDateTime(hold.confirmedAt)}`}
          {status === 'CANCELLED' && hold.cancelledAt && ` · Cancelled ${formatDateTime(hold.cancelledAt)}`}
        </p>
      </div>
      {status === 'HELD' && (
        <div className="text-right">
          <p className={cn('font-mono text-lg font-semibold tabular-nums', msLeft <= 60_000 && 'text-red-600')}>
            {formatCountdown(msLeft)}
          </p>
          <p className="text-xs text-muted-foreground">left to pay</p>
        </div>
      )}
      <ChevronRight className="size-4 shrink-0 text-muted-foreground" aria-hidden />
    </Link>
  )
}
