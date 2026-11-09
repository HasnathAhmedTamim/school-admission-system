import { AlertCircle, RotateCw } from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { getErrorMessage } from '@/lib/errors'

interface ErrorStateProps {
  title?: string
  error: unknown
  onRetry?: () => void
  retrying?: boolean
}

export function ErrorState({ title = 'Could not load data', error, onRetry, retrying }: ErrorStateProps) {
  return (
    <Alert variant="destructive">
      <AlertCircle aria-hidden />
      <AlertTitle>{title}</AlertTitle>
      <AlertDescription>
        <p>{getErrorMessage(error)}</p>
        {onRetry && (
          <Button variant="outline" size="sm" className="mt-2" onClick={onRetry} disabled={retrying}>
            <RotateCw data-icon="inline-start" className={retrying ? 'animate-spin' : undefined} aria-hidden />
            Try again
          </Button>
        )}
      </AlertDescription>
    </Alert>
  )
}
