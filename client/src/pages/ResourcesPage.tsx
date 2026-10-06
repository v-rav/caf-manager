import {
  Badge,
  Button,
  Dropdown,
  Input,
  Option,
  SearchBox,
  Switch,
  Text,
} from '@fluentui/react-components'
import { AddRegular, ArrowDownloadRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import { useMemo, useState } from 'react'
import { api } from '../api'
import { ConfirmDialog, Modal } from '../components/Modal'
import { DataTable } from '../components/DataTable'
import { ErrorText, FilterSelect, Loading, MultiFilterSelect, Panel } from '../components/common'
import { useAsync, useDebounced } from '../hooks'
import { useRegion } from '../region'
import { useSearchParams } from 'react-router-dom'
import type { Resource, ResourceUpsert } from '../types'

const CAPACITY_STATUSES = ['Available', 'Partially Utilized', 'Fully Utilized', 'Overloaded']

const emptyForm: ResourceUpsert = {
  name: '',
  psid: '',
  email: '',
  mobile: '',
  aliases: '',
  region: '',
  role: '',
  primarySkill: '',
  skills: '',
  experienceYears: 0,
  status: 'Active',
  separated: false,
  capacityLimit: 5,
  activeFlag: true,
  onboardingStatus: 'Active',
}

export function ResourcesPage() {
  const { region } = useRegion()
  const [search, setSearch] = useState('')
  const [roleSel, setRoleSel] = useState<string[]>([])
  const [skill, setSkill] = useState('')
  const debouncedSearch = useDebounced(search)
  const debouncedSkill = useDebounced(skill)
  // Role is filtered client-side (multi-select); search/skill/region stay server-side.
  const { data, loading, error, reload } = useAsync(
    () => api.resources({ search: debouncedSearch || undefined, region, skill: debouncedSkill || undefined }),
    [debouncedSearch, region, debouncedSkill],
  )
  const { data: regions } = useAsync(() => api.regions(), [])
  const { data: roles } = useAsync(() => api.roles(), [])
  const [searchParams] = useSearchParams()
  const [capSel, setCapSel] = useState<string[]>(() => {
    const q = searchParams.get('capacity')
    return q ? [q] : []
  })
  const [activeFilter, setActiveFilter] = useState('Active')
  const rows = useMemo(
    () =>
      (data ?? []).filter(
        (r) =>
          (roleSel.length === 0 || roleSel.includes(r.role)) &&
          (capSel.length === 0 || capSel.includes(r.capacityStatus)) &&
          (!activeFilter || (activeFilter === 'Active' ? r.activeFlag : !r.activeFlag)),
      ),
    [data, roleSel, capSel, activeFilter],
  )

  const [form, setForm] = useState<ResourceUpsert | null>(null)
  const [editId, setEditId] = useState<number | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<Resource | null>(null)
  const [busy, setBusy] = useState(false)

  const openCreate = () => {
    setEditId(null)
    setForm({ ...emptyForm, region: region ?? '' })
  }
  const openEdit = (r: Resource) => {
    setEditId(r.resourceId)
    setForm({
      name: r.name,
      psid: r.psid ?? '',
      email: r.email ?? '',
      mobile: r.mobile ?? '',
      aliases: r.aliases ?? '',
      region: r.region,
      role: r.role,
      primarySkill: r.primarySkill ?? '',
      skills: r.skills ?? '',
      experienceYears: r.experienceYears,
      status: r.status ?? 'Active',
      separated: r.separated,
      capacityLimit: r.capacityLimit,
      activeFlag: r.activeFlag,
      onboardingStatus: r.onboardingStatus,
    })
  }

  const patch = (p: Partial<ResourceUpsert>) => setForm((f) => (f ? { ...f, ...p } : f))

  const save = async () => {
    if (!form) return
    setBusy(true)
    try {
      if (editId) await api.updateResource(editId, form)
      else await api.createResource(form)
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
      await api.deleteResource(deleteTarget.resourceId)
      setDeleteTarget(null)
      reload()
    } finally {
      setBusy(false)
    }
  }

  const valid = !!form && !!form.name.trim() && !!form.region.trim() && !!form.role.trim()

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <Text size={600} weight="bold">
        Resource Hub
      </Text>
      <Panel
        title={`Resources${rows ? ` (${rows.length})` : ''}`}
        action={
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            <SearchBox placeholder="Search name / PSID" value={search} onChange={(_, d) => setSearch(d.value)} style={{ minWidth: 180 }} />
            <MultiFilterSelect label="Roles" values={roleSel} options={roles ?? []} onChange={setRoleSel} minWidth={170} />
            <MultiFilterSelect label="Capacity" values={capSel} options={CAPACITY_STATUSES} onChange={setCapSel} minWidth={190} />
            <FilterSelect label="Active" value={activeFilter} options={['Active', 'Inactive']} onChange={setActiveFilter} minWidth={130} />
            <SearchBox placeholder="Skill" value={skill} onChange={(_, d) => setSkill(d.value)} style={{ minWidth: 140 }} />
            <Button as="a" href={api.exportUrl('resources', region)} appearance="secondary" icon={<ArrowDownloadRegular />}>
              Export
            </Button>
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
          <DataTable<Resource>
            ariaLabel="Resources"
            rows={rows ?? []}
            rowKey={(r) => r.resourceId}
            defaultSort={{ key: 'utilization', dir: 'desc' }}
            emptyMessage="No resources match your filters."
            columns={[
              { key: 'name', header: 'Name', sortValue: (r) => r.name },
              { key: 'email', header: 'Email', sortValue: (r) => r.email ?? '', render: (r) => r.email ?? '—' },
              { key: 'mobile', header: 'Mobile', sortValue: (r) => r.mobile ?? '', render: (r) => r.mobile ?? '—' },
              { key: 'region', header: 'Region', sortValue: (r) => r.region },
              { key: 'role', header: 'Role', sortValue: (r) => r.role },
              { key: 'grade', header: 'Grade', sortValue: (r) => r.grade ?? '', render: (r) => r.grade ?? '—' },
              { key: 'skill', header: 'Primary Skill', sortValue: (r) => r.primarySkill ?? '', render: (r) => r.primarySkill ?? '—' },
              {
                key: 'active',
                header: 'Active',
                align: 'center',
                sortValue: (r) => (r.activeFlag ? 1 : 0),
                render: (r) => (
                  <Badge appearance="tint" color={r.activeFlag ? 'success' : 'danger'} size="small">
                    {r.activeFlag ? 'Active' : 'Inactive'}
                  </Badge>
                ),
              },
              {
                key: 'actions',
                header: 'Actions',
                render: (r) => (
                  <div style={{ display: 'flex', gap: 4 }}>
                    <Button size="small" appearance="subtle" icon={<EditRegular />} title="Edit" onClick={() => openEdit(r)} />
                    <Button size="small" appearance="subtle" icon={<DeleteRegular />} title="Delete" onClick={() => setDeleteTarget(r)} />
                  </div>
                ),
              },
            ]}
          />
        )}
      </Panel>

      <Modal
        open={!!form}
        title={editId ? 'Edit Resource' : 'Add Resource'}
        onClose={() => setForm(null)}
        onSubmit={save}
        submitDisabled={!valid}
        busy={busy}
      >
        {form && (
          <>
            <Input placeholder="Name *" value={form.name} onChange={(_, d) => patch({ name: d.value })} />
            <div style={{ display: 'flex', gap: 8 }}>
              <Input style={{ flex: 1 }} placeholder="PSID" value={form.psid} onChange={(_, d) => patch({ psid: d.value })} />
              <Input style={{ flex: 1 }} placeholder="Email" value={form.email} onChange={(_, d) => patch({ email: d.value })} />
              <Input style={{ flex: 1 }} placeholder="Mobile" value={form.mobile} onChange={(_, d) => patch({ mobile: d.value })} />
            </div>
            <Input placeholder="Aliases (; separated name variants)" value={form.aliases} onChange={(_, d) => patch({ aliases: d.value })} />
            <div style={{ display: 'flex', gap: 8 }}>
              <Dropdown style={{ flex: 1 }} placeholder="Region *" value={form.region} selectedOptions={[form.region]} onOptionSelect={(_, d) => patch({ region: d.optionValue ?? '' })}>
                {regions?.map((r) => (
                  <Option key={r.code} value={r.code}>
                    {r.code}
                  </Option>
                ))}
              </Dropdown>
              <Dropdown style={{ flex: 1 }} placeholder="Role *" value={form.role} selectedOptions={[form.role]} onOptionSelect={(_, d) => patch({ role: d.optionValue ?? '' })}>
                {roles?.map((r) => (
                  <Option key={r} value={r}>
                    {r}
                  </Option>
                ))}
              </Dropdown>
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <Input style={{ flex: 1 }} placeholder="Primary Skill" value={form.primarySkill} onChange={(_, d) => patch({ primarySkill: d.value })} />
              <Input style={{ flex: 1 }} placeholder="Skills (; separated)" value={form.skills} onChange={(_, d) => patch({ skills: d.value })} />
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <Input style={{ flex: 1 }} type="number" placeholder="Experience (yrs)" value={String(form.experienceYears)} onChange={(_, d) => patch({ experienceYears: Number(d.value) || 0 })} />
              <Input style={{ flex: 1 }} type="number" placeholder="Capacity Limit" value={String(form.capacityLimit)} onChange={(_, d) => patch({ capacityLimit: Number(d.value) || 5 })} />
            </div>
            <div style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
              <Switch label="Separated" checked={form.separated} onChange={(_, d) => patch({ separated: d.checked, activeFlag: d.checked ? false : form.activeFlag })} />
              <Switch label="Active" checked={form.activeFlag} disabled={form.separated} onChange={(_, d) => patch({ activeFlag: d.checked })} />
              <Dropdown
                style={{ flex: 1, minWidth: 180 }}
                placeholder="Onboarding"
                value={form.onboardingStatus ?? 'Active'}
                selectedOptions={[form.onboardingStatus ?? 'Active']}
                onOptionSelect={(_, d) => patch({ onboardingStatus: d.optionValue ?? 'Active' })}
              >
                {['Not Started', 'Access Requested', 'Access Granted', 'Trained', 'Active'].map((o) => (
                  <Option key={o} value={o}>
                    {o}
                  </Option>
                ))}
              </Dropdown>
            </div>
          </>
        )}
      </Modal>

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete resource"
        message={`Delete ${deleteTarget?.name}? This removes their account mappings and leave records.`}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={confirmDelete}
        busy={busy}
      />
    </div>
  )
}
