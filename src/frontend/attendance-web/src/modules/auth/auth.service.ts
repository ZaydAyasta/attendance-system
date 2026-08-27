import { apiClient, clearAntiforgeryToken, ensureAntiforgeryToken } from '@/services/api-client'

export type AuthRole = 'Admin' | 'User' | 'IT'

export interface AuthEmployee {
  employeeCode: string
  fullName: string
}

export interface AuthUser {
  id: string
  username: string
  role: AuthRole
  employeeId: string | null
  employee: AuthEmployee | null
}

export async function getCurrentUser(): Promise<AuthUser> {
  return (await apiClient.get<AuthUser>('/me')).data
}

export async function login(usernameOrEmail: string, password: string): Promise<AuthUser> {
  await ensureAntiforgeryToken()
  return (await apiClient.post<AuthUser>('/auth/login', { usernameOrEmail, password })).data
}

export async function logout(): Promise<void> {
  await apiClient.post('/auth/logout')
  clearAntiforgeryToken()
}
