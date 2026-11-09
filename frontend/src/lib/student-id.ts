import { z } from 'zod'

export const STUDENT_ID_LENGTH = 17

export const studentIdSchema = z
  .string()
  .trim()
  .regex(/^\d+$/, 'Use digits only (0–9).')
  .length(STUDENT_ID_LENGTH, `The birth registration number must be exactly ${STUDENT_ID_LENGTH} digits.`)

export function isValidStudentId(value: string): boolean {
  return studentIdSchema.safeParse(value).success
}

/** Shows a 17-digit number in readable groups: 2015 2691 2345 6780 1 */
export function formatStudentId(value: string): string {
  return value.replace(/(\d{4})(?=\d)/g, '$1 ')
}
