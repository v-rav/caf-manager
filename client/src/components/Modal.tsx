import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
} from '@fluentui/react-components'
import type { ReactNode } from 'react'

interface ModalProps {
  open: boolean
  title: string
  children: ReactNode
  onClose: () => void
  onSubmit?: () => void
  submitLabel?: string
  submitDisabled?: boolean
  busy?: boolean
  maxWidth?: number
}

/** Reusable form modal with Cancel / primary action. */
export function Modal({ open, title, children, onClose, onSubmit, submitLabel = 'Save', submitDisabled, busy, maxWidth }: ModalProps) {
  return (
    <Dialog open={open} onOpenChange={(_, d) => !d.open && onClose()}>
      <DialogSurface style={maxWidth ? { maxWidth, width: '90vw' } : undefined}>
        <DialogBody>
          <DialogTitle>{title}</DialogTitle>
          <DialogContent>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12, paddingTop: 8 }}>{children}</div>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose} disabled={busy}>
              Cancel
            </Button>
            {onSubmit && (
              <Button appearance="primary" onClick={onSubmit} disabled={submitDisabled || busy}>
                {busy ? 'Saving…' : submitLabel}
              </Button>
            )}
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}

interface ConfirmProps {
  open: boolean
  title: string
  message: string
  onCancel: () => void
  onConfirm: () => void
  busy?: boolean
}

export function ConfirmDialog({ open, title, message, onCancel, onConfirm, busy }: ConfirmProps) {
  return (
    <Dialog open={open} onOpenChange={(_, d) => !d.open && onCancel()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{title}</DialogTitle>
          <DialogContent>{message}</DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onCancel} disabled={busy}>
              Cancel
            </Button>
            <Button appearance="primary" onClick={onConfirm} disabled={busy}>
              {busy ? 'Deleting…' : 'Delete'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
