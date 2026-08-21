export type StatusSeverity = 'secondary' | 'success' | 'info' | 'warn' | 'danger' | 'contrast'

export interface StatusMetadata {
  label: string
  icon: string
  severity: StatusSeverity
}

const defaultStatusMetadata: StatusMetadata = {
  label: 'Requiere revisión',
  icon: 'pi pi-exclamation-triangle',
  severity: 'contrast',
}

const statusMetadata: Record<string, StatusMetadata> = {
  Present: {
    label: 'Asistió',
    icon: 'pi pi-check-circle',
    severity: 'success',
  },
  Incomplete: {
    label: 'Marcación incompleta',
    icon: 'pi pi-exclamation-circle',
    severity: 'warn',
  },
  UnexcusedAbsence: {
    label: 'Falta injustificada',
    icon: 'pi pi-times-circle',
    severity: 'danger',
  },
  Vacation: {
    label: 'Vacaciones',
    icon: 'pi pi-sun',
    severity: 'info',
  },
  MedicalLeave: {
    label: 'Descanso médico',
    icon: 'pi pi-heart',
    severity: 'info',
  },
  Permission: {
    label: 'Permiso',
    icon: 'pi pi-file-check',
    severity: 'info',
  },
  Commission: {
    label: 'Comisión',
    icon: 'pi pi-briefcase',
    severity: 'info',
  },
  JustifiedAbsence: {
    label: 'Ausencia justificada',
    icon: 'pi pi-shield',
    severity: 'info',
  },
  Holiday: {
    label: 'Feriado',
    icon: 'pi pi-flag',
    severity: 'secondary',
  },
  NonWorkingDay: {
    label: 'Día no laborable',
    icon: 'pi pi-calendar-minus',
    severity: 'secondary',
  },
  NotApplicable: {
    label: 'No aplica',
    icon: 'pi pi-minus-circle',
    severity: 'secondary',
  },
  MissingWorkCalendarDay: {
    label: 'Día sin configurar',
    icon: 'pi pi-calendar-times',
    severity: 'warn',
  },
  MultipleActiveAbsences: {
    label: 'Requiere revisión',
    icon: 'pi pi-exclamation-triangle',
    severity: 'danger',
  },
  IncompleteMarks: {
    label: 'Marcaciones incompletas',
    icon: 'pi pi-exclamation-circle',
    severity: 'warn',
  },
  MarksDuringAuthorizedAbsence: {
    label: 'Hay marcaciones durante una ausencia',
    icon: 'pi pi-info-circle',
    severity: 'warn',
  },
  MarksOnHoliday: {
    label: 'Hay marcaciones en feriado',
    icon: 'pi pi-info-circle',
    severity: 'warn',
  },
}

export function getStatusMetadata(code: string | null | undefined): StatusMetadata {
  if (!code) {
    return defaultStatusMetadata
  }

  return statusMetadata[code] ?? defaultStatusMetadata
}
