import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { StudentIdCard } from '@/components/shared/StudentIdCard'
import { renderWithProviders } from '@/test/utils'

const STORAGE_KEY = 'school-admission:student-id'

describe('StudentIdCard', () => {
  it('rejects anything that is not 17 digits with a plain message', async () => {
    const user = userEvent.setup()
    renderWithProviders(<StudentIdCard />)

    await user.type(screen.getByLabelText('Birth registration number'), '12345abc')
    await user.click(screen.getByRole('button', { name: 'Continue' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Use digits only (0–9).')

    await user.clear(screen.getByLabelText('Birth registration number'))
    await user.type(screen.getByLabelText('Birth registration number'), '123')
    await user.click(screen.getByRole('button', { name: 'Continue' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('exactly 17 digits')
    expect(window.localStorage.getItem(STORAGE_KEY)).toBeNull()
  })

  it('saves a valid number when Enter is pressed and remembers it after a reload', async () => {
    const user = userEvent.setup()
    const { unmount } = renderWithProviders(<StudentIdCard />)

    await user.type(screen.getByLabelText('Birth registration number'), '20152691234567801{Enter}')

    expect(await screen.findByTestId('current-student-id')).toHaveTextContent('2015 2691 2345 6780 1')
    expect(window.localStorage.getItem(STORAGE_KEY)).toBe('20152691234567801')

    unmount()
    renderWithProviders(<StudentIdCard />)
    expect(screen.getByTestId('current-student-id')).toHaveTextContent('2015 2691 2345 6780 1')
  })

  it('lets the parent switch to another student', async () => {
    window.localStorage.setItem(STORAGE_KEY, '20152691234567801')
    const user = userEvent.setup()
    renderWithProviders(<StudentIdCard />)

    await user.click(screen.getByRole('button', { name: /change student/i }))
    const input = screen.getByLabelText('Birth registration number')
    await user.clear(input)
    await user.type(input, '20152691234567802')
    await user.click(screen.getByRole('button', { name: 'Continue' }))

    expect(await screen.findByTestId('current-student-id')).toHaveTextContent('2015 2691 2345 6780 2')
  })
})
