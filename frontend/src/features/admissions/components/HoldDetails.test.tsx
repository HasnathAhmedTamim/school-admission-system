import { act, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { HoldDetails } from '@/features/admissions/components/HoldDetails'
import type { Hold } from '@/features/admissions/types'
import { renderWithProviders } from '@/test/utils'

const start = new Date('2026-11-09T10:00:00Z')

function heldFor(seconds: number): Hold {
  return {
    holdId: 'hold-1',
    classId: 'class-1',
    className: 'Class 6 - Morning Shift',
    status: 'HELD',
    createdAt: start.toISOString(),
    expiresAt: new Date(start.getTime() + seconds * 1000).toISOString(),
  }
}

describe('HoldDetails', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date', 'setInterval', 'clearInterval', 'setTimeout', 'clearTimeout'] })
    vi.setSystemTime(start)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('counts down locally and allows payment while the seat is held', () => {
    renderWithProviders(<HoldDetails hold={heldFor(600)} studentId="20152691234567801" onExpired={() => {}} />)

    expect(screen.getByTestId('hold-countdown')).toHaveTextContent('10:00')
    expect(screen.getByRole('button', { name: /pay admission fee/i })).toBeEnabled()

    act(() => vi.advanceTimersByTime(65_000))
    expect(screen.getByTestId('hold-countdown')).toHaveTextContent('8:55')
  })

  it('shows the hold as expired and disables payment the moment time runs out', () => {
    const onExpired = vi.fn()
    renderWithProviders(<HoldDetails hold={heldFor(30)} studentId="20152691234567801" onExpired={onExpired} />)

    act(() => vi.advanceTimersByTime(25_000))
    expect(screen.getByText('Less than a minute left!')).toBeInTheDocument()

    act(() => vi.advanceTimersByTime(6_000))
    expect(screen.getByText('Reservation expired')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /pay admission fee/i })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: /payment closed/i })).toBeDisabled()
    expect(onExpired).toHaveBeenCalledTimes(1)
  })

  it('shows a confirmed admission without payment actions', () => {
    const hold: Hold = { ...heldFor(600), status: 'CONFIRMED', confirmedAt: start.toISOString() }
    renderWithProviders(<HoldDetails hold={hold} studentId="20152691234567801" onExpired={() => {}} />)

    expect(screen.getByText('Admission confirmed')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /pay/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /cancel application/i })).not.toBeInTheDocument()
  })
})
