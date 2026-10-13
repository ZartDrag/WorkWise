import { Chip } from '@mui/material'
import type { Priority, Status } from '../types'
import { STATUS_LABELS } from '../types'

const priorityColor: Record<Priority, 'default' | 'warning' | 'error'> = {
  Low: 'default',
  Medium: 'warning',
  High: 'error',
}

const statusColor: Record<Status, 'info' | 'primary' | 'error' | 'success'> = {
  New: 'info',
  InProgress: 'primary',
  Blocked: 'error',
  Completed: 'success',
}

export function PriorityChip({ priority }: { priority: Priority }) {
  return <Chip size="small" label={priority} color={priorityColor[priority]} variant="outlined" />
}

export function StatusChip({ status }: { status: Status }) {
  return <Chip size="small" label={STATUS_LABELS[status]} color={statusColor[status]} />
}
