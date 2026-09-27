// GHCP adoption maturity 0–7 (FACTORY-OPERATING-SYSTEM.md A.10).
export const ADOPTION_LEVELS: { level: number; label: string }[] = [
  { level: 0, label: 'Not Discussed' },
  { level: 1, label: 'Customer Aware' },
  { level: 2, label: 'Evaluation' },
  { level: 3, label: 'Procurement In Progress' },
  { level: 4, label: 'Licensed' },
  { level: 5, label: 'Assessment using GHCP' },
  { level: 6, label: 'Remediation using GHCP' },
  { level: 7, label: 'End-to-End Migration using GHCP' },
]

export function adoptionLabel(level?: number): string {
  const l = level ?? 0
  return ADOPTION_LEVELS[l]?.label ?? `Level ${l}`
}
