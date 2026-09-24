import {
  Button,
  Dropdown,
  Input,
  Option,
  Tab,
  TabList,
  Text,
} from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import { useState } from 'react'
import { api } from '../api'
import { ConfirmDialog, Modal } from '../components/Modal'
import { DataTable } from '../components/DataTable'
import { ErrorText, Loading, Panel } from '../components/common'
import { KpiCard } from '../components/KpiCard'
import { useAsync } from '../hooks'
import { useRegion } from '../region'
import type { LeaveItem, LeaveUpsert } from '../types'

const LEAVE_TYPES = ['Leave', 'Holiday', 'Regional Holiday', 'Optional Holiday', 'Special Leave']
const today = () => new Date().toISOString().slice(0, 10)

export function LeavePage() {
  const { region } = useRegion()
  const [windowDays, setWindowDays] = useState(30)
  const { data, loading, error, reload } = useAsync(() => api.leave(windowDays, region), [windowDays, region])
  const { data: resources } = useAsync(() => api.resources({}), [])

  const [form, setForm] = useState<LeaveUpsert | null>(null)
  const [editId, setEditId] = useState<number | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<LeaveItem | null>(null)
  const [busy, setBusy] = useState(false)

  const openCreate = () => {
    setEditId(null)
    setForm({ resourceId: 0, leaveDate: today(), endDate: today(), leaveType: 'Leave' })
  }
  const openEdit = (l: LeaveItem) => {
    setEditId(l.id)
    setForm({ resourceId: l.resourceId, leaveDate: l.leaveDate.slice(0, 10), leaveType: l.leaveType })
  }

  const patch = (p: Partial<LeaveUpsert>) => setForm((f) => (f ? { ...f, ...p } : f))

  const save = async () => {
    if (!form) return
    setBusy(true)
    try {
      if (editId) await api.updateLeave(editId, form)
      else await api.createLeave(form)
      setForm(null)
      reload()
    } finally {
      setBusy(false)
    }
  }

  const confirmDelete = async () => {
    if (!deleteTarget) return
    setBusy(true)
    try {
      await api.deleteLeave(deleteTarget.id)
      setDeleteTarget(null)
      reload()
    } finally {
      setBusy(false)
    }
  }

  const valid = !!form && form.resourceId > 0 && !!form.leaveDate && (!form.endDate || form.endDate >= form.leaveDate)
  const resourceName = (id: number) => resources?.find((r) => r.resourceId === id)?.name ?? ''

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Text size={600} weight="bold">
          Leave Dashboard
        </Text>
        <Button appearance="primary" icon={<AddRegular />} onClick={openCreate}>
          Add Leave
        </Button>
      </div>

      <TabList selectedValue={windowDays} onTabSelect={(_, d) => setWindowDays(d.value as number)}>
        <Tab value={30}>Upcoming 30 Days</Tab>
        <Tab value={60}>Upcoming 60 Days</Tab>
        <Tab value={90}>Upcoming 90 Days</Tab>
      </TabList>

      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorText error={error} onRetry={reload} />
      ) : (
        <>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            <KpiCard label="Leave Days" value={data?.totalLeaveDays ?? 0} tone="brand" />
            <KpiCard label="Resources Affected" value={data?.distinctResources ?? 0} />
            <KpiCard label="Window" value={`${windowDays} days`} />
          </div>
          <Panel title={`Upcoming leave${data ? ` (${data.items.length})` : ''}`}>
            <DataTable<LeaveItem>
              ariaLabel="Leave"
              rows={data?.items ?? []}
              rowKey={(l) => l.id}
              defaultSort={{ key: 'date', dir: 'asc' }}
              emptyMessage="No leave scheduled in this window."
              columns={[
                { key: 'resource', header: 'Resource', sortValue: (l) => l.resourceName },
                { key: 'region', header: 'Region', sortValue: (l) => l.region },
                { key: 'date', header: 'Date', sortValue: (l) => l.leaveDate },
                { key: 'type', header: 'Type', sortValue: (l) => l.leaveType },
                {
                  key: 'actions',
                  header: 'Actions',
                  render: (l) => (
                    <div style={{ display: 'flex', gap: 4 }}>
                      <Button size="small" appearance="subtle" icon={<EditRegular />} title="Edit" onClick={() => openEdit(l)} />
                      <Button size="small" appearance="subtle" icon={<DeleteRegular />} title="Delete" onClick={() => setDeleteTarget(l)} />
                    </div>
                  ),
                },
              ]}
            />
          </Panel>
        </>
      )}

      <Modal
        open={!!form}
        title={editId ? 'Edit Leave' : 'Add Leave'}
        onClose={() => setForm(null)}
        onSubmit={save}
        submitDisabled={!valid}
        busy={busy}
      >
        {form && (
          <>
            <Dropdown
              placeholder="Resource *"
              value={resourceName(form.resourceId)}
              selectedOptions={form.resourceId ? [String(form.resourceId)] : []}
              onOptionSelect={(_, d) => patch({ resourceId: Number(d.optionValue) })}
            >
              {resources?.map((r) => (
                <Option key={r.resourceId} value={String(r.resourceId)} text={`${r.name} (${r.region})`}>
                  {r.name} ({r.region})
                </Option>
              ))}
            </Dropdown>
            <div style={{ display: 'flex', gap: 8 }}>
              <label style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 4 }}>
                <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                  {editId ? 'Date' : 'From'}
                </Text>
                <Input type="date" value={form.leaveDate} max={form.endDate} onChange={(_, d) => patch({ leaveDate: d.value })} />
              </label>
              {!editId && (
                <label style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 4 }}>
                  <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                    To
                  </Text>
                  <Input type="date" value={form.endDate ?? form.leaveDate} min={form.leaveDate} onChange={(_, d) => patch({ endDate: d.value })} />
                </label>
              )}
            </div>
            <Dropdown
              placeholder="Leave type"
              value={form.leaveType}
              selectedOptions={[form.leaveType]}
              onOptionSelect={(_, d) => patch({ leaveType: d.optionValue ?? 'Leave' })}
            >
              {LEAVE_TYPES.map((t) => (
                <Option key={t} value={t}>
                  {t}
                </Option>
              ))}
            </Dropdown>
          </>
        )}
      </Modal>

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete leave"
        message={`Delete ${deleteTarget?.leaveType} for ${deleteTarget?.resourceName} on ${deleteTarget?.leaveDate}?`}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={confirmDelete}
        busy={busy}
      />
    </div>
  )
}
