// Shared display formatters (keep pages from redefining their own).

/** Compact currency: $1.2B · $3.4M · $56K · $789. */
export const money = (n: number) =>
  n >= 1e9 ? `$${(n / 1e9).toFixed(1)}B` : n >= 1e6 ? `$${(n / 1e6).toFixed(1)}M` : n >= 1e3 ? `$${(n / 1e3).toFixed(0)}K` : `$${Math.round(n)}`

/** Full currency with thousands separators: $23,520. */
export const moneyFull = (n: number) => `$${Math.round(n).toLocaleString()}`

export const num = (n: number) => n.toLocaleString()

/** a ÷ b as a whole percentage, or an em-dash when b is 0. */
export const pct = (a: number, b: number) => (b > 0 ? `${Math.round((a / b) * 100)}%` : '—')

/** ISO date/time → local date, or an em-dash. */
export const day = (s?: string | null) => (s ? new Date(s).toLocaleDateString() : '—')
