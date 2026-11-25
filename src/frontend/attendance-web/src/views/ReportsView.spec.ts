import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ReportsView from './ReportsView.vue'

const { listEmployeeOptions, getReport, downloadReport } = vi.hoisted(() => ({
  listEmployeeOptions: vi.fn(),
  getReport: vi.fn(),
  downloadReport: vi.fn(),
}))

vi.mock('@/modules/employees/employees.service', () => ({ listEmployeeOptions }))
vi.mock('@/modules/reporting/reporting.service', () => ({ getReport, downloadReport }))

function mountReportsView() {
  return mount(ReportsView, {
    global: {
      stubs: {
        AppPageHeader: true,
        AppLoadingState: true,
        AppEmptyState: true,
        AppErrorState: true,
        DataTable: true,
        Column: true,
        Button: { props: ['label'], template: '<button>{{ label }}</button>' },
      },
    },
  })
}

describe('ReportsView', () => {
  beforeEach(() => {
    listEmployeeOptions.mockResolvedValue([
      { id: 'b89f8253-79bb-4bb2-98d1-67114482fe01', employeeCode: 'EMP-001', fullName: 'María Torres' },
    ])
    getReport.mockReset()
    getReport.mockResolvedValue({ summary: {}, rows: [] })
    downloadReport.mockReset().mockResolvedValue(undefined)
  })

  it('omits employeeId when generating a report for all employees', async () => {
    const wrapper = mountReportsView()
    await flushPromises()

    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    const parameters = getReport.mock.calls[0]?.[0]
    expect(parameters).toMatchObject({ from: expect.any(String), to: expect.any(String) })
    expect(parameters).not.toHaveProperty('employeeId')
  })

  it('includes employeeId when generating a report for one employee', async () => {
    const wrapper = mountReportsView()
    await flushPromises()
    const employeeId = 'b89f8253-79bb-4bb2-98d1-67114482fe01'

    await wrapper.find('select').setValue(employeeId)
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    expect(getReport).toHaveBeenCalledWith(expect.objectContaining({ employeeId }))
  })

  it('uses the applied period and employee when filters change before export', async () => {
    const wrapper = mountReportsView()
    await flushPromises()
    const from = wrapper.get('input[name="from"]')
    const to = wrapper.get('input[name="to"]')
    await from.setValue('2026-10-01')
    await to.setValue('2026-10-31')
    await wrapper.get('form').trigger('submit.prevent')
    await flushPromises()

    await from.setValue('2026-11-01')
    await to.setValue('2026-11-30')
    await wrapper.get('select').setValue('b89f8253-79bb-4bb2-98d1-67114482fe01')
    for (const label of ['Exportar Excel', 'Exportar PDF']) {
      await wrapper.findAll('button').find(button => button.text() === label)!.trigger('click')
      await flushPromises()
    }

    const appliedPeriod = { from: '2026-10-01', to: '2026-10-31' }
    expect(downloadReport.mock.calls).toEqual([['xlsx', appliedPeriod], ['pdf', appliedPeriod]])
  })
})
