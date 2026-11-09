import { useEffect, useState } from 'react'

const TICK_MS = 250

/**
 * Milliseconds left until `expiresAt`, ticking locally so the display does not wait for the next poll.
 * The server stays the authority: it rejects payment after expiry even if this device's clock is off.
 */
export function useCountdown(expiresAt: string | undefined) {
  const target = expiresAt ? Date.parse(expiresAt) : Number.NaN
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    if (Number.isNaN(target)) return
    const id = window.setInterval(() => {
      const current = Date.now()
      setNow(current)
      if (current >= target) window.clearInterval(id)
    }, TICK_MS)
    return () => window.clearInterval(id)
  }, [target])

  const msLeft = Number.isNaN(target) ? 0 : Math.max(0, target - now)
  return { msLeft, isExpired: !Number.isNaN(target) && msLeft <= 0 }
}
