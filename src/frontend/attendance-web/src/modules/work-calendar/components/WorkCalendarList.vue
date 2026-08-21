<script setup lang="ts">
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import { formatWorkCalendarDate } from '@/modules/work-calendar/work-calendar.presentation'
import type { WorkCalendarDay } from '@/modules/work-calendar/work-calendar.types'
import WorkCalendarDayTypeTag from './WorkCalendarDayTypeTag.vue'

interface Props {
  deletingDate?: string | null
  items: WorkCalendarDay[]
}

withDefaults(defineProps<Props>(), {
  deletingDate: null,
})

const emit = defineEmits<{
  delete: [day: WorkCalendarDay]
  edit: [day: WorkCalendarDay]
}>()
</script>

<template>
  <div class="work-calendar-list">
    <DataTable
      :value="items"
      data-key="date"
      class="work-calendar-list__table"
      striped-rows
    >
      <Column header="Fecha">
        <template #body="{ data }">
          {{ formatWorkCalendarDate(data.date) }}
        </template>
      </Column>
      <Column header="Tipo de día">
        <template #body="{ data }">
          <WorkCalendarDayTypeTag :day-type="data.dayType" />
        </template>
      </Column>
      <Column header="Descripción">
        <template #body="{ data }">
          {{ data.description ?? 'Sin descripción' }}
        </template>
      </Column>
      <Column header="Acciones">
        <template #body="{ data }">
          <div class="work-calendar-list__actions">
            <Button
              label="Editar"
              severity="secondary"
              text
              @click="emit('edit', data)"
            />
            <Button
              label="Eliminar"
              severity="danger"
              text
              :loading="deletingDate === data.date"
              @click="emit('delete', data)"
            />
          </div>
        </template>
      </Column>
    </DataTable>

    <div class="work-calendar-list__cards">
      <article
        v-for="day in items"
        :key="day.date"
        class="work-calendar-list__card"
      >
        <div class="work-calendar-list__card-header">
          <strong>{{ formatWorkCalendarDate(day.date) }}</strong>
          <WorkCalendarDayTypeTag :day-type="day.dayType" />
        </div>

        <p class="work-calendar-list__card-description">
          {{ day.description ?? 'Sin descripción' }}
        </p>

        <div class="work-calendar-list__actions">
          <Button
            label="Editar"
            severity="secondary"
            text
            @click="emit('edit', day)"
          />
          <Button
            label="Eliminar"
            severity="danger"
            text
            :loading="deletingDate === day.date"
            @click="emit('delete', day)"
          />
        </div>
      </article>
    </div>
  </div>
</template>
