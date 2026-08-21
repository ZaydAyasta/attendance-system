<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import { useAppShellStore } from '@/stores/app-shell'

const router = useRouter()
const appShellStore = useAppShellStore()

const quickLinks = computed(() =>
  appShellStore.visibleNavigationItems.filter((item) => item.route !== '/').slice(0, 6),
)

const taskDescriptionsByRoute: Record<string, string> = {
  '/attendance': 'Revisar la asistencia registrada de cada día.',
  '/absences': 'Consultar ausencias autorizadas y su estado.',
  '/work-calendar': 'Actualizar el calendario laboral cuando sea necesario.',
  '/work-assignments': 'Revisar asignaciones excepcionales por empleado.',
  '/employees': 'Consultar información de empleados para la gestión diaria.',
  '/reports': 'Preparar reportes para revisión administrativa.',
  '/system': 'Revisar la configuración y el estado general del sistema.',
  '/checkpoints': 'Supervisar checkpoints y diagnóstico operativo.',
}

const frequentTasks = computed(() => {
  const visibleItems = appShellStore.visibleNavigationItems.filter((item) => item.route !== '/')
  const technicalItems = visibleItems.filter(
    (item) => item.route === '/system' || item.route === '/checkpoints',
  )
  const sourceItems = technicalItems.length > 0 ? technicalItems : visibleItems

  return sourceItems
    .map((item) => taskDescriptionsByRoute[item.route])
    .filter((description): description is string => Boolean(description))
})

function goToAttendance(): void {
  void router.push('/attendance')
}
</script>

<template>
  <section>
    <AppPageHeader
      title="Resumen"
      description="Accede rápidamente a las tareas principales."
      action-label="Ver asistencia"
      action-icon="pi pi-clock"
      @action="goToAttendance"
    />

    <div class="app-grid app-grid--two">
      <article class="app-surface">
        <h2 class="app-surface__title">Accesos rápidos</h2>
        <p class="app-surface__description">
          Elige la sección que necesitas para continuar.
        </p>

        <div class="app-card-grid" style="margin-top: 1rem">
          <RouterLink
            v-for="item in quickLinks"
            :key="item.route"
            :to="item.route"
            class="app-card-link"
          >
            <i :class="[item.icon, 'app-card-link__icon']" aria-hidden="true"></i>
            <h3 class="app-card-link__title">{{ item.label }}</h3>
            <p class="app-card-link__description">{{ item.description }}</p>
            <span class="app-card-link__footer">
              <span>Ir a la sección</span>
              <i class="pi pi-arrow-right" aria-hidden="true"></i>
            </span>
          </RouterLink>
        </div>
      </article>

      <article class="app-surface">
        <h2 class="app-surface__title">Tareas frecuentes</h2>
        <ul class="app-feature-list">
          <li v-for="task in frequentTasks" :key="task">
            <i class="pi pi-check-circle" aria-hidden="true"></i>
            <span>{{ task }}</span>
          </li>
        </ul>
      </article>
    </div>
  </section>
</template>
