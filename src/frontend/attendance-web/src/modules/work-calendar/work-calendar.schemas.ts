import { z } from 'zod'
import { workCalendarDayTypes } from './work-calendar.types'

export const workCalendarDescriptionMaxLength = 500

export const workCalendarDayTypeSchema = z.enum(workCalendarDayTypes, {
  error: 'Selecciona un tipo de día válido.',
})

export const workCalendarFormSchema = z.object({
  date: z
    .string()
    .trim()
    .min(1, 'Selecciona una fecha.')
    .regex(/^\d{4}-\d{2}-\d{2}$/, 'Selecciona una fecha válida.'),
  dayType: workCalendarDayTypeSchema,
  description: z
    .string()
    .trim()
    .max(
      workCalendarDescriptionMaxLength,
      `La descripción no puede exceder ${workCalendarDescriptionMaxLength} caracteres.`,
    )
    .optional()
    .transform((value) => value ?? ''),
})

export const workCalendarRangeSchema = z
  .object({
    from: z.string().trim().min(1, 'Selecciona una fecha inicial.'),
    to: z.string().trim().min(1, 'Selecciona una fecha final.'),
  })
  .refine((value) => value.from <= value.to, {
    error: 'La fecha inicial debe ser menor o igual a la fecha final.',
    path: ['to'],
  })
