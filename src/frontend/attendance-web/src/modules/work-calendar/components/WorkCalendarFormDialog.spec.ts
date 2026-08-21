import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import WorkCalendarFormDialog from './WorkCalendarFormDialog.vue'

function mountDialog() {
  return mount(WorkCalendarFormDialog, {
    props: {
      visible: true,
      mode: 'create',
    },
    global: {
      stubs: {
        Button: {
          props: ['label', 'loading', 'severity', 'text', 'disabled'],
          emits: ['click'],
          template:
            '<button type="button" :disabled="disabled" v-bind="$attrs" @click="$emit(\'click\')">{{ label }}</button>',
        },
        Dialog: {
          props: ['visible', 'header'],
          emits: ['update:visible'],
          template:
            '<div v-if="visible"><h2>{{ header }}</h2><slot /><slot name="footer" /></div>',
        },
      },
    },
  })
}

function getButtonByText(wrapper: ReturnType<typeof mount>, text: string) {
  const button = wrapper.findAll('button').find((item) => item.text() === text)

  if (!button) {
    throw new Error(`Button with text "${text}" was not found.`)
  }

  return button
}

describe('WorkCalendarFormDialog', () => {
  it('validates that the date is required', async () => {
    const wrapper = mountDialog()

    await getButtonByText(wrapper, 'Guardar día').trigger('click')

    expect(wrapper.text()).toContain('Selecciona una fecha.')
  })

  it('validates that the day type is required', async () => {
    const wrapper = mountDialog()

    await wrapper.get('#work-calendar-date').setValue('2026-08-21')
    await getButtonByText(wrapper, 'Guardar día').trigger('click')

    expect(wrapper.text()).toContain('Selecciona un tipo de día válido.')
  })

  it('emits a normalized payload for a valid create', async () => {
    const wrapper = mountDialog()

    await wrapper.get('#work-calendar-date').setValue('2026-08-21')
    await wrapper.get('#work-calendar-day-type').setValue('Holiday')
    await wrapper.get('#work-calendar-description').setValue('  Feriado nacional  ')
    await getButtonByText(wrapper, 'Guardar día').trigger('click')

    expect(wrapper.emitted('save')).toEqual([
      [
        {
          date: '2026-08-21',
          dayType: 'Holiday',
          description: 'Feriado nacional',
        },
      ],
    ])
  })

  it('shows a fixed human-readable date when the dialog comes from the calendar', () => {
    const wrapper = mount(WorkCalendarFormDialog, {
      props: {
        visible: true,
        mode: 'create',
        initialDate: '2026-08-27',
        lockDate: true,
      },
      global: {
        stubs: {
          Button: {
            props: ['label', 'loading', 'severity', 'text', 'disabled'],
            emits: ['click'],
            template:
              '<button type="button" :disabled="disabled" v-bind="$attrs" @click="$emit(\'click\')">{{ label }}</button>',
          },
          Dialog: {
            props: ['visible', 'header'],
            emits: ['update:visible'],
            template:
              '<div v-if="visible"><h2>{{ header }}</h2><slot /><slot name="footer" /></div>',
          },
        },
      },
    })

    expect(wrapper.get('[data-testid="work-calendar-date-display"]').text()).toContain(
      '27 de agosto de 2026',
    )
  })

  it('does not show Version to the user', () => {
    const wrapper = mountDialog()

    expect(wrapper.text()).not.toContain('Version')
    expect(wrapper.text()).not.toContain('Versión')
  })

  it('renders a stacked form suitable for narrow screens conceptually', () => {
    const wrapper = mountDialog()

    expect(wrapper.find('.work-calendar-form').exists()).toBe(true)
    expect(wrapper.find('.work-calendar-form__actions').exists()).toBe(true)
    expect(wrapper.text()).toContain('Guardar día')
  })
})
