import axios from 'axios'
import { z } from 'zod'

const validationProblemSchema = z.object({
  errors: z.record(z.string(), z.array(z.string())).optional(),
  detail: z.string().optional(),
  title: z.string().optional(),
})

function getFirstValidationMessage(problem: unknown): string | null {
  const parsed = validationProblemSchema.safeParse(problem)

  if (!parsed.success) {
    return null
  }

  const firstErrorGroup = Object.values(parsed.data.errors ?? {})[0]
  const firstError = firstErrorGroup?.[0]

  if (firstError) {
    return firstError
  }

  return parsed.data.detail ?? parsed.data.title ?? null
}

export function getUserFriendlyApiError(error: unknown): string {
  if (!axios.isAxiosError(error)) {
    return 'Ocurrió un problema inesperado. Inténtalo nuevamente.'
  }

  if (!error.response) {
    return 'No pudimos conectarnos con el servidor.'
  }

  if (error.response.status === 400) {
    return (
      getFirstValidationMessage(error.response.data) ??
      'Revisa la información ingresada y vuelve a intentarlo.'
    )
  }

  if (error.response.status === 404) {
    return 'No encontramos la información solicitada.'
  }

  if (error.response.status === 409) {
    return 'La información cambió mientras estabas editándola. Actualiza e inténtalo nuevamente.'
  }

  if (error.response.status >= 500) {
    return 'Ocurrió un problema en el servidor. Vuelve a intentarlo en unos minutos.'
  }

  return 'No pudimos completar la solicitud.'
}
