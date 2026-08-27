<script setup lang="ts">
import { storeToRefs } from 'pinia'
import Button from 'primevue/button'
import Drawer from 'primevue/drawer'
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppNavigationMenu from '@/components/app/AppNavigationMenu.vue'
import { filterNavigationItems } from '@/config/navigation'
import { useAppShellStore } from '@/stores/app-shell'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const appShellStore = useAppShellStore()
const authStore = useAuthStore()
const { mobileNavigationOpen } = storeToRefs(appShellStore)
const compactNavigation = ref(false)
const visibleNavigationItems = computed(() => authStore.role ? filterNavigationItems(authStore.role) : [])
const currentPageTitle = computed(() => typeof route.meta.title === 'string' ? route.meta.title : 'Sistema de Asistencia')

let compactNavigationQuery: MediaQueryList | null = null
function updateCompactNavigation(event?: MediaQueryList | MediaQueryListEvent): void {
  compactNavigation.value = event?.matches ?? compactNavigationQuery?.matches ?? false
}
async function logout(): Promise<void> {
  await authStore.logout()
  await router.replace('/login')
}

watch(() => route.fullPath, () => appShellStore.closeMobileNavigation())
onMounted(() => {
  compactNavigationQuery = window.matchMedia('(max-width: 960px)')
  updateCompactNavigation(compactNavigationQuery)
  compactNavigationQuery.addEventListener('change', updateCompactNavigation)
})
onBeforeUnmount(() => compactNavigationQuery?.removeEventListener('change', updateCompactNavigation))
</script>

<template>
  <div class="app-shell">
    <div class="app-shell__body">
      <aside class="app-shell__sidebar" aria-label="Navegación principal"><div class="app-shell__sidebar-inner"><div class="app-shell__brand"><strong class="app-shell__brand-title">Sistema de Asistencia</strong><p class="app-shell__brand-copy">Consulta asistencia, ausencias, calendario laboral y asignaciones.</p></div><nav aria-label="Secciones del sistema"><AppNavigationMenu :items="visibleNavigationItems" :current-route-path="route.path" data-testid="sidebar-navigation" /></nav></div></aside>
      <main class="app-shell__main"><header class="app-shell__topbar"><div class="app-shell__topbar-actions"><Button v-if="compactNavigation" class="app-mobile-only" icon="pi pi-bars" label="Menú" severity="secondary" data-testid="menu-button" @click="appShellStore.toggleMobileNavigation()" /><div class="app-shell__topbar-title"><p>Sistema de Asistencia</p><strong>{{ currentPageTitle }}</strong></div></div><div class="app-shell__session"><span>{{ authStore.user?.username }}</span><Button label="Cerrar sesión" text @click="logout" /></div></header><div class="app-shell__content"><RouterView /></div></main>
    </div>
    <Drawer v-if="compactNavigation" v-model:visible="mobileNavigationOpen" header="Navegación" position="left" class="app-mobile-only"><nav aria-label="Secciones del sistema"><AppNavigationMenu :items="visibleNavigationItems" :current-route-path="route.path" /></nav><Button label="Cerrar sesión" text class="app-shell__drawer-logout" @click="logout" /></Drawer>
  </div>
</template>
