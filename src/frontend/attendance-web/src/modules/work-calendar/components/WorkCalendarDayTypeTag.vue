<script setup lang="ts">
import Tag from 'primevue/tag'
import { computed } from 'vue'
import { getWorkCalendarDayTypeMetadata } from '@/modules/work-calendar/work-calendar.presentation'
import type { WorkCalendarDayType } from '@/modules/work-calendar/work-calendar.types'

interface Props {
  dayType: WorkCalendarDayType
}

const props = defineProps<Props>()

const metadata = computed(() => getWorkCalendarDayTypeMetadata(props.dayType))
</script>

<template>
  <Tag :severity="metadata.severity" rounded>
    <span class="app-status-tag__content">
      <i :class="metadata.icon" aria-hidden="true"></i>
      <span>{{ metadata.label }}</span>
    </span>
  </Tag>
</template>
