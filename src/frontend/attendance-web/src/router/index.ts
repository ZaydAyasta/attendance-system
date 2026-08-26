import { createRouter, createWebHistory } from 'vue-router'
import AppLayout from '@/layouts/AppLayout.vue'
import AbsencesView from '@/views/AbsencesView.vue'
import AttendanceView from '@/views/AttendanceView.vue'
import CheckpointsView from '@/views/CheckpointsView.vue'
import EmployeesView from '@/views/EmployeesView.vue'
import HomeView from '@/views/HomeView.vue'
import ReportsView from '@/views/ReportsView.vue'
import SystemView from '@/views/SystemView.vue'
import WorkAssignmentsView from '@/views/WorkAssignmentsView.vue'
import WorkCalendarView from '@/views/WorkCalendarView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: AppLayout,
      children: [
        {
          path: '',
          name: 'home',
          component: HomeView,
          meta: {
            title: 'Resumen',
          },
        },
        {
          path: 'attendance',
          name: 'attendance',
          component: AttendanceView,
          meta: {
            title: 'Asistencia',
            allowedRoles: ['admin', 'user'],
          },
        },
        {
          path: 'absences',
          name: 'absences',
          component: AbsencesView,
          meta: {
            title: 'Ausencias',
            allowedRoles: ['admin', 'user'],
          },
        },
        {
          path: 'work-calendar',
          name: 'work-calendar',
          component: WorkCalendarView,
          meta: {
            title: 'Calendario laboral',
            allowedRoles: ['admin'],
          },
        },
        {
          path: 'work-assignments',
          name: 'work-assignments',
          component: WorkAssignmentsView,
          meta: {
            title: 'Asignaciones',
            allowedRoles: ['admin'],
          },
        },
        {
          path: 'employees',
          name: 'employees',
          component: EmployeesView,
          meta: {
            title: 'Empleados',
            allowedRoles: ['admin'],
          },
        },
        {
          path: 'reports',
          name: 'reports',
          component: ReportsView,
          meta: {
            title: 'Reportes',
            allowedRoles: ['admin'],
          },
        },
        {
          path: 'system',
          name: 'system',
          component: SystemView,
          meta: {
            title: 'Sistema',
            allowedRoles: ['it'],
          },
        },
        {
          path: 'checkpoints',
          name: 'checkpoints',
          component: CheckpointsView,
          meta: {
            title: 'Checkpoints',
            allowedRoles: ['it'],
          },
        },
      ],
    },
  ],
})

router.afterEach((to) => {
  const pageTitle = typeof to.meta.title === 'string' ? `${to.meta.title} | ` : ''
  document.title = `${pageTitle}Sistema de Asistencia`
})

export default router
