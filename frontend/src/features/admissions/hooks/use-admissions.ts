import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { config } from '@/lib/config'
import { getErrorMessage } from '@/lib/errors'
import { classKeys } from '@/features/classes/hooks/use-classes'
import {
  applyForClass,
  cancelHold,
  confirmHold,
  getHold,
  getMyHolds,
} from '@/features/admissions/services/holds-api'
import type { Hold } from '@/features/admissions/types'

export const admissionKeys = {
  all: ['admissions'] as const,
  myHolds: (studentId: string) => ['admissions', 'mine', studentId] as const,
  hold: (holdId: string, studentId: string) => ['admissions', 'hold', holdId, studentId] as const,
}

/** The student's applications, refreshed in the background. Disabled until a student ID is known. */
export function useMyHolds(studentId: string | null) {
  return useQuery({
    queryKey: admissionKeys.myHolds(studentId ?? ''),
    queryFn: () => getMyHolds(studentId!),
    enabled: studentId !== null,
    refetchInterval: config.pollIntervalMs,
  })
}

/** One application. Polls only while it is still on hold - the other statuses are final. */
export function useHold(holdId: string, studentId: string | null) {
  return useQuery({
    queryKey: admissionKeys.hold(holdId, studentId ?? ''),
    queryFn: () => getHold(holdId, studentId!),
    enabled: studentId !== null,
    refetchInterval: (query) => (query.state.data?.status === 'HELD' ? config.pollIntervalMs : false),
  })
}

function useInvalidateAdmissions() {
  const queryClient = useQueryClient()
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: admissionKeys.all }),
      queryClient.invalidateQueries({ queryKey: classKeys.all }),
    ])
}

export function useApply() {
  const invalidate = useInvalidateAdmissions()
  return useMutation({
    mutationFn: ({ classId, studentId }: { classId: string; studentId: string }) => applyForClass(classId, studentId),
    onSettled: invalidate,
  })
}

// Toasts live in the hook options rather than in mutate() callbacks: a successful payment or cancellation
// changes the status, which unmounts the dialog that started it, and mutate() callbacks skip unmounted callers.

export function useConfirmHold(hold: Hold, studentId: string) {
  const queryClient = useQueryClient()
  const invalidate = useInvalidateAdmissions()
  return useMutation({
    mutationFn: () => confirmHold(hold.holdId, studentId),
    onSuccess: (result) => {
      queryClient.setQueryData<Hold>(admissionKeys.hold(hold.holdId, studentId), (current) =>
        current ? { ...current, status: result.status, confirmedAt: result.confirmedAt } : current,
      )
      toast.success(`Payment received. Admission to ${hold.className} is confirmed.`)
    },
    onError: (error) => toast.error(getErrorMessage(error)),
    onSettled: invalidate,
  })
}

export function useCancelHold(hold: Hold, studentId: string) {
  const queryClient = useQueryClient()
  const invalidate = useInvalidateAdmissions()
  return useMutation({
    mutationFn: () => cancelHold(hold.holdId, studentId),
    onSuccess: () => {
      queryClient.setQueryData<Hold>(admissionKeys.hold(hold.holdId, studentId), (current) =>
        current ? { ...current, status: 'CANCELLED', cancelledAt: new Date().toISOString() } : current,
      )
      toast.success(`Application for ${hold.className} cancelled. The seat has been released.`)
    },
    onError: (error) => toast.error(getErrorMessage(error)),
    onSettled: invalidate,
  })
}
