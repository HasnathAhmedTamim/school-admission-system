import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { IdCard, Pencil } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useStudentId } from '@/hooks/use-student-id'
import { formatStudentId, STUDENT_ID_LENGTH, studentIdSchema } from '@/lib/student-id'

const formSchema = z.object({ studentId: studentIdSchema })
type FormValues = z.infer<typeof formSchema>

/** Lets the parent enter the student's birth registration number once; it is remembered on this device. */
export function StudentIdCard() {
  const { studentId, setStudentId, clearStudentId } = useStudentId()
  const [editing, setEditing] = useState(false)

  if (studentId && !editing) {
    return (
      <Card size="sm">
        <CardContent className="flex flex-wrap items-center gap-3">
          <IdCard className="size-5 text-muted-foreground" aria-hidden />
          <div className="min-w-0 flex-1">
            <p className="text-xs text-muted-foreground">Student birth registration number</p>
            <p className="font-mono text-base font-medium tracking-wide" data-testid="current-student-id">
              {formatStudentId(studentId)}
            </p>
          </div>
          <Button variant="outline" size="sm" onClick={() => setEditing(true)}>
            <Pencil data-icon="inline-start" aria-hidden />
            Change student
          </Button>
        </CardContent>
      </Card>
    )
  }

  return (
    <StudentIdForm
      defaultValue={studentId ?? ''}
      onSave={(value) => {
        setStudentId(value)
        setEditing(false)
      }}
      onCancel={studentId ? () => setEditing(false) : undefined}
      onClear={studentId ? () => { clearStudentId(); setEditing(false) } : undefined}
    />
  )
}

interface StudentIdFormProps {
  defaultValue: string
  onSave: (studentId: string) => void
  onCancel?: () => void
  onClear?: () => void
}

function StudentIdForm({ defaultValue, onSave, onCancel, onClear }: StudentIdFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { studentId: defaultValue },
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle>Who is applying?</CardTitle>
        <CardDescription>
          Enter the student’s {STUDENT_ID_LENGTH}-digit birth registration number. It is saved on this device so you can
          come back to your applications later.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit((values) => onSave(values.studentId))} className="flex flex-col gap-2" noValidate>
          <Label htmlFor="student-id">Birth registration number</Label>
          <div className="flex flex-col gap-2 sm:flex-row">
            <Input
              id="student-id"
              inputMode="numeric"
              autoComplete="off"
              maxLength={STUDENT_ID_LENGTH}
              placeholder="e.g. 20152691234567801"
              className="font-mono sm:max-w-xs"
              aria-invalid={errors.studentId ? true : undefined}
              aria-describedby={errors.studentId ? 'student-id-error' : undefined}
              {...register('studentId')}
            />
            <div className="flex gap-2">
              <Button type="submit">Continue</Button>
              {onCancel && (
                <Button type="button" variant="ghost" onClick={onCancel}>
                  Cancel
                </Button>
              )}
              {onClear && (
                <Button type="button" variant="ghost" onClick={onClear}>
                  Forget this student
                </Button>
              )}
            </div>
          </div>
          {errors.studentId && (
            <p id="student-id-error" role="alert" className="text-sm text-destructive">
              {errors.studentId.message}
            </p>
          )}
        </form>
      </CardContent>
    </Card>
  )
}
