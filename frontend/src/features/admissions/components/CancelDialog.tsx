import { useState } from 'react'
import { Loader2, X } from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { useCancelHold } from '@/features/admissions/hooks/use-admissions'
import type { Hold } from '@/features/admissions/types'

interface CancelDialogProps {
  hold: Hold
  studentId: string
  disabled?: boolean
}

export function CancelDialog({ hold, studentId, disabled }: CancelDialogProps) {
  const [open, setOpen] = useState(false)
  const cancel = useCancelHold(hold, studentId)

  const confirmCancel = () => cancel.mutate(undefined, { onSettled: () => setOpen(false) })

  return (
    <AlertDialog open={open} onOpenChange={setOpen}>
      <AlertDialogTrigger
        render={<Button size="lg" variant="outline" className="w-full sm:w-auto" disabled={disabled} />}
      >
        <X data-icon="inline-start" aria-hidden />
        Cancel application
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Cancel this application?</AlertDialogTitle>
          <AlertDialogDescription>
            The reserved seat in <strong className="text-foreground">{hold.className}</strong> will be released for
            other students. You can apply again later if seats are still available.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={cancel.isPending}>Keep application</AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={confirmCancel} disabled={cancel.isPending}>
            {cancel.isPending && <Loader2 data-icon="inline-start" className="animate-spin" aria-hidden />}
            Cancel application
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
