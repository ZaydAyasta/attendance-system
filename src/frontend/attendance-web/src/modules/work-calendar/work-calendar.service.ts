import apiClient from '@/services/api-client'
import type {
  CreateWorkCalendarDayRequest,
  UpdateWorkCalendarDayRequest,
  WorkCalendarDay,
  WorkCalendarRangeFilters,
} from './work-calendar.types'

const workCalendarBasePath = '/work-calendar'

export async function listWorkCalendarDays(
  filters: WorkCalendarRangeFilters,
): Promise<WorkCalendarDay[]> {
  const response = await apiClient.get<WorkCalendarDay[]>(workCalendarBasePath, {
    params: {
      from: filters.from,
      to: filters.to,
    },
  })

  return response.data
}

export async function createWorkCalendarDay(
  request: CreateWorkCalendarDayRequest,
): Promise<WorkCalendarDay> {
  const response = await apiClient.post<WorkCalendarDay>(workCalendarBasePath, request)

  return response.data
}

export async function updateWorkCalendarDay(
  date: string,
  request: UpdateWorkCalendarDayRequest,
): Promise<WorkCalendarDay> {
  const response = await apiClient.put<WorkCalendarDay>(`${workCalendarBasePath}/${date}`, request)

  return response.data
}

export async function deleteWorkCalendarDay(date: string): Promise<void> {
  await apiClient.delete(`${workCalendarBasePath}/${date}`)
}
