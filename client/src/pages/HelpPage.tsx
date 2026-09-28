import { Accordion, AccordionHeader, AccordionItem, AccordionPanel, Link, Text } from '@fluentui/react-components'
import type { CSSProperties } from 'react'
import { api } from '../api'
import { ErrorText, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import type { AcrRates } from '../types'

const money = (n: number) => `$${Math.round(n).toLocaleString()}`

export function HelpPage() {
  const { data: rates, loading, error, reload } = useAsync(() => api.acrRates(), [])

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16, maxWidth: 900 }}>
      <div>
        <Text size={600} weight="bold" style={{ display: 'block' }}>Help &amp; FAQ</Text>
        <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>How the portal computes the numbers you see. Rates are configurable (Admin → Configuration).</Text>
      </div>

      <Panel title="Frequently asked questions">
        <Accordion collapsible multiple>
          <AccordionItem value="acr">
            <AccordionHeader>How is ACR (Annual Consumption Revenue) calculated?</AccordionHeader>
            <AccordionPanel>
              {loading && !rates ? <Loading /> : error ? <ErrorText error={error} onRetry={reload} /> : rates ? (
                <AcrAnswer rates={rates} />
              ) : null}
            </AccordionPanel>
          </AccordionItem>
        </Accordion>
      </Panel>

      <ProposalsSection />
    </div>
  )
}

const CONTAINER_ACR_LINK = 'https://outlook.office.com/mail/MBX%3A7bb996a6-f854-4e67-a63b-0007862f5fc6%4072f988bf-86f1-41af-91ab-2d7cd011db47/applink/read/AAkALgAAAAAAHYQDEapmEc2byACqAC-EWg0A2hw4T10NJ06u6N-1HxGV3AABpsuOugAA/'

function ProposalsSection() {
  const th: CSSProperties = { textAlign: 'left', padding: '6px 10px', borderBottom: '2px solid var(--colorNeutralStroke2)', fontSize: 12 }
  const td: CSSProperties = { padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)' }
  const examples = [
    ['1 application containerized into 1 container', 1, 1],
    ['1 application split into 5 containers', 5, 5],
    ['10 applications converted to 10 containers', 10, 10],
    ['20 applications converted into 35 containers', 35, 35],
  ] as const

  return (
    <Panel title="Proposals">
      <div style={{ display: 'flex', flexDirection: 'column', gap: 14, maxWidth: 760 }}>
        <Text size={500} weight="bold">ACR Proposal for Containerized Applications</Text>

        <div>
          <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>Principle</Text>
          <ul style={{ margin: 0, paddingLeft: 20, display: 'flex', flexDirection: 'column', gap: 4 }}>
            <li><Text size={300}><b>1 Container = 1 Application = 1 ACR Unit</b></Text></li>
            <li><Text size={300}>FDO application count must reflect the actual number of <b>containers delivered</b>, not the original application count.</Text></li>
          </ul>
        </div>

        <div>
          <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>Examples</Text>
          <table style={{ borderCollapse: 'collapse', width: '100%' }}>
            <thead><tr><th style={th}>Scenario</th><th style={th}>FDO App Count</th><th style={th}>ACR Count</th></tr></thead>
            <tbody>
              {examples.map(([s, fdo, acr]) => (
                <tr key={s}><td style={td}>{s}</td><td style={td}>{fdo}</td><td style={td}>{acr}</td></tr>
              ))}
            </tbody>
          </table>
        </div>

        <div>
          <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>Rationale</Text>
          <Text size={300} style={{ display: 'block', marginBottom: 6 }}>As stated by Pradeep Kumar Mamidi (International Supplier):</Text>
          <div style={{ borderLeft: '3px solid var(--colorBrandStroke1)', paddingLeft: 12, color: 'var(--colorNeutralForeground2)' }}>
            <Text size={300}>
              If one application has 5 projects which are converted into 5 containers, update FDO as <b>5 apps, not 1 app</b>, to
              ensure correct ACR forecasting and variance tracking. Under-reporting container counts creates false variance
              between FDO and actual delivery.
            </Text>
          </div>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginTop: 8 }}>
            Source:{' '}
            <Link href={CONTAINER_ACR_LINK} target="_blank" rel="noopener noreferrer">ACR calculation for AKS (email thread)</Link>.
          </Text>
        </div>
      </div>
    </Panel>
  )
}

function AcrAnswer({ rates }: { rates: AcrRates }) {
  const months = rates.annualizationMonths || 12
  // Worked example: 10 apps on each target.
  const apps = 10
  const appSvc = apps * rates.appServiceCoresPerApp * rates.appServiceArpuPerCoreMonth * months
  const aksLinux = apps * rates.aksCoresPerApp * rates.aksLinuxArpuPerCoreMonth * months
  const aksWin = apps * rates.aksCoresPerApp * rates.aksWindowsArpuPerCoreMonth * months
  const acaCores = apps * rates.aksCoresPerApp
  const aca = rates.acaArpuPerCoreHour * acaCores * rates.acaUtilization * rates.acaHoursPerMonth * months

  const th: CSSProperties = { textAlign: 'left', padding: '6px 10px', borderBottom: '2px solid var(--colorNeutralStroke2)', fontSize: 12 }
  const td: CSSProperties = { padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)' }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
      <Text size={300}>
        <b>ACR</b> estimates the annual Azure consumption a workload will drive once migrated. The portal derives it from
        two App Factory thumb-rules — <b>how many cores an app needs</b> and <b>the $ rate per core</b> — then annualizes.
        You supply either an <b>app count</b> (the portal converts it to cores) or an explicit <b>core count</b>.
      </Text>

      <div>
        <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>1 · Cores per app (sizing thumb-rules)</Text>
        <table style={{ borderCollapse: 'collapse', width: '100%' }}>
          <thead><tr><th style={th}>Target</th><th style={th}>Cores per app</th></tr></thead>
          <tbody>
            <tr><td style={td}>App Service</td><td style={td}>{rates.appServiceCoresPerApp}</td></tr>
            <tr><td style={td}>Containerized (AKS / ACA)</td><td style={td}>{rates.aksCoresPerApp}</td></tr>
          </tbody>
        </table>
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginTop: 4 }}>
          Containerized workloads are sized higher ({rates.aksCoresPerApp} vs {rates.appServiceCoresPerApp}) because sidecars, ingress and platform overhead add cores per app.
        </Text>
      </div>

      <div>
        <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>2 · Rate per core</Text>
        <table style={{ borderCollapse: 'collapse', width: '100%' }}>
          <thead><tr><th style={th}>Target</th><th style={th}>Rate</th></tr></thead>
          <tbody>
            <tr><td style={td}>App Service</td><td style={td}>{money(rates.appServiceArpuPerCoreMonth)} / core / month</td></tr>
            <tr><td style={td}>AKS (Linux)</td><td style={td}>{money(rates.aksLinuxArpuPerCoreMonth)} / core / month</td></tr>
            <tr><td style={td}>AKS (Windows)</td><td style={td}>{money(rates.aksWindowsArpuPerCoreMonth)} / core / month</td></tr>
            <tr><td style={td}>Azure Container Apps</td><td style={td}>${rates.acaArpuPerCoreHour} / core / hour × {rates.acaUtilization} utilization × {rates.acaHoursPerMonth} hr/mo</td></tr>
          </tbody>
        </table>
      </div>

      <div>
        <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>3 · Formula</Text>
        <div style={{ background: 'var(--colorNeutralBackground3)', borderRadius: 6, padding: '10px 12px', fontFamily: 'monospace', fontSize: 13, lineHeight: 1.6 }}>
          cores = apps × cores-per-app<br />
          monthly ACR = cores × rate-per-core-month<br />
          <b>annual ACR = monthly ACR × {months} months</b><br />
          <span style={{ color: 'var(--colorNeutralForeground3)' }}>(ACA: monthly = rate/core/hr × cores × utilization × hours/mo)</span>
        </div>
      </div>

      <div>
        <Text size={400} weight="semibold" style={{ display: 'block', marginBottom: 4 }}>Worked example — {apps} apps</Text>
        <table style={{ borderCollapse: 'collapse', width: '100%' }}>
          <thead><tr><th style={th}>Target</th><th style={th}>Cores</th><th style={th}>Annual ACR</th></tr></thead>
          <tbody>
            <tr><td style={td}>App Service</td><td style={td}>{apps} × {rates.appServiceCoresPerApp} = {apps * rates.appServiceCoresPerApp}</td><td style={td}><b>{money(appSvc)}</b></td></tr>
            <tr><td style={td}>AKS (Linux)</td><td style={td}>{apps} × {rates.aksCoresPerApp} = {apps * rates.aksCoresPerApp}</td><td style={td}><b>{money(aksLinux)}</b></td></tr>
            <tr><td style={td}>AKS (Windows)</td><td style={td}>{apps} × {rates.aksCoresPerApp} = {apps * rates.aksCoresPerApp}</td><td style={td}><b>{money(aksWin)}</b></td></tr>
            <tr><td style={td}>Azure Container Apps</td><td style={td}>{acaCores}</td><td style={td}><b>{rates.acaArpuPerCoreHour ? money(aca) : '— (set ACA rate)'}</b></td></tr>
          </tbody>
        </table>
        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginTop: 4 }}>
          e.g. App Service: {apps} apps × {rates.appServiceCoresPerApp} cores × {money(rates.appServiceArpuPerCoreMonth)}/core/mo × {months} = {money(appSvc)}/yr.
        </Text>
      </div>

      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
        All rates are editable by an administrator in <b>Configuration → ACR calculation rates</b>, where a live estimator is also available.
      </Text>

      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
        Source of the thumb-rules:{' '}
        <Link href="https://microsoft.sharepoint.com/:x:/t/SMFTeamInternal/cQr-UQcfNWAqSqTwK5Jb9ie0EgUCFc3tPEve27xAUmSMNcs2kg" target="_blank" rel="noopener noreferrer">
          Factory_Realized ADS_ACR Calculation.xlsx
        </Link>.
      </Text>
    </div>
  )
}
