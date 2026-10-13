import type { FetchBaseQueryError } from '@reduxjs/toolkit/query'
import type { SerializedError } from '@reduxjs/toolkit'
import type { ApiProblem } from './types'

type RtkError = FetchBaseQueryError | SerializedError | undefined

export function getProblem(error: RtkError): ApiProblem | undefined {
  if (error && 'status' in error && typeof error.data === 'object' && error.data !== null) {
    return error.data as ApiProblem
  }
  return undefined
}

/** Human readable message for any RTK Query error, including network failures. */
export function getErrorMessage(error: RtkError): string {
  if (!error) return ''
  const problem = getProblem(error)
  if (problem) {
    const fieldErrors = problem.errors ? Object.values(problem.errors).flat() : []
    if (fieldErrors.length) return fieldErrors.join(' ')
    return problem.detail ?? problem.title ?? 'Request failed.'
  }
  if ('status' in error && error.status === 'FETCH_ERROR') return 'Cannot reach the WorkWise API. Is it running?'
  if ('message' in error && error.message) return error.message
  return 'Something went wrong.'
}
