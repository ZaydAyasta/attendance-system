import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useAuthStore } from '@/stores/auth'
import LoginView from './LoginView.vue'

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/login', component: LoginView },
      { path: '/', component: { template: '<main>Inicio</main>' } },
    ],
  })
}

async function mountLoginView() {
  const router = createTestRouter()
  await router.push('/login')
  await router.isReady()
  return mount(LoginView, {
    global: {
      plugins: [router],
      stubs: {
        Button: {
          props: ['label', 'type', 'loading', 'disabled'],
          template: '<button :type="type" :disabled="disabled"><slot />{{ label }}</button>',
        },
      },
    },
  })
}

describe('LoginView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('keeps remember me enabled by default and sends it with the login request', async () => {
    const store = useAuthStore()
    const login = vi.spyOn(store, 'login').mockResolvedValue()
    const wrapper = await mountLoginView()

    await wrapper.get('input[autocomplete="username"]').setValue('admin')
    await wrapper.get('input[autocomplete="current-password"]').setValue('Password1')
    expect((wrapper.get('input[type="checkbox"]').element as HTMLInputElement).checked).toBe(true)
    await wrapper.get('form').trigger('submit.prevent')

    expect(login).toHaveBeenCalledWith('admin', 'Password1', true)
  })

  it('explains when the server rate limits repeated login attempts', async () => {
    const store = useAuthStore()
    vi.spyOn(store, 'login').mockRejectedValue({ isAxiosError: true, response: { status: 429 } })
    const wrapper = await mountLoginView()

    await wrapper.get('form').trigger('submit.prevent')

    expect(wrapper.text()).toContain('Demasiados intentos. Espera un minuto antes de volver a intentarlo.')
  })
})
