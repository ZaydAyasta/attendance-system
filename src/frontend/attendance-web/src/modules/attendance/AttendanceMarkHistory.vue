<script setup lang="ts">
import Button from 'primevue/button'
import { nextTick, ref, useId, watch } from 'vue'
import {
  getAttendanceMarks, saveAttendanceMark, type AttendanceHistoryMark,
} from './attendance.service'
import { getUserFriendlyApiError } from '@/services/api-errors'

const props = defineProps<{ employeeId: string; date: string }>()
const emit = defineEmits<{ changed: [] }>()
const markTypes = {
  Entry: 'Entrada', Exit: 'Salida', LunchStart: 'Inicio de almuerzo', LunchEnd: 'Fin de almuerzo',
  CommissionExit: 'Salida a comisión', CommissionReturn: 'Regreso de comisión',
  OtherExit: 'Otra salida', OtherReturn: 'Otro retorno',
  OvertimeStart: 'Inicio de horas extra', OvertimeEnd: 'Fin de horas extra',
}
const sources: Record<string, string> = {
  Manual: 'Manual', DynamicQr: 'QR', StaticQr: 'QR estático', LegacyFingerprint: 'Importada',
}
const marks = ref<AttendanceHistoryMark[]>([])
const loading = ref(false)
const loadError = ref('')
const error = ref('')
const saving = ref(false)
const action = ref<'add' | 'correct' | 'void' | null>(null)
const selected = ref<AttendanceHistoryMark | null>(null)
const time = ref('')
const type = ref('Entry')
const reason = ref('')
const reasonInput = ref<HTMLTextAreaElement | null>(null)
const reasonError = ref('')
const reasonErrorId = useId()

function markLabel(value: string) {
  return markTypes[value as keyof typeof markTypes] ?? value
}

async function load() {
  loading.value = true
  loadError.value = ''
  try {
    marks.value = await getAttendanceMarks(props.employeeId, props.date)
  } catch (cause) {
    loadError.value = getUserFriendlyApiError(cause)
  } finally {
    loading.value = false
  }
}

function edit(nextAction: NonNullable<typeof action.value>, mark: AttendanceHistoryMark | null = null) {
  action.value = nextAction
  selected.value = mark
  time.value = mark?.localTime.slice(0, 5) ?? ''
  type.value = mark?.type ?? 'Entry'
  reason.value = ''
  reasonError.value = ''
  error.value = ''
}

async function save() {
  if (saving.value || !action.value) return
  const note = reason.value.trim()
  if (!note) {
    reasonError.value = 'Ingresa el motivo del cambio.'
    await nextTick()
    reasonInput.value?.focus()
    return
  }

  const change = action.value === 'void' ? { reason: note }
    : { date: props.date, time: `${time.value}:00`, type: type.value, reason: note }
  saving.value = true
  error.value = ''
  try {
    await saveAttendanceMark(props.employeeId, change, selected.value?.id)
    action.value = null
    emit('changed')
    await load()
  } catch (cause) {
    error.value = getUserFriendlyApiError(cause)
  } finally {
    saving.value = false
  }
}

watch(() => [props.employeeId, props.date], load, { immediate: true })
</script>

<template>
  <section>
    <h3>Marcaciones</h3>
    <p v-if="loading" role="status">Cargando marcaciones…</p>
    <p v-else-if="loadError" role="alert">{{ loadError }} <Button type="button" label="Reintentar" text @click="load" /></p>
    <p v-else-if="!marks.length">Este día no tiene marcaciones.</p>
    <ul v-else class="mark-history">
      <li v-for="mark in marks" :key="mark.id">
        <p><strong>{{ mark.localTime.slice(0, 5) }} · {{ markLabel(mark.type) }}</strong> · {{ sources[mark.source] ?? mark.source }} <span v-if="mark.voidedAt">· Anulada</span></p>
        <p v-if="mark.manualReason">{{ mark.manualReason }}</p>
        <p v-if="mark.voidReason">Motivo de anulación: {{ mark.voidReason }}</p>
        <div v-if="!mark.voidedAt" class="absence-card__actions">
          <Button type="button" label="Corregir" text :disabled="saving" @click="edit('correct', mark)" />
          <Button type="button" label="Anular" text severity="danger" :disabled="saving" @click="edit('void', mark)" />
        </div>
      </li>
    </ul>
    <Button v-if="!action" type="button" label="Registrar marcación manual" :disabled="loading || saving" @click="edit('add')" />
    <form v-else class="absence-form" @submit.prevent="save">
      <h4>{{ action === 'void' ? 'Anular marcación' : action === 'correct' ? 'Corregir marcación' : 'Registrar marcación manual' }}</h4>
      <p v-if="selected">{{ selected.localTime.slice(0, 5) }} · {{ markLabel(selected.type) }}</p>
      <template v-if="action !== 'void'">
        <label>Hora local <input v-model="time" name="time" type="time" required /></label>
        <label>Tipo
          <select v-model="type" name="type" required>
            <option v-for="(label, value) in markTypes" :key="value" :value="value">{{ label }}</option>
          </select>
        </label>
      </template>
      <label>Motivo
        <textarea ref="reasonInput" v-model="reason" name="reason" required maxlength="500"
          :aria-invalid="Boolean(reasonError)" :aria-describedby="reasonError ? reasonErrorId : undefined"
          @input="reasonError = ''" />
      </label>
      <p v-if="reasonError" :id="reasonErrorId">{{ reasonError }}</p>
      <p v-if="error" role="alert">{{ error }}</p>
      <div class="absence-card__actions">
        <Button type="button" label="Cancelar" text :disabled="saving" @click="action = null" />
        <Button type="submit" :label="action === 'void' ? 'Confirmar anulación' : 'Guardar marcación'" :disabled="saving" :loading="saving" />
      </div>
    </form>
  </section>
</template>
