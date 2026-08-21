import { beforeEach, describe, expect, it, vi } from 'vitest'
import apiClient from '@/services/api-client'
import {
  createWorkCalendarDay,
  deleteWorkCalendarDay,
  listWorkCalendarDays,
  updateWorkCalendarDay,
} from './work-calendar.service'

vi.mock('@/services/api-client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}))

describe('workCalendarService', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('builds the list URL with from and to filters', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({
      data: [],
    } as never)

    await listWorkCalendarDays({
      from: '2026-08-01',
      to: '2026-08-31',
    })

    expect(apiClient.get).toHaveBeenCalledWith('/work-calendar', {
      params: {
        from: '2026-08-01',
        to: '2026-08-31',
      },
    })
  })

  it('posts to create a day', async () => {
    vi.mocked(apiClient.post).mockResolvedValue({
      data: {
        date: '2026-08-21',
        dayType: 'Holiday',
        description: 'Feriado',
        version: 1,
      },
    } as never)

    await createWorkCalendarDay({
      date: '2026-08-21',
      dayType: 'Holiday',
      description: 'Feriado',
    })

    expect(apiClient.post).toHaveBeenCalledWith('/work-calendar', {
      date: '2026-08-21',
      dayType: 'Holiday',
      description: 'Feriado',
    })
  })

  it('puts the update to the date URL with version', async () => {
    vi.mocked(apiClient.put).mockResolvedValue({
      data: {
        date: '2026-08-21',
        dayType: 'WorkingDay',
        description: null,
        version: 2,
      },
    } as never)

    await updateWorkCalendarDay('2026-08-21', {
      dayType: 'WorkingDay',
      description: null,
      version: 7,
    })

    expect(apiClient.put).toHaveBeenCalledWith('/work-calendar/2026-08-21', {
      dayType: 'WorkingDay',
      description: null,
      version: 7,
    })
  })

  it('deletes using the date URL', async () => {
    vi.mocked(apiClient.delete).mockResolvedValue({} as never)

    await deleteWorkCalendarDay('2026-08-21')

    expect(apiClient.delete).toHaveBeenCalledWith('/work-calendar/2026-08-21')
  })
})
