import { Badge, Button, Checkbox, Dropdown, Input, Option, Text } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import { useState } from 'react'
import { api } from '../api'
import { useAuth } from '../auth'
import { ErrorText, Loading, Panel } from '../components/common'
import { Modal } from '../components/Modal'
import { useAsync } from '../hooks'
import type { GateTemplateGate, GateTemplateItem } from '../types'

const KINDS = ['Task', 'Prerequisite', 'Deliverable', 'Approval', 'Signoff']

export function GateTemplatePage() {
  const { user } = useAuth()
  const gates = useAsync(() => api.gateTemplate(), [])
  const [gateForm, setGateForm] = useState({ key: '', name: '', weight: '10' })
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [editGate, setEditGate] = useState<GateTemplateGate | null>(null)
  const [editItem, setEditItem] = useState<{ item: GateTemplateItem } | null>(null)
  const [addItemFor, setAddItemFor] = useState<GateTemplateGate | null>(null)

  if (user?.role !== 'Admin') {
    return (
      <div style={{ display: 'grid', placeItems: 'center', minHeight: '50vh', textAlign: 'center' }}>
        <div>
          <Text size={500} weight="bold" style={{ display: 'block' }}>Admins only</Text>
          <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>The gate template is restricted to administrators.</Text>
        </div>
      </div>
    )
  }

  const addGate = async () => {
    setBusy(true); setMsg(null)
    try {
      await api.createGate({ key: gateForm.key.trim(), name: gateForm.name.trim() || gateForm.key.trim(), weight: Number(gateForm.weight) || 10 })
      setGateForm({ key: '', name: '', weight: '10' })
      gates.reload()
    } catch { setMsg('Could not add gate (key may be taken).') } finally { setBusy(false) }
  }
  const delGate = async (g: GateTemplateGate) => {
    if (!confirm(`Delete gate "${g.key} · ${g.name}" and all its items?`)) return
    setMsg(null)
    try { await api.deleteGate(g.id); gates.reload() }
    catch { setMsg(`"${g.key}" has captured progress — deactivate it instead of deleting.`) }
  }
  const delItem = async (it: GateTemplateItem) => {
    if (!confirm(`Delete item "${it.label}"?`)) return
    setMsg(null)
    try { await api.deleteGateItem(it.id); gates.reload() }
    catch { setMsg(`"${it.label}" has captured progress — deactivate it instead of deleting.`) }
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div>
        <Text size={600} weight="bold" style={{ display: 'block' }}>Governance Gate Template</Text>
        <Text size={300} style={{ color: 'var(--colorNeutralForeground3)' }}>
          Add, rename, re-weight, reorder, activate/deactivate or delete gates and their checklist items. Deactivating hides
          a gate/item from new work; deleting is blocked once a nomination has captured progress on it.
        </Text>
      </div>

      {gates.loading && !gates.data ? <Loading /> : gates.error ? <ErrorText error={gates.error} onRetry={gates.reload} /> : (
        <>
          {(gates.data ?? []).map((g) => (
            <Panel
              key={g.id}
              title={`${g.key} · ${g.name}`}
              action={
                <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                  <Badge appearance="tint" color="brand">weight {g.weight}%</Badge>
                  <Badge appearance="tint" color={g.active ? 'success' : 'danger'}>{g.active ? 'active' : 'inactive'}</Badge>
                  <Button size="small" appearance="subtle" icon={<EditRegular />} onClick={() => setEditGate(g)}>Edit gate</Button>
                  <Button size="small" appearance="subtle" icon={<DeleteRegular />} onClick={() => delGate(g)} aria-label="Delete gate" />
                </div>
              }
            >
              {g.exitCriteria && <Text size={200} style={{ display: 'block', color: 'var(--colorNeutralForeground3)', marginBottom: 8 }}>Exit: {g.exitCriteria}</Text>}
              <div style={{ overflowX: 'auto' }}>
                <table style={{ borderCollapse: 'collapse', width: '100%', minWidth: 640 }}>
                  <thead>
                    <tr>
                      {['Item', 'Kind', 'Sub-stage', 'Role', 'Mandatory', 'Status', ''].map((h) => (
                        <th key={h} style={{ textAlign: 'left', padding: '6px 8px', borderBottom: '2px solid var(--colorNeutralStroke2)', fontSize: 11 }}>{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {g.items.map((i) => (
                      <tr key={i.id}>
                        <td style={{ padding: '5px 8px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}><Text weight={i.mandatory ? 'semibold' : 'regular'}>{i.label}</Text></td>
                        <td style={{ padding: '5px 8px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}><Badge appearance="tint" color="informative">{i.kind}</Badge></td>
                        <td style={{ padding: '5px 8px', borderBottom: '1px solid var(--colorNeutralStroke2)', color: 'var(--colorNeutralForeground3)' }}>{i.subStage ?? '—'}</td>
                        <td style={{ padding: '5px 8px', borderBottom: '1px solid var(--colorNeutralStroke2)', color: 'var(--colorNeutralForeground3)' }}>{i.responsibleRole}</td>
                        <td style={{ padding: '5px 8px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}>{i.mandatory ? '⚑' : '—'}</td>
                        <td style={{ padding: '5px 8px', borderBottom: '1px solid var(--colorNeutralStroke2)' }}><Badge appearance="tint" color={i.active ? 'success' : 'danger'}>{i.active ? 'active' : 'inactive'}</Badge></td>
                        <td style={{ padding: '5px 8px', borderBottom: '1px solid var(--colorNeutralStroke2)', whiteSpace: 'nowrap' }}>
                          <Button size="small" appearance="subtle" icon={<EditRegular />} onClick={() => setEditItem({ item: i })} aria-label="Edit item" />
                          <Button size="small" appearance="subtle" icon={<DeleteRegular />} onClick={() => delItem(i)} aria-label="Delete item" />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div style={{ marginTop: 8 }}>
                <Button size="small" appearance="secondary" icon={<AddRegular />} onClick={() => setAddItemFor(g)}>Add item</Button>
              </div>
            </Panel>
          ))}

          <Panel title="Add gate">
            <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end', flexWrap: 'wrap' }}>
              <div><Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>Key</Text><Input size="small" placeholder="G9" value={gateForm.key} onChange={(_, d) => setGateForm((f) => ({ ...f, key: d.value }))} style={{ width: 90 }} /></div>
              <div><Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>Name</Text><Input size="small" value={gateForm.name} onChange={(_, d) => setGateForm((f) => ({ ...f, name: d.value }))} style={{ minWidth: 220 }} /></div>
              <div><Text size={100} style={{ display: 'block', color: 'var(--colorNeutralForeground3)' }}>Weight %</Text><Input size="small" type="number" value={gateForm.weight} onChange={(_, d) => setGateForm((f) => ({ ...f, weight: d.value }))} style={{ width: 90 }} /></div>
              <Button appearance="primary" icon={<AddRegular />} disabled={busy || !gateForm.key.trim()} onClick={addGate}>Add gate</Button>
            </div>
            {msg && <Text size={200} style={{ color: 'var(--colorPaletteRedForeground1)', display: 'block', marginTop: 8 }}>{msg}</Text>}
          </Panel>
        </>
      )}

      {editGate && <EditGateModal gate={editGate} onClose={() => setEditGate(null)} onSaved={() => { setEditGate(null); gates.reload() }} />}
      {editItem && <EditItemModal item={editItem.item} onClose={() => setEditItem(null)} onSaved={() => { setEditItem(null); gates.reload() }} />}
      {addItemFor && <AddItemModal gate={addItemFor} onClose={() => setAddItemFor(null)} onSaved={() => { setAddItemFor(null); gates.reload() }} />}
    </div>
  )
}

function EditGateModal({ gate, onClose, onSaved }: { gate: GateTemplateGate; onClose: () => void; onSaved: () => void }) {
  const [key, setKey] = useState(gate.key)
  const [name, setName] = useState(gate.name)
  const [exit, setExit] = useState(gate.exitCriteria ?? '')
  const [weight, setWeight] = useState(String(gate.weight))
  const [ownerRole, setOwnerRole] = useState(gate.ownerRole)
  const [order, setOrder] = useState(String(gate.order))
  const [active, setActive] = useState(gate.active)
  const [busy, setBusy] = useState(false)
  const save = async () => {
    setBusy(true)
    try {
      await api.updateGate(gate.id, { key: key.trim(), name: name.trim(), exitCriteria: exit.trim() || null, weight: Number(weight) || 0, ownerRole: ownerRole.trim() || 'SA', order: Number(order) || gate.order, active })
      onSaved()
    } finally { setBusy(false) }
  }
  return (
    <Modal open title={`Edit gate — ${gate.key}`} onClose={onClose} onSubmit={save} submitDisabled={!key.trim() || !name.trim()} busy={busy} maxWidth={520}>
      <div style={{ display: 'flex', gap: 10 }}>
        <div style={{ width: 100 }}><Text size={200}>Key</Text><Input value={key} onChange={(_, d) => setKey(d.value)} style={{ width: '100%' }} /></div>
        <div style={{ flex: 1 }}><Text size={200}>Name</Text><Input value={name} onChange={(_, d) => setName(d.value)} style={{ width: '100%' }} /></div>
      </div>
      <div><Text size={200}>Exit criteria</Text><Input value={exit} onChange={(_, d) => setExit(d.value)} style={{ width: '100%' }} /></div>
      <div style={{ display: 'flex', gap: 10 }}>
        <div style={{ width: 100 }}><Text size={200}>Weight %</Text><Input type="number" value={weight} onChange={(_, d) => setWeight(d.value)} style={{ width: '100%' }} /></div>
        <div style={{ width: 100 }}><Text size={200}>Order</Text><Input type="number" value={order} onChange={(_, d) => setOrder(d.value)} style={{ width: '100%' }} /></div>
        <div style={{ flex: 1 }}><Text size={200}>Owner role</Text><Input value={ownerRole} onChange={(_, d) => setOwnerRole(d.value)} style={{ width: '100%' }} /></div>
      </div>
      <Checkbox checked={active} onChange={(_, d) => setActive(!!d.checked)} label="Active" />
    </Modal>
  )
}

function EditItemModal({ item, onClose, onSaved }: { item: GateTemplateItem; onClose: () => void; onSaved: () => void }) {
  const [label, setLabel] = useState(item.label)
  const [kind, setKind] = useState(item.kind)
  const [subStage, setSubStage] = useState(item.subStage ?? '')
  const [role, setRole] = useState(item.responsibleRole)
  const [mandatory, setMandatory] = useState(item.mandatory)
  const [order, setOrder] = useState(String(item.order))
  const [active, setActive] = useState(item.active)
  const [busy, setBusy] = useState(false)
  const save = async () => {
    setBusy(true)
    try {
      await api.updateGateItem(item.id, { label: label.trim(), kind, subStage: subStage.trim() || null, responsibleRole: role.trim() || 'SA', mandatory, order: Number(order) || item.order, active })
      onSaved()
    } finally { setBusy(false) }
  }
  return (
    <Modal open title="Edit item" onClose={onClose} onSubmit={save} submitDisabled={!label.trim()} busy={busy} maxWidth={520}>
      <div><Text size={200}>Label</Text><Input value={label} onChange={(_, d) => setLabel(d.value)} style={{ width: '100%' }} /></div>
      <div style={{ display: 'flex', gap: 10 }}>
        <div style={{ flex: 1 }}><Text size={200}>Kind</Text>
          <Dropdown value={kind} selectedOptions={[kind]} onOptionSelect={(_, d) => setKind(d.optionValue ?? kind)} style={{ width: '100%' }}>
            {KINDS.map((k) => <Option key={k} value={k}>{k}</Option>)}
          </Dropdown>
        </div>
        <div style={{ flex: 1 }}><Text size={200}>Sub-stage</Text><Input value={subStage} onChange={(_, d) => setSubStage(d.value)} style={{ width: '100%' }} /></div>
      </div>
      <div style={{ display: 'flex', gap: 10 }}>
        <div style={{ flex: 1 }}><Text size={200}>Role</Text><Input value={role} onChange={(_, d) => setRole(d.value)} style={{ width: '100%' }} /></div>
        <div style={{ width: 100 }}><Text size={200}>Order</Text><Input type="number" value={order} onChange={(_, d) => setOrder(d.value)} style={{ width: '100%' }} /></div>
      </div>
      <div style={{ display: 'flex', gap: 16 }}>
        <Checkbox checked={mandatory} onChange={(_, d) => setMandatory(!!d.checked)} label="Mandatory" />
        <Checkbox checked={active} onChange={(_, d) => setActive(!!d.checked)} label="Active" />
      </div>
    </Modal>
  )
}

function AddItemModal({ gate, onClose, onSaved }: { gate: GateTemplateGate; onClose: () => void; onSaved: () => void }) {
  const [label, setLabel] = useState('')
  const [kind, setKind] = useState('Task')
  const [subStage, setSubStage] = useState('')
  const [role, setRole] = useState('SA')
  const [mandatory, setMandatory] = useState(false)
  const [busy, setBusy] = useState(false)
  const save = async () => {
    setBusy(true)
    try {
      await api.createGateItem(gate.id, { label: label.trim(), kind, subStage: subStage.trim() || null, responsibleRole: role.trim() || 'SA', mandatory })
      onSaved()
    } finally { setBusy(false) }
  }
  return (
    <Modal open title={`Add item — ${gate.key} · ${gate.name}`} onClose={onClose} onSubmit={save} submitLabel="Add" submitDisabled={!label.trim()} busy={busy} maxWidth={520}>
      <div><Text size={200}>Label</Text><Input value={label} onChange={(_, d) => setLabel(d.value)} style={{ width: '100%' }} /></div>
      <div style={{ display: 'flex', gap: 10 }}>
        <div style={{ flex: 1 }}><Text size={200}>Kind</Text>
          <Dropdown value={kind} selectedOptions={[kind]} onOptionSelect={(_, d) => setKind(d.optionValue ?? kind)} style={{ width: '100%' }}>
            {KINDS.map((k) => <Option key={k} value={k}>{k}</Option>)}
          </Dropdown>
        </div>
        <div style={{ flex: 1 }}><Text size={200}>Sub-stage</Text><Input value={subStage} onChange={(_, d) => setSubStage(d.value)} style={{ width: '100%' }} /></div>
      </div>
      <div style={{ display: 'flex', gap: 16, alignItems: 'flex-end' }}>
        <div style={{ flex: 1 }}><Text size={200}>Role</Text><Input value={role} onChange={(_, d) => setRole(d.value)} style={{ width: '100%' }} /></div>
        <Checkbox checked={mandatory} onChange={(_, d) => setMandatory(!!d.checked)} label="Mandatory" />
      </div>
    </Modal>
  )
}
