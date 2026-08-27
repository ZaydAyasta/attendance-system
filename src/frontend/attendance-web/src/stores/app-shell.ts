import { ref } from 'vue'
import { defineStore } from 'pinia'

export const useAppShellStore = defineStore('app-shell', () => {
  const mobileNavigationOpen = ref(false)
  function closeMobileNavigation(): void { mobileNavigationOpen.value = false }
  function toggleMobileNavigation(): void { mobileNavigationOpen.value = !mobileNavigationOpen.value }
  return { mobileNavigationOpen, closeMobileNavigation, toggleMobileNavigation }
})
