import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import { Layout } from './components/Layout'
import { AccountsPage } from './pages/AccountsPage'
import { CapacityPage } from './pages/CapacityPage'
import { ConfigurationPage } from './pages/ConfigurationPage'
import { DashboardPage } from './pages/DashboardPage'
import { HistoryPage } from './pages/HistoryPage'
import { LeavePage } from './pages/LeavePage'
import { NominationsPage } from './pages/NominationsPage'
import { PerformancePage } from './pages/PerformancePage'
import { ResourcesPage } from './pages/ResourcesPage'
import { StrategicAccountsPage } from './pages/StrategicAccountsPage'
import { RegionProvider } from './region'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'resources', element: <ResourcesPage /> },
      { path: 'accounts', element: <AccountsPage /> },
      { path: 'capacity', element: <CapacityPage /> },
      { path: 'leave', element: <LeavePage /> },
      { path: 'strategic', element: <StrategicAccountsPage /> },
      { path: 'nominations', element: <NominationsPage /> },
      { path: 'performance', element: <PerformancePage /> },
      { path: 'history', element: <HistoryPage /> },
      { path: 'configuration', element: <ConfigurationPage /> },
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
