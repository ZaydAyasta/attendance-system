export const workCalendarDayTypes = ['WorkingDay', 'NonWorkingDay', 'Holiday'] as const

export type WorkCalendarDayType = (typeof workCalendarDayTypes)[number]

export interface WorkCalendarDay {
  date: string
  dayType: WorkCalendarDayType
  description: string | null
  version: number
}

export interface WorkCalendarRangeFilters {
  from: string
  to: string
}

export interface CreateWorkCalendarDayRequest {
  date: string
  dayType: WorkCalendarDayType
  description: string | null
}

export interface UpdateWorkCalendarDayRequest {
  dayType: WorkCalendarDayType
  description: string | null
  version: number
}

export interface WorkCalendarFormValues {
  date: string
  dayType: WorkCalendarDayType
  description: string | null
}

export interface BulkConfigureWorkCalendarDayRequest {
  date: string
  dayType: WorkCalendarDayType
  description: string | null
  version?: number
}

export interface BulkConfigureWorkCalendarRequest {
  days: BulkConfigureWorkCalendarDayRequest[]
  overwriteExisting: boolean
}

export interface BulkConfigureWorkCalendarResponse {
  created: number
  updated: number
  skipped: number
}
