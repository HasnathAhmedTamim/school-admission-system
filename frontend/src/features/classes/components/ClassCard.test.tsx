import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { Hold } from '@/features/admissions/types'
import { ClassCard } from '@/features/classes/components/ClassCard'
import type { SchoolClass } from '@/features/classes/types'
import { renderWithProviders } from '@/test/utils'

const schoolClass: SchoolClass = {
  id: 'class-1',
  name: 'Class 6 - Morning Shift',
  totalSeats: 10,
  heldSeats: 3,
  confirmedSeats: 2,
  availableSeats: 5,
  createdAt: '2026-11-09T10:00:00Z',
}

describe('ClassCard', () => {
  it('shows live seat numbers and applies for the class', async () => {
    const onApply = vi.fn()
    renderWithProviders(<ClassCard schoolClass={schoolClass} canApply applying={false} onApply={onApply} />)

    expect(screen.getByText('5')).toBeInTheDocument()
    expect(screen.getByText('3 on hold · 2 confirmed')).toBeInTheDocument()

    await userEvent.setup().click(screen.getByRole('button', { name: /apply and reserve seat/i }))
    expect(onApply).toHaveBeenCalledWith('class-1')
  })

  it('cannot apply to a full class', () => {
    renderWithProviders(
      <ClassCard
        schoolClass={{ ...schoolClass, heldSeats: 5, confirmedSeats: 5, availableSeats: 0 }}
        canApply
        applying={false}
        onApply={vi.fn()}
      />,
    )
    expect(screen.getByRole('button', { name: /no seats available/i })).toBeDisabled()
    expect(screen.getByText('Full')).toBeInTheDocument()
  })

  it('cannot apply before a student ID is entered', () => {
    renderWithProviders(<ClassCard schoolClass={schoolClass} canApply={false} applying={false} onApply={vi.fn()} />)
    expect(screen.getByRole('button', { name: /apply and reserve seat/i })).toBeDisabled()
  })

  it('links to the existing application instead of applying twice', () => {
    const hold: Hold = {
      holdId: 'hold-1',
      classId: 'class-1',
      className: schoolClass.name,
      status: 'HELD',
      createdAt: new Date().toISOString(),
      expiresAt: new Date(Date.now() + 600_000).toISOString(),
    }
    renderWithProviders(
      <ClassCard schoolClass={schoolClass} activeHold={hold} canApply applying={false} onApply={vi.fn()} />,
    )
    expect(screen.getByRole('link', { name: /seat reserved - pay now/i })).toHaveAttribute('href', '/applications/hold-1')
    expect(screen.queryByRole('button', { name: /apply/i })).not.toBeInTheDocument()
  })
})
