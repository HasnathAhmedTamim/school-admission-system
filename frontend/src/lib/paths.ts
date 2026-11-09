export const paths = {
  classes: '/',
  applications: '/applications',
  application: (holdId: string) => `/applications/${encodeURIComponent(holdId)}`,
  admin: '/admin',
} as const
