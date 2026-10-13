import { configureStore } from '@reduxjs/toolkit'
import { setupListeners } from '@reduxjs/toolkit/query'
import { workRequestsApi } from '../features/workRequests/workRequestsApi'

export const store = configureStore({
  reducer: {
    [workRequestsApi.reducerPath]: workRequestsApi.reducer,
  },
  middleware: (getDefault) => getDefault().concat(workRequestsApi.middleware),
})

setupListeners(store.dispatch)

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
