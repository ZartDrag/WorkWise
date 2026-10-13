import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  LinearProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  Typography,
} from '@mui/material'
import InboxIcon from '@mui/icons-material/InboxOutlined'
import { useListWorkRequestsQuery } from './workRequestsApi'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { WorkRequestFilters } from './components/WorkRequestFilters'
import { PriorityChip, StatusChip } from './components/Chips'
import { getErrorMessage } from './apiErrors'
import { formatDate, isOverdue } from './format'
import type { Status } from './types'

export function WorkRequestsPage() {
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<Status | ''>('')
  const [page, setPage] = useState(0) // MUI pagination is 0-based, the API is 1-based
  const [pageSize, setPageSize] = useState(10)
  const debouncedSearch = useDebouncedValue(search, 400)

  const { data, isLoading, isFetching, isError, error, refetch } = useListWorkRequestsQuery({
    status,
    search: debouncedSearch,
    page: page + 1,
    pageSize,
  })

  const items = data?.items ?? []
  const hasFilters = Boolean(status || debouncedSearch.trim())

  return (
    <Stack spacing={2}>
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ md: 'center' }}>
        <WorkRequestFilters
          search={search}
          status={status}
          onSearchChange={(v) => {
            setSearch(v)
            setPage(0)
          }}
          onStatusChange={(v) => {
            setStatus(v)
            setPage(0)
          }}
        />
      </Stack>

      <Paper variant="outlined">
        <Box sx={{ height: 4 }}>{isFetching && <LinearProgress />}</Box>

        {isError ? (
          <Alert
            severity="error"
            sx={{ m: 2 }}
            action={
              <Button color="inherit" size="small" onClick={() => refetch()}>
                Retry
              </Button>
            }
          >
            Could not load work requests. {getErrorMessage(error)}
          </Alert>
        ) : isLoading ? (
          <Typography sx={{ p: 4 }} color="text.secondary" align="center">
            Loading work requests…
          </Typography>
        ) : items.length === 0 ? (
          <Stack alignItems="center" spacing={1} sx={{ p: 6 }} color="text.secondary">
            <InboxIcon fontSize="large" />
            <Typography variant="subtitle1">
              {hasFilters ? 'No work requests match your filters.' : 'No work requests yet.'}
            </Typography>
            <Typography variant="body2">
              {hasFilters ? 'Try a different search or status.' : 'Create the first one to get started.'}
            </Typography>
          </Stack>
        ) : (
          <TableContainer>
            <Table size="medium">
              <TableHead>
                <TableRow>
                  <TableCell>Title</TableCell>
                  <TableCell>Client</TableCell>
                  <TableCell>Priority</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Due date</TableCell>
                  <TableCell align="right">Notes</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {items.map((w) => (
                  <TableRow key={w.id} hover>
                    <TableCell sx={{ maxWidth: 340 }}>
                      <Typography fontWeight={600}>{w.title}</Typography>
                      {w.description && (
                        <Typography variant="body2" color="text.secondary" noWrap>
                          {w.description}
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>{w.clientName}</TableCell>
                    <TableCell>
                      <PriorityChip priority={w.priority} />
                    </TableCell>
                    <TableCell>
                      <StatusChip status={w.status} />
                    </TableCell>
                    <TableCell>
                      <Typography
                        variant="body2"
                        color={isOverdue(w.dueDate, w.status) ? 'error' : 'text.primary'}
                        fontWeight={isOverdue(w.dueDate, w.status) ? 600 : 400}
                      >
                        {formatDate(w.dueDate)}
                      </Typography>
                    </TableCell>
                    <TableCell align="right">{w.noteCount}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}

        <TablePagination
          component="div"
          count={data?.totalCount ?? 0}
          page={page}
          rowsPerPage={pageSize}
          rowsPerPageOptions={[5, 10, 25, 50]}
          onPageChange={(_, p) => setPage(p)}
          onRowsPerPageChange={(e) => {
            setPageSize(Number(e.target.value))
            setPage(0)
          }}
        />
      </Paper>
    </Stack>
  )
}
