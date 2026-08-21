import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { beforeEach, describe, expect, it } from 'vitest'
import { useAppShellStore } from '@/stores/app-shell'
import HomeView from './HomeView.vue'

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/',
        component: HomeView,
      },
    ],
  })
}

async function mountHomeView() {
  const router = createTestRouter()

  await router.push('/')
  await router.isReady()

  return mount(HomeView, {
    global: {
      plugins: [router],
      stubs: {
        AppPageHeader: {
          props: ['title', 'description', 'actionLabel', 'actionIcon'],
          template: '<div><h1>{{ title }}</h1><p>{{ description }}</p></div>',
        },
      },
    },
  })
}

describe('HomeView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('shows only user tasks for the user role', async () => {
    const store = useAppShellStore()
    store.setCurrentRole('user')

    const wrapper = await mountHomeView()
    const text = wrapper.text()

    expect(text).toContain('Revisar la asistencia registrada de cada día.')
    expect(text).toContain('Consultar ausencias autorizadas y su estado.')
    expect(text).not.toContain('calendario laboral')
    expect(text).not.toContain('asignaciones excepcionales')
    expect(text).not.toContain('empleados')
    expect(text).not.toContain('reportes')
  })

  it('shows administrative tasks for the admin role', async () => {
    const store = useAppShellStore()
    store.setCurrentRole('admin')

    const wrapper = await mountHomeView()
    const text = wrapper.text()

    expect(text).toContain('Actualizar el calendario laboral cuando sea necesario.')
    expect(text).toContain('Revisar asignaciones excepcionales por empleado.')
    expect(text).toContain('Consultar información de empleados para la gestión diaria.')
    expect(text).toContain('Preparar reportes para revisión administrativa.')
  })

  it('shows only technical tasks for the it role', async () => {
    const store = useAppShellStore()
    store.setCurrentRole('it')

    const wrapper = await mountHomeView()
    const text = wrapper.text()

    expect(text).toContain('Revisar la configuración y el estado general del sistema.')
    expect(text).toContain('Supervisar checkpoints y diagnóstico operativo.')
    expect(text).not.toContain('Revisar la asistencia registrada de cada día.')
    expect(text).not.toContain('Consultar ausencias autorizadas y su estado.')
  })
})
