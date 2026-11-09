import { FileText } from 'lucide-react'
import { Link } from 'react-router'
import { EmptyState } from '@/components/shared/EmptyState'
import { ErrorState } from '@/components/shared/ErrorState'
import { LiveIndicator } from '@/components/shared/LiveIndicator'
import { PageHeader } from '@/components/shared/PageHeader'
import { StudentIdCard } from '@/components/shared/StudentIdCard'
import { buttonVariants } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { ApplicationListItem } from '@/features/admissions/components/ApplicationListItem'
import { useMyHolds } from '@/features/admissions/hooks/use-admissions'
import { useStudentId } from '@/hooks/use-student-id'
import { paths } from '@/lib/paths'

export function MyApplicationsPage() {
  const { studentId } = useStudentId()

  return (
    <div className="space-y-6">
      <PageHeader title="My applications" description="Track reserved seats, payments and admissions for the student." />
      <StudentIdCard />
      {studentId && <ApplicationsList studentId={studentId} />}
    </div>
  )
}

function ApplicationsList({ studentId }: { studentId: string }) {
  const { data: holds, error, isPending, isRefetchError, refetch, isFetching, dataUpdatedAt } = useMyHolds(studentId)

  return (
    <section className="space-y-3" aria-labelledby="applications-heading">
      <div className="flex items-center justify-between gap-2">
        <h2 id="applications-heading" className="text-lg font-semibold">
          Applications {holds && <span className="text-muted-foreground">({holds.length})</span>}
        </h2>
        {holds && <LiveIndicator updatedAt={dataUpdatedAt} isError={isRefetchError} />}
      </div>

      {isPending ? (
        <div className="space-y-3" aria-busy="true" aria-label="Loading applications">
          {Array.from({ length: 3 }, (_, i) => (
            <Skeleton key={i} className="h-20 w-full rounded-xl" />
          ))}
        </div>
      ) : !holds ? (
        <ErrorState title="Could not load applications" error={error} onRetry={() => refetch()} retrying={isFetching} />
      ) : holds.length === 0 ? (
        <EmptyState
          icon={FileText}
          title="No applications yet"
          description="Applications for this student will appear here after you apply for a class."
          action={
            <Link to={paths.classes} className={buttonVariants()}>
              Browse classes
            </Link>
          }
        />
      ) : (
        <div className="space-y-3">
          {holds.map((hold) => (
            <ApplicationListItem key={hold.holdId} hold={hold} />
          ))}
        </div>
      )}
    </section>
  )
}
