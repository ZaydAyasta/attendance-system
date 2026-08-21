import type { WorkCalendarDayType } from './work-calendar.types'

export interface WorkCalendarDayTypeMetadata {
  icon: string
  label: string
  shortLabel: string
  severity: 'success' | 'warn' | 'info'
}

export interface WorkCalendarVisualStateMetadata {
  icon: string
  label: string
  shortLabel: string
  severity: 'contrast' | 'secondary' | 'success' | 'warn' | 'info'
}

export interface WorkCalendarMonthCell {
  date: string
  dayNumber: number
  isCurrentMonth: boolean
}

const dayTypeMetadata: Record<WorkCalendarDayType, WorkCalendarDayTypeMetadata> = {
  WorkingDay: {
    label: 'Laborable',
    shortLabel: 'Lab.',
    icon: 'pi pi-briefcase',
    severity: 'success',
  },
  NonWorkingDay: {
    label: 'No laborable',
    shortLabel: 'No lab.',
    icon: 'pi pi-pause-circle',
    severity: 'warn',
  },
  Holiday: {
    label: 'Feriado',
    shortLabel: 'Feriado',
    icon: 'pi pi-flag',
    severity: 'info',
  },
}

const workCalendarVisualMetadata: Record<
  WorkCalendarDayType | 'Unconfigured' | 'Unavailable',
  WorkCalendarVisualStateMetadata
> = {
  ...dayTypeMetadata,
  Unconfigured: {
    label: 'Sin configurar',
    shortLabel: 'Sin conf.',
    icon: 'pi pi-question-circle',
    severity: 'secondary',
  },
  Unavailable: {
    label: 'Sin datos',
    shortLabel: 'Sin datos',
    icon: 'pi pi-exclamation-circle',
    severity: 'contrast',
  },
}

export const workCalendarWeekdayHeaders = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'] as const

export function getWorkCalendarDayTypeMetadata(
  dayType: WorkCalendarDayType,
): WorkCalendarDayTypeMetadata {
  return dayTypeMetadata[dayType]
}

export function getWorkCalendarVisualStateMetadata(
  state: WorkCalendarDayType | 'Unconfigured' | 'Unavailable',
): WorkCalendarVisualStateMetadata {
  return workCalendarVisualMetadata[state]
}

export function formatWorkCalendarDate(date: string): string {
  return new Intl.DateTimeFormat('es-PE', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  }).format(parseWorkCalendarDate(date))
}

export function formatWorkCalendarDateWithWeekday(date: string): string {
  return new Intl.DateTimeFormat('es-PE', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(parseWorkCalendarDate(date))
}

export function getCurrentMonthRange(today = new Date()): { from: string; to: string } {
  return getMonthRange(today)
}

export function getMonthRange(referenceDate: Date): { from: string; to: string } {
  const month = startOfMonth(referenceDate)
  const year = month.getFullYear()
  const monthIndex = month.getMonth()
  const start = new Date(year, monthIndex, 1)
  const end = new Date(year, monthIndex + 1, 0)

  return {
    from: formatDateInput(start),
    to: formatDateInput(end),
  }
}

export function getMonthLabel(referenceDate: Date): string {
  return new Intl.DateTimeFormat('es-PE', {
    month: 'long',
    year: 'numeric',
  }).format(startOfMonth(referenceDate))
}

export function buildWorkCalendarMonth(referenceDate: Date): WorkCalendarMonthCell[] {
  const month = startOfMonth(referenceDate)
  const year = month.getFullYear()
  const monthIndex = month.getMonth()
  const firstDay = new Date(year, monthIndex, 1)
  const lastDay = new Date(year, monthIndex + 1, 0)
  const firstWeekdayIndex = toMondayBasedIndex(firstDay.getDay())
  const lastWeekdayIndex = toMondayBasedIndex(lastDay.getDay())
  const gridStart = new Date(year, monthIndex, 1 - firstWeekdayIndex)
  const gridEnd = new Date(year, monthIndex, lastDay.getDate() + (6 - lastWeekdayIndex))
  const cells: WorkCalendarMonthCell[] = []

  for (const cursor = new Date(gridStart); cursor <= gridEnd; cursor.setDate(cursor.getDate() + 1)) {
    cells.push({
      date: formatDateInput(cursor),
      dayNumber: cursor.getDate(),
      isCurrentMonth: cursor.getMonth() === monthIndex,
    })
  }

  return cells
}

export function startOfMonth(referenceDate: Date): Date {
  return new Date(referenceDate.getFullYear(), referenceDate.getMonth(), 1)
}

export function addMonths(referenceDate: Date, amount: number): Date {
  return new Date(referenceDate.getFullYear(), referenceDate.getMonth() + amount, 1)
}

export function formatDateInput(date: Date): string {
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')

  return `${year}-${month}-${day}`
}

export function isDateWithinRange(date: string, from: string, to: string): boolean {
  return date >= from && date <= to
}

function parseWorkCalendarDate(date: string): Date {
  const [year, month, day] = date.split('-').map(Number)

  return new Date(year, month - 1, day)
}

function toMondayBasedIndex(day: number): number {
  return (day + 6) % 7
}
