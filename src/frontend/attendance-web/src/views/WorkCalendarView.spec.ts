import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import WorkCalendarView from './WorkCalendarView.vue'

const toastAdd = vi.fn()
const listWorkCalendarDaysMock = vi.fn()
const createWorkCalendarDayMock = vi.fn()
const updateWorkCalendarDayMock = vi.fn()
const deleteWorkCalendarDayMock = vi.fn()

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: toastAdd,
  }),
}))

vi.mock('@/modules/work-calendar/work-calendar.service', () => ({
  listWorkCalendarDays: (...args: unknown[]) => listWorkCalendarDaysMock(...args),
  createWorkCalendarDay: (...args: unknown[]) => createWorkCalendarDayMock(...args),
  updateWorkCalendarDay: (...args: unknown[]) => updateWorkCalendarDayMock(...args),
  deleteWorkCalendarDay: (...args: unknown[]) => deleteWorkCalendarDayMock(...args),
}))

function createAxiosConflictError() {
  return {
    isAxiosError: true,
    response: {
      status: 409,
      data: {},
    },
  }
}

function deferredPromise<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((innerResolve, innerReject) => {
    resolve = innerResolve
    reject = innerReject
  })

  return {
    promise,
    resolve,
    reject,
  }
}

function mountView() {
  return mount(WorkCalendarView, {
    global: {
      stubs: {
        Button: {
          props: ['label', 'loading', 'severity', 'text', 'disabled', 'type', 'icon', 'ariaLabel'],
          emits: ['click'],
          template:
            '<button :type="type ?? \'button\'" :disabled="disabled" v-bind="$attrs" @click="$emit(\'click\')"><slot />{{ label }}</button>',
        },
        Dialog: {
          props: ['visible', 'header'],
          emits: ['update:visible'],
          template:
            '<div v-if="visible" class="dialog-stub"><h2>{{ header }}</h2><slot /><slot name="footer" /></div>',
        },
        Message: {
          template: '<div class="message-stub"><slot /></div>',
        },
        DataTable: {
          template: '<div class="datatable-stub"><slot /></div>',
        },
        Column: {
          template: '<div class="column-stub"></div>',
        },
        Tag: {
          template: '<span class="tag-stub"><slot /></span>',
        },
        ProgressSpinner: {
          template: '<div>Cargando</div>',
        },
      },
    },
  })
}

function getButtonByText(wrapper: ReturnType<typeof mount>, text: string) {
  const button = wrapper.findAll('button').find((item) => item.text().includes(text))

  if (!button) {
    throw new Error(`Button with text "${text}" was not found.`)
  }

  return button
}

describe('WorkCalendarView', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 7, 21))
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('loads the current month range on mount', async () => {
    listWorkCalendarDaysMock.mockResolvedValue([])

    mountView()
    await flushPromises()

    expect(listWorkCalendarDaysMock).toHaveBeenCalledWith({
      from: '2026-08-01',
      to: '2026-08-31',
    })
  })

  it('loads the next month when navigating forward', async () => {
    listWorkCalendarDaysMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('[data-testid="work-calendar-next-month"]').trigger('click')
    await flushPromises()

    expect(listWorkCalendarDaysMock).toHaveBeenNthCalledWith(2, {
      from: '2026-09-01',
      to: '2026-09-30',
    })
  })

  it('loads the previous month when navigating backward', async () => {
    listWorkCalendarDaysMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('[data-testid="work-calendar-previous-month"]').trigger('click')
    await flushPromises()

    expect(listWorkCalendarDaysMock).toHaveBeenNthCalledWith(2, {
      from: '2026-07-01',
      to: '2026-07-31',
    })
  })

  it('shows the monthly loading state while the month is loading', async () => {
    const deferred = deferredPromise<unknown[]>()
    listWorkCalendarDaysMock.mockReturnValue(deferred.promise)

    const wrapper = mountView()
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('Cargando calendario laboral...')

    deferred.resolve([])
    await flushPromises()
  })

  it('shows unconfigured days without hiding the calendar', async () => {
    listWorkCalendarDaysMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.get('[data-testid="work-calendar-month-label"]').text()).toContain('agosto de 2026')
    expect(wrapper.get('[data-testid="work-calendar-day-2026-08-21"]').text()).toContain(
      'Sin configurar',
    )
    expect(wrapper.text()).not.toContain('No hay días configurados para este período.')
  })

  it('opens create mode with the selected date when clicking an unconfigured day', async () => {
    listWorkCalendarDaysMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('[data-testid="work-calendar-day-2026-08-27"]').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Agregar día')
    expect(wrapper.get('[data-testid="work-calendar-date-display"]').text()).toContain(
      'jueves, 27 de agosto de 2026',
    )
  })

  it('opens edit mode when clicking a configured day', async () => {
    listWorkCalendarDaysMock.mockResolvedValue([
      {
        date: '2026-08-21',
        dayType: 'WorkingDay',
        description: 'Jornada regular',
        version: 3,
      },
    ])

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('[data-testid="work-calendar-day-2026-08-21"]').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Editar día')
    expect(wrapper.text()).toContain('Eliminar día')
    expect(wrapper.get('[data-testid="work-calendar-date-display"]').text()).toContain(
      'viernes, 21 de agosto de 2026',
    )
  })

  it('renders human labels for the day types', async () => {
    listWorkCalendarDaysMock.mockResolvedValue([
      {
        date: '2026-08-21',
        dayType: 'WorkingDay',
        description: null,
        version: 1,
      },
      {
        date: '2026-08-22',
        dayType: 'NonWorkingDay',
        description: null,
        version: 2,
      },
      {
        date: '2026-08-23',
        dayType: 'Holiday',
        description: null,
        version: 3,
      },
    ])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Laborable')
    expect(wrapper.text()).toContain('No laborable')
    expect(wrapper.text()).toContain('Feriado')
  })

  it('keeps the monthly calendar visible when GET returns 200 with no records', async () => {
    listWorkCalendarDaysMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.find('[data-testid="work-calendar-day-2026-08-01"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('Haz clic en una fecha para registrarla.')
  })

  it('shows a real backend error without presenting the month as configured', async () => {
    listWorkCalendarDaysMock.mockRejectedValue(new Error('boom'))

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('No pudimos cargar la configuración de este mes.')
    expect(wrapper.text()).toContain('Volver a intentar')
  })

  it('creates a day and refreshes the visible month', async () => {
    listWorkCalendarDaysMock
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([
        {
          date: '2026-08-27',
          dayType: 'Holiday',
          description: 'Feriado local',
          version: 1,
        },
      ])
    createWorkCalendarDayMock.mockResolvedValue({
      date: '2026-08-27',
      dayType: 'Holiday',
      description: 'Feriado local',
      version: 1,
    })

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('[data-testid="work-calendar-day-2026-08-27"]').trigger('click')
    await wrapper.get('#work-calendar-day-type').setValue('Holiday')
    await wrapper.get('#work-calendar-description').setValue('Feriado local')
    await getButtonByText(wrapper, 'Guardar día').trigger('click')
    await flushPromises()

    expect(createWorkCalendarDayMock).toHaveBeenCalledWith({
      date: '2026-08-27',
      dayType: 'Holiday',
      description: 'Feriado local',
    })
    expect(listWorkCalendarDaysMock).toHaveBeenCalledTimes(2)
    expect(toastAdd).toHaveBeenCalled()
  })

  it('updates a configured day with the real version and refreshes the month', async () => {
    listWorkCalendarDaysMock
      .mockResolvedValueOnce([
        {
          date: '2026-08-21',
          dayType: 'WorkingDay',
          description: 'Jornada regular',
          version: 12,
        },
      ])
      .mockResolvedValueOnce([
        {
          date: '2026-08-21',
          dayType: 'Holiday',
          description: 'Actualizado',
          version: 13,
        },
      ])
    updateWorkCalendarDayMock.mockResolvedValue({
      date: '2026-08-21',
      dayType: 'Holiday',
      description: 'Actualizado',
      version: 13,
    })

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('[data-testid="work-calendar-day-2026-08-21"]').trigger('click')
    await wrapper.get('#work-calendar-day-type').setValue('Holiday')
    await wrapper.get('#work-calendar-description').setValue('Actualizado')
    await getButtonByText(wrapper, 'Guardar cambios').trigger('click')
    await flushPromises()

    expect(updateWorkCalendarDayMock).toHaveBeenCalledWith('2026-08-21', {
      dayType: 'Holiday',
      description: 'Actualizado',
      version: 12,
    })
    expect(listWorkCalendarDaysMock).toHaveBeenCalledTimes(2)
  })

  it('shows a human concurrency message on 409', async () => {
    listWorkCalendarDaysMock.mockResolvedValue([
      {
        date: '2026-08-21',
        dayType: 'WorkingDay',
        description: 'Jornada regular',
        version: 12,
      },
    ])
    updateWorkCalendarDayMock.mockRejectedValue(createAxiosConflictError())

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('[data-testid="work-calendar-day-2026-08-21"]').trigger('click')
    await wrapper.get('#work-calendar-day-type').setValue('Holiday')
    await getButtonByText(wrapper, 'Guardar cambios').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain(
      'Este día fue modificado por otra persona. Actualiza la información e inténtalo nuevamente.',
    )
  })

  it('deletes a configured day and refreshes the month', async () => {
    listWorkCalendarDaysMock
      .mockResolvedValueOnce([
        {
          date: '2026-08-21',
          dayType: 'WorkingDay',
          description: 'Jornada regular',
          version: 4,
        },
      ])
      .mockResolvedValueOnce([])
    deleteWorkCalendarDayMock.mockResolvedValue(undefined)

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('[data-testid="work-calendar-day-2026-08-21"]').trigger('click')
    await getButtonByText(wrapper, 'Eliminar día').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Eliminar configuración del día')

    await getButtonByText(wrapper, 'Eliminar día').trigger('click')
    await flushPromises()

    expect(deleteWorkCalendarDayMock).toHaveBeenCalledWith('2026-08-21')
    expect(listWorkCalendarDaysMock).toHaveBeenCalledTimes(2)
  })

  it('does not depend on the table in the default mobile-friendly calendar view', async () => {
    listWorkCalendarDaysMock.mockResolvedValue([])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.find('.datatable-stub').exists()).toBe(false)
    expect(wrapper.find('[data-testid="work-calendar-day-2026-08-21"]').exists()).toBe(true)
  })

  it('keeps the secondary list view working with the same service and dialog flow', async () => {
    listWorkCalendarDaysMock
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([
        {
          date: '2026-08-24',
          dayType: 'WorkingDay',
          description: 'Jornada de apoyo',
          version: 8,
        },
      ])

    const wrapper = mountView()
    await flushPromises()

    await wrapper.get('[data-testid="work-calendar-view-list"]').trigger('click')
    await flushPromises()

    expect(listWorkCalendarDaysMock).toHaveBeenNthCalledWith(2, {
      from: '2026-08-01',
      to: '2026-08-31',
    })
    expect(wrapper.text()).toContain('Jornada de apoyo')
  })

  it('does not display Version to the user', async () => {
    listWorkCalendarDaysMock.mockResolvedValue([
      {
        date: '2026-08-21',
        dayType: 'WorkingDay',
        description: null,
        version: 99,
      },
    ])

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).not.toContain('Version')
    expect(wrapper.text()).not.toContain('Versión')
  })
})
