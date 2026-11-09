import { CircleCheck, Clock, Loader2 } from 'lucide-react'
import { Link } from 'react-router'
import { Button, buttonVariants } from '@/components/ui/button'
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import type { Hold } from '@/features/admissions/types'
import { getDisplayStatus } from '@/features/admissions/lib/hold-status'
import { SeatStatusBadge } from '@/features/classes/components/SeatStatusBadge'
import { SeatUsageBar } from '@/features/classes/components/SeatUsageBar'
import type { SchoolClass } from '@/features/classes/types'
import { paths } from '@/lib/paths'

interface ClassCardProps {
  schoolClass: SchoolClass
  /** The student's current HELD or CONFIRMED application for this class, if any. */
  activeHold?: Hold
  canApply: boolean
  applying: boolean
  onApply: (classId: string) => void
}

export function ClassCard({ schoolClass, activeHold, canApply, applying, onApply }: ClassCardProps) {
  const { id, name, totalSeats, heldSeats, confirmedSeats, availableSeats } = schoolClass
  const isFull = availableSeats <= 0

  return (
    <Card className="justify-between" data-testid="class-card">
      <CardHeader>
        <div className="flex items-start justify-between gap-2">
          <CardTitle className="text-base">{name}</CardTitle>
          <SeatStatusBadge schoolClass={schoolClass} />
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        <p>
          <span className="text-3xl font-semibold tabular-nums">{availableSeats}</span>
          <span className="text-muted-foreground"> of {totalSeats} seats available</span>
        </p>
        <SeatUsageBar schoolClass={schoolClass} />
        <p className="text-xs text-muted-foreground">
          {heldSeats} on hold · {confirmedSeats} confirmed
        </p>
      </CardContent>
      <CardFooter>
        {activeHold ? (
          <Link
            to={paths.application(activeHold.holdId)}
            className={buttonVariants({ variant: 'outline', className: 'w-full' })}
          >
            {getDisplayStatus(activeHold) === 'CONFIRMED' ? (
              <>
                <CircleCheck data-icon="inline-start" className="text-emerald-600" aria-hidden />
                Admitted - view details
              </>
            ) : (
              <>
                <Clock data-icon="inline-start" className="text-amber-600" aria-hidden />
                Seat reserved - pay now
              </>
            )}
          </Link>
        ) : (
          <Button className="w-full" disabled={!canApply || isFull || applying} onClick={() => onApply(id)}>
            {applying && <Loader2 data-icon="inline-start" className="animate-spin" aria-hidden />}
            {applying ? 'Reserving seat…' : isFull ? 'No seats available' : 'Apply and reserve seat'}
          </Button>
        )}
      </CardFooter>
    </Card>
  )
}

export function ClassCardSkeleton() {
  return (
    <Card>
      <CardHeader>
        <Skeleton className="h-5 w-2/3" />
      </CardHeader>
      <CardContent className="space-y-3">
        <Skeleton className="h-8 w-1/2" />
        <Skeleton className="h-2 w-full" />
        <Skeleton className="h-4 w-1/3" />
      </CardContent>
      <CardFooter>
        <Skeleton className="h-8 w-full" />
      </CardFooter>
    </Card>
  )
}
