import axios from 'axios'
import type {
  Account,
  AccountDetail,
  AccountUpsert,
  AcrTarget,
  Analytics,
  Attainment,
  AppUser,
  PageAccess,
  CapacityRow,
  ExecutiveDashboard,
  DataStatus,
  Governance,
  Blocker,
  MigrationTool,
  MigrationActivity,
  CapabilityUtilization,
  NominationEvent,
  LookupValue,
  AcrRates,
  AcrEstimate,
  ImportChange,
  ImportRun,
  LeaveClash,
  LeaveItem,
  ReconciliationReport,
  SeedAssignmentResult,
  UnmatchedPeopleResult,
  LinkCleanupResult,
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
  TimeSeries,
  UserRole,
  WaveLink,
  WaveLinkUpsert,
} from './types'

// Same-origin in production (SPA served by the API); dev uses the Vite proxy.
const http = axios.create({ baseURL: '' })

export const api = {
  dashboard: (region?: string) =>
    http.get<ExecutiveDashboard>('/api/dashboard/executive', { params: { region } }).then((r) => r.data),

  analytics: (region?: string) =>
    http.get<Analytics>('/api/analytics', { params: { region } }).then((r) => r.data),
  attainment: (region?: string, fy?: number) =>
    http.get<Attainment>('/api/analytics/attainment', { params: { region, fy } }).then((r) => r.data),

  timeseries: (params: Record<string, string | undefined>) =>
    http.get<TimeSeries>('/api/analytics/timeseries', { params }).then((r) => r.data),

  timeseriesDetail: (params: Record<string, string | undefined>) =>
    http.get<Nomination[]>('/api/analytics/timeseries/detail', { params }).then((r) => r.data),

  adminStatus: () => http.get<DataStatus>('/api/admin/status').then((r) => r.data),

  reconciliation: (region?: string) =>
    http.get<ReconciliationReport>('/api/reconciliation', { params: { region } }).then((r) => r.data),

  seedAssignments: (region: string | undefined, apply: boolean) =>
    http
      .post<SeedAssignmentResult>('/api/reconciliation/seed', null, { params: { region, apply } })
      .then((r) => r.data),

  unmatchedPeople: (region?: string) =>
    http
      .get<UnmatchedPeopleResult>('/api/reconciliation/unmatched-people', { params: { region } })
      .then((r) => r.data),

  cleanupLinks: (region: string | undefined, apply: boolean) =>
    http
      .post<LinkCleanupResult>('/api/reconciliation/cleanup', null, { params: { region, apply } })
      .then((r) => r.data),

  exportUrl: (
    what: 'resources' | 'capacity' | 'nominations' | 'performance' | 'summary' | 'analytics' | 'reconciliation',
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
  nominationVocab: () =>
    http.get<{ classifications: string[]; velocityImpacts: string[] }>('/api/nominations/vocab').then((r) => r.data),

  updateNomination: (id: number, input: NominationUpdate) =>
    http.put<Nomination>(`/api/nominations/${id}`, input).then((r) => r.data),

  addWave: (nominationId: number, input: WaveLinkUpsert) =>
    http.post<WaveLink>(`/api/nominations/${nominationId}/waves`, input).then((r) => r.data),

  deleteWave: (nominationId: number, waveId: number) =>
    http.delete(`/api/nominations/${nominationId}/waves/${waveId}`).then((r) => r.data),

  assignNominationResource: (nominationId: number, resourceId: number, role?: string) =>
    http.post(`/api/nominations/${nominationId}/resources`, { resourceId, role }).then((r) => r.data),

  unassignNominationResource: (nominationId: number, resourceId: number) =>
    http.delete(`/api/nominations/${nominationId}/resources/${resourceId}`).then((r) => r.data),

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

  acrTargets: () =>
    http.get<AcrTarget[]>('/api/configuration/acr-targets').then((r) => r.data),
  updateAcrTargets: (updates: { fiscalYear: number; target: number }[]) =>
    http.put<AcrTarget[]>('/api/configuration/acr-targets', updates).then((r) => r.data),

  login: (username: string, password: string) =>
    http.post<AppUser>('/api/auth/login', { username, password }).then((r) => r.data),
  logout: () => http.post('/api/auth/logout').then((r) => r.data),
  me: () => http.get<AppUser>('/api/auth/me').then((r) => r.data),
  changePassword: (currentPassword: string, newPassword: string) =>
    http.post('/api/auth/change-password', { currentPassword, newPassword }).then((r) => r.data),
  users: () => http.get<AppUser[]>('/api/users').then((r) => r.data),
  createUser: (input: { username: string; displayName: string; role: UserRole; active: boolean; password?: string }) =>
    http.post<AppUser>('/api/users', input).then((r) => r.data),
  updateUser: (id: number, input: { username: string; displayName: string; role: UserRole; active: boolean; password?: string }) =>
    http.put<AppUser>(`/api/users/${id}`, input).then((r) => r.data),
  resetUserPassword: (id: number, newPassword: string) =>
    http.post(`/api/users/${id}/reset-password`, { newPassword }).then((r) => r.data),
  provisionSaLogins: () =>
    http.post<{ created: number; skipped: number; tempPassword: string; users: { username: string; displayName: string }[] }>('/api/users/provision-sa').then((r) => r.data),
  pageAccess: () =>
    http.get<PageAccess[]>('/api/access/pages').then((r) => r.data),
  savePageAccess: (updates: { key: string; allowedRoles: string[] }[]) =>
    http.put<PageAccess[]>('/api/access/pages', updates).then((r) => r.data),

  governance: (nominationId: number) =>
    http.get<Governance>(`/api/nominations/${nominationId}/governance`).then((r) => r.data),
  updateGovernanceItem: (nominationId: number, itemDefId: number, body: { status: string; owner?: string | null; ref?: string | null; notes?: string | null }) =>
    http.put<Governance>(`/api/nominations/${nominationId}/governance/items/${itemDefId}`, body).then((r) => r.data),
  raiseBlocker: (nominationId: number, body: { category: string; clockStopped: boolean; owner?: string | null; expectedResolutionUtc?: string | null; notes?: string | null; gateItemDefId?: number | null }) =>
    http.post<Governance>(`/api/nominations/${nominationId}/governance/blockers`, body).then((r) => r.data),
  resolveBlocker: (nominationId: number, blockerId: number) =>
    http.post<Governance>(`/api/nominations/${nominationId}/governance/blockers/${blockerId}/resolve`).then((r) => r.data),
  openBlockers: (region?: string) =>
    http.get<Blocker[]>('/api/governance/blockers', { params: { region } }).then((r) => r.data),
  blockerCategories: () =>
    http.get<string[]>('/api/governance/blocker-categories').then((r) => r.data),
  blockerOwners: () =>
    http.get<string[]>('/api/governance/blocker-owners').then((r) => r.data),
  milestoneTypes: () =>
    http.get<string[]>('/api/governance/milestone-types').then((r) => r.data),
  addMilestone: (nominationId: number, body: { milestoneKey: string; occurredOn: string; toolUsed?: string | null; notes?: string | null }) =>
    http.post<Governance>(`/api/nominations/${nominationId}/governance/milestones`, body).then((r) => r.data),
  deleteMilestone: (nominationId: number, milestoneId: number) =>
    http.delete<Governance>(`/api/nominations/${nominationId}/governance/milestones/${milestoneId}`).then((r) => r.data),
  migrationTools: () =>
    http.get<MigrationTool[]>('/api/governance/migration-tools').then((r) => r.data),
  migrationActivities: () =>
    http.get<MigrationActivity[]>('/api/governance/migration-activities').then((r) => r.data),
  addToolUsage: (nominationId: number, body: { toolId: number; activityId?: number | null; stage?: number | null; usedOn?: string | null; notes?: string | null }) =>
    http.post<Governance>(`/api/nominations/${nominationId}/governance/tool-usages`, body).then((r) => r.data),
  deleteToolUsage: (nominationId: number, usageId: number) =>
    http.delete<Governance>(`/api/nominations/${nominationId}/governance/tool-usages/${usageId}`).then((r) => r.data),
  capability: (region?: string) =>
    http.get<CapabilityUtilization>('/api/governance/capability', { params: { region } }).then((r) => r.data),
  // Capability masters (Admin CRUD).
  capTools: () =>
    http.get<MigrationTool[]>('/api/capability-master/tools').then((r) => r.data),
  capActivities: () =>
    http.get<MigrationActivity[]>('/api/capability-master/activities').then((r) => r.data),
  createCapTool: (body: { name: string; category: string; vendor?: string | null }) =>
    http.post('/api/capability-master/tools', body).then((r) => r.data),
  updateCapTool: (id: number, body: { name: string; category: string; vendor?: string | null; active?: boolean }) =>
    http.put(`/api/capability-master/tools/${id}`, body).then((r) => r.data),
  deleteCapTool: (id: number) =>
    http.delete(`/api/capability-master/tools/${id}`).then((r) => r.data),
  setCapToolActivities: (id: number, activityIds: number[]) =>
    http.put(`/api/capability-master/tools/${id}/activities`, { activityIds }).then((r) => r.data),
  createCapActivity: (body: { name: string; stage?: string | null }) =>
    http.post('/api/capability-master/activities', body).then((r) => r.data),
  updateCapActivity: (id: number, body: { name: string; stage?: string | null; active?: boolean }) =>
    http.put(`/api/capability-master/activities/${id}`, body).then((r) => r.data),
  deleteCapActivity: (id: number) =>
    http.delete(`/api/capability-master/activities/${id}`).then((r) => r.data),
  lookups: (category?: string) =>
    http.get<LookupValue[]>('/api/configuration/lookups', { params: { category } }).then((r) => r.data),
  addLookup: (category: string, value: string) =>
    http.post<LookupValue>('/api/configuration/lookups', { category, value }).then((r) => r.data),
  updateLookup: (id: number, name: string) =>
    http.put<LookupValue>(`/api/configuration/lookups/${id}`, { name }).then((r) => r.data),
  deleteLookup: (id: number) =>
    http.delete(`/api/configuration/lookups/${id}`).then((r) => r.data),
  acrRates: () =>
    http.get<AcrRates>('/api/acr/rates').then((r) => r.data),
  saveAcrRates: (rates: AcrRates) =>
    http.put<AcrRates>('/api/acr/rates', rates).then((r) => r.data),
  acrEstimate: (body: { targetService: string; apps?: number | null; cores?: number | null }) =>
    http.post<AcrEstimate>('/api/acr/estimate', body).then((r) => r.data),
  governanceEvents: (nominationId: number) =>
    http.get<NominationEvent[]>(`/api/nominations/${nominationId}/governance/events`).then((r) => r.data),
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
