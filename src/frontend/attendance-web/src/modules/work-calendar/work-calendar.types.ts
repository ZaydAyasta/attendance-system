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
