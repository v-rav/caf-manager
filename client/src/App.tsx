import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import { Layout } from './components/Layout'
import { AccountsPage } from './pages/AccountsPage'
import { AnalyticsPage } from './pages/AnalyticsPage'
import { BackupPage } from './pages/BackupPage'
import { CapacityPage } from './pages/CapacityPage'
import { ConfigurationPage } from './pages/ConfigurationPage'
import { DashboardPage } from './pages/DashboardPage'
import { HistoryPage } from './pages/HistoryPage'
import { LeavePage } from './pages/LeavePage'
import { NominationsPage } from './pages/NominationsPage'
import { PerformancePage } from './pages/PerformancePage'
import { ReconciliationPage } from './pages/ReconciliationPage'
import { ResourcesPage } from './pages/ResourcesPage'
import { WorkspaceMockPage } from './pages/WorkspaceMockPage'
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
      { path: 'performance', element: <PerformancePage /> },
      { path: 'history', element: <HistoryPage /> },
      { path: 'configuration', element: <ConfigurationPage /> },
      { path: 'backup', element: <BackupPage /> },
      { path: 'workspace', element: <WorkspaceMockPage /> },
    ],
  },
])

export default function App() {
  return (
    <RegionProvider>
      <RouterProvider router={router} />
    </RegionProvider>
  )
}
