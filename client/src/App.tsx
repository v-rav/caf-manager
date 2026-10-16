import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import { Button, Spinner, Text } from '@fluentui/react-components'
import { lazy, Suspense, type ComponentType, type LazyExoticComponent } from 'react'
import { Layout } from './components/Layout'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage, ForcedPasswordChange } from './pages/LoginPage'
import { NominationsPage } from './pages/NominationsPage'
import { NominationWorkspacePage } from './pages/NominationWorkspacePage'
import { AuthProvider, useAuth } from './auth'
import { RegionProvider } from './region'
import { FyProvider } from './fy'

// Less-used pages load on first visit, keeping the initial download small. Pages use named exports.
function page<K extends string>(load: () => Promise<Record<K, ComponentType>>, name: K): LazyExoticComponent<ComponentType> {
  return lazy(() => load().then((m) => ({ default: m[name] })))
}
const AccountsPage = page(() => import('./pages/AccountsPage'), 'AccountsPage')
const AccessPage = page(() => import('./pages/AccessPage'), 'AccessPage')
const CapabilityMasterPage = page(() => import('./pages/CapabilityMasterPage'), 'CapabilityMasterPage')
const RulesPage = page(() => import('./pages/RulesPage'), 'RulesPage')
const GateTemplatePage = page(() => import('./pages/GateTemplatePage'), 'GateTemplatePage')
const HelpPage = page(() => import('./pages/HelpPage'), 'HelpPage')
const AnalyticsPage = page(() => import('./pages/AnalyticsPage'), 'AnalyticsPage')
const VelocityPage = page(() => import('./pages/VelocityPage'), 'VelocityPage')
const BackupPage = page(() => import('./pages/BackupPage'), 'BackupPage')
const CapacityPage = page(() => import('./pages/CapacityPage'), 'CapacityPage')
const ConfigurationPage = page(() => import('./pages/ConfigurationPage'), 'ConfigurationPage')
const HistoryPage = page(() => import('./pages/HistoryPage'), 'HistoryPage')
const LeavePage = page(() => import('./pages/LeavePage'), 'LeavePage')
const GovernanceBoardPage = page(() => import('./pages/GovernanceBoardPage'), 'GovernanceBoardPage')
const StrategicRegisterPage = page(() => import('./pages/StrategicRegisterPage'), 'StrategicRegisterPage')
const AdoptionPage = page(() => import('./pages/AdoptionPage'), 'AdoptionPage')
const FlowPage = page(() => import('./pages/FlowPage'), 'FlowPage')
const EffectivenessPage = page(() => import('./pages/EffectivenessPage'), 'EffectivenessPage')
const PerformancePage = page(() => import('./pages/PerformancePage'), 'PerformancePage')
const ReconciliationPage = page(() => import('./pages/ReconciliationPage'), 'ReconciliationPage')
const NameConsistencyPage = page(() => import('./pages/NameConsistencyPage'), 'NameConsistencyPage')
const ResourcesPage = page(() => import('./pages/ResourcesPage'), 'ResourcesPage')

const L = (C: ComponentType) => <Suspense fallback={<Spinner label="Loading…" style={{ padding: 40 }} />}><C /></Suspense>

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'analytics', element: L(AnalyticsPage) },
      { path: 'velocity', element: L(VelocityPage) },
      // ACR Recovery was retired (unused ledger, heuristic estimates); its data-quality signal lives on Nominations.
      { path: 'acr-recovery', element: <Navigate to="/nominations?quality=Missing%20cores" replace /> },
      { path: 'resources', element: L(ResourcesPage) },
      { path: 'accounts', element: L(AccountsPage) },
      { path: 'capacity', element: L(CapacityPage) },
      { path: 'reconciliation', element: L(ReconciliationPage) },
      { path: 'name-consistency', element: L(NameConsistencyPage) },
      { path: 'leave', element: L(LeavePage) },
      { path: 'nominations', element: <NominationsPage /> },
      { path: 'nominations/:id', element: <NominationWorkspacePage /> },
      { path: 'governance', element: L(GovernanceBoardPage) },
      { path: 'strategic', element: L(StrategicRegisterPage) },
      { path: 'adoption', element: L(AdoptionPage) },
      { path: 'flow', element: L(FlowPage) },
      { path: 'effectiveness', element: L(EffectivenessPage) },
      { path: 'performance', element: L(PerformancePage) },
      { path: 'history', element: L(HistoryPage) },
      { path: 'configuration', element: L(ConfigurationPage) },
      { path: 'capability', element: L(CapabilityMasterPage) },
      { path: 'rules', element: L(RulesPage) },
      { path: 'gates', element: L(GateTemplatePage) },
      { path: 'help', element: L(HelpPage) },
      { path: 'access', element: L(AccessPage) },
      { path: 'backup', element: L(BackupPage) },
      // The P0 workspace mock was retired; old bookmarks land on the real nominations grid.
      { path: 'workspace', element: <Navigate to="/nominations" replace /> },
      { path: '*', element: <NotFound /> },
    ],
  },
])

function NotFound() {
  return (
    <div style={{ padding: 40, display: 'flex', flexDirection: 'column', gap: 12, alignItems: 'flex-start' }}>
      <Text size={600} weight="bold">Page not found</Text>
      <Text>This page doesn’t exist or has moved. Use the menu on the left, or go back to the dashboard.</Text>
      <Button appearance="primary" as="a" href="/">Go to dashboard</Button>
    </div>
  )
}

function AuthGate() {
  const { user, loading } = useAuth()
  if (loading) return <div style={{ minHeight: '100vh', display: 'grid', placeItems: 'center' }}><Spinner label="Loading…" /></div>
  if (!user) return <LoginPage />
  if (user.mustChangePassword) return <ForcedPasswordChange />
  return <RouterProvider router={router} />
}

export default function App() {
  return (
    <AuthProvider>
      <RegionProvider>
        <FyProvider>
          <AuthGate />
        </FyProvider>
      </RegionProvider>
    </AuthProvider>
  )
}
