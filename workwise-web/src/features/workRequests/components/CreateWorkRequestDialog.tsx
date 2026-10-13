import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  TextField,
} from '@mui/material'
import Grid from '@mui/material/Grid2'
import { useCreateWorkRequestMutation } from '../workRequestsApi'
import { getErrorMessage, getProblem } from '../apiErrors'
import { PRIORITIES, STATUSES, STATUS_LABELS } from '../types'
import type { CreateWorkRequestBody, Priority, Status } from '../types'

interface FormValues {
  title: string
  clientName: string
  description: string
  priority: Priority | ''
  status: Status | ''
  dueDate: string
  notes: string
}

const defaults: FormValues = {
  title: '',
  clientName: '',
  description: '',
  priority: 'Medium',
  status: 'New',
  dueDate: '',
  notes: '',
}

const fieldNames = Object.keys(defaults) as (keyof FormValues)[]

interface Props {
  open: boolean
  onClose: () => void
  onCreated: (title: string) => void
}

/** Client-side rules mirror the API's FluentValidation rules (required fields, max lengths, enum values). */
export function CreateWorkRequestDialog({ open, onClose, onCreated }: Props) {
  const [createWorkRequest, { isLoading, error, reset: resetMutation }] = useCreateWorkRequestMutation()
  const {
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<FormValues>({ defaultValues: defaults, mode: 'onTouched' })

  useEffect(() => {
    if (open) {
      reset(defaults)
      resetMutation()
    }
  }, [open, reset, resetMutation])

  const onSubmit = async (values: FormValues) => {
    const body: CreateWorkRequestBody = {
      title: values.title.trim(),
      clientName: values.clientName.trim(),
      description: values.description.trim() || undefined,
      priority: values.priority as Priority,
      status: values.status as Status,
      dueDate: values.dueDate,
      notes: values.notes.trim() || undefined,
    }
    try {
      const created = await createWorkRequest(body).unwrap()
      onCreated(created.title)
      onClose()
    } catch (err) {
      // Map server-side field errors (ValidationProblemDetails) back onto the form.
      const problem = getProblem(err as never)
      Object.entries(problem?.errors ?? {}).forEach(([field, messages]) => {
        if (fieldNames.includes(field as keyof FormValues)) {
          setError(field as keyof FormValues, { type: 'server', message: messages.join(' ') })
        }
      })
    }
  }

  const problem = getProblem(error)
  const unmappedError =
    error && !Object.keys(problem?.errors ?? {}).some((k) => fieldNames.includes(k as keyof FormValues))

  return (
    <Dialog open={open} onClose={isLoading ? undefined : onClose} fullWidth maxWidth="sm">
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <DialogTitle>New work request</DialogTitle>
        <DialogContent dividers>
          {unmappedError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {getErrorMessage(error)}
            </Alert>
          )}
          <Grid container spacing={2}>
            <Grid size={12}>
              <Controller
                name="title"
                control={control}
                rules={{
                  validate: (v) => v.trim().length > 0 || 'Title is required.',
                  maxLength: { value: 200, message: 'Title must be 200 characters or fewer.' },
                }}
                render={({ field }) => (
                  <TextField {...field} label="Title" required fullWidth autoFocus
                    error={!!errors.title} helperText={errors.title?.message} />
                )}
              />
            </Grid>
            <Grid size={12}>
              <Controller
                name="clientName"
                control={control}
                rules={{
                  validate: (v) => v.trim().length > 0 || 'Client name is required.',
                  maxLength: { value: 150, message: 'Client name must be 150 characters or fewer.' },
                }}
                render={({ field }) => (
                  <TextField {...field} label="Client name" required fullWidth
                    error={!!errors.clientName} helperText={errors.clientName?.message} />
                )}
              />
            </Grid>
            <Grid size={12}>
              <Controller
                name="description"
                control={control}
                rules={{ maxLength: { value: 2000, message: 'Description must be 2000 characters or fewer.' } }}
                render={({ field }) => (
                  <TextField {...field} label="Description" fullWidth multiline minRows={2}
                    error={!!errors.description} helperText={errors.description?.message} />
                )}
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 4 }}>
              <Controller
                name="priority"
                control={control}
                rules={{ validate: (v) => (PRIORITIES as readonly string[]).includes(v) || 'Priority is required.' }}
                render={({ field }) => (
                  <TextField {...field} select label="Priority" required fullWidth
                    error={!!errors.priority} helperText={errors.priority?.message}>
                    {PRIORITIES.map((p) => <MenuItem key={p} value={p}>{p}</MenuItem>)}
                  </TextField>
                )}
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 4 }}>
              <Controller
                name="status"
                control={control}
                rules={{ validate: (v) => (STATUSES as readonly string[]).includes(v) || 'Status is required.' }}
                render={({ field }) => (
                  <TextField {...field} select label="Status" required fullWidth
                    error={!!errors.status} helperText={errors.status?.message}>
                    {STATUSES.map((s) => <MenuItem key={s} value={s}>{STATUS_LABELS[s]}</MenuItem>)}
                  </TextField>
                )}
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 4 }}>
              <Controller
                name="dueDate"
                control={control}
                rules={{ required: 'Due date is required.' }}
                render={({ field }) => (
                  <TextField {...field} type="date" label="Due date" required fullWidth
                    InputLabelProps={{ shrink: true }}
                    error={!!errors.dueDate} helperText={errors.dueDate?.message} />
                )}
              />
            </Grid>
            <Grid size={12}>
              <Controller
                name="notes"
                control={control}
                rules={{ maxLength: { value: 2000, message: 'Notes must be 2000 characters or fewer.' } }}
                render={({ field }) => (
                  <TextField {...field} label="Notes (optional)" fullWidth multiline minRows={2}
                    error={!!errors.notes} helperText={errors.notes?.message} />
                )}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions sx={{ px: 3, py: 2 }}>
          <Button onClick={onClose} disabled={isLoading}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={isLoading}>
            {isLoading ? 'Creating…' : 'Create'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
