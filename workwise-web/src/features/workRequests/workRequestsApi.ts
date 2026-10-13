import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type {
  CreateWorkRequestBody,
  ListWorkRequestsArgs,
  PagedResult,
  Status,
  WorkRequestDetails,
  WorkRequestNote,
  WorkRequestSummary,
} from './types'

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

export const workRequestsApi = createApi({
  reducerPath: 'workRequestsApi',
  baseQuery: fetchBaseQuery({ baseUrl: `${baseUrl}/api` }),
  tagTypes: ['WorkRequest'],
  endpoints: (build) => ({
    listWorkRequests: build.query<PagedResult<WorkRequestSummary>, ListWorkRequestsArgs>({
      query: ({ status, search, page, pageSize }) => ({
        url: 'work-requests',
        params: {
          ...(status ? { status } : {}),
          ...(search?.trim() ? { search: search.trim() } : {}),
          page,
          pageSize,
        },
      }),
      providesTags: (result) => [
        { type: 'WorkRequest', id: 'LIST' },
        ...(result?.items.map((w) => ({ type: 'WorkRequest' as const, id: w.id })) ?? []),
      ],
    }),
    getWorkRequest: build.query<WorkRequestDetails, string>({
      query: (id) => `work-requests/${id}`,
      providesTags: (_r, _e, id) => [{ type: 'WorkRequest', id }],
    }),
    createWorkRequest: build.mutation<WorkRequestDetails, CreateWorkRequestBody>({
      query: (body) => ({ url: 'work-requests', method: 'POST', body }),
      invalidatesTags: [{ type: 'WorkRequest', id: 'LIST' }],
    }),
    updateWorkRequestStatus: build.mutation<WorkRequestSummary, { id: string; status: Status }>({
      query: ({ id, status }) => ({ url: `work-requests/${id}/status`, method: 'PATCH', body: { status } }),
      invalidatesTags: (_r, _e, { id }) => [{ type: 'WorkRequest', id }, { type: 'WorkRequest', id: 'LIST' }],
    }),
    addWorkRequestNote: build.mutation<WorkRequestNote, { id: string; content: string }>({
      query: ({ id, content }) => ({ url: `work-requests/${id}/notes`, method: 'POST', body: { content } }),
      invalidatesTags: (_r, _e, { id }) => [{ type: 'WorkRequest', id }, { type: 'WorkRequest', id: 'LIST' }],
    }),
  }),
})

export const {
  useListWorkRequestsQuery,
  useGetWorkRequestQuery,
  useCreateWorkRequestMutation,
  useUpdateWorkRequestStatusMutation,
  useAddWorkRequestNoteMutation,
} = workRequestsApi
