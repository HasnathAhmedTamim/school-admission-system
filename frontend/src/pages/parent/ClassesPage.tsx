import { PageHeader } from '@/components/shared/PageHeader'
import { StudentIdCard } from '@/components/shared/StudentIdCard'

export function ClassesPage() {
  return (
    <div className="space-y-6">
      <PageHeader title="Apply for admission" description="Choose a class to reserve a seat for the student." />
      <StudentIdCard />
    </div>
  )
}
