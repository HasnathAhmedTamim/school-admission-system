import { useMemo } from 'react'
import { School } from 'lucide-react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { EmptyState } from '@/components/shared/EmptyState'
import { ErrorState } from '@/components/shared/ErrorState'
import { LiveIndicator } from '@/components/shared/LiveIndicator'
import { PageHeader } from '@/components/shared/PageHeader'
import { StudentIdCard } from '@/components/shared/StudentIdCard'
import { useApply, useMyHolds } from '@/features/admissions/hooks/use-admissions'
import { getHoldDurationMs, isActiveHold } from '@/features/admissions/lib/hold-status'
import type { Hold } from '@/features/admissions/types'
import { ClassCard, ClassCardSkeleton } from '@/features/classes/components/ClassCard'
import { useClasses } from '@/features/classes/hooks/use-classes'
import { useStudentId } from '@/hooks/use-student-id'
import { getErrorMessage } from '@/lib/errors'
import { formatDuration } from '@/lib/format'
import { paths } from '@/lib/paths'

export function ClassesPage() {
  const navigate = useNavigate()
  const { studentId } = useStudentId()
  const { data: classes, error, isPending, isRefetchError, refetch, isFetching, dataUpdatedAt } = useClasses()
  const { data: myHolds } = useMyHolds(studentId)
  const apply = useApply()

  const activeHoldByClass = useMemo(() => {
    const map = new Map<string, Hold>()
    for (const hold of myHolds ?? []) {
      if (isActiveHold(hold)) map.set(hold.classId, hold)
    }
    return map
  }, [myHolds])

  const onApply = (classId: string) => {
    if (!studentId) return
    apply.mutate(
      { classId, studentId },
      {
        onSuccess: (hold) => {
          toast.success(
            `Seat reserved in ${hold.className}. Pay within ${formatDuration(getHoldDurationMs(hold))} to confirm admission.`,
          )
          navigate(paths.application(hold.holdId))
        },
        onError: (applyError) => toast.error(getErrorMessage(applyError)),
      },
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Apply for admission"
        description="Choose a class to reserve a seat. The seat is held for a limited time while you pay the admission fee."
      />

      <StudentIdCard />

      <section className="space-y-3" aria-labelledby="classes-heading">
        <div className="flex items-center justify-between gap-2">
          <h2 id="classes-heading" className="text-lg font-semibold">
            Available classes
          </h2>
          {classes && <LiveIndicator updatedAt={dataUpdatedAt} isError={isRefetchError} />}
        </div>

        {!studentId && classes && classes.length > 0 && (
          <p className="text-sm text-muted-foreground">
            Enter the birth registration number above to apply. You can browse seat availability without it.
          </p>
        )}

        {isPending ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 3 }, (_, i) => (
              <ClassCardSkeleton key={i} />
            ))}
          </div>
        ) : !classes ? (
          <ErrorState title="Could not load classes" error={error} onRetry={() => refetch()} retrying={isFetching} />
        ) : classes.length === 0 ? (
          <EmptyState
            icon={School}
            title="No classes are open yet"
            description="The school has not opened any classes for admission. Please check back later."
          />
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {classes.map((schoolClass) => (
              <ClassCard
                key={schoolClass.id}
                schoolClass={schoolClass}
                activeHold={activeHoldByClass.get(schoolClass.id)}
                canApply={studentId !== null && !apply.isPending}
                applying={apply.isPending && apply.variables?.classId === schoolClass.id}
                onApply={onApply}
              />
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
