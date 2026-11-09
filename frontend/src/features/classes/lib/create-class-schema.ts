import { z } from 'zod'

export const NAME_MAX_LENGTH = 100
export const MAX_SEATS = 10_000

/** Mirrors the backend rules so most mistakes are caught before the request is sent. */
export const createClassSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Enter a class name.')
    .max(NAME_MAX_LENGTH, `Keep the name under ${NAME_MAX_LENGTH} characters.`),
  totalSeats: z
    .number({ error: 'Enter the number of seats.' })
    .int('Seats must be a whole number.')
    .min(1, 'A class needs at least 1 seat.')
    .max(MAX_SEATS, `A class can have at most ${MAX_SEATS.toLocaleString('en-US')} seats.`),
})

export type CreateClassValues = z.infer<typeof createClassSchema>
