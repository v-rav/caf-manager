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

export interface DataStatus {
  lastRefreshUtc?: string
  resources: number
  accounts: number
  nominations: number
  leaveRecords: number
  performanceReviews: number
}

export interface ImportRun {
  id: number
  startedUtc: string
  completedUtc: string
  source: string
  fileName?: string
  added: number
  updated: number
  withdrawn: number
  unchanged: number
}

export interface FieldChange {
  field: string
  from?: string
  to?: string
}

export interface ImportChange {
  id: number
  entityType: string
  externalKey?: string
  label?: string
  changeType: string
  changes: FieldChange[]
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
  onboardingStatus: string
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
  segment?: string
  resourceCount: number
  projectManager?: string
  solutionArchitect?: string
  cftl?: string
  accountOwner?: string
  customerPoc?: string
  backupOwner?: string
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
  ownershipHistory: OwnershipHistory[]
}

export interface OwnershipHistory {
  id: number
  role: string
  previousOwner?: string
  newOwner?: string
  changedOn: string
  notes?: string
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
  tpid?: string
  technology?: string
  region: string
  status: string
  openedDate: string
  remarks?: string
  migrationStatus?: string
  stageAgeDays?: number
  currentState?: string
  solutionArchitect?: string
  cftlPrimary?: string
  projectCoordinator?: string
  blockedReason?: string
  blockedSince?: string
  followUpDate?: string
  daysSinceUpdate: number
  staleTier: string
  dbLinked: boolean
  alzLinked: boolean
  securityLinked: boolean
  waveCount: number
  noWavesLinked: boolean
  waves: WaveLink[]
}

export interface WaveLink {
  id: number
  waveType: string
  reference: string
  notes?: string
}

export interface NominationUpdate {
  status: string
  migrationStatus?: string
  blockedReason?: string
  blockedSince?: string
  followUpDate?: string
  remarks?: string
  projectCoordinator?: string
  cftlPrimary?: string
  solutionArchitect?: string
}

export interface WaveLinkUpsert {
  waveType: string
  reference: string
  notes?: string
}

export interface LeaveClash {
  resourceId: number
  resourceName: string
  region: string
  activeAccounts: number
  nextLeaveStart: string
  nextLeaveEnd: string
  leaveDaysInWindow: number
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

export interface SegmentOption {
  id: number
  name: string
  sortOrder: number
}

export interface OperationsSetting {
  key: string
  value: string
  description?: string
}

export interface PerformanceReview {
  id: number
  personName: string
  role?: string
  reportingManager?: string
  region: string
  reviewDate: string
  communicationVerbal?: number
  communicationWritten?: number
  attitude?: number
  processUnderstanding?: number
  offeringUnderstanding?: number
  score?: number
  pending: boolean
  trainingNeeds: string[]
  trendDelta?: number
  previousScore?: number
  reviewCount: number
  comments?: string
}

export interface PerformanceReviewUpsert {
  personName: string
  role?: string
  reportingManager?: string
  region?: string
  reviewDate?: string
  communicationVerbal?: number
  communicationWritten?: number
  attitude?: number
  processUnderstanding?: number
  offeringUnderstanding?: number
  comments?: string
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
  onboardingStatus?: string
}

export interface AccountUpsert {
  accountName: string
  region: string
  status?: string
  strategicFlag: boolean
  priorityWeight: number
  segment?: string
  projectManager?: string
  solutionArchitect?: string
  cftl?: string
  accountOwner?: string
  customerPoc?: string
  backupOwner?: string
}

export interface LeaveUpsert {
  resourceId: number
  leaveDate: string
  endDate?: string
  leaveType: string
}
