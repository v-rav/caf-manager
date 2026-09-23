// Shared API response shapes (mirror the backend DTOs).

export interface NameValue {
  name: string
  value: number
}

export interface ExecutiveDashboard {
  totalResources: number
  activeAccounts: number
  availableResources: number
  partiallyUtilizedResources: number
  fullyUtilizedResources: number
  overloadedResources: number
  resourcesOnLeave: number
  strategicAccounts: number
  openNominations: number
  regionDistribution: NameValue[]
  capacityDistribution: NameValue[]
  strategicAccountCoverage: NameValue[]
}

export interface Resource {
  resourceId: number
  psid?: string
  name: string
  email?: string
  region: string
  role: string
  primarySkill?: string
  skills?: string
  experienceYears: number
  status?: string
  dedicatedFlag: boolean
  capacityLimit: number
  activeFlag: boolean
  accountCount: number
  utilizationPercent: number
  capacityStatus: string
  onLeaveToday: boolean
}

export interface AccountSummary {
  accountId: number
  accountName: string
  region: string
  strategicFlag: boolean
  relationshipType?: string
}

export interface LeaveItem {
  id: number
  resourceId: number
  resourceName: string
  region: string
  leaveDate: string
  leaveType: string
}

export interface ResourceDetail extends Resource {
  accounts: AccountSummary[]
  upcomingLeave: LeaveItem[]
}

export interface Account {
  accountId: number
  accountName: string
  region: string
  status?: string
  strategicFlag: boolean
  priorityWeight: number
  resourceCount: number
}

export interface ResourceSummary {
  resourceId: number
  name: string
  region: string
  role: string
  relationshipType?: string
}

export interface Engagement {
  id: number
  date: string
  resourceName?: string
  meetingName?: string
  duration: number
  region?: string
  remarks?: string
}

export interface AccountDetail extends Account {
  assignedResources: ResourceSummary[]
  recentActivity: Engagement[]
}

export interface CapacityRow {
  resourceId: number
  resourceName: string
  region: string
  role: string
  accountCount: number
  capacityLimit: number
  utilizationPercent: number
  capacityStatus: string
  heatColor: string
}

export interface StrategicAccount {
  accountId: number
  accountName: string
  region: string
  priorityWeight: number
  riskFlag: boolean
  executiveVisibilityFlag: boolean
  assignedResourceCount: number
  recentActivityCount: number
  lastActivityDate?: string
  riskIndicator: string
}

export interface LeaveWindow {
  windowDays: number
  totalLeaveDays: number
  distinctResources: number
  items: LeaveItem[]
}

export interface Nomination {
  id: number
  accountId?: number
  accountName?: string
  technology?: string
  region: string
  status: string
  openedDate: string
  remarks?: string
  migrationStatus?: string
  currentState?: string
  solutionArchitect?: string
  cftlPrimary?: string
  projectCoordinator?: string
}

export interface RegionOption {
  code: string
  displayName: string
}

export interface RoleCapacity {
  roleName: string
  description?: string
  capacityLimit: number
  configured: boolean
}

export interface ResourceUpsert {
  psid?: string
  name: string
  email?: string
  region: string
  role: string
  primarySkill?: string
  skills?: string
  experienceYears: number
  status?: string
  dedicatedFlag: boolean
  capacityLimit: number
  activeFlag: boolean
}

export interface AccountUpsert {
  accountName: string
  region: string
  status?: string
  strategicFlag: boolean
  priorityWeight: number
}

export interface LeaveUpsert {
  resourceId: number
  leaveDate: string
  leaveType: string
}
