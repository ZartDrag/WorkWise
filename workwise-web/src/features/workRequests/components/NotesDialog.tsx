import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  List,
  ListItem,
  ListItemText,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useAddWorkRequestNoteMutation, useGetWorkRequestQuery } from '../workRequestsApi'
import { getErrorMessage } from '../apiErrors'
import { formatDate, formatDateTime } from '../format'
import { PriorityChip, StatusChip } from './Chips'

interface Props {
  workRequestId: string | null
  onClose: () => void
  onNoteAdded: () => void
}

/** Shows a work request's details and notes, and lets the user add a new note. */
export function NotesDialog({ workRequestId, onClose, onNoteAdded }: Props) {
  const open = workRequestId !== null
  const { data, isLoading, isError, error, refetch } = useGetWorkRequestQuery(workRequestId ?? '', { skip: !open })
  const [addNote, { isLoading: isSaving, error: saveError, reset: resetMutation }] = useAddWorkRequestNoteMutation()
  const { control, handleSubmit, reset, formState: { errors } } = useForm<{ content: string }>({
    defaultValues: { content: '' },
  })

  useEffect(() => {
    if (open) {
      reset({ content: '' })
      resetMutation()
    }
  }, [open, workRequestId, reset, resetMutation])

  const onSubmit = async ({ content }: { content: string }) => {
    try {
      await addNote({ id: workRequestId!, content: content.trim() }).unwrap()
      reset({ content: '' })
      onNoteAdded()
    } catch {
      // error is rendered from the mutation state
    }
  }

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{data?.title ?? 'Work request'}</DialogTitle>
      <DialogContent dividers>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}><CircularProgress /></Box>
        ) : isError ? (
          <Alert severity="error" action={<Button color="inherit" size="small" onClick={() => refetch()}>Retry</Button>}>
            {getErrorMessage(error)}
          </Alert>
        ) : data ? (
          <Stack spacing={2}>
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
              <Typography variant="body2" color="text.secondary">{data.clientName}</Typography>
              <PriorityChip priority={data.priority} />
              <StatusChip status={data.status} />
              <Typography variant="body2" color="text.secondary">Due {formatDate(data.dueDate)}</Typography>
            </Stack>
            {data.description && <Typography variant="body2">{data.description}</Typography>}

            <Typography variant="subtitle2">Notes ({data.notes.length})</Typography>
            {data.notes.length === 0 ? (
              <Typography variant="body2" color="text.secondary">No notes yet.</Typography>
            ) : (
              <List dense disablePadding sx={{ maxHeight: 260, overflow: 'auto', bgcolor: 'grey.50', borderRadius: 1 }}>
                {data.notes.map((n) => (
                  <ListItem key={n.id} divider>
                    <ListItemText
                      primary={n.content}
                      secondary={formatDateTime(n.createdAt)}
                      primaryTypographyProps={{ sx: { whiteSpace: 'pre-wrap' } }}
                    />
                  </ListItem>
                ))}
              </List>
            )}

            <form id="add-note-form" onSubmit={handleSubmit(onSubmit)} noValidate>
              <Controller
                name="content"
                control={control}
                rules={{
                  validate: (v) => v.trim().length > 0 || 'Note content is required.',
                  maxLength: { value: 2000, message: 'Note must be 2000 characters or fewer.' },
                }}
                render={({ field }) => (
                  <TextField {...field} label="Add a note" fullWidth multiline minRows={2}
                    error={!!errors.content || !!saveError}
                    helperText={errors.content?.message ?? (saveError ? getErrorMessage(saveError) : ' ')} />
                )}
              />
            </form>
          </Stack>
        ) : null}
      </DialogContent>
      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose}>Close</Button>
        <Button type="submit" form="add-note-form" variant="contained" disabled={isSaving || !data}>
          {isSaving ? 'Saving…' : 'Add note'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
