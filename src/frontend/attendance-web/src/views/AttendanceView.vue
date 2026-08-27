<script setup lang="ts">
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import { computed, reactive, ref } from 'vue'
import { z } from 'zod'
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import AppEmptyState from '@/components/state/AppEmptyState.vue'
import AppErrorState from '@/components/state/AppErrorState.vue'
import AppLoadingState from '@/components/state/AppLoadingState.vue'
import { getAttendanceRange, getMyAttendanceRange } from '@/modules/attendance/attendance.service'
import { attendanceLabel, formatMinutes } from '@/modules/attendance/attendance.presentation'
import type { DailyAttendance } from '@/modules/attendance/attendance.types'
import { listActiveEmployees, type EmployeeOption } from '@/modules/employees/employees.service'
import { getUserFriendlyApiError } from '@/services/api-errors'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const isUser = computed(() => authStore.role === 'User')
const now = new Date()
const filters = reactive({ employeeId: '', from: new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10), to: new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().slice(0, 10) })
const employees = ref<EmployeeOption[]>([])
const employeesLoading = ref(false)
const employeesError = ref('')
const items = ref<DailyAttendance[]>([])
const loading = ref(false)
const error = ref('')
const queried = ref(false)
const detail = ref<DailyAttendance | null>(null)
const adminSchema = z.object({ employeeId: z.string().uuid('Selecciona un empleado.'), from: z.string(), to: z.string() })
const dateSchema = z.object({ from: z.string(), to: z.string() }).refine((value) => value.to >= value.from, { message: 'La fecha final no puede ser anterior a la fecha inicial.', path: ['to'] })

async function loadEmployees() { employeesLoading.value = true; employeesError.value = ''; try { employees.value = await listActiveEmployees(); if (!employees.value.length) employeesError.value = 'No hay empleados activos disponibles.' } catch { employeesError.value = 'No pudimos cargar los empleados.' } finally { employeesLoading.value = false } }
async function query() {
  const validation = (isUser.value ? dateSchema : adminSchema).safeParse(filters)
  if (!validation.success) { error.value = validation.error.issues[0]?.message ?? ''; return }
  loading.value = true
  try { items.value = isUser.value ? (await getMyAttendanceRange(filters.from, filters.to)).days : (await getAttendanceRange(filters.employeeId, filters.from, filters.to)).days; queried.value = true; error.value = '' } catch (exception) { error.value = getUserFriendlyApiError(exception) } finally { loading.value = false }
}
if (!isUser.value) void loadEmployees()
</script>

<template><section><AppPageHeader :title="isUser ? 'Mi asistencia' : 'Asistencia'" :description="isUser ? 'Consulta tu estado diario y tiempo trabajado.' : 'Consulta el estado diario y el tiempo trabajado de cada empleado.'" /><form class="absence-filters app-surface" @submit.prevent="query"><label v-if="!isUser">Empleado<select v-model="filters.employeeId" :disabled="employeesLoading"><option value="">{{ employeesLoading ? 'Cargando empleados...' : 'Selecciona un empleado' }}</option><option v-for="employee in employees" :key="employee.id" :value="employee.id">{{ employee.fullName }} — {{ employee.employeeCode }}</option></select></label><label>Desde<input v-model="filters.from" type="date" /></label><label>Hasta<input v-model="filters.to" type="date" /></label><Button label="Consultar" type="submit" :disabled="Boolean(employeesError)" /></form><p v-if="employeesError">{{ employeesError }} <Button label="Volver a intentar" text @click="loadEmployees" /></p><AppLoadingState v-if="loading" label="Cargando asistencia..." /><AppErrorState v-else-if="error" title="No pudimos cargar la asistencia." :description="error" @retry="query" /><AppEmptyState v-else-if="queried && !items.length" title="No hay información de asistencia para este período." description="Consulta otro rango." /><div v-else-if="queried" class="absence-list"><DataTable :value="items" class="absence-table"><Column header="Fecha" field="date" /><Column header="Estado"><template #body="{ data }">{{ attendanceLabel(data.status) }}</template></Column><Column header="Tiempo trabajado"><template #body="{ data }">{{ formatMinutes(data.workedMinutes) }}</template></Column><Column header="Almuerzo"><template #body="{ data }">{{ formatMinutes(data.lunchMinutes) }}</template></Column><Column header="Anomalías"><template #body="{ data }">{{ data.anomalies.map(attendanceLabel).join(', ') || '—' }}</template></Column><Column header="Detalle"><template #body="{ data }"><Button label="Ver detalle" text @click="detail = data" /></template></Column></DataTable><div class="absence-cards"><article v-for="item in items" :key="item.date" class="app-surface absence-card"><strong>{{ item.date }}</strong><span>Estado: {{ attendanceLabel(item.status) }}</span><span>Trabajado: {{ formatMinutes(item.workedMinutes) }}</span><span>Almuerzo: {{ formatMinutes(item.lunchMinutes) }}</span><div class="absence-card__actions"><Button label="Ver detalle" text @click="detail = item" /></div></article></div></div><Dialog :visible="Boolean(detail)" modal class="app-form-dialog" header="Detalle de asistencia" @update:visible="detail = null"><template v-if="detail"><p><strong>Estado:</strong> {{ attendanceLabel(detail.status) }}</p><p><strong>Tiempo trabajado:</strong> {{ formatMinutes(detail.workedMinutes) }}</p><p><strong>Almuerzo:</strong> {{ formatMinutes(detail.lunchMinutes) }}</p><p v-for="issue in detail.timeIssues" :key="issue">{{ attendanceLabel(issue) }}</p><p v-for="anomaly in detail.anomalies" :key="anomaly">{{ attendanceLabel(anomaly) }}</p></template></Dialog></section></template>
