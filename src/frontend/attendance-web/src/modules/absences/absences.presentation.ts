export const absenceTypeLabels: Record<string, string> = { Vacation: 'Vacaciones', MedicalLeave: 'Licencia médica', Commission: 'Comisión', JustifiedAbsence: 'Ausencia justificada', Permission: 'Permiso' }
export const absenceStatusLabels: Record<string, string> = { Active: 'Activa', Cancelled: 'Cancelada' }
export function formatAbsenceDate(date: string): string { return new Intl.DateTimeFormat('es-PE', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(`${date}T12:00:00`)) }
