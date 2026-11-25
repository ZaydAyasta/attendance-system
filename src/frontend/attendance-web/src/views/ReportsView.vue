<script setup lang="ts">
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import { onMounted, reactive, ref } from 'vue'
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import AppLoadingState from '@/components/state/AppLoadingState.vue'
import AppEmptyState from '@/components/state/AppEmptyState.vue'
import AppErrorState from '@/components/state/AppErrorState.vue'
import { listEmployeeOptions, type EmployeeOption } from '@/modules/employees/employees.service'
import { attendanceLabel, formatMinutes } from '@/modules/attendance/attendance.presentation'
import { downloadReport, getReport, type Report, type ReportParameters } from '@/modules/reporting/reporting.service'
import { getUserFriendlyApiError } from '@/services/api-errors'

const now = new Date()
const first = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10)
const last = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().slice(0, 10)
const filters = reactive({ from: first, to: last, employeeId: '' })
const employees = ref<EmployeeOption[]>([])
const report = ref<Report | null>(null)
const appliedParameters = ref<ReportParameters | null>(null)
const loading = ref(false)
const error = ref('')
const employeeError = ref('')
const exportError = ref('')
const exporting = ref('')

async function generate() {
  if (loading.value) return

  const { from, to, employeeId } = filters
  if (from > to) {
    error.value = 'La fecha Desde debe ser anterior o igual a Hasta.'
    return
  }

  const parameters = employeeId ? { from, to, employeeId } : { from, to }
  loading.value = true
  error.value = ''
  exportError.value = ''
  try {
    report.value = await getReport(parameters)
    appliedParameters.value = parameters
  } catch (cause) {
    error.value = getUserFriendlyApiError(cause)
  } finally {
    loading.value = false
  }
}

async function exportFile(kind: 'xlsx' | 'pdf') {
  const parameters = appliedParameters.value
  if (!parameters || loading.value || error.value || exporting.value) return

  exporting.value = kind
  exportError.value = ''
  try {
    await downloadReport(kind, parameters)
  } catch {
    exportError.value = 'No pudimos generar el archivo.'
  } finally {
    exporting.value = ''
  }
}

onMounted(async () => {
  try {
    employees.value = await listEmployeeOptions()
  } catch {
    employeeError.value = 'No pudimos cargar la lista de empleados.'
  }
})
</script>
<template><section><AppPageHeader title="Reportes" description="Consulta y exporta la asistencia por período."/><form class="absence-filters app-surface" @submit.prevent="generate"><label>Desde<input v-model="filters.from" name="from" type="date" required/></label><label>Hasta<input v-model="filters.to" name="to" type="date" required/></label><label>Empleado<select v-model="filters.employeeId" name="employeeId"><option value="">Todos los empleados</option><option v-for="e in employees" :key="e.id" :value="e.id">{{e.fullName}} — {{e.employeeCode}}</option></select></label><Button label="Generar reporte" type="submit" :loading="loading" :disabled="loading"/></form><p v-if="employeeError" role="status">{{ employeeError }}</p><div class="report-actions"><Button label="Exportar Excel" icon="pi pi-file-excel" :disabled="!report || loading || Boolean(error) || Boolean(exporting)" :loading="exporting==='xlsx'" @click="exportFile('xlsx')"/><Button label="Exportar PDF" icon="pi pi-file-pdf" :disabled="!report || loading || Boolean(error) || Boolean(exporting)" :loading="exporting==='pdf'" @click="exportFile('pdf')"/></div><p v-if="exportError" role="alert">{{ exportError }}</p><AppLoadingState v-if="loading" label="Generando reporte..."/><AppErrorState v-else-if="error" title="No pudimos generar el reporte." :description="error" @retry="generate"/><AppEmptyState v-else-if="!report" title="Selecciona un período para generar el reporte." description="No se genera automáticamente al cambiar filtros."/><template v-else><div class="app-card-grid report-summary"><article class="app-surface"><strong>Empleados incluidos</strong><span>{{report.summary.employeesIncluded}}</span></article><article class="app-surface"><strong>Días evaluados</strong><span>{{report.summary.daysEvaluated}}</span></article><article class="app-surface"><strong>Presentes</strong><span>{{report.summary.present}}</span></article><article class="app-surface"><strong>Ausencias</strong><span>{{report.summary.unexcusedAbsences}}</span></article><article class="app-surface"><strong>Incompletos</strong><span>{{report.summary.incomplete}}</span></article><article class="app-surface"><strong>Horas trabajadas</strong><span>{{formatMinutes(report.summary.totalWorkedMinutes)}}</span></article></div><AppEmptyState v-if="!report.rows.length" title="No hay información de asistencia para este período." description="Consulta otro rango."/><div v-else class="absence-list"><DataTable :value="report.rows" paginator :rows="25" class="absence-table"><Column header="Empleado" field="employeeName"/><Column header="Código" field="employeeCode"/><Column header="Fecha" field="date"/><Column header="Estado"><template #body="{data}">{{attendanceLabel(data.failure??data.status)}}</template></Column><Column header="Tiempo trabajado"><template #body="{data}">{{formatMinutes(data.workedMinutes)}}</template></Column><Column header="Almuerzo"><template #body="{data}">{{formatMinutes(data.lunchMinutes)}}</template></Column><Column header="Anomalías"><template #body="{data}">{{data.anomalies.map(attendanceLabel).join(', ')||'—'}}</template></Column></DataTable><div class="absence-cards"><article v-for="r in report.rows" :key="`${r.employeeId}-${r.date}`" class="app-surface absence-card"><strong>{{r.employeeName}}</strong><span>{{r.employeeCode}} · {{r.date}}</span><span>{{attendanceLabel(r.failure??r.status)}}</span><span>Trabajado: {{formatMinutes(r.workedMinutes)}}</span><span v-if="r.anomalies.length">{{attendanceLabel(r.anomalies[0])}}</span></article></div></div></template></section></template>
