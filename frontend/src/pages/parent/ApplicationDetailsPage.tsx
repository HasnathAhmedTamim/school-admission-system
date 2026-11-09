import { useCallback } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, FileQuestion, ShieldAlert } from 'lucide-react'
import { Link, useParams } from 'react-router'
import { EmptyState } from '@/components/shared/EmptyState'
import { ErrorState } from '@/components/shared/ErrorState'
import { StudentIdCard } from '@/components/shared/StudentIdCard'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { buttonVariants } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { HoldDetails } from '@/features/admissions/components/HoldDetails'
import { useHold } from '@/features/admissions/hooks/use-admissions'
import { classKeys } from '@/features/classes/hooks/use-classes'
import { useStudentId } from '@/hooks/use-student-id'
import { getErrorCode, getErrorMessage } from '@/lib/errors'
import { paths } from '@/lib/paths'

export function ApplicationDetailsPage() {
  const { holdId = '' } = useParams()
  const { studentId } = useStudentId()

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <Link to={paths.applications} className={buttonVariants({ variant: 'ghost', size: 'sm', className: '-ml-2' })}>
        <ArrowLeft data-icon="inline-start" aria-hidden />
        My applications
      </Link>

      {studentId ? (
        <ApplicationDetails holdId={holdId} studentId={studentId} />
      ) : (
        <>
          <p className="text-sm text-muted-foreground">
            Enter the birth registration number used for this application to see its status.
          </p>
          <StudentIdCard />
        </>
      )}
    </div>
  )
}

function ApplicationDetails({ holdId, studentId }: { holdId: string; studentId: string }) {
  const queryClient = useQueryClient()
  const { data: hold, error, isPending, refetch, isFetching } = useHold(holdId, studentId)

  const onExpired = useCallback(() => {
    void refetch()
    void queryClient.invalidateQueries({ queryKey: classKeys.all })
  }, [refetch, queryClient])

  if (isPending) {
    return <Skeleton className="h-72 w-full rounded-xl" aria-label="Loading application" />
  }

  if (!hold) {
    const code = getErrorCode(error)
    if (code === 'NOT_YOUR_APPLICATION') {
      return (
        <div className="space-y-4">
          <Alert variant="destructive">
            <ShieldAlert aria-hidden />
            <AlertTitle>This application belongs to another student</AlertTitle>
            <AlertDescription>{getErrorMessage(error)}</AlertDescription>
          </Alert>
          <StudentIdCard />
        </div>
      )
    }
    if (code === 'HOLD_NOT_FOUND') {
      return (
        <EmptyState
          icon={FileQuestion}
          title="Application not found"
          description="This link may be wrong or the application may not exist."
          action={
            <Link to={paths.applications} className={buttonVariants({ variant: 'outline' })}>
              View my applications
            </Link>
          }
        />
      )
    }
    return <ErrorState title="Could not load the application" error={error} onRetry={() => refetch()} retrying={isFetching} />
  }

  return <HoldDetails hold={hold} studentId={studentId} onExpired={onExpired} />
}
