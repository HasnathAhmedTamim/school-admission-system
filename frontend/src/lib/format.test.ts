import { describe, expect, it } from 'vitest'
import { formatCountdown, formatDuration } from '@/lib/format'
import { formatStudentId, isValidStudentId } from '@/lib/student-id'

describe('formatCountdown', () => {
  it('shows minutes and seconds, rounding partial seconds up', () => {
    expect(formatCountdown(600_000)).toBe('10:00')
    expect(formatCountdown(65_100)).toBe('1:06')
    expect(formatCountdown(999)).toBe('0:01')
  })

  it('never goes below zero', () => {
    expect(formatCountdown(0)).toBe('0:00')
    expect(formatCountdown(-5_000)).toBe('0:00')
  })

  it('adds hours for long holds', () => {
    expect(formatCountdown(3_723_000)).toBe('1:02:03')
  })
})

describe('formatDuration', () => {
  it('reads naturally', () => {
    expect(formatDuration(600_000)).toBe('10 minutes')
    expect(formatDuration(90_000)).toBe('1 minute 30 seconds')
    expect(formatDuration(45_000)).toBe('45 seconds')
  })
})

describe('student ID', () => {
  it('accepts exactly 17 digits', () => {
    expect(isValidStudentId('20152691234567801')).toBe(true)
    expect(isValidStudentId('2015269123456780')).toBe(false)
    expect(isValidStudentId('201526912345678012')).toBe(false)
    expect(isValidStudentId('2015269123456780a')).toBe(false)
  })

  it('is shown in groups of four', () => {
    expect(formatStudentId('20152691234567801')).toBe('2015 2691 2345 6780 1')
  })
})
