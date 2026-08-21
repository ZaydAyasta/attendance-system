<script setup lang="ts">
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import { reactive, watch } from 'vue'
import {
  workCalendarDayTypeSchema,
  workCalendarDescriptionMaxLength,
  workCalendarFormSchema,
} from '@/modules/work-calendar/work-calendar.schemas'
import {
  formatWorkCalendarDateWithWeekday,
  getWorkCalendarDayTypeMetadata,
} from '@/modules/work-calendar/work-calendar.presentation'
import type {
  WorkCalendarDay,
  WorkCalendarDayType,
  WorkCalendarFormValues,
} from '@/modules/work-calendar/work-calendar.types'

interface Props {
  initialDate?: string | null
  initialValue?: WorkCalendarDay | null
  lockDate?: boolean
  mode: 'create' | 'edit'
  saving?: boolean
  visible: boolean
}

const props = withDefaults(defineProps<Props>(), {
  initialDate: null,
  initialValue: null,
  lockDate: false,
  saving: false,
})

const emit = defineEmits<{
  close: []
  delete: []
  save: [value: WorkCalendarFormValues]
}>()

interface FormState {
  date: string
  dayType: WorkCalendarDayType | ''
  description: string
}

const dayTypeOptions = workCalendarDayTypeSchema.options.map((value) => ({
  value,
  ...getWorkCalendarDayTypeMetadata(value),
}))

const form = reactive<FormState>({
  date: '',
  dayType: '',
  description: '',
})

const errors = reactive<Record<string, string>>({})

function resetForm(): void {
  form.date = props.initialValue?.date ?? props.initialDate ?? ''
  form.dayType = props.initialValue?.dayType ?? ''
  form.description = props.initialValue?.description ?? ''

  clearErrors()
}

function clearErrors(): void {
  errors.date = ''
  errors.dayType = ''
  errors.description = ''
}

watch(
  () => [props.visible, props.initialValue, props.mode],
  () => {
    if (props.visible) {
      resetForm()
    }
  },
  { immediate: true },
)

function handleSave(): void {
  clearErrors()

  const result = workCalendarFormSchema.safeParse({
    date: form.date,
    dayType: form.dayType,
    description: form.description,
  })

  if (!result.success) {
    const fieldErrors = result.error.flatten().fieldErrors
    errors.date = fieldErrors.date?.[0] ?? ''
    errors.dayType = fieldErrors.dayType?.[0] ?? ''
    errors.description = fieldErrors.description?.[0] ?? ''
    return
  }

  emit('save', {
    date: result.data.date,
    dayType: result.data.dayType,
    description: result.data.description.trim() === '' ? null : result.data.description.trim(),
  })
}

function isDateLocked(): boolean {
  return props.mode === 'edit' || props.lockDate
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :header="mode === 'create' ? 'Agregar día' : 'Editar día'"
    :style="{ width: 'min(100%, 36rem)' }"
    @update:visible="emit('close')"
  >
    <form class="work-calendar-form" @submit.prevent="handleSave">
      <div class="work-calendar-form__field">
        <label class="work-calendar-form__label" for="work-calendar-date">Fecha</label>
        <div
          v-if="isDateLocked()"
          id="work-calendar-date"
          class="work-calendar-form__control work-calendar-form__control--static"
          data-testid="work-calendar-date-display"
        >
          {{ formatWorkCalendarDateWithWeekday(form.date) }}
        </div>
        <input
          v-else
          id="work-calendar-date"
          v-model="form.date"
          class="work-calendar-form__control"
          type="date"
          :aria-invalid="Boolean(errors.date)"
          :aria-describedby="errors.date ? 'work-calendar-date-error' : undefined"
        />
        <small
          v-if="isDateLocked()"
          class="work-calendar-form__help"
        >
          {{
            mode === 'edit'
              ? 'La fecha no se puede cambiar desde esta edición.'
              : 'La fecha se definió desde el calendario.'
          }}
        </small>
        <small
          v-if="errors.date"
          id="work-calendar-date-error"
          class="work-calendar-form__error"
        >
          {{ errors.date }}
        </small>
      </div>

      <div class="work-calendar-form__field">
        <label class="work-calendar-form__label" for="work-calendar-day-type">Tipo de día</label>
        <select
          id="work-calendar-day-type"
          v-model="form.dayType"
          class="work-calendar-form__control"
          :aria-invalid="Boolean(errors.dayType)"
          :aria-describedby="errors.dayType ? 'work-calendar-day-type-error' : undefined"
        >
          <option value="">Selecciona una opción</option>
          <option
            v-for="option in dayTypeOptions"
            :key="option.value"
            :value="option.value"
          >
            {{ option.label }}
          </option>
        </select>
        <small
          v-if="errors.dayType"
          id="work-calendar-day-type-error"
          class="work-calendar-form__error"
        >
          {{ errors.dayType }}
        </small>
      </div>

      <div class="work-calendar-form__field">
        <label class="work-calendar-form__label" for="work-calendar-description">
          Descripción
        </label>
        <textarea
          id="work-calendar-description"
          v-model="form.description"
          class="work-calendar-form__control work-calendar-form__control--textarea"
          rows="4"
          :maxlength="workCalendarDescriptionMaxLength"
          :aria-invalid="Boolean(errors.description)"
          :aria-describedby="errors.description ? 'work-calendar-description-error' : 'work-calendar-description-help'"
        />
        <small id="work-calendar-description-help" class="work-calendar-form__help">
          Campo opcional.
        </small>
        <small
          v-if="errors.description"
          id="work-calendar-description-error"
          class="work-calendar-form__error"
        >
          {{ errors.description }}
        </small>
      </div>
    </form>

    <template #footer>
      <div class="work-calendar-form__actions">
        <Button
          v-if="mode === 'edit'"
          label="Eliminar día"
          severity="danger"
          text
          :disabled="saving"
          @click="emit('delete')"
        />
        <Button
          label="Cancelar"
          severity="secondary"
          text
          :disabled="saving"
          @click="emit('close')"
        />
        <Button
          :label="mode === 'create' ? 'Guardar día' : 'Guardar cambios'"
          :loading="saving"
          @click="handleSave"
        />
      </div>
    </template>
  </Dialog>
</template>
