import { useState } from 'react'
import { Box, CircularProgress, ListItemIcon, Menu, MenuItem, Tooltip } from '@mui/material'
import CheckIcon from '@mui/icons-material/Check'
import ArrowDropDownIcon from '@mui/icons-material/ArrowDropDown'
import { useUpdateWorkRequestStatusMutation } from '../workRequestsApi'
import { getErrorMessage } from '../apiErrors'
import { STATUSES, STATUS_LABELS } from '../types'
import type { Status } from '../types'
import { StatusChip } from './Chips'

interface Props {
  id: string
  status: Status
  onResult: (message: string, severity: 'success' | 'error') => void
}

/** Status chip that opens a menu to change the work request's status. */
export function StatusMenu({ id, status, onResult }: Props) {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)
  const [updateStatus, { isLoading }] = useUpdateWorkRequestStatusMutation()

  const change = async (next: Status) => {
    setAnchor(null)
    if (next === status) return
    try {
      await updateStatus({ id, status: next }).unwrap()
      onResult(`Status changed to ${STATUS_LABELS[next]}`, 'success')
    } catch (err) {
      onResult(`Could not update status. ${getErrorMessage(err as never)}`, 'error')
    }
  }

  return (
    <>
      <Tooltip title="Change status">
        <Box
          component="button"
          type="button"
          aria-label={`Change status (currently ${STATUS_LABELS[status]})`}
          onClick={(e) => setAnchor(e.currentTarget)}
          disabled={isLoading}
          sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.5, border: 0, p: 0, background: 'none', cursor: 'pointer' }}
        >
          <StatusChip status={status} />
          {isLoading ? <CircularProgress size={14} /> : <ArrowDropDownIcon fontSize="small" color="action" />}
        </Box>
      </Tooltip>
      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        {STATUSES.map((s) => (
          <MenuItem key={s} selected={s === status} onClick={() => change(s)}>
            <ListItemIcon>{s === status && <CheckIcon fontSize="small" />}</ListItemIcon>
            {STATUS_LABELS[s]}
          </MenuItem>
        ))}
      </Menu>
    </>
  )
}
