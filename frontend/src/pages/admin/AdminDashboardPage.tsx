import { School } from 'lucide-react'
import { EmptyState } from '@/components/shared/EmptyState'
import { ErrorState } from '@/components/shared/ErrorState'
import { LiveIndicator } from '@/components/shared/LiveIndicator'
import { PageHeader } from '@/components/shared/PageHeader'
import { ClassesTable, ClassesTableSkeleton } from '@/features/classes/components/ClassesTable'
import { CreateClassDialog } from '@/features/classes/components/CreateClassDialog'
import { useClasses } from '@/features/classes/hooks/use-classes'
import { sumSeats } from '@/features/classes/lib/seat-status'
import { AdmissionStats } from '@/features/dashboard/components/AdmissionStats'

export function AdminDashboardPage() {
  const { data: classes, error, isPending, isRefetchError, refetch, isFetching, dataUpdatedAt } = useClasses()

  return (
    <div className="space-y-6">
      <PageHeader
        title="Admission dashboard"
        description="Create classes and watch seat availability as parents apply."
        actions={<CreateClassDialog />}
      />

      <AdmissionStats totals={classes ? sumSeats(classes) : undefined} />

      <section className="space-y-3" aria-labelledby="classes-heading">
        <div className="flex items-center justify-between gap-2">
          <h2 id="classes-heading" className="text-lg font-semibold">
            Classes {classes && <span className="text-muted-foreground">({classes.length})</span>}
          </h2>
          {classes && <LiveIndicator updatedAt={dataUpdatedAt} isError={isRefetchError} />}
        </div>

        {isPending ? (
          <ClassesTableSkeleton />
        ) : !classes ? (
          <ErrorState title="Could not load classes" error={error} onRetry={() => refetch()} retrying={isFetching} />
        ) : classes.length === 0 ? (
          <EmptyState
            icon={School}
            title="No classes yet"
            description="Create the first class so parents can start applying."
            action={<CreateClassDialog />}
          />
        ) : (
          <ClassesTable classes={classes} />
        )}
      </section>
    </div>
  )
}
