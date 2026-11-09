import { useCallback, useSyncExternalStore } from 'react'
import { isValidStudentId } from '@/lib/student-id'

const STORAGE_KEY = 'school-admission:student-id'
const listeners = new Set<() => void>()

function read(): string | null {
  try {
    const value = window.localStorage.getItem(STORAGE_KEY)
    return value && isValidStudentId(value) ? value : null
  } catch {
    return null
  }
}

function write(value: string | null) {
  try {
    if (value) {
      window.localStorage.setItem(STORAGE_KEY, value)
    } else {
      window.localStorage.removeItem(STORAGE_KEY)
    }
  } catch {
    // Storage can be unavailable (private mode); the ID then lasts for this page only.
  }
  listeners.forEach((listener) => listener())
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  const onStorage = (event: StorageEvent) => {
    if (event.key === STORAGE_KEY) listener()
  }
  window.addEventListener('storage', onStorage)
  return () => {
    listeners.delete(listener)
    window.removeEventListener('storage', onStorage)
  }
}

/** The current student's birth registration number, remembered across reloads and tabs. */
export function useStudentId() {
  const studentId = useSyncExternalStore(subscribe, read, () => null)
  const setStudentId = useCallback((value: string) => write(value.trim()), [])
  const clearStudentId = useCallback(() => write(null), [])
  return { studentId, setStudentId, clearStudentId }
}
