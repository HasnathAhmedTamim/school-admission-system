import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { Loader2, Plus } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useCreateClass } from '@/features/classes/hooks/use-classes'
import {
  createClassSchema,
  MAX_SEATS,
  NAME_MAX_LENGTH,
  type CreateClassValues,
} from '@/features/classes/lib/create-class-schema'
import { getErrorCode, getErrorMessage, getFieldErrors } from '@/lib/errors'

export function CreateClassDialog() {
  const [open, setOpen] = useState(false)
  const createClass = useCreateClass()
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<CreateClassValues>({
    resolver: zodResolver(createClassSchema),
    defaultValues: { name: '', totalSeats: undefined },
  })

  const onOpenChange = (next: boolean) => {
    setOpen(next)
    if (!next) {
      reset()
      createClass.reset()
    }
  }

  const onSubmit = handleSubmit((values) =>
    createClass.mutate(values, {
      onSuccess: (created) => {
        toast.success(`${created.name} created with ${created.totalSeats} seats.`)
        onOpenChange(false)
      },
      onError: (error) => {
        if (getErrorCode(error) === 'CLASS_NAME_TAKEN') {
          setError('name', { message: getErrorMessage(error) }, { shouldFocus: true })
          return
        }
        const fieldErrors = getFieldErrors(error)
        let handled = false
        for (const field of ['name', 'totalSeats'] as const) {
          if (fieldErrors[field]) {
            setError(field, { message: fieldErrors[field] })
            handled = true
          }
        }
        if (!handled) toast.error(getErrorMessage(error))
      },
    }),
  )

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogTrigger render={<Button />}>
        <Plus data-icon="inline-start" aria-hidden />
        New class
      </DialogTrigger>
      <DialogContent className="sm:max-w-md">
        <form onSubmit={onSubmit} noValidate className="grid gap-4">
          <DialogHeader>
            <DialogTitle>Create a class</DialogTitle>
            <DialogDescription>Parents can apply as soon as the class is created.</DialogDescription>
          </DialogHeader>

          <div className="grid gap-2">
            <Label htmlFor="class-name">Class name</Label>
            <Input
              id="class-name"
              placeholder="e.g. Class 6 - Morning Shift"
              maxLength={NAME_MAX_LENGTH}
              autoComplete="off"
              aria-invalid={errors.name ? true : undefined}
              aria-describedby="class-name-help"
              {...register('name')}
            />
            <p id="class-name-help" className={errors.name ? 'text-sm text-destructive' : 'text-xs text-muted-foreground'}>
              {errors.name?.message ?? 'Include the shift if the class has more than one, e.g. Morning or Day.'}
            </p>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="total-seats">Total seats</Label>
            <Input
              id="total-seats"
              type="number"
              inputMode="numeric"
              min={1}
              max={MAX_SEATS}
              step={1}
              placeholder="e.g. 120"
              aria-invalid={errors.totalSeats ? true : undefined}
              aria-describedby={errors.totalSeats ? 'total-seats-error' : undefined}
              {...register('totalSeats', { valueAsNumber: true })}
            />
            {errors.totalSeats && (
              <p id="total-seats-error" className="text-sm text-destructive">
                {errors.totalSeats.message}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={createClass.isPending}>
              {createClass.isPending && <Loader2 data-icon="inline-start" className="animate-spin" aria-hidden />}
              Create class
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
