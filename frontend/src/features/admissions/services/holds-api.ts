import { apiClient, studentHeaders } from '@/lib/api-client'
import type { ConfirmHoldResult, Hold, HoldCreated } from '@/features/admissions/types'

export async function applyForClass(classId: string, studentId: string): Promise<HoldCreated> {
  const { data } = await apiClient.post<HoldCreated>(
    `/classes/${encodeURIComponent(classId)}/holds`,
    undefined,
    { headers: studentHeaders(studentId) },
  )
  return data
}

export async function getHold(holdId: string, studentId: string): Promise<Hold> {
  const { data } = await apiClient.get<Hold>(`/holds/${encodeURIComponent(holdId)}`, {
    headers: studentHeaders(studentId),
  })
  return data
}

export async function confirmHold(holdId: string, studentId: string): Promise<ConfirmHoldResult> {
  const { data } = await apiClient.post<ConfirmHoldResult>(
    `/holds/${encodeURIComponent(holdId)}/confirm`,
    undefined,
    { headers: studentHeaders(studentId) },
  )
  return data
}

export async function cancelHold(holdId: string, studentId: string): Promise<void> {
  await apiClient.delete(`/holds/${encodeURIComponent(holdId)}`, {
    headers: studentHeaders(studentId),
  })
}

export async function getMyHolds(studentId: string): Promise<Hold[]> {
  const { data } = await apiClient.get<Hold[]>('/me/holds', { headers: studentHeaders(studentId) })
  return data
}
