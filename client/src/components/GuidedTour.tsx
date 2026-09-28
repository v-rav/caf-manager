import { Button, Text } from '@fluentui/react-components'
import { DismissRegular } from '@fluentui/react-icons'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

// First-run guided tour: a lightweight step-through overlay that orients a new user.
// Not a DOM spotlight — a centered card over a dimmed page. Each step can navigate to
// the relevant route so the page shows behind the overlay. Shown once (localStorage),
// re-launchable from the account menu.

export const TOUR_SEEN_KEY = 'caf.tourSeen'

type TourStep = { title: string; body: string; route?: string }

const STEPS: TourStep[] = [
  { title: 'Welcome to the CAF Operations Portal', body: 'This portal gives EMEA governance one view of the migration pipeline, resource capacity, and delivery health. After an FDO import, the portal becomes the system of record. This 60-second tour shows you around.' },
  { title: 'The grouped left navigation', body: 'Pages are grouped into Overview, Delivery, Resourcing, and Admin. Only the group you’re in stays expanded, so the sidebar never scrolls. What you see depends on your role — Admin sees everything.' },
  { title: 'Region & Fiscal-Year scope', body: 'The two selectors in the header scope every pipeline page. Region filters Global / EMEA / ASIA; Fiscal Year (FY starts Jul 1) filters to work completed or nominated in that year. Pick “All FY” to see everything.' },
  { title: 'Nominations — the pipeline', body: 'The grid tracks every migration: Stage (1–4), Status (on track / blocked), owners, waves, and an SLA-aware Summary. Defaults to Approved work. Click any account to open its SA Workspace.', route: '/nominations' },
  { title: 'The SA Workspace', body: 'Each nomination has a cockpit: an 8-gate stepper, the current-gate checklist, blockers, dated milestones, an audit timeline, and the Migration Capability panel (which tool accelerated which activity). Advance a gate only when it’s green.' },
  { title: 'Executive Dashboard & Analytics', body: 'The dashboard has four tabbed views (Leadership, Operational, GHCP Adoption, Factory Productivity). Migration Analytics adds distribution/value cuts and a fiscal-year Trends time-series. KPI cards drill through to the grid.', route: '/' },
  { title: 'Need help anytime?', body: 'The “?” next to your name explains the page you’re on. The Help & FAQ page has the full usage guide, the SA Workspace walkthrough, the FDO hygiene guide, and how ACR is calculated. You can replay this tour from the account menu.', route: '/help' },
]

export function GuidedTour({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [i, setI] = useState(0)
  const navigate = useNavigate()
  if (!open) return null

  const step = STEPS[i]
  const last = i === STEPS.length - 1
  const go = (next: number) => {
    const s = STEPS[next]
    if (s?.route) navigate(s.route)
    setI(next)
  }
  const finish = () => {
    localStorage.setItem(TOUR_SEEN_KEY, '1')
    setI(0)
    onClose()
  }

  return (
    <div
      style={{
        position: 'fixed', inset: 0, zIndex: 1000,
        background: 'rgba(0,0,0,0.45)',
        display: 'grid', placeItems: 'center',
      }}
      onClick={finish}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          width: 'min(520px, 92vw)',
          background: 'var(--colorNeutralBackground1)',
          borderRadius: 10,
          boxShadow: '0 12px 40px rgba(0,0,0,0.35)',
          padding: 20,
          display: 'flex', flexDirection: 'column', gap: 12,
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 8 }}>
          <Text size={500} weight="bold">{step.title}</Text>
          <Button appearance="subtle" size="small" icon={<DismissRegular />} aria-label="Skip tour" onClick={finish} />
        </div>
        <Text size={300} style={{ color: 'var(--colorNeutralForeground2)', minHeight: 66 }}>{step.body}</Text>

        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 4 }}>
          <div style={{ display: 'flex', gap: 5 }}>
            {STEPS.map((_, n) => (
              <span key={n} style={{
                width: 7, height: 7, borderRadius: '50%',
                background: n === i ? 'var(--colorBrandBackground)' : 'var(--colorNeutralStroke2)',
              }} />
            ))}
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            {i > 0 && <Button appearance="secondary" onClick={() => go(i - 1)}>Back</Button>}
            {!last
              ? <Button appearance="primary" onClick={() => go(i + 1)}>Next</Button>
              : <Button appearance="primary" onClick={finish}>Done</Button>}
          </div>
        </div>
      </div>
    </div>
  )
}
