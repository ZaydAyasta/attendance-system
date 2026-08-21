import type { UserRole } from '@/types/user-role'

export interface NavigationItem {
  label: string
  description: string
  icon: string
  route: string
  allowedRoles?: UserRole[]
}

export const navigationItems: NavigationItem[] = [
  {
    label: 'Resumen',
    description: 'Vista general y accesos rápidos.',
    icon: 'pi pi-home',
    route: '/',
  },
  {
    label: 'Asistencia',
    description: 'Consulta marcaciones y estados diarios.',
    icon: 'pi pi-clock',
    route: '/attendance',
  },
  {
    label: 'Ausencias',
    description: 'Registra y consulta ausencias autorizadas.',
    icon: 'pi pi-calendar-minus',
    route: '/absences',
    allowedRoles: ['admin', 'user'],
  },
  {
    label: 'Calendario laboral',
    description: 'Administra días laborables, no laborables y feriados.',
    icon: 'pi pi-calendar',
    route: '/work-calendar',
    allowedRoles: ['admin'],
  },
  {
    label: 'Asignaciones',
    description: 'Gestiona asignaciones excepcionales por empleado.',
    icon: 'pi pi-briefcase',
    route: '/work-assignments',
    allowedRoles: ['admin'],
  },
  {
    label: 'Empleados',
    description: 'Consulta el padrón y la información operativa.',
    icon: 'pi pi-users',
    route: '/employees',
    allowedRoles: ['admin'],
  },
  {
    label: 'Reportes',
    description: 'Prepara salidas consolidadas para revisión interna.',
    icon: 'pi pi-chart-bar',
    route: '/reports',
    allowedRoles: ['admin'],
  },
  {
    label: 'Sistema',
    description: 'Revisa configuración técnica y estados internos.',
    icon: 'pi pi-cog',
    route: '/system',
    allowedRoles: ['it'],
  },
  {
    label: 'Checkpoints',
    description: 'Supervisa puntos de captura y diagnóstico operativo.',
    icon: 'pi pi-map-marker',
    route: '/checkpoints',
    allowedRoles: ['it'],
  },
]

export function filterNavigationItems(role: UserRole): NavigationItem[] {
  return navigationItems.filter((item) => item.allowedRoles === undefined || item.allowedRoles.includes(role))
}
