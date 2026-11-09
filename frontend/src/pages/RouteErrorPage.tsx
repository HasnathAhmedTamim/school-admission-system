import { buttonVariants } from '@/components/ui/button'

/** Last-resort screen if a page crashes while rendering. */
export function RouteErrorPage() {
  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-3 p-6 text-center">
      <h1 className="text-2xl font-semibold tracking-tight">Something went wrong</h1>
      <p className="max-w-md text-sm text-muted-foreground">
        The page could not be displayed. Your applications are safe - reload the page to continue.
      </p>
      <a href="/" className={buttonVariants()}>
        Reload
      </a>
    </div>
  )
}
