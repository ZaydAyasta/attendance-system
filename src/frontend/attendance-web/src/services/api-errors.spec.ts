import { describe, expect, it } from 'vitest'
import { getUserFriendlyApiError } from './api-errors'

describe('getUserFriendlyApiError', () => {
  it('returns the first validation message when the backend sends validation errors', () => {
    const error = {
      isAxiosError: true,
      response: {
        status: 400,
        data: {
          errors: {
            employeeId: ['Selecciona un trabajador.'],
          },
        },
      },
    }

    expect(getUserFriendlyApiError(error)).toBe('Selecciona un trabajador.')
  })

  it('returns a friendly concurrency message for 409 responses', () => {
    const error = {
      isAxiosError: true,
      response: {
        status: 409,
        data: {},
      },
    }

    expect(getUserFriendlyApiError(error)).toBe(
      'La información cambió mientras estabas editándola. Actualiza e inténtalo nuevamente.',
    )
  })

  it('returns a network message when there is no response', () => {
    const error = {
      isAxiosError: true,
      response: undefined,
    }

    expect(getUserFriendlyApiError(error)).toBe('No pudimos conectarnos con el servidor.')
  })

  it('returns a server message for 500 responses', () => {
    const error = {
      isAxiosError: true,
      response: {
        status: 500,
        data: {},
      },
    }

    expect(getUserFriendlyApiError(error)).toBe(
      'Ocurrió un problema en el servidor. Vuelve a intentarlo en unos minutos.',
    )
  })
})
