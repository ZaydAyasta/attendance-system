import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { getDefaultRouteForRole } from '@/config/navigation'
import type { UserRole } from '@/types/user-role'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: () => import('@/views/LoginView.vue'), meta: { public: true, title: 'Iniciar sesión' } },
    { path: '/', component: () => import('@/layouts/AppLayout.vue'), children: [
      { path: 'access-denied', name: 'access-denied', component: () => import('@/views/AccessDeniedView.vue'), meta: { title: 'Acceso denegado' } },
      { path: '', name: 'home', component: () => import('@/views/HomeView.vue'), meta: { title: 'Resumen', allowedRoles: ['Admin'] } },
      { path: 'attendance', name: 'attendance', component: () => import('@/views/AttendanceView.vue'), meta: { title: 'Asistencia', allowedRoles: ['Admin', 'User'] } },
      { path: 'mark', name: 'mark', component: () => import('@/views/MarkView.vue'), meta: { title: 'Marcar asistencia', allowedRoles: ['User'] } },
      { path: 'absences', name: 'absences', component: () => import('@/views/AbsencesView.vue'), meta: { title: 'Ausencias', allowedRoles: ['Admin', 'User'] } },
      { path: 'work-calendar', name: 'work-calendar', component: () => import('@/views/WorkCalendarView.vue'), meta: { title: 'Calendario laboral', allowedRoles: ['Admin'] } },
      { path: 'work-assignments', name: 'work-assignments', component: () => import('@/views/WorkAssignmentsView.vue'), meta: { title: 'Asignaciones', allowedRoles: ['Admin'] } },
      { path: 'employees', name: 'employees', component: () => import('@/views/EmployeesView.vue'), meta: { title: 'Empleados', allowedRoles: ['Admin'] } },
      { path: 'users', name: 'users', component: () => import('@/views/UsersView.vue'), meta: { title: 'Usuarios del sistema', allowedRoles: ['Admin'] } },
      { path: 'reports', name: 'reports', component: () => import('@/views/ReportsView.vue'), meta: { title: 'Reportes', allowedRoles: ['Admin'] } },
      { path: 'audit', name: 'audit', component: () => import('@/views/AuditView.vue'), meta: { title: 'Auditoría', allowedRoles: ['Admin'] } },
      { path: 'system', name: 'system', component: () => import('@/views/SystemView.vue'), meta: { title: 'Sistema', allowedRoles: ['IT'] } },
      { path: 'checkpoints', name: 'checkpoints', component: () => import('@/views/CheckpointsView.vue'), meta: { title: 'Checkpoints', allowedRoles: ['IT'] } },
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
