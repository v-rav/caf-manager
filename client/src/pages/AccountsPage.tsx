import {
  Button,
  Dropdown,
  Input,
  Option,
  SearchBox,
  Switch,
  Text,
} from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import { useState } from 'react'
import { api } from '../api'
import { ConfirmDialog, Modal } from '../components/Modal'
import { DataTable } from '../components/DataTable'
import { ErrorText, Loading, Panel, StatusBadge } from '../components/common'
import { useAsync, useDebounced } from '../hooks'
import { useRegion } from '../region'
import type { Account, AccountUpsert } from '../types'

const priorityLabel = (w: number) => (w >= 3 ? 'Executive Critical' : w === 2 ? 'Strategic' : 'Standard')

const emptyForm: AccountUpsert = { accountName: '', region: '', status: 'Active', strategicFlag: false, priorityWeight: 1 }

export function AccountsPage() {
  const { region } = useRegion()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebounced(search)
  const { data, loading, error, reload } = useAsync(() => api.accounts(debouncedSearch || undefined, region), [debouncedSearch, region])
  const { data: regions } = useAsync(() => api.regions(), [])

  const [form, setForm] = useState<AccountUpsert | null>(null)
  const [editId, setEditId] = useState<number | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<Account | null>(null)
  const [busy, setBusy] = useState(false)

  const openCreate = () => {
    setEditId(null)
    setForm({ ...emptyForm, region: region ?? '' })
  }
  const openEdit = (a: Account) => {
    setEditId(a.accountId)
    setForm({
      accountName: a.accountName,
      region: a.region,
      status: a.status ?? 'Active',
      strategicFlag: a.strategicFlag,
      priorityWeight: a.priorityWeight,
    })
  }

  const patch = (p: Partial<AccountUpsert>) => setForm((f) => (f ? { ...f, ...p } : f))

  const save = async () => {
    if (!form) return
    setBusy(true)
    try {
      if (editId) await api.updateAccount(editId, form)
      else await api.createAccount(form)
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
      await api.deleteAccount(deleteTarget.accountId)
      setDeleteTarget(null)
      reload()
    } finally {
      setBusy(false)
    }
  }

  const valid = !!form && !!form.accountName.trim() && !!form.region.trim()

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <Text size={600} weight="bold">
        Account Hub
      </Text>
      <Panel
        title={`Accounts${data ? ` (${data.length})` : ''}`}
        action={
          <div style={{ display: 'flex', gap: 8 }}>
            <SearchBox placeholder="Search account" value={search} onChange={(_, d) => setSearch(d.value)} style={{ minWidth: 200 }} />
            <Button appearance="primary" icon={<AddRegular />} onClick={openCreate}>
              Add
            </Button>
          </div>
        }
      >
        {loading ? (
          <Loading />
        ) : error ? (
          <ErrorText error={error} onRetry={reload} />
        ) : (
          <DataTable<Account>
            ariaLabel="Accounts"
            rows={data ?? []}
            rowKey={(a) => a.accountId}
            defaultSort={{ key: 'account', dir: 'asc' }}
            emptyMessage="No accounts match your search."
            columns={[
              { key: 'account', header: 'Account', sortValue: (a) => a.accountName },
              { key: 'region', header: 'Region', sortValue: (a) => a.region },
              { key: 'resources', header: 'Resources', align: 'center', sortValue: (a) => a.resourceCount },
              { key: 'priority', header: 'Priority', sortValue: (a) => a.priorityWeight, render: (a) => priorityLabel(a.priorityWeight) },
              { key: 'strategic', header: 'Strategic', align: 'center', sortValue: (a) => (a.strategicFlag ? 1 : 0), render: (a) => (a.strategicFlag ? <StatusBadge status="Green" /> : '—') },
              { key: 'status', header: 'Status', sortValue: (a) => a.status ?? '', render: (a) => a.status ?? '—' },
              {
                key: 'actions',
                header: 'Actions',
                render: (a) => (
                  <div style={{ display: 'flex', gap: 4 }}>
                    <Button size="small" appearance="subtle" icon={<EditRegular />} title="Edit" onClick={() => openEdit(a)} />
                    <Button size="small" appearance="subtle" icon={<DeleteRegular />} title="Delete" onClick={() => setDeleteTarget(a)} />
                  </div>
                ),
              },
            ]}
          />
        )}
      </Panel>

      <Modal
        open={!!form}
        title={editId ? 'Edit Account' : 'Add Account'}
        onClose={() => setForm(null)}
        onSubmit={save}
        submitDisabled={!valid}
        busy={busy}
      >
        {form && (
          <>
            <Input placeholder="Account name *" value={form.accountName} onChange={(_, d) => patch({ accountName: d.value })} />
            <div style={{ display: 'flex', gap: 8 }}>
              <Dropdown style={{ flex: 1 }} placeholder="Region *" value={form.region} selectedOptions={[form.region]} onOptionSelect={(_, d) => patch({ region: d.optionValue ?? '' })}>
                {regions?.map((r) => (
                  <Option key={r.code} value={r.code}>
                    {r.code}
                  </Option>
                ))}
              </Dropdown>
              <Input style={{ flex: 1 }} placeholder="Status" value={form.status} onChange={(_, d) => patch({ status: d.value })} />
            </div>
            <div style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
              <Dropdown
                style={{ flex: 1 }}
                placeholder="Priority"
                value={form.priorityWeight === 3 ? 'Executive Critical' : form.priorityWeight === 2 ? 'Strategic' : 'Standard'}
                selectedOptions={[String(form.priorityWeight)]}
                onOptionSelect={(_, d) => patch({ priorityWeight: Number(d.optionValue) })}
              >
                <Option value="1">Standard</Option>
                <Option value="2">Strategic</Option>
                <Option value="3">Executive Critical</Option>
              </Dropdown>
              <Switch label="Strategic" checked={form.strategicFlag} onChange={(_, d) => patch({ strategicFlag: d.checked })} />
            </div>
          </>
        )}
      </Modal>

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete account"
        message={`Delete ${deleteTarget?.accountName}? Resource mappings to this account will be removed.`}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={confirmDelete}
        busy={busy}
      />
    </div>
  )
}
