export interface SchoolClass {
  id: string
  name: string
  totalSeats: number
  heldSeats: number
  confirmedSeats: number
  availableSeats: number
  createdAt: string
}

export interface CreateClassInput {
  name: string
  totalSeats: number
}
