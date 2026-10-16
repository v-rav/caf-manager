import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import { Button, Spinner, Text } from '@fluentui/react-components'
import { Layout } from './components/Layout'
import { AccountsPage } from './pages/AccountsPage'
import { AccessPage } from './pages/AccessPage'
import { CapabilityMasterPage } from './pages/CapabilityMasterPage'
import { RulesPage } from './pages/RulesPage'
import { GateTemplatePage } from './pages/GateTemplatePage'
import { HelpPage } from './pages/HelpPage'
import { AnalyticsPage } from './pages/AnalyticsPage'
import { VelocityPage } from './pages/VelocityPage'
import { BackupPage } from './pages/BackupPage'
import { CapacityPage } from './pages/CapacityPage'
import { ConfigurationPage } from './pages/ConfigurationPage'
import { DashboardPage } from './pages/DashboardPage'
import { HistoryPage } from './pages/HistoryPage'
import { LeavePage } from './pages/LeavePage'
import { LoginPage, ForcedPasswordChange } from './pages/LoginPage'
import { NominationsPage } from './pages/NominationsPage'
import { NominationWorkspacePage } from './pages/NominationWorkspacePage'
import { GovernanceBoardPage } from './pages/GovernanceBoardPage'
import { StrategicRegisterPage } from './pages/StrategicRegisterPage'
import { AdoptionPage } from './pages/AdoptionPage'
import { FlowPage } from './pages/FlowPage'
import { EffectivenessPage } from './pages/EffectivenessPage'
import { PerformancePage } from './pages/PerformancePage'
import { ReconciliationPage } from './pages/ReconciliationPage'
import { NameConsistencyPage } from './pages/NameConsistencyPage'
import { ResourcesPage } from './pages/ResourcesPage'
import { AuthProvider, useAuth } from './auth'
import { RegionProvider } from './region'
import { FyProvider } from './fy'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'analytics', element: <AnalyticsPage /> },
      { path: 'velocity', element: <VelocityPage /> },
      // ACR Recovery was retired (unused ledger, heuristic estimates); its data-quality signal lives on Nominations.
      { path: 'acr-recovery', element: <Navigate to="/nominations?quality=Missing%20cores" replace /> },
      { path: 'resources', element: <ResourcesPage /> },
      { path: 'accounts', element: <AccountsPage /> },
      { path: 'capacity', element: <CapacityPage /> },
      { path: 'reconciliation', element: <ReconciliationPage /> },
      { path: 'name-consistency', element: <NameConsistencyPage /> },
      { path: 'leave', element: <LeavePage /> },
      { path: 'nominations', element: <NominationsPage /> },
      { path: 'nominations/:id', element: <NominationWorkspacePage /> },
      { path: 'governance', element: <GovernanceBoardPage /> },
      { path: 'strategic', element: <StrategicRegisterPage /> },
      { path: 'adoption', element: <AdoptionPage /> },
      { path: 'flow', element: <FlowPage /> },
      { path: 'effectiveness', element: <EffectivenessPage /> },
      { path: 'performance', element: <PerformancePage /> },
      { path: 'history', element: <HistoryPage /> },
      { path: 'configuration', element: <ConfigurationPage /> },
      { path: 'capability', element: <CapabilityMasterPage /> },
      { path: 'rules', element: <RulesPage /> },
      { path: 'gates', element: <GateTemplatePage /> },
      { path: 'help', element: <HelpPage /> },
      { path: 'access', element: <AccessPage /> },
      { path: 'backup', element: <BackupPage /> },
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
