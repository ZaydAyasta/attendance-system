export const absenceTypes = ['Vacation', 'MedicalLeave', 'Commission', 'JustifiedAbsence', 'Permission'] as const
export type AbsenceType = typeof absenceTypes[number]
export type AbsenceStatus = 'Active' | 'Cancelled'
export interface AbsenceEmployeeSummary { employeeCode: string; fullName: string }
export interface Absence { id: string; employeeId: string; employee: AbsenceEmployeeSummary; startDate: string; endDate: string; type: AbsenceType; status: AbsenceStatus; reason: string | null; notes: string | null; version: number }
export interface AbsenceFilters { from: string; to: string; status?: AbsenceStatus }
export interface AbsenceRequest { employeeId: string; startDate: string; endDate: string; type: AbsenceType; reason: string | null; notes: string | null }
