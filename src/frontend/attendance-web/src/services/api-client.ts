import axios from 'axios'
import { appEnv } from '@/config/env'

export const apiClient = axios.create({
  baseURL: appEnv.VITE_API_BASE_URL,
  timeout: 10000,
  withCredentials: true,
  headers: {
    Accept: 'application/json',
    'Content-Type': 'application/json',
  },
})

let antiforgeryToken: string | null = null
let redirectInProgress = false

export async function ensureAntiforgeryToken(): Promise<void> {
  if (antiforgeryToken) return
  const response = await axios.get<{ token: string }>(`${appEnv.VITE_API_BASE_URL}/auth/csrf`, {
    withCredentials: true,
  })
  antiforgeryToken = response.data.token
}

export function clearAntiforgeryToken(): void {
  antiforgeryToken = null
}

apiClient.interceptors.request.use(async (config) => {
  const method = config.method?.toLowerCase()
  if (method && !['get', 'head', 'options'].includes(method)) {
    await ensureAntiforgeryToken()
    config.headers.set('X-CSRF-TOKEN', antiforgeryToken)
  }
  return config
})

async function redirectAfterAuthorizationFailure(status: number): Promise<void> {
  if (redirectInProgress) return
  redirectInProgress = true

  try {
    const [{ default: router }, { useAuthStore }, { getDefaultRouteForRole }] = await Promise.all([
      import('@/router'),
      import('@/stores/auth'),
      import('@/config/navigation'),
    ])
    const authStore = useAuthStore()
    const currentPath = router.currentRoute.value.fullPath

    if (status === 401) {
      clearAntiforgeryToken()
      authStore.clearSession()
      if (router.currentRoute.value.path !== '/login') {
        await router.replace({ path: '/login', query: { redirect: currentPath } })
      }
      return
    }

    const destination = authStore.role === null ? '/login' : getDefaultRouteForRole(authStore.role)
    if (router.currentRoute.value.path !== destination) await router.replace(destination)
  } finally {
    redirectInProgress = false
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && (error.response?.status === 401 || error.response?.status === 403)) {
      void redirectAfterAuthorizationFailure(error.response.status)
    }
    return Promise.reject(error)
  },
)

export default apiClient
