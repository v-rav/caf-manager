import {
  Button,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Text,
} from '@fluentui/react-components'
import {
  ChevronLeftRegular,
  ChevronRightRegular,
} from '@fluentui/react-icons'
import { useMemo, useState, type CSSProperties, type ReactNode } from 'react'

export interface Column<T> {
  key: string
  header: ReactNode
  /** Cell content. Falls back to the sort value or an em-dash. */
  render?: (row: T) => ReactNode
  /** Provide to make the column sortable; also used as a fallback cell value. */
  sortValue?: (row: T) => string | number | null | undefined
  align?: 'start' | 'center' | 'end'
  width?: number | string
  minWidth?: number
}

interface DataTableProps<T> {
  columns: Column<T>[]
  rows: T[]
  rowKey: (row: T) => string | number
  ariaLabel: string
  rowStyle?: (row: T) => CSSProperties | undefined
  emptyMessage?: string
  /** 0 disables pagination. */
  pageSize?: number
  /** Scroll region cap so headers stay reachable. */
  maxHeight?: number
  defaultSort?: { key: string; dir: 'asc' | 'desc' }
}

const collator = new Intl.Collator(undefined, { numeric: true, sensitivity: 'base' })

export function DataTable<T>({
  columns,
  rows,
  rowKey,
  ariaLabel,
  rowStyle,
  emptyMessage = 'No records to display.',
  pageSize = 25,
  maxHeight = 560,
  defaultSort,
}: DataTableProps<T>) {
  const [sortKey, setSortKey] = useState(defaultSort?.key)
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>(defaultSort?.dir ?? 'asc')
  const [page, setPage] = useState(0)

  const sorted = useMemo(() => {
    const col = columns.find((c) => c.key === sortKey)
    if (!col?.sortValue) return rows
    const dir = sortDir === 'asc' ? 1 : -1
    return [...rows].sort((a, b) => {
      const av = col.sortValue!(a)
      const bv = col.sortValue!(b)
      if (av == null && bv == null) return 0
      if (av == null) return 1
      if (bv == null) return -1
      if (typeof av === 'number' && typeof bv === 'number') return (av - bv) * dir
      return collator.compare(String(av), String(bv)) * dir
    })
  }, [rows, columns, sortKey, sortDir])

  const pageCount = pageSize > 0 ? Math.max(1, Math.ceil(sorted.length / pageSize)) : 1
  const current = Math.min(page, pageCount - 1)
  const paged = pageSize > 0 ? sorted.slice(current * pageSize, current * pageSize + pageSize) : sorted

  const toggleSort = (col: Column<T>) => {
    if (!col.sortValue) return
    if (sortKey === col.key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortKey(col.key)
      setSortDir('asc')
    }
    setPage(0)
  }

  const headBg = 'var(--colorNeutralBackground1)'

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
      <div style={{ maxHeight, overflow: 'auto', border: '1px solid var(--colorNeutralStroke2)', borderRadius: 6 }}>
        <Table size="small" aria-label={ariaLabel} style={{ minWidth: 'max-content' }}>
          <TableHeader>
            <TableRow>
              {columns.map((c) => {
                const active = sortKey === c.key
                return (
                  <TableHeaderCell
                    key={c.key}
                    onClick={() => toggleSort(c)}
                    style={{
                      position: 'sticky',
                      top: 0,
                      zIndex: 1,
                      background: headBg,
                      cursor: c.sortValue ? 'pointer' : 'default',
                      userSelect: 'none',
                      width: c.width,
                      minWidth: c.minWidth,
                      textAlign: c.align,
                      whiteSpace: 'nowrap',
                      fontSize: 12,
                    }}
                  >
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontWeight: 600 }}>
                      {c.header}
                      {c.sortValue && (
                        <span style={{ color: active ? 'var(--colorBrandForeground1)' : 'var(--colorNeutralForeground4)', fontSize: 11 }}>
                          {active ? (sortDir === 'asc' ? '▲' : '▼') : '↕'}
                        </span>
                      )}
                    </span>
                  </TableHeaderCell>
                )
              })}
            </TableRow>
          </TableHeader>
          <TableBody>
            {paged.map((row, i) => (
              <TableRow
                key={rowKey(row)}
                style={{
                  background: rowStyle?.(row) ? undefined : i % 2 ? 'var(--colorNeutralBackground2)' : undefined,
                  ...rowStyle?.(row),
                }}
              >
                {columns.map((c) => (
                  <TableCell key={c.key} style={{ textAlign: c.align, verticalAlign: 'middle', fontSize: 12 }}>
                    {c.render ? c.render(row) : (c.sortValue?.(row) ?? '—')}
                  </TableCell>
                ))}
              </TableRow>
            ))}
            {paged.length === 0 && (
              <TableRow>
                <TableCell colSpan={columns.length} style={{ color: 'var(--colorNeutralForeground3)', padding: 24, textAlign: 'center' }}>
                  {emptyMessage}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {pageSize > 0 && sorted.length > pageSize && (
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 12 }}>
          <Text size={200} style={{ color: 'var(--colorNeutralForeground3)' }}>
            {current * pageSize + 1}–{Math.min((current + 1) * pageSize, sorted.length)} of {sorted.length}
          </Text>
          <Button
            size="small"
            appearance="subtle"
            icon={<ChevronLeftRegular />}
            disabled={current === 0}
            onClick={() => setPage((p) => Math.max(0, p - 1))}
            aria-label="Previous page"
          />
          <Text size={200}>
            {current + 1} / {pageCount}
          </Text>
          <Button
            size="small"
            appearance="subtle"
            icon={<ChevronRightRegular />}
            disabled={current >= pageCount - 1}
            onClick={() => setPage((p) => Math.min(pageCount - 1, p + 1))}
            aria-label="Next page"
          />
        </div>
      )}
    </div>
  )
}
