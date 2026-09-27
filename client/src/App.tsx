import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import { Spinner } from '@fluentui/react-components'
import { Layout } from './components/Layout'
import { AccountsPage } from './pages/AccountsPage'
import { AnalyticsPage } from './pages/AnalyticsPage'
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
import { PerformancePage } from './pages/PerformancePage'
import { ReconciliationPage } from './pages/ReconciliationPage'
import { ResourcesPage } from './pages/ResourcesPage'
import { WorkspaceMockPage } from './pages/WorkspaceMockPage'
import { AuthProvider, useAuth } from './auth'
import { RegionProvider } from './region'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'analytics', element: <AnalyticsPage /> },
      { path: 'resources', element: <ResourcesPage /> },
      { path: 'accounts', element: <AccountsPage /> },
      { path: 'capacity', element: <CapacityPage /> },
      { path: 'reconciliation', element: <ReconciliationPage /> },
      { path: 'leave', element: <LeavePage /> },
      { path: 'nominations', element: <NominationsPage /> },
      { path: 'nominations/:id', element: <NominationWorkspacePage /> },
      { path: 'governance', element: <GovernanceBoardPage /> },
      { path: 'strategic', element: <StrategicRegisterPage /> },
      { path: 'performance', element: <PerformancePage /> },
      { path: 'history', element: <HistoryPage /> },
      { path: 'configuration', element: <ConfigurationPage /> },
      { path: 'backup', element: <BackupPage /> },
      { path: 'workspace', element: <WorkspaceMockPage /> },
    ],
  },
])

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
        <AuthGate />
      </RegionProvider>
    </AuthProvider>
  )
}
