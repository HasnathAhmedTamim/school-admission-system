import { Armchair, CircleCheck, Clock, Users, type LucideIcon } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import type { SeatTotals } from '@/features/classes/lib/seat-status'

const stats: { key: keyof SeatTotals; label: string; hint: string; icon: LucideIcon }[] = [
  { key: 'totalSeats', label: 'Total seats', hint: 'across all classes', icon: Users },
  { key: 'heldSeats', label: 'On hold', hint: 'waiting for payment', icon: Clock },
  { key: 'confirmedSeats', label: 'Confirmed', hint: 'admission fee paid', icon: CircleCheck },
  { key: 'availableSeats', label: 'Available', hint: 'open for applications', icon: Armchair },
]

export function AdmissionStats({ totals }: { totals?: SeatTotals }) {
  return (
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
      {stats.map(({ key, label, hint, icon: Icon }) => (
        <Card key={key} size="sm">
          <CardContent className="space-y-1">
            <div className="flex items-center justify-between text-muted-foreground">
              <span className="text-sm font-medium">{label}</span>
              <Icon className="size-4" aria-hidden />
            </div>
            {totals ? (
              <p className="text-2xl font-semibold tabular-nums" data-testid={`stat-${key}`}>
                {totals[key].toLocaleString()}
              </p>
            ) : (
              <Skeleton className="h-8 w-16" />
            )}
            <p className="text-xs text-muted-foreground">{hint}</p>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
