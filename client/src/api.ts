import axios from 'axios'
import type {
  Account,
  AccountDetail,
  AccountUpsert,
  CapacityRow,
  ExecutiveDashboard,
  DataStatus,
  ImportChange,
  ImportRun,
  LeaveClash,
  LeaveItem,
  LeaveUpsert,
  LeaveWindow,
  Nomination,
  NominationUpdate,
  OperationsSetting,
  PerformanceReview,
  PerformanceReviewUpsert,
  RegionOption,
  Resource,
  ResourceDetail,
  ResourceUpsert,
  RestoreResult,
  RoleCapacity,
  SegmentOption,
  StrategicAccount,
  WaveLink,
  WaveLinkUpsert,
} from './types'

// Same-origin in production (SPA served by the API); dev uses the Vite proxy.
const http = axios.create({ baseURL: '' })

export const api = {
  dashboard: (region?: string) =>
    http.get<ExecutiveDashboard>('/api/dashboard/executive', { params: { region } }).then((r) => r.data),

  adminStatus: () => http.get<DataStatus>('/api/admin/status').then((r) => r.data),

  exportUrl: (
    what: 'resources' | 'capacity' | 'nominations' | 'performance' | 'summary',
    region?: string,
    extra?: Record<string, string | undefined>,
  ) => {
    const qs = new URLSearchParams()
    if (region) qs.set('region', region)
    if (extra) for (const [k, v] of Object.entries(extra)) if (v) qs.set(k, v)
    const q = qs.toString()
    return `/api/export/${what}${q ? `?${q}` : ''}`
  },

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

  leaveClashes: (region?: string) =>
    http.get<LeaveClash[]>('/api/leave/clashes', { params: { region } }).then((r) => r.data),

  createLeave: (input: LeaveUpsert) => http.post<LeaveItem>('/api/leave', input).then((r) => r.data),

  updateLeave: (id: number, input: LeaveUpsert) =>
    http.put<LeaveItem>(`/api/leave/${id}`, input).then((r) => r.data),

  deleteLeave: (id: number) => http.delete(`/api/leave/${id}`).then((r) => r.data),

  nominations: (region?: string, status?: string) =>
    http.get<Nomination[]>('/api/nominations', { params: { region, status } }).then((r) => r.data),

  updateNomination: (id: number, input: NominationUpdate) =>
    http.put<Nomination>(`/api/nominations/${id}`, input).then((r) => r.data),

  addWave: (nominationId: number, input: WaveLinkUpsert) =>
    http.post<WaveLink>(`/api/nominations/${nominationId}/waves`, input).then((r) => r.data),

  deleteWave: (nominationId: number, waveId: number) =>
    http.delete(`/api/nominations/${nominationId}/waves/${waveId}`).then((r) => r.data),

  regions: () => http.get<RegionOption[]>('/api/configuration/regions').then((r) => r.data),

  roles: () => http.get<{ roleName: string }[]>('/api/configuration/roles').then((r) => r.data.map((x) => x.roleName)),

  roleCapacity: () => http.get<RoleCapacity[]>('/api/configuration/capacity').then((r) => r.data),

  updateRoleCapacity: (updates: { roleName: string; capacityLimit: number }[]) =>
    http.put<RoleCapacity[]>('/api/configuration/capacity', updates).then((r) => r.data),

  segments: () => http.get<SegmentOption[]>('/api/configuration/segments').then((r) => r.data),

  addSegment: (name: string) =>
    http.post<SegmentOption>('/api/configuration/segments', { name }).then((r) => r.data),

  updateSegment: (id: number, name: string) =>
    http.put<SegmentOption>(`/api/configuration/segments/${id}`, { name }).then((r) => r.data),

  tools: () => http.get<SegmentOption[]>('/api/configuration/tools').then((r) => r.data),
  addTool: (name: string) => http.post<SegmentOption>('/api/configuration/tools', { name }).then((r) => r.data),
  updateTool: (id: number, name: string) =>
    http.put<SegmentOption>(`/api/configuration/tools/${id}`, { name }).then((r) => r.data),

  skills: () => http.get<SegmentOption[]>('/api/configuration/skills').then((r) => r.data),
  addSkill: (name: string) => http.post<SegmentOption>('/api/configuration/skills', { name }).then((r) => r.data),
  updateSkill: (id: number, name: string) =>
    http.put<SegmentOption>(`/api/configuration/skills/${id}`, { name }).then((r) => r.data),

  operationsSettings: () =>
    http.get<OperationsSetting[]>('/api/configuration/settings').then((r) => r.data),
  updateOperationsSettings: (updates: { key: string; value: string }[]) =>
    http.put<OperationsSetting[]>('/api/configuration/settings', updates).then((r) => r.data),

  performance: (region?: string) =>
    http.get<PerformanceReview[]>('/api/performance', { params: { region } }).then((r) => r.data),
  performanceHistory: (personName: string) =>
    http.get<PerformanceReview[]>(`/api/performance/${encodeURIComponent(personName)}/history`).then((r) => r.data),
  addPerformanceReview: (input: PerformanceReviewUpsert) =>
    http.post<PerformanceReview>('/api/performance', input).then((r) => r.data),

  refresh: () => http.post('/api/admin/refresh').then((r) => r.data),

  uploadData: (kind: 'nominations' | 'resources' | 'leave' | 'engagement' | 'accounts', file: File) => {
    const form = new FormData()
    form.append('file', file)
    return http.post(`/api/admin/upload?kind=${kind}`, form).then((r) => r.data)
  },

  imports: (take = 100) => http.get<ImportRun[]>('/api/imports', { params: { take } }).then((r) => r.data),
  importChanges: (runId: number) =>
    http.get<ImportChange[]>(`/api/imports/${runId}/changes`).then((r) => r.data),

  backupUrl: () => '/api/backup/download',
  restoreDatabase: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return http.post<RestoreResult>('/api/backup/restore', form).then((r) => r.data)
  },
}
