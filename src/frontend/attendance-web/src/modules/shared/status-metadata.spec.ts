import { describe, expect, it } from 'vitest'
import { getStatusMetadata } from './status-metadata'

describe('getStatusMetadata', () => {
  it('maps attendance status to a human-friendly label', () => {
    expect(getStatusMetadata('Present')).toMatchObject({
      label: 'Asistió',
      icon: 'pi pi-check-circle',
      severity: 'success',
    })
  })

  it('maps failures to a friendly label', () => {
    expect(getStatusMetadata('MissingWorkCalendarDay')).toMatchObject({
      label: 'Día sin configurar',
      severity: 'warn',
    })
  })

  it('falls back to a generic review state for unknown codes', () => {
    expect(getStatusMetadata('UnexpectedBackendValue')).toMatchObject({
      label: 'Requiere revisión',
      severity: 'contrast',
    })
  })
})
