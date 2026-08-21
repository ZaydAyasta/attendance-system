import { describe, expect, it } from 'vitest'
import {
  formatWorkCalendarDate,
  getCurrentMonthRange,
  getWorkCalendarDayTypeMetadata,
} from './work-calendar.presentation'

describe('work-calendar.presentation', () => {
  it('maps WorkingDay to a human label', () => {
    expect(getWorkCalendarDayTypeMetadata('WorkingDay')).toMatchObject({
      label: 'Laborable',
      icon: 'pi pi-briefcase',
    })
  })

  it('maps NonWorkingDay to a human label', () => {
    expect(getWorkCalendarDayTypeMetadata('NonWorkingDay')).toMatchObject({
      label: 'No laborable',
      icon: 'pi pi-pause-circle',
    })
  })

  it('maps Holiday to a human label', () => {
    expect(getWorkCalendarDayTypeMetadata('Holiday')).toMatchObject({
      label: 'Feriado',
      icon: 'pi pi-flag',
    })
  })

  it('formats dates for display', () => {
    expect(formatWorkCalendarDate('2026-08-21')).toContain('2026')
  })

  it('returns the current month range without timezone drift', () => {
    expect(getCurrentMonthRange(new Date(2026, 7, 21))).toEqual({
      from: '2026-08-01',
      to: '2026-08-31',
    })
  })
})
