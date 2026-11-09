import { GraduationCap } from 'lucide-react'
import { Link, NavLink, Outlet } from 'react-router'
import { cn } from '@/lib/utils'

const navItems = [
  { to: '/', label: 'Apply', end: true },
  { to: '/applications', label: 'My applications', end: false },
  { to: '/admin', label: 'Admin', end: false },
]

export function RootLayout() {
  return (
    <div className="flex min-h-svh flex-col bg-muted/30">
      <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur supports-backdrop-filter:bg-background/80">
        <div className="mx-auto flex h-14 w-full max-w-6xl items-center gap-4 px-4">
          <Link to="/" className="flex items-center gap-2 font-semibold">
            <GraduationCap className="size-5" aria-hidden />
            <span>School Admission</span>
          </Link>
          <nav aria-label="Main" className="ml-auto flex items-center gap-1">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  cn(
                    'rounded-md px-3 py-1.5 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground',
                    isActive && 'bg-muted text-foreground',
                  )
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
        </div>
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6 sm:py-8">
        <Outlet />
      </main>
    </div>
  )
}
