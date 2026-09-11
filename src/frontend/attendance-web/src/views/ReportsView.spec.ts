import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ReportsView from './ReportsView.vue'

const { listActiveEmployees, getReport } = vi.hoisted(() => ({
  listActiveEmployees: vi.fn(),
  getReport: vi.fn(),
}))

vi.mock('@/modules/employees/employees.service', () => ({ listActiveEmployees }))
vi.mock('@/modules/reporting/reporting.service', () => ({ getReport, downloadReport: vi.fn() }))

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
        Button: { template: '<button><slot /></button>' },
      },
    },
  })
}

describe('ReportsView', () => {
  beforeEach(() => {
    listActiveEmployees.mockResolvedValue([
      { id: 'b89f8253-79bb-4bb2-98d1-67114482fe01', employeeCode: 'EMP-001', fullName: 'María Torres' },
    ])
    getReport.mockReset()
    getReport.mockResolvedValue({ summary: {}, rows: [] })
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
})
