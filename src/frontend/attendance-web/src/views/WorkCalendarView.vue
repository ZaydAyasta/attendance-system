<script setup lang="ts">
import axios from 'axios'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import { reactive, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import AppEmptyState from '@/components/state/AppEmptyState.vue'
import AppErrorState from '@/components/state/AppErrorState.vue'
import AppLoadingState from '@/components/state/AppLoadingState.vue'
import WorkCalendarFormDialog from '@/modules/work-calendar/components/WorkCalendarFormDialog.vue'
import WorkCalendarList from '@/modules/work-calendar/components/WorkCalendarList.vue'
import WorkCalendarMonth from '@/modules/work-calendar/components/WorkCalendarMonth.vue'
import {
  addMonths,
  formatWorkCalendarDateWithWeekday,
  getCurrentMonthRange,
  getMonthRange,
  isDateWithinRange,
  startOfMonth,
} from '@/modules/work-calendar/work-calendar.presentation'
import { workCalendarRangeSchema } from '@/modules/work-calendar/work-calendar.schemas'
import {
  createWorkCalendarDay,
  deleteWorkCalendarDay,
  listWorkCalendarDays,
  updateWorkCalendarDay,
} from '@/modules/work-calendar/work-calendar.service'
import { getUserFriendlyApiError } from '@/services/api-errors'
import type { WorkCalendarDay, WorkCalendarFormValues } from '@/modules/work-calendar/work-calendar.types'

type WorkCalendarViewMode = 'calendar' | 'list'

const toast = useToast()
const todayMonth = startOfMonth(new Date())
const defaultRange = getCurrentMonthRange(todayMonth)

const activeView = ref<WorkCalendarViewMode>('calendar')
const currentMonth = ref(todayMonth)

const filters = reactive({
  from: defaultRange.from,
  to: defaultRange.to,
})

const filterErrors = reactive<Record<string, string>>({
  from: '',
  to: '',
})

const monthDays = ref<WorkCalendarDay[]>([])
const monthLoading = ref(false)
const monthReady = ref(false)
const monthError = ref<string | null>(null)
const loadedMonthRangeKey = ref<string | null>(null)

const listDays = ref<WorkCalendarDay[]>([])
const listLoading = ref(false)
const listError = ref<string | null>(null)
const listLoaded = ref(false)

const conflictMessage = ref<string | null>(null)
const dialogVisible = ref(false)
const dialogMode = ref<'create' | 'edit'>('create')
const selectedDay = ref<WorkCalendarDay | null>(null)
const dialogDate = ref<string | null>(null)
const dialogDateLocked = ref(false)
const saving = ref(false)
const deleteTarget = ref<WorkCalendarDay | null>(null)
const deletingDate = ref<string | null>(null)

function getRangeKey(from: string, to: string): string {
  return `${from}:${to}`
}

function getVisibleMonthRange() {
  return getMonthRange(currentMonth.value)
}

function clearFilterErrors(): void {
  filterErrors.from = ''
  filterErrors.to = ''
}

function resetConflictMessage(): void {
  conflictMessage.value = null
}

function showSuccessToast(detail: string): void {
  toast.add({
    severity: 'success',
    summary: 'Listo',
    detail,
    life: 3000,
  })
}

function showErrorToast(detail: string): void {
  toast.add({
    severity: 'error',
    summary: 'No se pudo completar la acción',
    detail,
    life: 4000,
  })
}

function isConcurrencyConflict(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 409
}

async function loadMonth(): Promise<void> {
  const { from, to } = getVisibleMonthRange()
  const rangeKey = getRangeKey(from, to)
  const preserveVisibleMonth = loadedMonthRangeKey.value === rangeKey && monthReady.value

  monthError.value = null
  monthLoading.value = true

  if (!preserveVisibleMonth) {
    monthReady.value = false
    monthDays.value = []
  }

  try {
    monthDays.value = await listWorkCalendarDays({ from, to })
    monthReady.value = true
    loadedMonthRangeKey.value = rangeKey
  } catch (error) {
    monthError.value = getUserFriendlyApiError(error)

    if (!preserveVisibleMonth) {
      monthReady.value = false
      loadedMonthRangeKey.value = null
    }
  } finally {
    monthLoading.value = false
  }
}

async function loadList(): Promise<void> {
  clearFilterErrors()
  listError.value = null

  const validation = workCalendarRangeSchema.safeParse({
    from: filters.from,
    to: filters.to,
  })

  if (!validation.success) {
    const fieldErrors = validation.error.flatten().fieldErrors
    filterErrors.from = fieldErrors.from?.[0] ?? ''
    filterErrors.to = fieldErrors.to?.[0] ?? ''
    return
  }

  listLoading.value = true

  try {
    listDays.value = await listWorkCalendarDays(validation.data)
    listLoaded.value = true
  } catch (error) {
    listError.value = getUserFriendlyApiError(error)
  } finally {
    listLoading.value = false
  }
}

function setActiveView(view: WorkCalendarViewMode): void {
  activeView.value = view

  if (view === 'list' && !listLoaded.value) {
    void loadList()
  }
}

function openCreateDialog(): void {
  dialogMode.value = 'create'
  selectedDay.value = null
  dialogDate.value = null
  dialogDateLocked.value = false
  dialogVisible.value = true
}

function openCreateDialogForDate(date: string): void {
  dialogMode.value = 'create'
  selectedDay.value = null
  dialogDate.value = date
  dialogDateLocked.value = true
  dialogVisible.value = true
}

function openEditDialog(day: WorkCalendarDay): void {
  dialogMode.value = 'edit'
  selectedDay.value = day
  dialogDate.value = day.date
  dialogDateLocked.value = true
  dialogVisible.value = true
}

function closeDialog(): void {
  dialogVisible.value = false
}

function requestDelete(day: WorkCalendarDay): void {
  deleteTarget.value = day
}

function requestDeleteFromDialog(): void {
  if (!selectedDay.value) {
    return
  }

  dialogVisible.value = false
  requestDelete(selectedDay.value)
}

function closeDeleteDialog(): void {
  deleteTarget.value = null
}

async function refreshAffectedRanges(date: string): Promise<void> {
  const tasks: Promise<void>[] = []
  const monthRange = getVisibleMonthRange()

  if (isDateWithinRange(date, monthRange.from, monthRange.to)) {
    tasks.push(loadMonth())
  }

  if (listLoaded.value && isDateWithinRange(date, filters.from, filters.to)) {
    tasks.push(loadList())
  }

  if (tasks.length === 0) {
    return
  }

  await Promise.all(tasks)
}

async function handleSave(formValues: WorkCalendarFormValues): Promise<void> {
  saving.value = true
  resetConflictMessage()

  try {
    let affectedDate = formValues.date

    if (dialogMode.value === 'create') {
      await createWorkCalendarDay(formValues)
      showSuccessToast('Día agregado correctamente.')
    } else if (selectedDay.value) {
      affectedDate = selectedDay.value.date

      await updateWorkCalendarDay(selectedDay.value.date, {
        dayType: formValues.dayType,
        description: formValues.description,
        version: selectedDay.value.version,
      })

      showSuccessToast('Cambios guardados.')
    }

    dialogVisible.value = false
    await refreshAffectedRanges(affectedDate)
  } catch (error) {
    if (isConcurrencyConflict(error) && dialogMode.value === 'edit') {
      dialogVisible.value = false
      conflictMessage.value =
        'Este día fue modificado por otra persona. Actualiza la información e inténtalo nuevamente.'
      return
    }

    showErrorToast(getUserFriendlyApiError(error))
  } finally {
    saving.value = false
  }
}

async function confirmDelete(): Promise<void> {
  if (!deleteTarget.value) {
    return
  }

  const target = deleteTarget.value
  deletingDate.value = target.date
  resetConflictMessage()

  try {
    await deleteWorkCalendarDay(target.date)
    showSuccessToast('Día eliminado del calendario.')
    closeDeleteDialog()
    await refreshAffectedRanges(target.date)
  } catch (error) {
    if (isConcurrencyConflict(error)) {
      closeDeleteDialog()
      conflictMessage.value =
        'Este día fue modificado por otra persona. Actualiza la información e inténtalo nuevamente.'
      return
    }

    showErrorToast(getUserFriendlyApiError(error))
  } finally {
    deletingDate.value = null
  }
}

async function reloadVisibleView(): Promise<void> {
  if (activeView.value === 'list') {
    await loadList()
    return
  }

  await loadMonth()
}

function goToPreviousMonth(): void {
  currentMonth.value = addMonths(currentMonth.value, -1)
  void loadMonth()
}

function goToNextMonth(): void {
  currentMonth.value = addMonths(currentMonth.value, 1)
  void loadMonth()
}

function goToCurrentMonth(): void {
  currentMonth.value = todayMonth
  void loadMonth()
}

void loadMonth()
</script>

<template>
  <section>
    <AppPageHeader
      title="Calendario laboral"
      description="Define qué días son laborables, no laborables o feriados."
      action-label="Agregar día"
      action-icon="pi pi-plus"
      @action="openCreateDialog"
    />

    <div
      class="app-surface work-calendar-view-switcher"
      role="tablist"
      aria-label="Vista del calendario laboral"
    >
      <Button
        label="Calendario"
        :text="activeView !== 'calendar'"
        :severity="activeView === 'calendar' ? 'contrast' : 'secondary'"
        data-testid="work-calendar-view-calendar"
        @click="setActiveView('calendar')"
      />
      <Button
        label="Lista"
        :text="activeView !== 'list'"
        :severity="activeView === 'list' ? 'contrast' : 'secondary'"
        data-testid="work-calendar-view-list"
        @click="setActiveView('list')"
      />
    </div>

    <Message
      v-if="conflictMessage"
      severity="warn"
      :closable="false"
      class="work-calendar-conflict"
    >
      <div class="work-calendar-conflict__content">
        <span>{{ conflictMessage }}</span>
        <Button label="Actualizar" text @click="reloadVisibleView" />
      </div>
    </Message>

    <section v-if="activeView === 'calendar'" class="work-calendar-content">
      <WorkCalendarMonth
        :days="monthDays"
        :error="monthError"
        :loading="monthLoading"
        :month="currentMonth"
        :ready="monthReady"
        @previous-month="goToPreviousMonth"
        @next-month="goToNextMonth"
        @current-month="goToCurrentMonth"
        @retry="loadMonth"
        @select-empty="openCreateDialogForDate"
        @select-day="openEditDialog"
      />

      <p
        v-if="monthReady && monthDays.length === 0 && !monthError"
        class="work-calendar-month__hint"
      >
        No hay días configurados para este mes. Haz clic en una fecha para registrarla.
      </p>
    </section>

    <section v-else class="work-calendar-content">
      <article class="app-surface">
        <form class="work-calendar-filters" @submit.prevent="loadList">
          <div class="work-calendar-filters__field">
            <label class="work-calendar-filters__label" for="work-calendar-from">Desde</label>
            <input
              id="work-calendar-from"
              v-model="filters.from"
              class="work-calendar-filters__control"
              type="date"
              :aria-invalid="Boolean(filterErrors.from)"
            />
            <small v-if="filterErrors.from" class="work-calendar-filters__error">
              {{ filterErrors.from }}
            </small>
          </div>

          <div class="work-calendar-filters__field">
            <label class="work-calendar-filters__label" for="work-calendar-to">Hasta</label>
            <input
              id="work-calendar-to"
              v-model="filters.to"
              class="work-calendar-filters__control"
              type="date"
              :aria-invalid="Boolean(filterErrors.to)"
            />
            <small v-if="filterErrors.to" class="work-calendar-filters__error">
              {{ filterErrors.to }}
            </small>
          </div>

          <div class="work-calendar-filters__actions">
            <Button label="Consultar" type="submit" />
          </div>
        </form>
      </article>

      <AppLoadingState
        v-if="listLoading"
        label="Cargando calendario laboral..."
      />

      <AppErrorState
        v-else-if="listError"
        title="No pudimos cargar el calendario laboral."
        :description="listError"
        @retry="loadList"
      />

      <AppEmptyState
        v-else-if="listLoaded && listDays.length === 0"
        title="No hay días configurados para este período."
        description="Puedes agregar una fecha para este rango."
        action-label="Agregar día"
        @action="openCreateDialog"
      />

      <WorkCalendarList
        v-else-if="listLoaded"
        :items="listDays"
        :deleting-date="deletingDate"
        @edit="openEditDialog"
        @delete="requestDelete"
      />
    </section>

    <WorkCalendarFormDialog
      :visible="dialogVisible"
      :mode="dialogMode"
      :initial-date="dialogDate"
      :initial-value="selectedDay"
      :lock-date="dialogDateLocked"
      :saving="saving"
      @close="closeDialog"
      @delete="requestDeleteFromDialog"
      @save="handleSave"
    />

    <Dialog
      :visible="Boolean(deleteTarget)"
      modal
      header="Eliminar configuración del día"
      :style="{ width: 'min(100%, 32rem)' }"
      @update:visible="closeDeleteDialog"
    >
      <p class="work-calendar-delete__description">
        Se eliminará la configuración del
        <strong v-if="deleteTarget">
          {{ formatWorkCalendarDateWithWeekday(deleteTarget.date) }}
        </strong>.
      </p>

      <template #footer>
        <div class="work-calendar-form__actions">
          <Button
            label="Volver"
            severity="secondary"
            text
            :disabled="Boolean(deletingDate)"
            @click="closeDeleteDialog"
          />
          <Button
            label="Eliminar día"
            severity="danger"
            :loading="Boolean(deletingDate)"
            @click="confirmDelete"
          />
        </div>
      </template>
    </Dialog>
  </section>
</template>
