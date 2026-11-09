import { cn } from '@/lib/utils'

interface LiveIndicatorProps {
  updatedAt: number
  isError?: boolean
}

/** Shows that the data refreshes on its own, and when it last did. */
export function LiveIndicator({ updatedAt, isError }: LiveIndicatorProps) {
  const time = updatedAt > 0 ? new Date(updatedAt).toLocaleTimeString() : null

  return (
    <span className="inline-flex items-center gap-2 text-xs text-muted-foreground" aria-live="off">
      <span className="relative flex size-2">
        {!isError && <span className="absolute inline-flex size-full animate-ping rounded-full bg-emerald-400 opacity-75" />}
        <span className={cn('relative inline-flex size-2 rounded-full', isError ? 'bg-amber-500' : 'bg-emerald-500')} />
      </span>
      {isError ? 'Reconnecting…' : time ? `Live · updated ${time}` : 'Live'}
    </span>
  )
}
