import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { SeatStatusBadge } from '@/features/classes/components/SeatStatusBadge'
import { SeatUsageBar } from '@/features/classes/components/SeatUsageBar'
import type { SchoolClass } from '@/features/classes/types'

const numberCell = 'text-right tabular-nums'

export function ClassesTable({ classes }: { classes: SchoolClass[] }) {
  return (
    <div className="overflow-hidden rounded-xl border bg-background">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Class</TableHead>
            <TableHead className="text-right">Total</TableHead>
            <TableHead className="text-right">On hold</TableHead>
            <TableHead className="text-right">Confirmed</TableHead>
            <TableHead className="text-right">Available</TableHead>
            <TableHead className="w-40 min-w-32">Occupancy</TableHead>
            <TableHead>Status</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {classes.map((c) => (
            <TableRow key={c.id}>
              <TableCell className="min-w-48 font-medium whitespace-normal">{c.name}</TableCell>
              <TableCell className={numberCell}>{c.totalSeats}</TableCell>
              <TableCell className={numberCell}>{c.heldSeats}</TableCell>
              <TableCell className={numberCell}>{c.confirmedSeats}</TableCell>
              <TableCell className={`${numberCell} font-medium`}>{c.availableSeats}</TableCell>
              <TableCell>
                <SeatUsageBar schoolClass={c} />
              </TableCell>
              <TableCell>
                <SeatStatusBadge schoolClass={c} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

export function ClassesTableSkeleton({ rows = 4 }: { rows?: number }) {
  return (
    <div className="space-y-3 rounded-xl border bg-background p-4" aria-busy="true" aria-label="Loading classes">
      {Array.from({ length: rows }, (_, i) => (
        <Skeleton key={i} className="h-8 w-full" />
      ))}
    </div>
  )
}
