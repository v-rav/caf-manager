import { Button, Dropdown, Option, Text, Tooltip } from '@fluentui/react-components'
import {
  ArrowClockwiseRegular,
  BuildingRegular,
  CalendarLtrRegular,
  DataPieRegular,
  GaugeRegular,
  NavigationRegular,
  PeopleRegular,
  SettingsRegular,
  StarRegular,
  ClipboardTaskListLtrRegular,
  PersonStarRegular,
} from '@fluentui/react-icons'
import { useState, type ReactNode } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { api } from '../api'
import { useAsync } from '../hooks'
import { useRegion } from '../region'

const NAV: { to: string; label: string; end?: boolean; icon: ReactNode }[] = [
  { to: '/', label: 'Executive Dashboard', end: true, icon: <DataPieRegular /> },
  { to: '/resources', label: 'Resource Hub', icon: <PeopleRegular /> },
  { to: '/accounts', label: 'Account Hub', icon: <BuildingRegular /> },
  { to: '/capacity', label: 'Capacity', icon: <GaugeRegular /> },
  { to: '/leave', label: 'Leave', icon: <CalendarLtrRegular /> },
  { to: '/strategic', label: 'Strategic Accounts', icon: <StarRegular /> },
  { to: '/nominations', label: 'Nominations', icon: <ClipboardTaskListLtrRegular /> },
  { to: '/performance', label: 'Performance', icon: <PersonStarRegular /> },
  { to: '/configuration', label: 'Configuration', icon: <SettingsRegular /> },
]

const EXPANDED = 240
const COLLAPSED = 56

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

export function Layout() {
  const { region, setRegion } = useRegion()
  const { data: regions } = useAsync(() => api.regions(), [])
  const { data: status } = useAsync(() => api.adminStatus(), [])
  const [refreshing, setRefreshing] = useState(false)
  const [collapsed, setCollapsed] = useState(() => localStorage.getItem('nav-collapsed') === '1')

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
        <nav style={{ display: 'flex', flexDirection: 'column', gap: 4, marginTop: collapsed ? 8 : 0 }}>
          {NAV.map((item) => (
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
                  padding: collapsed ? '10px 0' : '8px 12px',
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
          ))}
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
            <Button appearance="secondary" icon={<ArrowClockwiseRegular />} disabled={refreshing} onClick={runRefresh}>
              {refreshing ? 'Refreshing…' : 'Refresh Data'}
            </Button>
          </div>
        </header>

        <main style={{ padding: 24, flex: 1, minWidth: 0 }}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
