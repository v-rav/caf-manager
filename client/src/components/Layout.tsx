import {
  Avatar,
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Dropdown,
  Field,
  Input,
  Menu,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  Option,
  Popover,
  PopoverSurface,
  PopoverTrigger,
  Text,
  Tooltip,
} from '@fluentui/react-components'
import {
  ArrowClockwiseRegular,
  BuildingRegular,
  CalendarLtrRegular,
  DatabaseRegular,
  DataPieRegular,
  DataTrendingRegular,
  GaugeRegular,
  HistoryRegular,
  LinkMultipleRegular,
  NavigationRegular,
  PeopleRegular,
  SettingsRegular,
  ClipboardTaskListLtrRegular,
  ShieldTaskRegular,
  RocketRegular,
  SparkleRegular,
  DataFunnelRegular,
  PeopleSettingsRegular,
  PersonStarRegular,
  WrenchRegular,
  QuestionCircleRegular,
  MoneyRegular,
  TrophyRegular,
  ChevronDownRegular,
  ChevronRightRegular,
} from '@fluentui/react-icons'
import { useMemo, useState, useEffect, type ReactNode } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { api } from '../api'
import { useAuth } from '../auth'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import { useFy, currentFy, fyLabel } from '../fy'
import { guideForKey } from '../pageGuide'
import { GuidedTour, TOUR_SEEN_KEY } from './GuidedTour'

type NavItem = { to: string; label: string; end?: boolean; icon: ReactNode }

// Grouped nav so the sidebar stays short: only the active group is expanded by default.
const NAV_GROUPS: { title: string; items: NavItem[] }[] = [
  {
    title: 'Overview',
    items: [
      { to: '/', label: 'Executive Dashboard', end: true, icon: <DataPieRegular /> },
      { to: '/analytics', label: 'Migration Analytics', icon: <DataTrendingRegular /> },
      { to: '/acr-recovery', label: 'ACR Recovery', icon: <MoneyRegular /> },
      { to: '/help', label: 'Help & FAQ', icon: <QuestionCircleRegular /> },
    ],
  },
  {
    title: 'Delivery',
    items: [
      { to: '/nominations', label: 'Nominations', icon: <ClipboardTaskListLtrRegular /> },
      { to: '/governance', label: 'Governance Board', icon: <ShieldTaskRegular /> },
      { to: '/flow', label: 'Migration Flow', icon: <DataFunnelRegular /> },
      { to: '/effectiveness', label: 'Ownership Effectiveness', icon: <TrophyRegular /> },
      { to: '/strategic', label: 'Strategic Register', icon: <RocketRegular /> },
      { to: '/adoption', label: 'GHCP Adoption', icon: <SparkleRegular /> },
      { to: '/workspace', label: 'SA Workspace (preview)', icon: <ClipboardTaskListLtrRegular /> },
    ],
  },
  {
    title: 'Resourcing',
    items: [
      { to: '/resources', label: 'Resource Hub', icon: <PeopleRegular /> },
      { to: '/accounts', label: 'Account Hub', icon: <BuildingRegular /> },
      { to: '/capacity', label: 'Capacity', icon: <GaugeRegular /> },
      { to: '/leave', label: 'Leave', icon: <CalendarLtrRegular /> },
      { to: '/reconciliation', label: 'Reconciliation', icon: <LinkMultipleRegular /> },
      { to: '/performance', label: 'Performance', icon: <PersonStarRegular /> },
    ],
  },
  {
    title: 'Admin',
    items: [
      { to: '/history', label: 'Import History', icon: <HistoryRegular /> },
      { to: '/configuration', label: 'Configuration', icon: <SettingsRegular /> },
      { to: '/gates', label: 'Gate Template', icon: <ShieldTaskRegular /> },
      { to: '/capability', label: 'Capability Masters', icon: <WrenchRegular /> },
      { to: '/access', label: 'User & Access', icon: <PeopleSettingsRegular /> },
      { to: '/backup', label: 'Backup & Restore', icon: <DatabaseRegular /> },
    ],
  },
]

const NAV: NavItem[] = NAV_GROUPS.flatMap((g) => g.items)

const EXPANDED = 240
const COLLAPSED = 56

// FY scope options: current fiscal year and the previous three.
const FY_OPTIONS = [0, 1, 2, 3].map((n) => currentFy() - n)

function timeAgo(iso?: string): string {
  if (!iso) return 'never'
  const then = new Date(iso).getTime()
  if (Number.isNaN(then)) return 'unknown'
  const mins = Math.max(0, Math.round((Date.now() - then) / 60000))
  if (mins < 1) return 'just now'
  if (mins < 60) return `${mins}m ago`
  const hrs = Math.round(mins / 60)
  if (hrs < 24) return `${hrs}h ago`
  return `${Math.round(hrs / 24)}d ago`
}

// Header account menu: identity, replay tour, change password, sign out.
function UserMenu({ onStartTour }: { onStartTour: () => void }) {
  const { user, logout } = useAuth()
  const [open, setOpen] = useState(false)
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [msg, setMsg] = useState<string | null>(null)
  if (!user) return null

  const change = async () => {
    setMsg(null)
    try {
      await api.changePassword(current, next)
      setMsg('Password updated.')
      setCurrent('')
      setNext('')
    } catch {
      setMsg('Current password is incorrect.')
    }
  }

  return (
    <>
      <Menu>
        <MenuTrigger disableButtonEnhancement>
          <Button appearance="subtle" style={{ minWidth: 0 }}>
            <Avatar name={user.displayName} size={24} color="colorful" />
            <span style={{ marginLeft: 8, whiteSpace: 'nowrap' }}>{user.displayName}</span>
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            <MenuItem disabled>{user.username} · {user.role}</MenuItem>
            <MenuItem onClick={onStartTour}>Take a tour</MenuItem>
            <MenuItem onClick={() => { setMsg(null); setOpen(true) }}>Change password</MenuItem>
            <MenuItem onClick={() => void logout()}>Sign out</MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>
      <Dialog open={open} onOpenChange={(_, d) => setOpen(d.open)}>
        <DialogSurface style={{ maxWidth: 380 }}>
          <DialogBody>
            <DialogTitle>Change password</DialogTitle>
            <DialogContent>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12, paddingTop: 8 }}>
                <Field label="Current password"><Input type="password" value={current} onChange={(_, d) => setCurrent(d.value)} /></Field>
                <Field label="New password" validationMessage={msg ?? undefined} validationState={msg && !msg.includes('updated') ? 'error' : msg ? 'success' : 'none'}>
                  <Input type="password" value={next} onChange={(_, d) => setNext(d.value)} />
                </Field>
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={() => setOpen(false)}>Close</Button>
              <Button appearance="primary" disabled={!current || next.length < 6} onClick={change}>Update</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </>
  )
}

export function Layout() {
  const { region, setRegion } = useRegion()
  const { fy, setFy } = useFy()
  const { user } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [tourOpen, setTourOpen] = useState(false)
  const { data: regions } = useAsync(() => api.regions(), [])
  const { data: access } = useAsync(() => api.pageAccess(), [])
  const { data: status } = useAsync(() => api.adminStatus(), [])
  const [refreshing, setRefreshing] = useState(false)
  const [collapsed, setCollapsed] = useState(() => localStorage.getItem('nav-collapsed') === '1')

  // Admin sees everything; others only pages their role is granted (default-allow while the matrix loads).
  const canAccess = useMemo(() => {
    const isAdmin = user?.role === 'Admin'
    return (key: string) => {
      if (isAdmin) return true
      if (!access) return true
      const p = access.find((a) => a.key === key)
      return p ? p.allowedRoles.includes(user?.role ?? '') : false
    }
  }, [access, user])
  const keyOf = (to: string) => (to === '/' ? 'dashboard' : to.replace(/^\//, ''))
  const currentKey = location.pathname === '/' ? 'dashboard' : location.pathname.split('/')[1]
  const navItems = NAV.filter((item) => canAccess(keyOf(item.to)))

  // Contextual help for the page you're on (nomination detail maps to the SA Workspace guide).
  const helpKey = /^\/nominations\/[^/]+$/.test(location.pathname) ? 'saworkspace' : currentKey
  const pageGuide = guideForKey(helpKey)

  // Show the guided tour once on first login; re-launchable from the account menu.
  useEffect(() => {
    if (user && localStorage.getItem(TOUR_SEEN_KEY) !== '1') setTourOpen(true)
  }, [user])

  // Collapsible nav groups: keep only the active group expanded so the sidebar never scrolls.
  const activeGroup = NAV_GROUPS.find((g) => g.items.some((i) => keyOf(i.to) === currentKey))?.title
  const [openGroups, setOpenGroups] = useState<Set<string>>(() => new Set(activeGroup ? [activeGroup] : []))
  useEffect(() => {
    if (activeGroup) setOpenGroups((s) => (s.has(activeGroup) ? s : new Set(s).add(activeGroup)))
  }, [activeGroup])
  const toggleGroup = (title: string) =>
    setOpenGroups((s) => {
      const n = new Set(s)
      if (n.has(title)) n.delete(title)
      else n.add(title)
      return n
    })

  const renderLink = (item: NavItem) => (
    <Tooltip
      key={item.to}
      content={item.label}
      relationship="label"
      positioning="after"
      visible={collapsed ? undefined : false}
    >
      <NavLink
        to={item.to}
        end={item.end}
        style={({ isActive }) => ({
          display: 'flex',
          alignItems: 'center',
          gap: 10,
          justifyContent: collapsed ? 'center' : 'flex-start',
          padding: collapsed ? '10px 0' : '7px 12px',
          borderRadius: 6,
          textDecoration: 'none',
          fontSize: 14,
          color: isActive ? 'var(--colorBrandForeground1)' : 'var(--colorNeutralForeground1)',
          background: isActive ? 'var(--colorBrandBackground2)' : 'transparent',
          fontWeight: isActive ? 600 : 400,
        })}
      >
        <span style={{ fontSize: 20, display: 'flex' }}>{item.icon}</span>
        {!collapsed && <span style={{ whiteSpace: 'nowrap' }}>{item.label}</span>}
      </NavLink>
    </Tooltip>
  )

  const toggle = () => {
    setCollapsed((c) => {
      localStorage.setItem('nav-collapsed', c ? '0' : '1')
      return !c
    })
  }

  const runRefresh = async () => {
    setRefreshing(true)
    try {
      await api.refresh()
      window.location.reload()
    } finally {
      setRefreshing(false)
    }
  }

  return (
    <div style={{ display: 'flex', minHeight: '100vh', background: 'var(--colorNeutralBackground3)' }}>
      <aside
        style={{
          width: collapsed ? COLLAPSED : EXPANDED,
          transition: 'width 0.2s ease',
          background: 'var(--colorNeutralBackground1)',
          borderRight: '1px solid var(--colorNeutralStroke2)',
          padding: collapsed ? '12px 8px' : 16,
          position: 'sticky',
          top: 0,
          height: '100vh',
          overflow: 'hidden',
          flexShrink: 0,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {!collapsed && (
          <>
            <Text weight="bold" size={500} style={{ display: 'block', marginBottom: 4, whiteSpace: 'nowrap' }}>
              CAF Operations
            </Text>
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 20 }}>
              Governance Portal
            </Text>
          </>
        )}
        <nav style={{ display: 'flex', flexDirection: 'column', gap: 4, marginTop: collapsed ? 8 : 0, overflowY: 'auto', flex: 1, minHeight: 0, paddingRight: 2 }}>
          {collapsed
            ? navItems.map((item) => renderLink(item))
            : NAV_GROUPS.map((group) => {
                const items = group.items.filter((item) => canAccess(keyOf(item.to)))
                if (items.length === 0) return null
                const open = openGroups.has(group.title)
                return (
                  <div key={group.title} style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    <button
                      onClick={() => toggleGroup(group.title)}
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        background: 'transparent',
                        border: 'none',
                        cursor: 'pointer',
                        padding: '6px 12px',
                        marginTop: 6,
                        color: 'var(--colorNeutralForeground3)',
                        fontSize: 11,
                        fontWeight: 700,
                        textTransform: 'uppercase',
                        letterSpacing: 0.6,
                      }}
                    >
                      <span>{group.title}</span>
                      <span style={{ display: 'flex', fontSize: 12 }}>{open ? <ChevronDownRegular /> : <ChevronRightRegular />}</span>
                    </button>
                    {open && items.map((item) => renderLink(item))}
                  </div>
                )
              })}
        </nav>
      </aside>

      <div style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        <header
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            gap: 12,
            flexWrap: 'wrap',
            padding: '12px 24px',
            background: 'var(--colorNeutralBackground1)',
            borderBottom: '1px solid var(--colorNeutralStroke2)',
            position: 'sticky',
            top: 0,
            zIndex: 10,
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Button appearance="subtle" icon={<NavigationRegular />} onClick={toggle} aria-label="Toggle navigation" />
            <Text weight="semibold" size={400}>
              {region ? `${region} View` : 'Global View'}
            </Text>
          </div>
          <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
            {status && (
              <Tooltip
                relationship="description"
                content={`Resources ${status.resources} · Accounts ${status.accounts} · Nominations ${status.nominations} · Leave ${status.leaveRecords} · Reviews ${status.performanceReviews}`}
              >
                <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', whiteSpace: 'nowrap' }}>
                  Updated {timeAgo(status.lastRefreshUtc)}
                </Text>
              </Tooltip>
            )}
            <Dropdown
              placeholder="Global View"
              value={region ?? 'Global View'}
              selectedOptions={[region ?? '']}
              onOptionSelect={(_, d) => setRegion(d.optionValue || undefined)}
              style={{ minWidth: 150 }}
            >
              <Option value="">Global View</Option>
              {regions?.map((r) => (
                <Option key={r.code} value={r.code}>
                  {r.code}
                </Option>
              ))}
            </Dropdown>
            <Dropdown
              value={fy === 'all' ? 'All FY' : fyLabel(fy)}
              selectedOptions={[String(fy)]}
              onOptionSelect={(_, d) => setFy(d.optionValue === 'all' ? 'all' : Number(d.optionValue))}
              style={{ minWidth: 110 }}
            >
              <Option value="all">All FY</Option>
              {FY_OPTIONS.map((y) => (
                <Option key={y} value={String(y)}>{fyLabel(y)}</Option>
              ))}
            </Dropdown>
            <Button appearance="secondary" icon={<ArrowClockwiseRegular />} disabled={refreshing} onClick={runRefresh}>
              {refreshing ? 'Refreshing…' : 'Refresh Data'}
            </Button>
            {pageGuide && (
              <Popover withArrow>
                <PopoverTrigger disableButtonEnhancement>
                  <Tooltip content="Help for this page" relationship="label">
                    <Button appearance="subtle" icon={<QuestionCircleRegular />} aria-label="Help for this page" />
                  </Tooltip>
                </PopoverTrigger>
                <PopoverSurface>
                  <div style={{ maxWidth: 320, display: 'flex', flexDirection: 'column', gap: 8 }}>
                    <Text weight="bold" size={400}>{pageGuide.name}</Text>
                    <Text size={300} style={{ color: 'var(--colorNeutralForeground2)' }}>{pageGuide.body}</Text>
                    <Button appearance="secondary" size="small" onClick={() => navigate('/help')} style={{ alignSelf: 'flex-start' }}>Open full guide</Button>
                  </div>
                </PopoverSurface>
              </Popover>
            )}
            <UserMenu onStartTour={() => setTourOpen(true)} />
          </div>
        </header>

        <main style={{ padding: 24, flex: 1, minWidth: 0 }}>
          {canAccess(currentKey) ? (
            <Outlet />
          ) : (
            <div style={{ display: 'grid', placeItems: 'center', minHeight: '60vh', textAlign: 'center' }}>
              <div>
                <Text size={500} weight="bold" style={{ display: 'block' }}>No access</Text>
                <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>Your role does not have access to this page. Contact an administrator.</Text>
              </div>
            </div>
          )}
        </main>
      </div>
      <GuidedTour open={tourOpen} onClose={() => setTourOpen(false)} />
    </div>
  )
}
