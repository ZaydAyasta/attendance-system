<script setup lang="ts">
import Button from 'primevue/button'
import Message from 'primevue/message'
import { computed } from 'vue'
import {
  buildWorkCalendarMonth,
  formatWorkCalendarDateWithWeekday,
  getMonthLabel,
  getWorkCalendarVisualStateMetadata,
  workCalendarWeekdayHeaders,
} from '@/modules/work-calendar/work-calendar.presentation'
import type { WorkCalendarDay } from '@/modules/work-calendar/work-calendar.types'

interface Props {
  days: WorkCalendarDay[]
  error?: string | null
  loading?: boolean
  month: Date
  ready?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  error: null,
  loading: false,
  ready: false,
})

const emit = defineEmits<{
  currentMonth: []
  nextMonth: []
  previousMonth: []
  retry: []
  selectDay: [day: WorkCalendarDay]
  selectEmpty: [date: string]
}>()

const configuredDays = computed(() => new Map(props.days.map((day) => [day.date, day])))

const monthLabel = computed(() => getMonthLabel(props.month))

const cells = computed(() =>
  buildWorkCalendarMonth(props.month).map((cell) => {
    const configuredDay = configuredDays.value.get(cell.date)
    const state = props.ready
      ? configuredDay?.dayType ?? 'Unconfigured'
      : props.loading
        ? 'Unavailable'
        : 'Unavailable'

    return {
      ...cell,
      day: configuredDay ?? null,
      metadata: getWorkCalendarVisualStateMetadata(state),
    }
  }),
)

const legendItems = computed(() =>
  ['WorkingDay', 'NonWorkingDay', 'Holiday', 'Unconfigured'].map((state) =>
    getWorkCalendarVisualStateMetadata(state as 'WorkingDay' | 'NonWorkingDay' | 'Holiday' | 'Unconfigured'),
  ),
)

function handleSelect(date: string): void {
  if (!props.ready) {
    return
  }

  const configuredDay = configuredDays.value.get(date)

  if (configuredDay) {
    emit('selectDay', configuredDay)
    return
  }

  emit('selectEmpty', date)
}
</script>

<template>
  <section class="app-surface work-calendar-month">
    <div class="work-calendar-month__toolbar">
      <div class="work-calendar-month__navigation">
        <Button
          text
          icon="pi pi-angle-left"
          aria-label="Mes anterior"
          data-testid="work-calendar-previous-month"
          @click="emit('previousMonth')"
        />
        <h2 class="work-calendar-month__title" data-testid="work-calendar-month-label">
          {{ monthLabel }}
        </h2>
        <Button
          text
          icon="pi pi-angle-right"
          aria-label="Mes siguiente"
          data-testid="work-calendar-next-month"
          @click="emit('nextMonth')"
        />
      </div>

      <div class="work-calendar-month__toolbar-actions">
        <span v-if="loading" class="work-calendar-month__loading">
          Cargando calendario laboral...
        </span>
        <Button
          label="Hoy"
          text
          data-testid="work-calendar-current-month"
          @click="emit('currentMonth')"
        />
      </div>
    </div>

    <Message
      v-if="error"
      severity="error"
      :closable="false"
      class="work-calendar-month__error"
    >
      <div class="work-calendar-month__error-content">
        <span>No pudimos cargar la configuración de este mes.</span>
        <Button label="Volver a intentar" text @click="emit('retry')" />
      </div>
    </Message>

    <div class="work-calendar-month__legend" aria-label="Leyenda de estados">
      <span
        v-for="item in legendItems"
        :key="item.label"
        class="work-calendar-month__legend-item"
      >
        <i :class="item.icon" aria-hidden="true"></i>
        <span>{{ item.label }}</span>
      </span>
    </div>

    <div class="work-calendar-month__grid" role="grid" aria-label="Calendario laboral mensual">
      <div
        v-for="header in workCalendarWeekdayHeaders"
        :key="header"
        class="work-calendar-month__weekday"
        role="columnheader"
      >
        {{ header }}
      </div>

      <button
        v-for="cell in cells"
        :key="cell.date"
        type="button"
        class="work-calendar-month__day"
        :class="{
          'work-calendar-month__day--outside': !cell.isCurrentMonth,
          'work-calendar-month__day--disabled': !ready,
        }"
        :disabled="!ready"
        :aria-label="formatWorkCalendarDateWithWeekday(cell.date)"
        :data-testid="`work-calendar-day-${cell.date}`"
        @click="handleSelect(cell.date)"
      >
        <span class="work-calendar-month__day-number">{{ cell.dayNumber }}</span>

        <span class="work-calendar-month__day-status">
          <i :class="cell.metadata.icon" aria-hidden="true"></i>
          <span class="work-calendar-month__day-status-full">
            {{ cell.metadata.label }}
          </span>
          <span class="work-calendar-month__day-status-short">
            {{ cell.metadata.shortLabel }}
          </span>
        </span>
      </button>
    </div>
  </section>
</template>
