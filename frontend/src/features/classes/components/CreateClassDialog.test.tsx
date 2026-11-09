import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { CreateClassDialog } from '@/features/classes/components/CreateClassDialog'
import { createClass } from '@/features/classes/services/classes-api'
import { apiError, renderWithProviders } from '@/test/utils'

vi.mock('@/features/classes/services/classes-api', () => ({
  createClass: vi.fn(),
  getClasses: vi.fn().mockResolvedValue([]),
}))

const createClassMock = vi.mocked(createClass)

async function openDialog() {
  const user = userEvent.setup()
  renderWithProviders(<CreateClassDialog />)
  await user.click(screen.getByRole('button', { name: /new class/i }))
  return user
}

describe('CreateClassDialog', () => {
  beforeEach(() => {
    createClassMock.mockReset()
  })

  it('validates the form before calling the API', async () => {
    const user = await openDialog()
    await user.click(screen.getByRole('button', { name: 'Create class' }))

    expect(await screen.findByText('Enter a class name.')).toBeInTheDocument()
    expect(screen.getByText('Enter the number of seats.')).toBeInTheDocument()

    await user.type(screen.getByLabelText('Class name'), 'Class 6')
    await user.type(screen.getByLabelText('Total seats'), '20000')
    await user.click(screen.getByRole('button', { name: 'Create class' }))
    expect(await screen.findByText(/at most 10,000 seats/)).toBeInTheDocument()
    expect(createClassMock).not.toHaveBeenCalled()
  })

  it('creates the class with a trimmed name and closes', async () => {
    createClassMock.mockResolvedValue({
      id: 'c1',
      name: 'Class 6 - Morning Shift',
      totalSeats: 120,
      heldSeats: 0,
      confirmedSeats: 0,
      availableSeats: 120,
      createdAt: '2026-11-09T10:00:00Z',
    })
    const user = await openDialog()

    await user.type(screen.getByLabelText('Class name'), '  Class 6 - Morning Shift  ')
    await user.type(screen.getByLabelText('Total seats'), '120')
    await user.click(screen.getByRole('button', { name: 'Create class' }))

    await waitFor(() => expect(createClassMock).toHaveBeenCalled())
    expect(createClassMock.mock.calls[0][0]).toEqual({ name: 'Class 6 - Morning Shift', totalSeats: 120 })
    expect(await screen.findByText('Class 6 - Morning Shift created with 120 seats.')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByLabelText('Class name')).not.toBeInTheDocument())
  })

  it('shows a duplicate name from the server next to the name field', async () => {
    createClassMock.mockRejectedValue(apiError(409, { error: 'CLASS_NAME_TAKEN', message: 'Taken' }))
    const user = await openDialog()

    await user.type(screen.getByLabelText('Class name'), 'Class 6 - Day Shift')
    await user.type(screen.getByLabelText('Total seats'), '50')
    await user.click(screen.getByRole('button', { name: 'Create class' }))

    expect(await screen.findByText(/a class with this name already exists/i)).toBeInTheDocument()
    expect(screen.getByLabelText('Class name')).toBeInTheDocument()
  })
})
