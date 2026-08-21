<script setup lang="ts">
import { storeToRefs } from 'pinia'
import Button from 'primevue/button'
import Drawer from 'primevue/drawer'
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import AppNavigationMenu from '@/components/app/AppNavigationMenu.vue'
import { useAppShellStore } from '@/stores/app-shell'

const route = useRoute()
const appShellStore = useAppShellStore()
const { currentRole, mobileNavigationOpen, roleOptions, visibleNavigationItems } =
  storeToRefs(appShellStore)
const compactNavigation = ref(false)

let compactNavigationQuery: MediaQueryList | null = null

function updateCompactNavigation(event?: MediaQueryList | MediaQueryListEvent): void {
  compactNavigation.value = event?.matches ?? compactNavigationQuery?.matches ?? false
}

const currentPageTitle = computed(() => {
  if (typeof route.meta.title === 'string') {
    return route.meta.title
  }

  return 'Sistema de Asistencia'
})

watch(
  () => route.fullPath,
  () => {
    appShellStore.closeMobileNavigation()
  },
)

onMounted(() => {
  compactNavigationQuery = window.matchMedia('(max-width: 960px)')
  updateCompactNavigation(compactNavigationQuery)
  compactNavigationQuery.addEventListener('change', updateCompactNavigation)
})

onBeforeUnmount(() => {
  compactNavigationQuery?.removeEventListener('change', updateCompactNavigation)
})
</script>

<template>
  <div class="app-shell">
    <div class="app-shell__body">
      <aside class="app-shell__sidebar" aria-label="Navegación principal">
        <div class="app-shell__sidebar-inner">
          <div class="app-shell__brand">
            <strong class="app-shell__brand-title">Sistema de Asistencia</strong>
            <p class="app-shell__brand-copy">
              Consulta asistencia, ausencias, calendario laboral y asignaciones.
            </p>
          </div>

          <nav aria-label="Secciones del sistema">
            <AppNavigationMenu
              :items="visibleNavigationItems"
              :current-route-path="route.path"
              data-testid="sidebar-navigation"
            />
          </nav>

          <p class="app-shell__role-note">
            <strong>Vista provisional por rol</strong>
            Sirve sólo para diseño y navegación. No reemplaza autorización real.
          </p>
        </div>
      </aside>

      <main class="app-shell__main">
        <header class="app-shell__topbar">
          <div class="app-shell__topbar-actions">
            <Button
              v-if="compactNavigation"
              class="app-mobile-only"
              icon="pi pi-bars"
              label="Menú"
              severity="secondary"
              data-testid="menu-button"
              @click="appShellStore.toggleMobileNavigation()"
            />

            <div class="app-shell__topbar-title">
              <p>Sistema de Asistencia</p>
              <strong>{{ currentPageTitle }}</strong>
            </div>
          </div>

          <label v-if="!compactNavigation" class="app-shell__role-switch app-desktop-only">
            <span class="app-shell__role-switch-label">Vista provisional por rol</span>
            <select
              v-model="currentRole"
              class="app-shell__role-select"
              data-testid="role-select-desktop"
            >
              <option v-for="role in roleOptions" :key="role.value" :value="role.value">
                {{ role.label }}
              </option>
            </select>
          </label>
        </header>

        <div class="app-shell__content">
          <RouterView />
        </div>
      </main>
    </div>

    <Drawer
      v-if="compactNavigation"
      v-model:visible="mobileNavigationOpen"
      header="Navegación"
      position="left"
      class="app-mobile-only"
    >
      <nav aria-label="Secciones del sistema">
        <AppNavigationMenu
          :items="visibleNavigationItems"
          :current-route-path="route.path"
          data-testid="drawer-navigation"
        />
      </nav>
      <label class="app-shell__role-switch app-shell__role-switch--drawer">
        <span class="app-shell__role-switch-label">Vista provisional por rol</span>
        <select
          v-model="currentRole"
          class="app-shell__role-select"
          data-testid="role-select-drawer"
        >
          <option v-for="role in roleOptions" :key="role.value" :value="role.value">
            {{ role.label }}
          </option>
        </select>
      </label>
      <p class="app-shell__role-note" style="margin-top: 1rem">
        Vista provisional. La autorización real se implementará en una etapa posterior.
      </p>
    </Drawer>
  </div>
</template>
