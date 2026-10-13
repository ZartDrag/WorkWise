import { InputAdornment, MenuItem, Stack, TextField } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import type { Status } from '../types'
import { STATUSES, STATUS_LABELS } from '../types'

interface Props {
  search: string
  status: Status | ''
  onSearchChange: (value: string) => void
  onStatusChange: (value: Status | '') => void
}

export function WorkRequestFilters({ search, status, onSearchChange, onStatusChange }: Props) {
  return (
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ flex: 1 }}>
      <TextField
        size="small"
        placeholder="Search by title or client"
        value={search}
        onChange={(e) => onSearchChange(e.target.value)}
        sx={{ minWidth: 280 }}
        inputProps={{ 'aria-label': 'Search work requests', maxLength: 200 }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon fontSize="small" />
            </InputAdornment>
          ),
        }}
      />
      <TextField
        select
        size="small"
        label="Status"
        value={status}
        onChange={(e) => onStatusChange(e.target.value as Status | '')}
        sx={{ minWidth: 180 }}
      >
        <MenuItem value="">All statuses</MenuItem>
        {STATUSES.map((s) => (
          <MenuItem key={s} value={s}>
            {STATUS_LABELS[s]}
          </MenuItem>
        ))}
      </TextField>
    </Stack>
  )
}
