import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CheckpointsView from './CheckpointsView.vue'

const toastAdd = vi.fn()
const listCheckpointsMock = vi.fn()
const createCheckpointMock = vi.fn()
const updateCheckpointMock = vi.fn()
const setCheckpointStatusMock = vi.fn()
const getCheckpointQrMock = vi.fn()

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: toastAdd }),
}))

vi.mock('@/modules/checkpoints/checkpoints.service', () => ({
  listCheckpoints: (...args: unknown[]) => listCheckpointsMock(...args),
  createCheckpoint: (...args: unknown[]) => createCheckpointMock(...args),
  updateCheckpoint: (...args: unknown[]) => updateCheckpointMock(...args),
  setCheckpointStatus: (...args: unknown[]) => setCheckpointStatusMock(...args),
  getCheckpointQr: (...args: unknown[]) => getCheckpointQrMock(...args),
}))

function mountView() {
  return mount(CheckpointsView, {
    global: {
      stubs: {
        Button: { props: ['label', 'type', 'loading'], emits: ['click'], template: '<button :type="type ?? \'button\'" @click="$emit(\'click\')">{{ label }}</button>' },
        Dialog: { props: ['visible', 'header'], emits: ['update:visible'], template: '<div v-if="visible"><h2>{{ header }}</h2><slot /><slot name="footer" /></div>' },
        DataTable: { template: '<div><slot /></div>' },
        Column: { template: '<div><slot name="body" :data="{}" /></div>' },
        QRCodeVue: { template: '<div />' },
        AppPageHeader: { emits: ['action'], template: '<button @click="$emit(\'action\')">Crear checkpoint</button>' },
        AppLoadingState: { template: '<div />' },
        AppEmptyState: { emits: ['action'], template: '<button @click="$emit(\'action\')">Crea el primer punto de captura</button>' },
        AppErrorState: { template: '<div />' },
      },
    },
  })
}

describe('CheckpointsView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    listCheckpointsMock.mockResolvedValue([])
    createCheckpointMock.mockResolvedValue({})
  })

  it('uses dynamic QR mode by default when creating a checkpoint', async () => {
    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('button').trigger('click')

    expect((wrapper.get('select').element as HTMLSelectElement).value).toBe('EntryExit')
    expect((wrapper.findAll('select')[1].element as HTMLSelectElement).value).toBe('Dynamic')
    expect(wrapper.text()).toContain('El QR se renueva automáticamente')
  })

  it('creates a checkpoint with a static QR mode', async () => {
    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('button').trigger('click')
    await wrapper.findAll('input')[0].setValue('PUERTA-01')
    await wrapper.findAll('input')[1].setValue('Puerta principal')
    await wrapper.findAll('select')[1].setValue('Static')
    expect(wrapper.text()).toContain('Se crea un único QR permanente')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(createCheckpointMock).toHaveBeenCalledWith({
      code: 'PUERTA-01',
      name: 'Puerta principal',
      type: 'EntryExit',
      qrMode: 'Static',
    })
  })

  it('requires a static QR when creating a general checkpoint', async () => {
    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('button').trigger('click')
    await wrapper.get('select').setValue('General')

    const qrMode = wrapper.findAll('select')[1].element as HTMLSelectElement
    expect(qrMode.value).toBe('Static')
    expect(qrMode.disabled).toBe(true)
    expect(wrapper.text()).toContain('un único QR estático compartido')
  })
})
