// Shared API response shapes (mirror the backend DTOs).

export interface NameValue {
  name: string
  value: number
}

export interface TimeBucket {
  key: string
  label: string
  values: NameValue[]
  total: number
}

export interface TimeSeries {
  basis: string
  granularity: string
  measure: string
  splitBy: string
  series: string[]
  buckets: TimeBucket[]
  total: number
  recordsWithoutDate: number
  fiscalYears: number[]
}

export interface Analytics {
  totalApproved: number
  inFlight: number
  completed: number
  totalAcr: number
  nnrAcr: number
  totalCores: number
  withAcr: number
  toolAttached: number
  automationUsed: number
  toolFlagDenom: number
  byStage: NameValue[]
  byHealth: NameValue[]
  bySla: NameValue[]
  byRegion: NameValue[]
  bySegment: NameValue[]
  byMigrationPath: NameValue[]
  byModeOfAccess: NameValue[]
  waveLinkage: NameValue[]
  acrByRegion: NameValue[]
  acrBySegment: NameValue[]
  acrByMigrationPath: NameValue[]
  coresByStage: NameValue[]
  topPartnersByAcr: NameValue[]
  acrByApproval: NameValue[]
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

export interface RestoreResult {
  success: boolean
  message: string
  restoredBytes: number
  restoredUtc: string
  nominations: number
  resources: number
  accounts: number
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
  mobile?: string
  aliases?: string
  region: string
  role: string
  primarySkill?: string
  skills?: string
  experienceYears: number
  status?: string
  separated: boolean
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
  tpid?: string
  externalAccountId?: string
  aliases?: string
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
  accounts?: string[]
  capacityLimit: number
  utilizationPercent: number
  capacityStatus: string
  heatColor: string
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
  approvalStatus?: string
  stageAgeDays?: number
  currentState?: string
  solutionArchitect?: string
  cftlPrimary?: string
  projectCoordinator?: string
  blockedReason?: string
  blockedSince?: string
  followUpDate?: string
  primaryMigrationPath?: string
  partnerName?: string
  totalCores?: number
  isToolAttached?: boolean
  isAutomationUsed?: boolean
  modeOfAccess?: string
  totalAcr?: number
  nnrAcr?: number
  nominatedDate?: string
  approvalDate?: string
  actualStartDate?: string
  actualEndDate?: string
  plannedStartDate?: string
  plannedEndDate?: string
  totalDays?: number
  daysSinceUpdate: number
  staleTier: string
  dbLinked: boolean
  alzLinked: boolean
  securityLinked: boolean
  waveCount: number
  noWavesLinked: boolean
  waves: WaveLink[]
  assignedResources: NominationResource[]
  assignedResourceCount: number
}

export interface NominationResource {
  resourceId: number
  name: string
  region: string
  role?: string
}

export interface ReconciliationRow {
  resourceId: number
  resourceName: string
  resourceRegion: string
  accountId: number
  accountName: string
  tpid?: string
  segment?: string
  relationshipType?: string
  inMaster: boolean
  inFlight: boolean
  matchState: string
  suggestedAccountId?: number
  suggestedAccountName?: string
  suggestedTpid?: string
  utilizationEffect: string
}

export interface ReconciliationSummary {
  totalLinks: number
  resources: number
  linkedAccounts: number
  master: number
  suggestMerge: number
  orphan: number
  keep: number
  drop: number
  inFlightAccounts: number
}

export interface ReconciliationReport {
  summary: ReconciliationSummary
  rows: ReconciliationRow[]
}

export interface SeedAssignmentResult {
  applied: boolean
  inFlightLinks: number
  candidates: number
  wouldCreate: number
  created: number
  removedSeed: number
  saCreated: number
  engineerCreated: number
  saUnmatchedWaves: number
  skippedExisting: number
  resourcesAffected: number
  nominationsAffected: number
  sample: {
    nominationId: number
    accountName?: string
    resourceId: number
    resourceName: string
    role: string
    alreadyExisted: boolean
  }[]
}

export interface UnmatchedPerson {
  name: string
  role: string
  waveCount: number
  regions: string
  sampleAccounts: string
}

export interface UnmatchedPeopleResult {
  totalPeople: number
  totalReferences: number
  people: UnmatchedPerson[]
}

export interface LinkCleanupResult {
  applied: boolean
  mergeable: number
  repointed: number
  deduped: number
  orphans: number
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

export interface AcrTarget {
  fiscalYear: number
  label: string
  target: number
}

export interface AttainmentBucket {
  key: string
  label: string
  monthIndex: number
  target: number
  completed: number
  inflight: number
  vtt: number
}

export interface Attainment {
  fiscalYear: number
  label: string
  measure: string
  annualTarget: number
  targetSet: boolean
  buckets: AttainmentBucket[]
  currentMonthIndex: number
  targetToDate: number
  completedYtd: number
  inflightYtd: number
  vtt: number
  attainmentPct: number
  pacePct: number
  completedCount: number
  avgNominationSize: number
  nominationsNeeded: number
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
  mobile?: string
  aliases?: string
  region: string
  role: string
  primarySkill?: string
  skills?: string
  experienceYears: number
  status?: string
  separated: boolean
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
  aliases?: string
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
