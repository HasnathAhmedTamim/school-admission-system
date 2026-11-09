import { PageHeader } from '@/components/shared/PageHeader'
import { StudentIdCard } from '@/components/shared/StudentIdCard'

export function MyApplicationsPage() {
  return (
    <div className="space-y-6">
      <PageHeader title="My applications" description="Track reserved seats, payments and admissions." />
      <StudentIdCard />
    </div>
  )
}
