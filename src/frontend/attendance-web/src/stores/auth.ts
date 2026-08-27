import axios from 'axios'
import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { getCurrentUser, login as loginRequest, logout as logoutRequest, type AuthRole, type AuthUser } from '@/modules/auth/auth.service'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
  const initialized = ref(false)
  const loading = ref(false)
  const isAuthenticated = computed(() => user.value !== null)
  const role = computed<AuthRole | null>(() => user.value?.role ?? null)

  async function initialize(): Promise<void> {
    if (initialized.value) return
    loading.value = true
    try {
      user.value = await getCurrentUser()
    } catch (error) {
      if (!axios.isAxiosError(error) || error.response?.status !== 401) throw error
      user.value = null
    } finally {
      initialized.value = true
      loading.value = false
    }
  }

  async function login(usernameOrEmail: string, password: string): Promise<void> {
    loading.value = true
    try {
      user.value = await loginRequest(usernameOrEmail, password)
      initialized.value = true
    } finally {
      loading.value = false
    }
  }

  async function logout(): Promise<void> {
    try {
      await logoutRequest()
    } finally {
      user.value = null
      initialized.value = true
    }
  }

  function clearSession(): void {
    user.value = null
    initialized.value = true
  }

  return { user, initialized, loading, isAuthenticated, role, initialize, login, logout, clearSession }
})
