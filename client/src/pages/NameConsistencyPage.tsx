import { Badge, Button, Checkbox, Dropdown, Option, Tab, TabList, Text, Tooltip } from '@fluentui/react-components'
import { ArrowClockwiseRegular, CheckmarkRegular } from '@fluentui/react-icons'
import { useState } from 'react'
import { api } from '../api'
import { KpiCard } from '../components/KpiCard'
import { ErrorText, FilterSelect, Loading, Panel } from '../components/common'
import { useAsync } from '../hooks'
import type { NameSuggestion, PerformanceNameSuggestion, OwnershipSuggestion } from '../types'

const THRESHOLDS: Record<string, number> = {
  'Strict (0.95)': 0.95,
  'Balanced (0.88)': 0.88,
  'Loose (0.82)': 0.82,
}

function scoreColor(s: number): 'success' | 'brand' | 'warning' {
  return s >= 0.95 ? 'success' : s >= 0.88 ? 'brand' : 'warning'
}

export function NameConsistencyPage() {
  const [tab, setTab] = useState<'accounts' | 'resources' | 'performance' | 'ownership'>('accounts')
  const [strictness, setStrictness] = useState('Balanced (0.88)')
  const t = THRESHOLDS[strictness] ?? 0.88
  const { data, loading, error, reload } = useAsync<any>(
    () =>
      tab === 'accounts'
        ? api.nameConsistencyAccounts(t)
        : tab === 'resources'
          ? api.nameConsistencyResources(t)
          : tab === 'performance'
            ? api.nameConsistencyPerformance(t)
            : api.nameConsistencyOwnership(t),
    [tab, strictness],
  )

  // Per-group selected variant ids (default all selected once a group renders).
  const [selected, setSelected] = useState<Record<number, Set<number>>>({})
  const [done, setDone] = useState<Record<number, string>>({})
  const [busy, setBusy] = useState<number | null>(null)

  // Performance tab apply state (keyed by the current free-text name).
  const [perfDone, setPerfDone] = useState<Record<string, string>>({})
  const [perfBusy, setPerfBusy] = useState<string | null>(null)

  const applyPerf = async (s: PerformanceNameSuggestion) => {
    setPerfBusy(s.currentName)
    try {
      const res = await api.applyPerformanceName({ currentName: s.currentName, rosterName: s.rosterName })
      setPerfDone((d) => ({ ...d, [s.currentName]: res.message }))
    } finally {
      setPerfBusy(null)
    }
  }

  // Ownership tab: per owner-name chosen candidate + apply state.
  const [ownPick, setOwnPick] = useState<Record<string, number>>({})
  const [ownDone, setOwnDone] = useState<Record<string, string>>({})
  const [ownBusy, setOwnBusy] = useState<string | null>(null)
  const pickOf = (s: OwnershipSuggestion) => ownPick[s.ownerName] ?? s.candidates[0]?.resourceId ?? 0
  const addOwnAlias = async (s: OwnershipSuggestion) => {
    const rid = pickOf(s)
    if (!rid) return
    setOwnBusy(s.ownerName)
    try {
      const res = await api.applyResourceAliases({ resourceId: rid, aliases: [s.ownerName] })
      setOwnDone((d) => ({ ...d, [s.ownerName]: res.message }))
    } finally {
      setOwnBusy(null)
    }
  }
  const fixRosterName = async (s: OwnershipSuggestion) => {
    const rid = pickOf(s)
    if (!rid) return
    setOwnBusy(s.ownerName)
    try {
      const res = await api.renameResource({ resourceId: rid, newName: s.ownerName })
      setOwnDone((d) => ({ ...d, [s.ownerName]: res.message }))
    } finally {
      setOwnBusy(null)
    }
  }

  const sel = (g: NameSuggestion): Set<number> =>
    selected[g.canonicalId] ?? new Set(g.variants.map((v) => v.id))

  const toggle = (g: NameSuggestion, id: number) => {
    const next = new Set(sel(g))
    next.has(id) ? next.delete(id) : next.add(id)
    setSelected((s) => ({ ...s, [g.canonicalId]: next }))
  }

  const apply = async (g: NameSuggestion) => {
    const ids = sel(g)
    const names = g.variants.filter((v) => ids.has(v.id)).map((v) => v.name)
    if (names.length === 0) return
    setBusy(g.canonicalId)
    try {
      const res =
        tab === 'accounts'
          ? await api.applyAccountAliases({ canonicalName: g.canonicalName, aliases: names })
          : await api.applyResourceAliases({ resourceId: g.canonicalId, aliases: names })
      setDone((d) => ({ ...d, [g.canonicalId]: res.message }))
    } finally {
      setBusy(null)
    }
  }

  return (
    <div style={{ display: 'grid', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 12 }}>
        <div>
          <Text size={600} weight="semibold" block>
            Name Consistency
          </Text>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
            Deterministic fuzzy matching surfaces likely-same names (diacritics, suffixes, word order, typos,
            abbreviations). Review and register variants as aliases — nothing is applied automatically.
          </Text>
        </div>
        <div style={{ display: 'flex', alignItems: 'flex-end', gap: 12 }}>
          <FilterSelect
            label="Match strictness"
            value={strictness}
            options={Object.keys(THRESHOLDS)}
            onChange={setStrictness}
            minWidth={170}
          />
          <Button icon={<ArrowClockwiseRegular />} onClick={reload} appearance="secondary">
            Refresh
          </Button>
        </div>
      </div>

      <TabList selectedValue={tab} onTabSelect={(_, d) => setTab(d.value as 'accounts' | 'resources' | 'performance' | 'ownership')}>
        <Tab value="accounts">Accounts</Tab>
        <Tab value="resources">Resources</Tab>
        <Tab value="performance">Performance</Tab>
        <Tab value="ownership">Ownership</Tab>
      </TabList>

      {loading && <Loading />}
      {error && <ErrorText error={error} />}

      {data && !loading && tab !== 'performance' && tab !== 'ownership' && data.groups && (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 12 }}>
            <KpiCard label="Suggested groups" value={data.groupCount} tone="brand" />
            <KpiCard label="Variant names" value={data.variantCount} tone="warning" />
            <KpiCard label="Match strictness" value={data.threshold.toFixed(2)} tone="neutral" />
          </div>

          {data.groups.length === 0 && (
            <Panel title="Name consistency">
              <Text>No inconsistent {tab} names found at this strictness. Try a looser setting.</Text>
            </Panel>
          )}

          {data.groups.map((g: NameSuggestion) => {
            const ids = sel(g)
            const message = done[g.canonicalId]
            return (
              <Panel
                key={g.canonicalId}
                title={g.canonicalName}
                action={
                  message ? (
                    <Badge appearance="tint" color="success" icon={<CheckmarkRegular />}>
                      {message}
                    </Badge>
                  ) : (
                    <Button
                      appearance="primary"
                      size="small"
                      disabled={busy === g.canonicalId || ids.size === 0}
                      onClick={() => apply(g)}
                    >
                      Register {ids.size} alias{ids.size === 1 ? '' : 'es'}
                    </Button>
                  )
                }
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                  <Badge appearance="filled" color="brand" size="small">
                    Canonical
                  </Badge>
                  {g.canonicalTpid && (
                    <Badge appearance="tint" color="informative" size="small">
                      TPID {g.canonicalTpid}
                    </Badge>
                  )}
                </div>

                <div style={{ marginTop: 10, display: 'grid', gap: 6 }}>
                  {g.variants.map((v) => (
                    <div
                      key={v.id}
                      style={{ display: 'flex', alignItems: 'center', gap: 10, opacity: message ? 0.6 : 1 }}
                    >
                      <Checkbox
                        checked={ids.has(v.id)}
                        disabled={!!message}
                        onChange={() => toggle(g, v.id)}
                        label={v.name}
                      />
                      <Tooltip content="Similarity to the canonical name" relationship="label">
                        <Badge appearance="tint" color={scoreColor(v.score)} size="small">
                          {Math.round(v.score * 100)}%
                        </Badge>
                      </Tooltip>
                      {v.tpid && (
                        <Badge appearance="outline" size="small">
                          TPID {v.tpid}
                        </Badge>
                      )}
                      {v.linkedCount > 0 && (
                        <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                          {v.linkedCount} link{v.linkedCount === 1 ? '' : 's'}
                        </Text>
                      )}
                    </div>
                  ))}
                </div>
              </Panel>
            )
          })}
        </>
      )}

      {data && !loading && tab === 'performance' && data.suggestions && data.matchedCount === undefined && (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 12 }}>
            <KpiCard label="Suggested corrections" value={data.count} tone="brand" />
            <KpiCard label="Match strictness" value={data.threshold.toFixed(2)} tone="neutral" />
          </div>

          {data.suggestions.length === 0 && (
            <Panel title="Performance names">
              <Text>Every performance-review name matches the roster at this strictness.</Text>
            </Panel>
          )}

          {data.suggestions.length > 0 && (
            <Panel title="Review names vs roster">
              <div style={{ display: 'grid', gap: 10 }}>
                {data.suggestions.map((s: PerformanceNameSuggestion) => {
                  const msg = perfDone[s.currentName]
                  return (
                    <div
                      key={s.currentName}
                      style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap', opacity: msg ? 0.6 : 1 }}
                    >
                      <Text weight="semibold" style={{ minWidth: 150 }}>
                        {s.currentName}
                      </Text>
                      <Text style={{ color: 'var(--colorNeutralForeground3)' }}>→</Text>
                      <Text weight="semibold">{s.rosterName}</Text>
                      {s.psid && (
                        <Badge appearance="outline" size="small">
                          PSID {s.psid}
                        </Badge>
                      )}
                      <Tooltip content="Similarity to the roster name" relationship="label">
                        <Badge appearance="tint" color={scoreColor(s.score)} size="small">
                          {Math.round(s.score * 100)}%
                        </Badge>
                      </Tooltip>
                      <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                        {s.reviewCount} review{s.reviewCount === 1 ? '' : 's'}
                      </Text>
                      {msg ? (
                        <Badge appearance="tint" color="success" icon={<CheckmarkRegular />}>
                          {msg}
                        </Badge>
                      ) : (
                        <Button
                          appearance="primary"
                          size="small"
                          disabled={perfBusy === s.currentName}
                          onClick={() => applyPerf(s)}
                        >
                          Rename to roster
                        </Button>
                      )}
                    </div>
                  )
                })}
              </div>
            </Panel>
          )}
        </>
      )}

      {data && !loading && tab === 'ownership' && data.matchedCount !== undefined && (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 12 }}>
            <KpiCard label="Unmatched owners" value={data.count} tone="warning" />
            <KpiCard label="Already matched" value={data.matchedCount} tone="success" />
          </div>

          <Panel title="FDO ownership (SA / CFTL / PM) vs roster">
            <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
              These ownership names don't exactly match a roster person. Verify the candidate by email, then
              either add the FDO name as an alias, or fix the roster name to match. No roster candidate = add
              the person on the Resource Hub.
            </Text>
            <div style={{ marginTop: 10, display: 'grid', gap: 10 }}>
              {data.suggestions.map((s: OwnershipSuggestion) => {
                const msg = ownDone[s.ownerName]
                const chosenId = pickOf(s)
                const chosen = s.candidates.find((c) => c.resourceId === chosenId)
                const noMatch = s.candidates.length === 0
                return (
                  <div
                    key={s.ownerName}
                    style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap', opacity: msg ? 0.6 : 1 }}
                  >
                    <Text weight="semibold" style={{ minWidth: 160 }}>
                      {s.ownerName}
                    </Text>
                    {s.roles.map((r) => (
                      <Badge key={r} appearance="tint" color="brand" size="small">
                        {r}
                      </Badge>
                    ))}
                    <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
                      {s.nominationCount} nom{s.nominationCount === 1 ? '' : 's'}
                    </Text>
                    <Text style={{ color: 'var(--colorNeutralForeground3)' }}>→</Text>
                    {noMatch ? (
                      <Badge appearance="tint" color="danger">
                        No roster match — add on Resource Hub
                      </Badge>
                    ) : (
                      <>
                        <Dropdown
                          size="small"
                          style={{ minWidth: 260 }}
                          value={chosen ? `${chosen.resourceName} · ${Math.round(chosen.score * 100)}%` : ''}
                          selectedOptions={chosenId ? [String(chosenId)] : []}
                          onOptionSelect={(_, d) => setOwnPick((p) => ({ ...p, [s.ownerName]: Number(d.optionValue) }))}
                        >
                          {s.candidates.map((c) => (
                            <Option key={c.resourceId} value={String(c.resourceId)} text={c.resourceName}>
                              {c.resourceName} · {c.email ?? 'no email'} · {Math.round(c.score * 100)}%
                            </Option>
                          ))}
                        </Dropdown>
                        <Tooltip content="Best-candidate similarity" relationship="label">
                          <Badge appearance="tint" color={scoreColor(s.bestScore)} size="small">
                            {Math.round(s.bestScore * 100)}%
                          </Badge>
                        </Tooltip>
                      </>
                    )}
                    {msg ? (
                      <Badge appearance="tint" color="success" icon={<CheckmarkRegular />}>
                        {msg}
                      </Badge>
                    ) : (
                      !noMatch && (
                        <>
                          <Button size="small" appearance="primary" disabled={ownBusy === s.ownerName} onClick={() => addOwnAlias(s)}>
                            Add alias
                          </Button>
                          <Button size="small" appearance="secondary" disabled={ownBusy === s.ownerName} onClick={() => fixRosterName(s)}>
                            Fix roster name
                          </Button>
                        </>
                      )
                    )}
                  </div>
                )
              })}
            </div>
          </Panel>
        </>
      )}
    </div>
  )
}
