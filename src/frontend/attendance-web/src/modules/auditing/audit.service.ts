import apiClient from '@/services/api-client'

export interface AuditEvent {
  id: string
  actor: { userId: string | null; displayName: string }
  action: string
  entityType: string
  entityId: string
  occurredAt: string
  outcome: string
  metadata: Record<string, unknown> | null
}

export interface AuditPage { items: AuditEvent[]; page: number; pageSize: number; total: number }
export interface AuditFilters { from?: string; to?: string; action?: string; entityType?: string; page?: number; pageSize?: number }

export async function listAuditEvents(filters: AuditFilters): Promise<AuditPage> {
  return (await apiClient.get<AuditPage>('/audit-events', { params: filters })).data
}
