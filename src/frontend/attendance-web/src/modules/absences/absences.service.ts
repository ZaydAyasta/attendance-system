import apiClient from '@/services/api-client'
import type { Absence, AbsenceFilters, AbsenceRequest } from './absences.types'
const basePath = '/absences'
export async function listAbsences(filters: AbsenceFilters): Promise<Absence[]> { return (await apiClient.get<Absence[]>(basePath, { params: filters })).data }
export async function createAbsence(request: AbsenceRequest): Promise<Absence> { return (await apiClient.post<Absence>(basePath, request)).data }
export async function updateAbsence(id: string, request: Omit<AbsenceRequest, 'employeeId'> & { version: number }): Promise<Absence> { return (await apiClient.put<Absence>(`${basePath}/${id}`, request)).data }
export async function cancelAbsence(id: string, version: number): Promise<void> { await apiClient.post(`${basePath}/${id}/cancel`, { version }) }
