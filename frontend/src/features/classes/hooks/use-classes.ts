import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { config } from '@/lib/config'
import { createClass, getClasses } from '@/features/classes/services/classes-api'

export const classKeys = {
  all: ['classes'] as const,
}

/** All classes with live seat counts, refreshed in the background. */
export function useClasses() {
  return useQuery({
    queryKey: classKeys.all,
    queryFn: getClasses,
    refetchInterval: config.pollIntervalMs,
  })
}

export function useCreateClass() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createClass,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: classKeys.all }),
  })
}
