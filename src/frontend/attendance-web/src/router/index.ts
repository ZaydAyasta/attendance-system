import { createRouter, createWebHistory } from 'vue-router'
import AbsencesView from '@/views/AbsencesView.vue'
import AccessDeniedView from '@/views/AccessDeniedView.vue'
import AuditView from '@/views/AuditView.vue'
import AppLayout from '@/layouts/AppLayout.vue'
import AttendanceView from '@/views/AttendanceView.vue'
import CheckpointsView from '@/views/CheckpointsView.vue'
import EmployeesView from '@/views/EmployeesView.vue'
import UsersView from '@/views/UsersView.vue'
import HomeView from '@/views/HomeView.vue'
import LoginView from '@/views/LoginView.vue'
import MarkView from '@/views/MarkView.vue'
import ReportsView from '@/views/ReportsView.vue'
import SystemView from '@/views/SystemView.vue'
import WorkAssignmentsView from '@/views/WorkAssignmentsView.vue'
import WorkCalendarView from '@/views/WorkCalendarView.vue'
import { useAuthStore } from '@/stores/auth'
import { getDefaultRouteForRole } from '@/config/navigation'
import type { UserRole } from '@/types/user-role'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: LoginView, meta: { public: true, title: 'Iniciar sesión' } },
    { path: '/', component: AppLayout, children: [
      { path: 'access-denied', name: 'access-denied', component: AccessDeniedView, meta: { title: 'Acceso denegado' } },
      { path: '', name: 'home', component: HomeView, meta: { title: 'Resumen', allowedRoles: ['Admin'] } },
      { path: 'attendance', name: 'attendance', component: AttendanceView, meta: { title: 'Asistencia', allowedRoles: ['Admin', 'User'] } },
      { path: 'mark', name: 'mark', component: MarkView, meta: { title: 'Marcar asistencia', allowedRoles: ['User'] } },
      { path: 'absences', name: 'absences', component: AbsencesView, meta: { title: 'Ausencias', allowedRoles: ['Admin', 'User'] } },
      { path: 'work-calendar', name: 'work-calendar', component: WorkCalendarView, meta: { title: 'Calendario laboral', allowedRoles: ['Admin'] } },
      { path: 'work-assignments', name: 'work-assignments', component: WorkAssignmentsView, meta: { title: 'Asignaciones', allowedRoles: ['Admin'] } },
      { path: 'employees', name: 'employees', component: EmployeesView, meta: { title: 'Empleados', allowedRoles: ['Admin'] } },
      { path: 'users', name: 'users', component: UsersView, meta: { title: 'Usuarios del sistema', allowedRoles: ['Admin'] } },
      { path: 'reports', name: 'reports', component: ReportsView, meta: { title: 'Reportes', allowedRoles: ['Admin'] } },
      { path: 'audit', name: 'audit', component: AuditView, meta: { title: 'Auditoría', allowedRoles: ['Admin'] } },
      { path: 'system', name: 'system', component: SystemView, meta: { title: 'Sistema', allowedRoles: ['IT'] } },
      { path: 'checkpoints', name: 'checkpoints', component: CheckpointsView, meta: { title: 'Checkpoints', allowedRoles: ['IT'] } },
    ] },
  ],
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()
  await authStore.initialize()
  if (to.meta.public === true) return authStore.isAuthenticated ? '/' : true
  if (!authStore.isAuthenticated) return { path: '/login', query: { redirect: to.fullPath } }
  const allowedRoles = to.meta.allowedRoles as UserRole[] | undefined
  if (allowedRoles === undefined || (authStore.role !== null && allowedRoles.includes(authStore.role))) return true
  return { path: authStore.role === null ? '/login' : getDefaultRouteForRole(authStore.role) }
})

router.afterEach((to) => { document.title = `${typeof to.meta.title === 'string' ? `${to.meta.title} | ` : ''}Sistema de Asistencia` })
export default router
