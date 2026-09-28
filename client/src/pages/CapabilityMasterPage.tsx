import { Badge, Button, Checkbox, Dropdown, Input, Option, Text } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular, LinkRegular } from '@fluentui/react-icons'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { useAuth } from '../auth'
import { ErrorText, Loading, Panel } from '../components/common'
import { Modal } from '../components/Modal'
import { useAsync } from '../hooks'
import type { MigrationActivity, MigrationTool } from '../types'

const CATEGORIES = ['Assessment', 'GHCP', 'Accelerator', 'IaC', 'Other']
const STAGES = ['Assessment', 'Planning & Architecture', 'Modernization', 'Engineering Automation', 'Migration', 'Operations']

export function CapabilityMasterPage() {
  const { user } = useAuth()
  if (user?.role !== 'Admin') {
    return (
      <div style={{ display: 'grid', placeItems: 'center', minHeight: '50vh', textAlign: 'center' }}>
        <div>
          <Text size={500} weight="bold" style={{ display: 'block' }}>Admins only</Text>
          <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>Capability masters are restricted to administrators.</Text>
        </div>
      </div>
    )
  }
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
      <div>
        <Text size={600} weight="bold" style={{ display: 'block' }}>Migration Capability Masters</Text>
        <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>
          Manage the tool and activity catalogs and each tool's supported activities. Actual usage is captured per nomination in the SA Workspace.
        </Text>
      </div>
      <ActivitiesPanel />
      <ToolsPanel />
    </div>
  )
}

function ToolsPanel() {
  const tools = useAsync(() => api.capTools(), [])
  const acts = useAsync(() => api.capActivities(), [])
  const [form, setForm] = useState({ name: '', category: 'GHCP', vendor: '' })
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [edit, setEdit] = useState<MigrationTool | null>(null)
  const [supported, setSupported] = useState<MigrationTool | null>(null)

  const add = async () => {
    setBusy(true); setMsg(null)
    try {
      await api.createCapTool({ name: form.name.trim(), category: form.category, vendor: form.vendor.trim() || null })
      setForm({ name: '', category: 'GHCP', vendor: '' })
      tools.reload()
    } catch { setMsg('Could not add tool (name may be taken).') } finally { setBusy(false) }
  }
  const del = async (t: MigrationTool) => {
    if (!confirm(`Delete tool "${t.name}"?`)) return
    setMsg(null)
    try { await api.deleteCapTool(t.id); tools.reload() }
    catch { setMsg(`"${t.name}" has captured usage — deactivate it instead of deleting.`) }
  }

  return (
    <Panel title={`Tools${tools.data ? ` · ${tools.data.length}` : ''}`}>
      {tools.loading || acts.loading ? <Loading /> : tools.error ? <ErrorText error={tools.error} onRetry={tools.reload} /> : (
        <>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ borderCollapse: 'collapse', width: '100%', minWidth: 640 }}>
              <thead>
                <tr>
                  {['Tool', 'Category', 'Vendor', 'Supported activities', 'Status', ''].map((h) => (
                    <th key={h} style={{ textAlign: 'left', padding: '8px 10px', borderBottom: '2px solid var(--colorNeutralStroke2)', fontSize: 12 }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {(tools.data ?? []).map((t) => (
                  <tr key={t.id}>
                    <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}><Text weight="semibold">{t.name}</Text></td>
                    <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}><Badge appearance="tint" color="brand">{t.category}</Badge></td>
                    <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)', color: 'var(--colorNeutralForeground3)' }}>{t.vendor ?? '—'}</td>
                    <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}>
                      <Button size="small" appearance="subtle" icon={<LinkRegular />} onClick={() => setSupported(t)}>
                        {t.supportedActivityIds.length ? `${t.supportedActivityIds.length} activities` : 'Any activity'}
                      </Button>
                    </td>
                    <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}>
                      <Badge appearance="tint" color={t.active ? 'success' : 'danger'}>{t.active ? 'active' : 'inactive'}</Badge>
                    </td>
                    <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)', whiteSpace: 'nowrap' }}>
                      <Button size="small" appearance="subtle" icon={<EditRegular />} onClick={() => setEdit(t)} aria-label="Edit tool" />
                      <Button size="small" appearance="subtle" icon={<DeleteRegular />} onClick={() => del(t)} aria-label="Delete tool" />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end', flexWrap: 'wrap', borderTop: '1px solid var(--colorNeutralStroke2)', paddingTop: 12, marginTop: 8 }}>
            <div><Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>Tool</Text><Input size="small" value={form.name} onChange={(_, d) => setForm((f) => ({ ...f, name: d.value }))} /></div>
            <div><Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>Category</Text>
              <Dropdown size="small" value={form.category} selectedOptions={[form.category]} onOptionSelect={(_, d) => setForm((f) => ({ ...f, category: d.optionValue ?? f.category }))} style={{ minWidth: 130 }}>
                {CATEGORIES.map((c) => <Option key={c} value={c}>{c}</Option>)}
              </Dropdown>
            </div>
            <div><Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>Vendor</Text><Input size="small" value={form.vendor} onChange={(_, d) => setForm((f) => ({ ...f, vendor: d.value }))} /></div>
            <Button appearance="primary" icon={<AddRegular />} disabled={busy || !form.name.trim()} onClick={add}>Add tool</Button>
          </div>
          {msg && <Text size={200} style={{ color: 'var(--colorPaletteRedForeground1)', display: 'block', marginTop: 8 }}>{msg}</Text>}
        </>
      )}

      {edit && (
        <EditToolModal tool={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); tools.reload() }} />
      )}
      {supported && (
        <SupportedModal tool={supported} activities={acts.data ?? []} onClose={() => setSupported(null)} onSaved={() => { setSupported(null); tools.reload() }} />
      )}
    </Panel>
  )
}

function EditToolModal({ tool, onClose, onSaved }: { tool: MigrationTool; onClose: () => void; onSaved: () => void }) {
  const [name, setName] = useState(tool.name)
  const [category, setCategory] = useState(tool.category)
  const [vendor, setVendor] = useState(tool.vendor ?? '')
  const [active, setActive] = useState(tool.active)
  const [busy, setBusy] = useState(false)
  const save = async () => {
    setBusy(true)
    try { await api.updateCapTool(tool.id, { name: name.trim(), category, vendor: vendor.trim() || null, active }); onSaved() }
    finally { setBusy(false) }
  }
  return (
    <Modal open title={`Edit — ${tool.name}`} onClose={onClose} onSubmit={save} submitDisabled={!name.trim()} busy={busy}>
      <div><Text size={200}>Name</Text><Input value={name} onChange={(_, d) => setName(d.value)} style={{ width: '100%' }} /></div>
      <div><Text size={200}>Category</Text>
        <Dropdown value={category} selectedOptions={[category]} onOptionSelect={(_, d) => setCategory(d.optionValue ?? category)} style={{ width: '100%' }}>
          {CATEGORIES.map((c) => <Option key={c} value={c}>{c}</Option>)}
        </Dropdown>
      </div>
      <div><Text size={200}>Vendor</Text><Input value={vendor} onChange={(_, d) => setVendor(d.value)} style={{ width: '100%' }} /></div>
      <Checkbox checked={active} onChange={(_, d) => setActive(!!d.checked)} label="Active" />
    </Modal>
  )
}

function SupportedModal({ tool, activities, onClose, onSaved }: {
  tool: MigrationTool; activities: MigrationActivity[]; onClose: () => void; onSaved: () => void
}) {
  const [sel, setSel] = useState<Set<number>>(new Set(tool.supportedActivityIds))
  const [busy, setBusy] = useState(false)
  const byStage = useMemo(() => {
    const m = new Map<string, MigrationActivity[]>()
    for (const a of activities) { const s = a.stage || 'Other'; (m.get(s) ?? m.set(s, []).get(s)!).push(a) }
    return [...m.entries()]
  }, [activities])
  const toggle = (id: number, on: boolean) => setSel((s) => { const n = new Set(s); on ? n.add(id) : n.delete(id); return n })
  const save = async () => {
    setBusy(true)
    try { await api.setCapToolActivities(tool.id, [...sel]); onSaved() } finally { setBusy(false) }
  }
  return (
    <Modal open title={`Supported activities — ${tool.name}`} onClose={onClose} onSubmit={save} busy={busy} maxWidth={560}>
      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
        Tick the activities this tool can support. Leave all unticked to mean it supports <b>any</b> activity.
      </Text>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10, maxHeight: '55vh', overflowY: 'auto' }}>
        {byStage.map(([stage, list]) => (
          <div key={stage}>
            <Text size={200} weight="semibold" style={{ display: 'block', marginBottom: 2 }}>{stage}</Text>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 2 }}>
              {list.map((a) => <Checkbox key={a.id} checked={sel.has(a.id)} onChange={(_, d) => toggle(a.id, !!d.checked)} label={a.name} />)}
            </div>
          </div>
        ))}
      </div>
    </Modal>
  )
}

function ActivitiesPanel() {
  const acts = useAsync(() => api.capActivities(), [])
  const [form, setForm] = useState({ name: '', stage: 'Modernization' })
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [edit, setEdit] = useState<MigrationActivity | null>(null)

  const add = async () => {
    setBusy(true); setMsg(null)
    try {
      await api.createCapActivity({ name: form.name.trim(), stage: form.stage || null })
      setForm({ name: '', stage: 'Modernization' })
      acts.reload()
    } catch { setMsg('Could not add activity (name may be taken).') } finally { setBusy(false) }
  }
  const del = async (a: MigrationActivity) => {
    if (!confirm(`Delete activity "${a.name}"?`)) return
    setMsg(null)
    try { await api.deleteCapActivity(a.id); acts.reload() }
    catch { setMsg(`"${a.name}" has captured usage — deactivate it instead of deleting.`) }
  }

  return (
    <Panel title={`Activities${acts.data ? ` · ${acts.data.length}` : ''}`}>
      {acts.loading ? <Loading /> : acts.error ? <ErrorText error={acts.error} onRetry={acts.reload} /> : (
        <>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ borderCollapse: 'collapse', width: '100%', minWidth: 480 }}>
              <thead>
                <tr>
                  {['Activity', 'Stage', 'Status', ''].map((h) => (
                    <th key={h} style={{ textAlign: 'left', padding: '8px 10px', borderBottom: '2px solid var(--colorNeutralStroke2)', fontSize: 12 }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {(acts.data ?? []).map((a) => (
                  <tr key={a.id}>
                    <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}><Text weight="semibold">{a.name}</Text></td>
                    <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)', color: 'var(--colorNeutralForeground3)' }}>{a.stage ?? '—'}</td>
                    <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}>
                      <Badge appearance="tint" color={a.active ? 'success' : 'danger'}>{a.active ? 'active' : 'inactive'}</Badge>
                    </td>
                    <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--colorNeutralStroke2)', whiteSpace: 'nowrap' }}>
                      <Button size="small" appearance="subtle" icon={<EditRegular />} onClick={() => setEdit(a)} aria-label="Edit activity" />
                      <Button size="small" appearance="subtle" icon={<DeleteRegular />} onClick={() => del(a)} aria-label="Delete activity" />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end', flexWrap: 'wrap', borderTop: '1px solid var(--colorNeutralStroke2)', paddingTop: 12, marginTop: 8 }}>
            <div><Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>Activity</Text><Input size="small" value={form.name} onChange={(_, d) => setForm((f) => ({ ...f, name: d.value }))} /></div>
            <div><Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>Stage</Text>
              <Dropdown size="small" value={form.stage} selectedOptions={[form.stage]} onOptionSelect={(_, d) => setForm((f) => ({ ...f, stage: d.optionValue ?? f.stage }))} style={{ minWidth: 180 }}>
                {STAGES.map((s) => <Option key={s} value={s}>{s}</Option>)}
              </Dropdown>
            </div>
            <Button appearance="primary" icon={<AddRegular />} disabled={busy || !form.name.trim()} onClick={add}>Add activity</Button>
          </div>
          {msg && <Text size={200} style={{ color: 'var(--colorPaletteRedForeground1)', display: 'block', marginTop: 8 }}>{msg}</Text>}
        </>
      )}
      {edit && <EditActivityModal activity={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); acts.reload() }} />}
    </Panel>
  )
}

function EditActivityModal({ activity, onClose, onSaved }: { activity: MigrationActivity; onClose: () => void; onSaved: () => void }) {
  const [name, setName] = useState(activity.name)
  const [stage, setStage] = useState(activity.stage ?? '')
  const [active, setActive] = useState(activity.active)
  const [busy, setBusy] = useState(false)
  const save = async () => {
    setBusy(true)
    try { await api.updateCapActivity(activity.id, { name: name.trim(), stage: stage || null, active }); onSaved() }
    finally { setBusy(false) }
  }
  return (
    <Modal open title={`Edit — ${activity.name}`} onClose={onClose} onSubmit={save} submitDisabled={!name.trim()} busy={busy}>
      <div><Text size={200}>Name</Text><Input value={name} onChange={(_, d) => setName(d.value)} style={{ width: '100%' }} /></div>
      <div><Text size={200}>Stage</Text>
        <Dropdown value={stage} selectedOptions={[stage]} onOptionSelect={(_, d) => setStage(d.optionValue ?? stage)} style={{ width: '100%' }}>
          {STAGES.map((s) => <Option key={s} value={s}>{s}</Option>)}
        </Dropdown>
      </div>
      <Checkbox checked={active} onChange={(_, d) => setActive(!!d.checked)} label="Active" />
    </Modal>
  )
}
