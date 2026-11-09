import type { ReactElement } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { AxiosError, AxiosHeaders, type AxiosResponse } from 'axios'
import { MemoryRouter } from 'react-router'
import { Toaster } from '@/components/ui/sonner'
import type { ApiErrorBody } from '@/types/api'

export function renderWithProviders(ui: ReactElement, { route = '/' }: { route?: string } = {}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>
        {ui}
        <Toaster />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

/** An Axios error shaped like a real backend error response. */
export function apiError(status: number, body: ApiErrorBody): AxiosError {
  const config = { headers: new AxiosHeaders() }
  const response = { status, statusText: '', data: body, headers: {}, config } as AxiosResponse
  return new AxiosError(body.message, 'ERR_BAD_REQUEST', config, {}, response)
}
