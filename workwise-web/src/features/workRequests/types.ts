export const PRIORITIES = ['Low', 'Medium', 'High'] as const
export const STATUSES = ['New', 'InProgress', 'Blocked', 'Completed'] as const

export type Priority = (typeof PRIORITIES)[number]
export type Status = (typeof STATUSES)[number]

export const STATUS_LABELS: Record<Status, string> = {
  New: 'New',
  InProgress: 'In progress',
  Blocked: 'Blocked',
  Completed: 'Completed',
}

export interface WorkRequestSummary {
  id: string
  title: string
  clientName: string
  description: string | null
  priority: Priority
  status: Status
  dueDate: string
  createdAt: string
  updatedAt: string
  noteCount: number
}

export interface WorkRequestNote {
  id: string
  content: string
  createdAt: string
}

export interface WorkRequestDetails extends Omit<WorkRequestSummary, 'noteCount'> {
  notes: WorkRequestNote[]
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ListWorkRequestsArgs {
  status?: Status | ''
  search?: string
  page: number
  pageSize: number
}

export interface CreateWorkRequestBody {
  title: string
  clientName: string
  description?: string
  priority: Priority
  status: Status
  dueDate: string
  notes?: string
}

/** RFC 7807 ProblemDetails / ValidationProblemDetails returned by the API. */
export interface ApiProblem {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
  traceId?: string
  correlationId?: string
}
