import { createBrowserRouter } from 'react-router'
import { RootLayout } from '@/layouts/RootLayout'
import { AdminDashboardPage } from '@/pages/admin/AdminDashboardPage'
import { NotFoundPage } from '@/pages/NotFoundPage'
import { RouteErrorPage } from '@/pages/RouteErrorPage'
import { ApplicationDetailsPage } from '@/pages/parent/ApplicationDetailsPage'
import { ClassesPage } from '@/pages/parent/ClassesPage'
import { MyApplicationsPage } from '@/pages/parent/MyApplicationsPage'

export const paths = {
  classes: '/',
  applications: '/applications',
  application: (holdId: string) => `/applications/${holdId}`,
  admin: '/admin',
} as const

export const router = createBrowserRouter([
  {
    element: <RootLayout />,
    errorElement: <RouteErrorPage />,
    children: [
      { index: true, element: <ClassesPage /> },
      { path: 'applications', element: <MyApplicationsPage /> },
      { path: 'applications/:holdId', element: <ApplicationDetailsPage /> },
      { path: 'admin', element: <AdminDashboardPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
