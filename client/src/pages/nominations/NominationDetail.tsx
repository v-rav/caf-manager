import { Badge, Text } from '@fluentui/react-components'
import type { ReactNode } from 'react'
import type { Nomination } from '../../types'

// Read-only labelled value used in the detail panel and the Manage dialog's FDO context.
export function ReadField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block' }}>
        {label}
      </Text>
      <Text size={300}>{children}</Text>
    </div>
  )
}

const roDash = (v: unknown) => (v === null || v === undefined || v === '' ? '—' : String(v))
const roYesNo = (b?: boolean | null) => (b == null ? '—' : b ? 'Yes' : 'No')
const roUsd = (v?: number | null) => (v ? '$' + Math.round(v).toLocaleString() : '—')
const roDays = (v?: number | null) => (v != null ? `${v}d` : '—')

// Present wave-type short labels for a nomination (empty = none linked yet).
export function presentWaves(n: Nomination): string[] {
  const out: string[] = []
  if (n.dbLinked) out.push('DB')
  if (n.securityLinked) out.push('Sec')
  if (n.alzLinked) out.push('ALZ')
  return out
}

// Wave-presence chip: brand (blue) when the wave is linked, red when it isn't.
export function LinkChip({ label, linked }: { label: string; linked: boolean }) {
  return (
    <Badge appearance={linked ? 'filled' : 'tint'} color={linked ? 'brand' : 'danger'} size="small">
      {label}
    </Badge>
  )
}

// Full read-only field grid for a nomination — rendered inline when a grid row is expanded.
export function NominationDetail({ n }: { n: Nomination }) {
  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: '8px 16px' }}>
      <ReadField label="Task Id">{roDash(n.externalTaskId)}</ReadField>
      <ReadField label="Current State">{n.currentState ? <Badge appearance="tint" color={n.healthTone ?? 'informative'}>{n.currentState}</Badge> : '—'}</ReadField>
      <ReadField label="Age in stage">{roDays(n.stageAgeDays)}</ReadField>
      <ReadField label="Short name">{roDash(n.shortName)}</ReadField>
      <ReadField label="Approval status">{roDash(n.approvalStatus)}</ReadField>
      <ReadField label="Classification">{roDash(n.classification)}</ReadField>
      <ReadField label="Strategic">{n.isStrategic ? `Yes · ${n.strategicTier || '—'}` : 'No'}</ReadField>
      <ReadField label="Days in flight">{n.daysInFlight != null ? `${n.daysInFlight}d${n.daysInFlightSource ? ` · ${n.daysInFlightSource}` : ''}` : '—'}</ReadField>
      <ReadField label="MSI">{`${n.msiScore ?? '—'} · ${n.msiBand ?? ''}`}</ReadField>
      <ReadField label="GHCP level">{n.ghcpAdoptionLevel != null ? `${n.ghcpAdoptionLevel}/7` : '—'}</ReadField>
      <ReadField label="Primary path">{roDash(n.primaryMigrationPath)}</ReadField>
      <ReadField label="Partner">{roDash(n.partnerName)}</ReadField>
      <ReadField label="Total cores">{roDash(n.totalCores)}</ReadField>
      <ReadField label="Mode of access">{roDash(n.modeOfAccess)}</ReadField>
      <ReadField label="Total ACR">{roUsd(n.totalAcr)}</ReadField>
      <ReadField label="NNR ACR">{roUsd(n.nnrAcr)}</ReadField>
      <ReadField label="Tool attached">{roYesNo(n.isToolAttached)}</ReadField>
      <ReadField label="Automation used">{roYesNo(n.isAutomationUsed)}</ReadField>
      <ReadField label="GHCP tools (FDO)">{n.ghcpToolsUsed?.length ? n.ghcpToolsUsed.join(' · ') : '—'}</ReadField>
      <ReadField label="Nominated">{roDash(n.nominatedDate)}</ReadField>
      <ReadField label="Approval date">{roDash(n.approvalDate)}</ReadField>
      <ReadField label="Planned start · end">{`${roDash(n.plannedStartDate)} · ${roDash(n.plannedEndDate)}`}</ReadField>
      <ReadField label="Actual start · end">{`${roDash(n.actualStartDate)} · ${roDash(n.actualEndDate)}`}</ReadField>
      <ReadField label="Total days">{roDash(n.totalDays)}</ReadField>
      <ReadField label="Kickoff → start lag">{roDays(n.kickoffToStartLagDays)}</ReadField>
      <ReadField label="SLA tier">{roDash(n.staleTier)}</ReadField>
      <ReadField label="Effective age">{n.effectiveAgeDays != null ? `${n.effectiveAgeDays}d${n.clockStopped ? ' · paused' : ''}` : '—'}</ReadField>
      <ReadField label="Data quality">{n.dataQualityFlags?.length ? n.dataQualityFlags.join(' · ') : '—'}</ReadField>
      <ReadField label="Days since update">{roDays(n.daysSinceUpdate)}</ReadField>
      <ReadField label="Waves">{n.noWavesLinked ? 'None' : (presentWaves(n).join(' · ') || String(n.waveCount))}</ReadField>
      <ReadField label="Remarks">{roDash(n.remarks)}</ReadField>
      <div style={{ gridColumn: '1 / -1' }}>
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block' }}>
          Team ({n.assignedResources?.length ?? 0})
        </Text>
        {n.assignedResources && n.assignedResources.length > 0 ? (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 4 }}>
            {n.assignedResources.map((m) => (
              <Badge key={m.resourceId} appearance="tint" color="brand">{m.name}{m.role ? ` · ${m.role}` : ''}</Badge>
            ))}
          </div>
        ) : (
          <Text size={300}>No resources assigned yet.</Text>
        )}
      </div>
    </div>
  )
}
