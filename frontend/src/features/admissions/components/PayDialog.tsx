import { useState } from 'react'
import { CreditCard, Loader2 } from 'lucide-react'
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
import { useConfirmHold } from '@/features/admissions/hooks/use-admissions'
import type { Hold } from '@/features/admissions/types'
import { formatCountdown } from '@/lib/format'

interface PayDialogProps {
  hold: Hold
  studentId: string
  msLeft: number
  expired: boolean
}

/** Simulated admission-fee payment; confirming it turns the hold into an admission. */
export function PayDialog({ hold, studentId, msLeft, expired }: PayDialogProps) {
  const [open, setOpen] = useState(false)
  const confirm = useConfirmHold(hold, studentId)

  const pay = () => confirm.mutate(undefined, { onSettled: () => setOpen(false) })

  return (
    <AlertDialog open={open && !expired} onOpenChange={setOpen}>
      <AlertDialogTrigger render={<Button size="lg" className="w-full sm:w-auto" disabled={expired} />}>
        <CreditCard data-icon="inline-start" aria-hidden />
        Pay admission fee
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Pay admission fee</AlertDialogTitle>
          <AlertDialogDescription>
            This is a simulated payment - no money is charged. Paying confirms the seat in{' '}
            <strong className="text-foreground">{hold.className}</strong>. Time left:{' '}
            <span className="font-mono text-foreground tabular-nums">{formatCountdown(msLeft)}</span>
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={confirm.isPending}>Not now</AlertDialogCancel>
          <AlertDialogAction onClick={pay} disabled={confirm.isPending || expired}>
            {confirm.isPending && <Loader2 data-icon="inline-start" className="animate-spin" aria-hidden />}
            Pay and confirm
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
