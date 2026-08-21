import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { filterNavigationItems } from '@/config/navigation'
import type { RoleOption, UserRole } from '@/types/user-role'

export const useAppShellStore = defineStore('app-shell', () => {
  const currentRole = ref<UserRole>('admin')
  const mobileNavigationOpen = ref(false)
  const roleOptions = ref<RoleOption[]>([
    { label: 'Administrador', value: 'admin' },
    { label: 'Usuario', value: 'user' },
    { label: 'TI', value: 'it' },
  ])

  const visibleNavigationItems = computed(() => filterNavigationItems(currentRole.value))

  function setCurrentRole(role: UserRole): void {
    currentRole.value = role
  }

  function openMobileNavigation(): void {
    mobileNavigationOpen.value = true
  }

  function closeMobileNavigation(): void {
    mobileNavigationOpen.value = false
  }

  function toggleMobileNavigation(): void {
    mobileNavigationOpen.value = !mobileNavigationOpen.value
  }

  return {
    currentRole,
    mobileNavigationOpen,
    roleOptions,
    visibleNavigationItems,
    closeMobileNavigation,
    openMobileNavigation,
    setCurrentRole,
    toggleMobileNavigation,
  }
})
