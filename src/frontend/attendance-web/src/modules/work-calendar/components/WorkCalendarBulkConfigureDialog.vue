<script setup lang="ts">
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import { computed, reactive, ref, watch } from 'vue'
import { getMonthLabel, getMonthRange } from '@/modules/work-calendar/work-calendar.presentation'
import type { BulkConfigureWorkCalendarRequest, WorkCalendarDay, WorkCalendarDayType } from '@/modules/work-calendar/work-calendar.types'

const props = withDefaults(defineProps<{
  days: WorkCalendarDay[]
  month: Date
  saving?: boolean
  visible: boolean
}>(), { saving: false })

const emit = defineEmits<{ close: []; apply: [request: BulkConfigureWorkCalendarRequest] }>()

const weekdayLabels = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo']
const dayTypes: { label: string; value: WorkCalendarDayType }[] = [
  { label: 'Laborable', value: 'WorkingDay' },
  { label: 'No laborable', value: 'NonWorkingDay' },
  { label: 'Feriado', value: 'Holiday' },
]
const weekdayConfiguration = reactive<WorkCalendarDayType[]>([])
const overwriteExisting = ref(false)
const previewVisible = ref(false)

function applyUsualWeek(): void {
  weekdayConfiguration.splice(0, weekdayConfiguration.length,
    'WorkingDay', 'WorkingDay', 'WorkingDay', 'WorkingDay', 'WorkingDay', 'NonWorkingDay', 'NonWorkingDay')
}

function reset(): void {
  applyUsualWeek()
  overwriteExisting.value = false
  previewVisible.value = false
}

watch(() => props.visible, (visible) => { if (visible) reset() })

const request = computed<BulkConfigureWorkCalendarRequest>(() => {
  const { from, to } = getMonthRange(props.month)
  const existing = new Map(props.days.map(day => [day.date, day]))
  const days = []
  for (const cursor = new Date(`${from}T12:00:00`); cursor <= new Date(`${to}T12:00:00`); cursor.setDate(cursor.getDate() + 1)) {
    const date = cursor.toISOString().slice(0, 10)
    const current = existing.get(date)
    days.push({
      date,
      dayType: weekdayConfiguration[(cursor.getDay() + 6) % 7],
      description: null,
      ...(current ? { version: current.version } : {}),
    })
  }
  return { days, overwriteExisting: overwriteExisting.value }
})

const preview = computed(() => {
  const existing = new Map(props.days.map(day => [day.date, day]))
  const distribution: Record<WorkCalendarDayType, number> = { WorkingDay: 0, NonWorkingDay: 0, Holiday: 0 }
  let created = 0
  let kept = 0
  let updated = 0
  for (const day of request.value.days) {
    const current = existing.get(day.date)
    if (current && !overwriteExisting.value) { kept++; continue }
    if (current) updated++; else created++
    distribution[day.dayType]++
  }
  return { created, kept, updated, distribution }
})

function openPreview(): void { previewVisible.value = true }
</script>

<template>
  <Dialog :visible="visible" modal header="Configurar mes" :style="{ width: 'min(100%, 42rem)' }" @update:visible="emit('close')">
    <div class="work-calendar-bulk">
      <p class="work-calendar-bulk__month">{{ getMonthLabel(month) }}</p>
      <template v-if="!previewVisible">
        <div class="work-calendar-bulk__section-heading">
          <h3>Configuración por día de la semana</h3>
          <Button label="Usar semana habitual" text @click="applyUsualWeek" />
        </div>
        <p class="work-calendar-bulk__help">Revisa y confirma esta propuesta antes de aplicarla.</p>
        <div class="work-calendar-bulk__weekdays">
          <label v-for="(label, index) in weekdayLabels" :key="label" class="work-calendar-bulk__weekday">
            <span>{{ label }}</span>
            <select v-model="weekdayConfiguration[index]" :aria-label="`${label}: tipo de día`">
              <option v-for="dayType in dayTypes" :key="dayType.value" :value="dayType.value">{{ dayType.label }}</option>
            </select>
          </label>
        </div>
        <fieldset class="work-calendar-bulk__scope">
          <legend>Aplicar a</legend>
          <label><input v-model="overwriteExisting" type="radio" :value="false" /> Solo días sin configurar</label>
          <label><input v-model="overwriteExisting" type="radio" :value="true" /> Reemplazar configuraciones existentes</label>
        </fieldset>
        <Message v-if="overwriteExisting" severity="info" :closable="false">Se reemplazarán {{ preview.updated }} configuraciones existentes según la versión cargada.</Message>
      </template>
      <template v-else>
        <h3>Vista previa</h3>
        <p>Se configurarán {{ preview.created + preview.updated }} días.</p>
        <ul class="work-calendar-bulk__summary">
          <li>{{ preview.distribution.WorkingDay }} Laborables</li>
          <li>{{ preview.distribution.NonWorkingDay }} No laborables</li>
          <li>{{ preview.distribution.Holiday }} Feriados</li>
          <li v-if="preview.kept">{{ preview.kept }} configuraciones existentes se mantendrán</li>
          <li v-if="overwriteExisting">{{ preview.updated }} configuraciones existentes serán reemplazadas</li>
        </ul>
      </template>
    </div>
    <template #footer>
      <div class="work-calendar-form__actions">
        <Button :label="previewVisible ? 'Volver' : 'Cancelar'" severity="secondary" text :disabled="saving" @click="previewVisible ? previewVisible = false : emit('close')" />
        <Button v-if="!previewVisible" label="Vista previa" @click="openPreview" />
        <Button v-else label="Aplicar configuración" :loading="saving" :disabled="saving" @click="emit('apply', request)" />
      </div>
    </template>
  </Dialog>
</template>
