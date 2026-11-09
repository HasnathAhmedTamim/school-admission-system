import { formatCountdown } from '@/lib/format'
import { cn } from '@/lib/utils'

const URGENT_MS = 60_000

interface HoldCountdownProps {
  msLeft: number
  totalMs: number
}

/** Large mm:ss timer with a shrinking bar; turns red in the last minute. */
export function HoldCountdown({ msLeft, totalMs }: HoldCountdownProps) {
  const urgent = msLeft <= URGENT_MS
  const percentLeft = totalMs > 0 ? Math.min(100, (msLeft / totalMs) * 100) : 0

  return (
    <div className="space-y-2">
      <div className="flex items-baseline justify-between gap-2">
        <p
          className={cn('font-mono text-4xl font-semibold tabular-nums', urgent ? 'text-red-600' : 'text-foreground')}
          role="timer"
          aria-live="off"
          aria-label={`${formatCountdown(msLeft)} left to pay`}
          data-testid="hold-countdown"
        >
          {formatCountdown(msLeft)}
        </p>
        <p className={cn('text-sm', urgent ? 'font-medium text-red-600' : 'text-muted-foreground')}>
          {urgent ? 'Less than a minute left!' : 'left to pay the admission fee'}
        </p>
      </div>
      <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
        <div
          className={cn('h-full transition-[width] duration-300 ease-linear', urgent ? 'bg-red-500' : 'bg-amber-400')}
          style={{ width: `${percentLeft}%` }}
        />
      </div>
    </div>
  )
}
