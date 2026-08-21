export type UserRole = 'admin' | 'user' | 'it'

export interface RoleOption {
  label: string
  value: UserRole
}
