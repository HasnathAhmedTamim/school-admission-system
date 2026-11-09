import { apiClient } from '@/lib/api-client'
import type { CreateClassInput, SchoolClass } from '@/features/classes/types'

export async function getClasses(): Promise<SchoolClass[]> {
  const { data } = await apiClient.get<SchoolClass[]>('/classes')
  return data
}

export async function getClass(classId: string): Promise<SchoolClass> {
  const { data } = await apiClient.get<SchoolClass>(`/classes/${encodeURIComponent(classId)}`)
  return data
}

export async function createClass(input: CreateClassInput): Promise<SchoolClass> {
  const { data } = await apiClient.post<SchoolClass>('/classes', input)
  return data
}
