import apiClient from '@/services/api-client'

export type IdentityRole = 'Admin' | 'User' | 'IT'

export interface IdentityEmployeeSummary {
  employeeCode: string
  fullName: string
}

export interface IdentityUser {
  id: string
  username: string
  email: string | null
  role: IdentityRole
  employeeId: string | null
  employee: IdentityEmployeeSummary | null
  isActive: boolean
}

export interface CreateIdentityUserRequest {
  username: string
  email: string | null
  password: string
  role: IdentityRole
  employeeId: string | null
}

export async function listIdentityUsers(): Promise<IdentityUser[]> {
  return (await apiClient.get<IdentityUser[]>('/identity/users')).data
}

export async function createIdentityUser(request: CreateIdentityUserRequest): Promise<IdentityUser> {
  return (await apiClient.post<IdentityUser>('/identity/users', request)).data
}

export async function updateIdentityUser(id: string, request: Pick<CreateIdentityUserRequest, 'username' | 'email' | 'role'>): Promise<IdentityUser> {
  return (await apiClient.put<IdentityUser>(`/identity/users/${id}`, request)).data
}

export async function setIdentityUserStatus(id: string, isActive: boolean): Promise<IdentityUser> {
  return (await apiClient.put<IdentityUser>(`/identity/users/${id}/status`, { isActive })).data
}

export async function resetIdentityUserPassword(id: string, password: string): Promise<void> {
  await apiClient.post(`/identity/users/${id}/reset-password`, { password })
}
