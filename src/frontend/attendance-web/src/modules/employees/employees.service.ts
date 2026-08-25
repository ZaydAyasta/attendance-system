import apiClient from '@/services/api-client'

export interface EmployeeOption { id: string; employeeCode: string; fullName: string }

export async function listActiveEmployees(): Promise<EmployeeOption[]> {
  const response = await apiClient.get<EmployeeOption[]>('/employees', { params: { isActive: true } })
  return response.data
}
