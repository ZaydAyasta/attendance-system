import { mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import AppLayout from './AppLayout.vue'

const PlaceholderView = {
  template: '<div>Vista de prueba</div>',
}

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/',
        component: AppLayout,
        children: [
          {
            path: '',
            component: PlaceholderView,
            meta: {
              title: 'Resumen',
            },
          },
          {
            path: 'attendance',
            component: PlaceholderView,
          },
          {
            path: 'absences',
            component: PlaceholderView,
          },
          {
            path: 'work-calendar',
            component: PlaceholderView,
          },
          {
            path: 'work-assignments',
            component: PlaceholderView,
          },
          {
            path: 'employees',
            component: PlaceholderView,
          },
          {
            path: 'reports',
            component: PlaceholderView,
          },
          {
            path: 'system',
            component: PlaceholderView,
          },
          {
            path: 'checkpoints',
            component: PlaceholderView,
          },
        ],
      },
    ],
  })
}

describe('AppLayout', () => {
  function setMatchMedia(matches: boolean) {
    const listeners = new Set<(event: MediaQueryListEvent) => void>()

    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: vi.fn().mockImplementation(() => ({
        matches,
        media: '(max-width: 960px)',
        addEventListener: (_event: string, listener: (event: MediaQueryListEvent) => void) => {
          listeners.add(listener)
        },
        removeEventListener: (_event: string, listener: (event: MediaQueryListEvent) => void) => {
          listeners.delete(listener)
        },
      })),
    })

    return {
      emit(nextMatches: boolean) {
        for (const listener of listeners) {
          listener({ matches: nextMatches } as MediaQueryListEvent)
        }
      },
    }
  }

  beforeEach(() => {
    setMatchMedia(false)
  })

  afterEach(() => {
    document.body.innerHTML = ''
  })

  it('keeps the menu button hidden on desktop and updates navigation immediately when the role changes', async () => {
    const router = createTestRouter()
    const pinia = createPinia()

    await router.push('/')
    await router.isReady()

    const wrapper = mount(AppLayout, {
      attachTo: document.body,
      global: {
        plugins: [pinia, router],
        stubs: {
          Button: {
            props: ['icon', 'label', 'severity'],
            emits: ['click'],
            template:
              '<button type="button" v-bind="$attrs" @click="$emit(\'click\')">{{ label }}</button>',
          },
          Drawer: {
            props: ['visible', 'header', 'position'],
            template: '<div v-if="visible" class="drawer-stub"><slot /></div>',
          },
        },
      },
    })

    const sidebarText = () => wrapper.find('[data-testid="sidebar-navigation"]').text()

    expect(wrapper.find('[data-testid="menu-button"]').exists()).toBe(false)
    expect(wrapper.get('[data-testid="role-select-desktop"]').text()).toContain('Administrador')
    expect(wrapper.get('[data-testid="role-select-desktop"]').text()).toContain('Usuario')
    expect(wrapper.get('[data-testid="role-select-desktop"]').text()).toContain('TI')

    expect(sidebarText()).toContain('Calendario laboral')
    expect(sidebarText()).not.toContain('Sistema')

    await wrapper.get('[data-testid="role-select-desktop"]').setValue('it')

    expect(sidebarText()).toContain('Sistema')
    expect(sidebarText()).toContain('Checkpoints')
    expect(sidebarText()).not.toContain('Calendario laboral')
    expect(sidebarText()).not.toContain('Asignaciones')
  })

  it('shows the menu button only when compact navigation is active', async () => {
    const media = setMatchMedia(true)
    const router = createTestRouter()
    const pinia = createPinia()

    await router.push('/')
    await router.isReady()

    const wrapper = mount(AppLayout, {
      attachTo: document.body,
      global: {
        plugins: [pinia, router],
        stubs: {
          Button: {
            props: ['icon', 'label', 'severity'],
            emits: ['click'],
            template:
              '<button type="button" v-bind="$attrs" @click="$emit(\'click\')">{{ label }}</button>',
          },
          Drawer: {
            props: ['visible', 'header', 'position'],
            template: '<div v-if="visible" class="drawer-stub"><slot /></div>',
          },
        },
      },
    })

    await wrapper.vm.$nextTick()

    expect(wrapper.find('[data-testid="menu-button"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="role-select-desktop"]').exists()).toBe(false)

    media.emit(false)
    await wrapper.vm.$nextTick()

    expect(wrapper.find('[data-testid="menu-button"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="role-select-desktop"]').exists()).toBe(true)
  })
})
