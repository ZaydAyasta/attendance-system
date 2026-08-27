import { mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useAuthStore } from '@/stores/auth'
import AppLayout from './AppLayout.vue'

const PlaceholderView = { template: '<div>Vista de prueba</div>' }
function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: AppLayout, children: [{ path: '', component: PlaceholderView, meta: { title: 'Resumen' } }] }],
  })
}
function setMatchMedia(matches: boolean) { const listeners = new Set<(event: MediaQueryListEvent) => void>(); Object.defineProperty(window, 'matchMedia', { writable: true, value: vi.fn().mockImplementation(() => ({ matches, media: '(max-width: 960px)', addEventListener: (_event: string, listener: (event: MediaQueryListEvent) => void) => listeners.add(listener), removeEventListener: (_event: string, listener: (event: MediaQueryListEvent) => void) => listeners.delete(listener) })) }); return { emit: (next: boolean) => listeners.forEach((listener) => listener({ matches: next } as MediaQueryListEvent)) } }
async function mountLayout(role: 'Admin' | 'User' | 'IT') { const router = createTestRouter(); const pinia = createPinia(); useAuthStore(pinia).user = { id: '1', username: 'tester', role, employeeId: null, employee: null }; await router.push('/'); await router.isReady(); return mount(AppLayout, { attachTo: document.body, global: { plugins: [pinia, router], stubs: { Button: { props: ['icon', 'label', 'severity'], emits: ['click'], template: '<button type="button" v-bind="$attrs" @click="$emit(\'click\')">{{ label }}</button>' }, Drawer: { props: ['visible'], template: '<div v-if="visible"><slot /></div>' } } } }) }

describe('AppLayout', () => {
  beforeEach(() => setMatchMedia(false))
  afterEach(() => { document.body.innerHTML = '' })

  it('derives desktop navigation from the authenticated role without a role selector', async () => {
    const wrapper = await mountLayout('Admin')
    expect(wrapper.find('[data-testid="menu-button"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="role-select-desktop"]').exists()).toBe(false)
    expect(wrapper.get('[data-testid="sidebar-navigation"]').text()).toContain('Calendario laboral')
  })

  it('shows the mobile menu only at the compact breakpoint', async () => {
    const media = setMatchMedia(true)
    const wrapper = await mountLayout('IT')
    await wrapper.vm.$nextTick()
    expect(wrapper.find('[data-testid="menu-button"]').exists()).toBe(true)
    expect(wrapper.get('[data-testid="sidebar-navigation"]').text()).toContain('Sistema')
    expect(wrapper.get('[data-testid="sidebar-navigation"]').text()).not.toContain('Empleados')
    media.emit(false)
    await wrapper.vm.$nextTick()
    expect(wrapper.find('[data-testid="menu-button"]').exists()).toBe(false)
  })
})
