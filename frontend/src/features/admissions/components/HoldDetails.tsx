import { useEffect } from 'react'
import { CircleCheck, CircleSlash, TimerOff } from 'lucide-react'
import { Link } from 'react-router'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button, buttonVariants } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { CancelDialog } from '@/features/admissions/components/CancelDialog'
import { HoldCountdown } from '@/features/admissions/components/HoldCountdown'
import { HoldStatusBadge } from '@/features/admissions/components/HoldStatusBadge'
import { PayDialog } from '@/features/admissions/components/PayDialog'
import { getHoldDurationMs } from '@/features/admissions/lib/hold-status'
import type { Hold } from '@/features/admissions/types'
import { useCountdown } from '@/hooks/use-countdown'
import { formatDateTime, formatTime } from '@/lib/format'
import { paths } from '@/lib/paths'

interface HoldDetailsProps {
  hold: Hold
  studentId: string
  /** Called once when the local countdown reaches zero, so the server's EXPIRED status is fetched. */
  onExpired: () => void
}

export function HoldDetails({ hold, studentId, onExpired }: HoldDetailsProps) {
  const isHeld = hold.status === 'HELD'
  const { msLeft, isExpired } = useCountdown(isHeld ? hold.expiresAt : undefined)
  const status = isHeld && isExpired ? 'EXPIRED' : hold.status

  useEffect(() => {
    if (isHeld && isExpired) onExpired()
  }, [isHeld, isExpired, onExpired])

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="space-y-1">
            <CardTitle className="text-xl">{hold.className}</CardTitle>
            <CardDescription>Applied {formatDateTime(hold.createdAt)}</CardDescription>
          </div>
          <HoldStatusBadge status={status} />
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        {status === 'HELD' && (
          <>
            <HoldCountdown msLeft={msLeft} totalMs={getHoldDurationMs(hold)} />
            <p className="text-sm text-muted-foreground">
              A seat is reserved for this student until <strong className="text-foreground">{formatTime(hold.expiresAt)}</strong>.
              Pay the admission fee before then to confirm admission, otherwise the seat is released automatically.
            </p>
          </>
        )}

        {status === 'CONFIRMED' && (
          <Alert className="border-emerald-200 bg-emerald-50 text-emerald-900">
            <CircleCheck aria-hidden />
            <AlertTitle>Admission confirmed</AlertTitle>
            <AlertDescription className="text-emerald-800">
              The admission fee was paid{hold.confirmedAt ? ` on ${formatDateTime(hold.confirmedAt)}` : ''}. The seat is
              secured for this student.
            </AlertDescription>
          </Alert>
        )}

        {status === 'EXPIRED' && (
          <Alert>
            <TimerOff aria-hidden />
            <AlertTitle>Reservation expired</AlertTitle>
            <AlertDescription>
              Payment was not completed by {formatTime(hold.expiresAt)}, so the seat was released. Payment is no longer
              possible for this application - you can apply again if seats are available.
            </AlertDescription>
          </Alert>
        )}

        {status === 'CANCELLED' && (
          <Alert>
            <CircleSlash aria-hidden />
            <AlertTitle>Application cancelled</AlertTitle>
            <AlertDescription>
              This application was cancelled{hold.cancelledAt ? ` on ${formatDateTime(hold.cancelledAt)}` : ''} and the
              seat was released.
            </AlertDescription>
          </Alert>
        )}
      </CardContent>

      <CardFooter className="flex flex-col gap-2 sm:flex-row">
        {status === 'HELD' && (
          <>
            <PayDialog hold={hold} studentId={studentId} msLeft={msLeft} expired={isExpired} />
            <CancelDialog hold={hold} studentId={studentId} disabled={isExpired} />
          </>
        )}
        {status === 'EXPIRED' && (
          <>
            <Button size="lg" className="w-full sm:w-auto" disabled>
              Payment closed
            </Button>
            <Link to={paths.classes} className={buttonVariants({ size: 'lg', variant: 'outline', className: 'w-full sm:w-auto' })}>
              Apply again
            </Link>
          </>
        )}
        {status === 'CANCELLED' && (
          <Link to={paths.classes} className={buttonVariants({ size: 'lg', variant: 'outline', className: 'w-full sm:w-auto' })}>
            Apply again
          </Link>
        )}
        {status === 'CONFIRMED' && (
          <Link to={paths.applications} className={buttonVariants({ size: 'lg', variant: 'outline', className: 'w-full sm:w-auto' })}>
            View all applications
          </Link>
        )}
      </CardFooter>
    </Card>
  )
}
