import { beforeEach, describe, expect, it, vi } from 'vitest'
import apiClient from '@/services/api-client'
import { captureActionLabel, getTodayMarks, markCapture, resolveCapture } from './attendance-capture.service'

vi.mock('@/services/api-client', () => ({ default: { get: vi.fn(), post: vi.fn() } }))

describe('attendanceCaptureService', () => {
  beforeEach(() => vi.clearAllMocks())
  it('uses the personal capture endpoints without an employee id', async () => {
    vi.mocked(apiClient.post).mockResolvedValue({ data: {} } as never)
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] } as never)
    await resolveCapture('qr'); await markCapture('qr', 'Entry'); await getTodayMarks()
    expect(apiClient.post).toHaveBeenNthCalledWith(1, '/attendance/capture/resolve', { qrToken: 'qr' })
    expect(apiClient.post).toHaveBeenNthCalledWith(2, '/attendance/capture/mark', { qrToken: 'qr', action: 'Entry' })
    expect(apiClient.get).toHaveBeenCalledWith('/me/attendance/marks/today')
  })
  it('uses human labels for capture actions', () => expect(captureActionLabel('CommissionReturn')).toBe('Regresar de comisión'))
})
