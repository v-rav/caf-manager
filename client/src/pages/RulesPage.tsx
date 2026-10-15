import { Badge, Button, Dropdown, Input, Option, SpinButton, Switch, Tab, TabList, Text, Tooltip } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular, EyeRegular } from '@fluentui/react-icons'
import { useEffect, useMemo, useState } from 'react'
import { api } from '../api'
import { DataTable } from '../components/DataTable'
import { Modal } from '../components/Modal'
import { ErrorText, Loading, PageHeader, Panel } from '../components/common'
import { useAsync } from '../hooks'
import type { KeywordRule, KeywordRuleInput, KeywordRulePreview, RuleDomainInfo } from '../types'

type Editing = { mode: 'create' | 'update' | 'delete'; id?: number; form: KeywordRuleInput }

function errorText(e: unknown): string {
  const d = (e as { response?: { data?: unknown } })?.response?.data
  return typeof d === 'string' ? d : 'Request failed.'
}

// Admin page for the data-driven classification rules (stage, health, wave type, tool alias, activity, container path).
export function RulesPage() {
  const { data, loading, error, reload } = useAsync(() => api.keywordRules(), [])
  const [domainKey, setDomainKey] = useState<string>('')
  const [editing, setEditing] = useState<Editing | null>(null)
  const [preview, setPreview] = useState<KeywordRulePreview | null>(null)
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)

  useEffect(() => {
    if (data && !domainKey) setDomainKey(data.domains[0]?.key ?? '')
  }, [data, domainKey])

  const domain: RuleDomainInfo | undefined = data?.domains.find((d) => d.key === domainKey)
  const rules = useMemo(() => (data?.rules ?? []).filter((r) => r.domain === domainKey), [data, domainKey])

  const open = (mode: Editing['mode'], r?: KeywordRule) => {
    setPreview(null)
    setMsg(null)
    const nextPriority = (rules.reduce((m, x) => Math.max(m, x.priority), 0) || 0) + 10
    setEditing({
      mode,
      id: r?.id,
      form: r
        ? { domain: r.domain, pattern: r.pattern, matchType: r.matchType, target: r.target, priority: r.priority, active: r.active }
        : { domain: domainKey, pattern: '', matchType: domainKey === 'ToolAlias' ? 'Exact' : 'Contains', target: domain?.targets?.[0] ?? '', priority: nextPriority, active: true },
    })
  }

  const runPreview = async () => {
    if (!editing) return
    setBusy(true)
    setMsg(null)
    try {
      setPreview(await api.previewKeywordRule({ op: editing.mode, id: editing.id, rule: editing.mode === 'delete' ? undefined : editing.form }))
    } catch (e) {
      setMsg(errorText(e))
    } finally {
      setBusy(false)
    }
  }

  const save = async () => {
    if (!editing) return
    setBusy(true)
    setMsg(null)
    try {
      if (editing.mode === 'create') await api.createKeywordRule(editing.form)
      else if (editing.mode === 'update') await api.updateKeywordRule(editing.id!, editing.form)
      else await api.deleteKeywordRule(editing.id!)
      setEditing(null)
      reload()
    } catch (e) {
      setMsg(errorText(e))
    } finally {
      setBusy(false)
    }
  }

  const setForm = (patch: Partial<KeywordRuleInput>) => {
    setPreview(null)
    setEditing((e) => (e ? { ...e, form: { ...e.form, ...patch } } : e))
  }

  if (loading && !data) return <Loading label="Loading rules…" />
  if (error) return <ErrorText error={error} onRetry={reload} />
  if (!data) return null

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <PageHeader
        title="Classification Rules"
        subtitle="Keyword rules that turn raw FDO text into portal classifications. Edits take effect immediately (wave type on the next Detail View import). Preview shows exactly which records would change before you save."
      />

      <TabList selectedValue={domainKey} onTabSelect={(_, d) => setDomainKey(String(d.value))}>
        {data.domains.map((d) => (
          <Tab key={d.key} value={d.key}>
            {d.key} <Badge appearance="tint" size="small" style={{ marginLeft: 6 }}>{data.rules.filter((r) => r.domain === d.key).length}</Badge>
          </Tab>
        ))}
      </TabList>

      {domain && (
        <Panel
          title={domain.label}
          action={<Button appearance="primary" icon={<AddRegular />} onClick={() => open('create')}>Add rule</Button>}
        >
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)', display: 'block', marginBottom: 10 }}>
            Source: <b>{domain.source}</b> · {domain.allMatches ? 'every matching rule applies' : 'first match by priority wins'}
            {domain.fallback ? ` · no match → ${domain.fallback}` : ''}. {domain.description}
          </Text>
          <DataTable<KeywordRule>
            ariaLabel={`${domain.key} rules`}
            rows={rules}
            rowKey={(r) => r.id}
            defaultSort={{ key: 'priority', dir: 'asc' }}
            emptyMessage="No rules — nothing in this domain will classify."
            columns={[
              { key: 'priority', header: 'Priority', align: 'center', sortValue: (r) => r.priority, render: (r) => r.priority },
              { key: 'pattern', header: 'Pattern', sortValue: (r) => r.pattern, render: (r) => <code>{r.pattern}</code> },
              { key: 'match', header: 'Match', sortValue: (r) => r.matchType, render: (r) => <Badge appearance="outline" size="small">{r.matchType}</Badge> },
              { key: 'target', header: 'Classifies as', sortValue: (r) => r.target, render: (r) => <Badge appearance="tint" color="brand">{r.target}</Badge> },
              { key: 'active', header: 'Active', align: 'center', sortValue: (r) => (r.active ? 1 : 0), render: (r) => (r.active ? <Badge color="success" appearance="tint" size="small">On</Badge> : <Badge appearance="tint" size="small">Off</Badge>) },
              { key: 'by', header: 'Updated by', sortValue: (r) => r.updatedBy ?? '', render: (r) => r.updatedBy ?? '—' },
              {
                key: 'actions',
                header: '',
                align: 'center',
                render: (r) => (
                  <div style={{ display: 'flex', gap: 4, justifyContent: 'center' }}>
                    <Tooltip content="Edit" relationship="label"><Button size="small" appearance="subtle" icon={<EditRegular />} onClick={() => open('update', r)} /></Tooltip>
                    <Tooltip content="Delete" relationship="label"><Button size="small" appearance="subtle" icon={<DeleteRegular />} onClick={() => open('delete', r)} /></Tooltip>
                  </div>
                ),
              },
            ]}
          />
        </Panel>
      )}

      <Modal
        open={!!editing}
        title={editing ? `${editing.mode === 'create' ? 'Add' : editing.mode === 'update' ? 'Edit' : 'Delete'} ${domainKey} rule` : ''}
        onClose={() => setEditing(null)}
        onSubmit={save}
        submitLabel={editing?.mode === 'delete' ? 'Delete' : 'Save'}
        busy={busy}
        submitDisabled={!editing || (editing.mode !== 'delete' && (!editing.form.pattern.trim() || !editing.form.target.trim()))}
        maxWidth={720}
      >
        {editing && editing.mode !== 'delete' && (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              <Text size={200}>Pattern</Text>
              <Input value={editing.form.pattern} onChange={(_, d) => setForm({ pattern: d.value })} placeholder="text to match" />
            </label>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              <Text size={200}>Match type</Text>
              <Dropdown value={editing.form.matchType} selectedOptions={[editing.form.matchType]}
                onOptionSelect={(_, d) => setForm({ matchType: (d.optionValue as 'Contains' | 'Exact') ?? 'Contains' })}>
                <Option value="Contains">Contains</Option>
                <Option value="Exact">Exact</Option>
              </Dropdown>
            </label>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              <Text size={200}>Classifies as</Text>
              {domain?.targets ? (
                <Dropdown value={editing.form.target} selectedOptions={[editing.form.target]}
                  onOptionSelect={(_, d) => setForm({ target: d.optionValue ?? '' })}>
                  {domain.targets.map((t) => <Option key={t} value={t}>{t}</Option>)}
                </Dropdown>
              ) : (
                <Input value={editing.form.target} onChange={(_, d) => setForm({ target: d.value })} />
              )}
            </label>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              <Text size={200}>Priority (lower runs first)</Text>
              <SpinButton min={0} max={100000} step={10} value={editing.form.priority}
                onChange={(_, d) => setForm({ priority: Math.round(d.value ?? Number(d.displayValue) ?? editing.form.priority) })} />
            </label>
            <Switch checked={editing.form.active ?? true} onChange={(_, d) => setForm({ active: d.checked })} label="Active" />
          </div>
        )}
        {editing?.mode === 'delete' && (
          <Text>
            Delete rule <code>{editing.form.pattern}</code> → <b>{editing.form.target}</b>? Preview the impact first.
          </Text>
        )}
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <Button icon={<EyeRegular />} onClick={runPreview} disabled={busy}>Preview impact</Button>
          {preview && (
            <Text size={200}>
              <b>{preview.changedRecords}</b> record(s) across <b>{preview.changedValues}</b> distinct value(s) would change
              (of {preview.totalRecords} records).
            </Text>
          )}
        </div>
        {msg && <Text style={{ color: 'var(--colorPaletteRedForeground1)' }}>{msg}</Text>}
        {preview && preview.samples.length > 0 && (
          <div style={{ maxHeight: 260, overflow: 'auto', border: '1px solid var(--colorNeutralStroke2)', borderRadius: 6 }}>
            <table style={{ width: '100%', fontSize: 12, borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ textAlign: 'left', background: 'var(--colorNeutralBackground3)' }}>
                  <th style={{ padding: 6 }}>Source text</th><th style={{ padding: 6 }}>Records</th>
                  <th style={{ padding: 6 }}>Now</th><th style={{ padding: 6 }}>After</th>
                </tr>
              </thead>
              <tbody>
                {preview.samples.map((s) => (
                  <tr key={s.text} style={{ borderTop: '1px solid var(--colorNeutralStroke2)' }}>
                    <td style={{ padding: 6, maxWidth: 320, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={s.text}>{s.text}</td>
                    <td style={{ padding: 6 }}>{s.count}</td>
                    <td style={{ padding: 6 }}>{s.from}</td>
                    <td style={{ padding: 6, fontWeight: 600 }}>{s.to}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Modal>
    </div>
  )
}
