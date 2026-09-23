import axios from 'axios'
import type {
  Account,
  AccountDetail,
  AccountUpsert,
  CapacityRow,
  ExecutiveDashboard,
  LeaveItem,
  LeaveUpsert,
  LeaveWindow,
  Nomination,
  RegionOption,
  Resource,
  ResourceDetail,
  ResourceUpsert,
  RoleCapacity,
  StrategicAccount,
} from './types'

// Same-origin in production (SPA served by the API); dev uses the Vite proxy.
const http = axios.create({ baseURL: '' })

export const api = {
  dashboard: (region?: string) =>
    http.get<ExecutiveDashboard>('/api/dashboard/executive', { params: { region } }).then((r) => r.data),

  resources: (params: Record<string, string | undefined>) =>
    http.get<Resource[]>('/api/resources', { params }).then((r) => r.data),

  resource: (id: number) => http.get<ResourceDetail>(`/api/resources/${id}`).then((r) => r.data),

  createResource: (input: ResourceUpsert) =>
    http.post<ResourceDetail>('/api/resources', input).then((r) => r.data),

  updateResource: (id: number, input: ResourceUpsert) =>
    http.put<ResourceDetail>(`/api/resources/${id}`, input).then((r) => r.data),

  deleteResource: (id: number) => http.delete(`/api/resources/${id}`).then((r) => r.data),

  assignAccount: (resourceId: number, accountId: number, relationshipType = 'Primary') =>
    http.post(`/api/resources/${resourceId}/accounts`, { accountId, relationshipType }).then((r) => r.data),

  unassignAccount: (resourceId: number, accountId: number) =>
    http.delete(`/api/resources/${resourceId}/accounts/${accountId}`).then((r) => r.data),

  accounts: (search?: string, region?: string) =>
    http.get<Account[]>('/api/accounts', { params: { search, region } }).then((r) => r.data),

  account: (id: number) => http.get<AccountDetail>(`/api/accounts/${id}`).then((r) => r.data),

  createAccount: (input: AccountUpsert) =>
    http.post<AccountDetail>('/api/accounts', input).then((r) => r.data),

  updateAccount: (id: number, input: AccountUpsert) =>
    http.put<AccountDetail>(`/api/accounts/${id}`, input).then((r) => r.data),

  deleteAccount: (id: number) => http.delete(`/api/accounts/${id}`).then((r) => r.data),

  capacity: (region?: string) =>
    http.get<CapacityRow[]>('/api/capacity', { params: { region } }).then((r) => r.data),

  strategicAccounts: (region?: string) =>
    http.get<StrategicAccount[]>('/api/strategicaccounts', { params: { region } }).then((r) => r.data),

  leave: (windowDays: number, region?: string) =>
    http.get<LeaveWindow>('/api/leave', { params: { windowDays, region } }).then((r) => r.data),

  createLeave: (input: LeaveUpsert) => http.post<LeaveItem>('/api/leave', input).then((r) => r.data),

  updateLeave: (id: number, input: LeaveUpsert) =>
    http.put<LeaveItem>(`/api/leave/${id}`, input).then((r) => r.data),

  deleteLeave: (id: number) => http.delete(`/api/leave/${id}`).then((r) => r.data),

  nominations: (region?: string, status?: string) =>
    http.get<Nomination[]>('/api/nominations', { params: { region, status } }).then((r) => r.data),

  regions: () => http.get<RegionOption[]>('/api/configuration/regions').then((r) => r.data),

  roles: () => http.get<{ roleName: string }[]>('/api/configuration/roles').then((r) => r.data.map((x) => x.roleName)),

  roleCapacity: () => http.get<RoleCapacity[]>('/api/configuration/capacity').then((r) => r.data),

  updateRoleCapacity: (updates: { roleName: string; capacityLimit: number }[]) =>
    http.put<RoleCapacity[]>('/api/configuration/capacity', updates).then((r) => r.data),

  refresh: () => http.post('/api/admin/refresh').then((r) => r.data),
}
