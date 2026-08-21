<script setup lang="ts">
import Tag from 'primevue/tag'
import { computed } from 'vue'
import { getStatusMetadata } from '@/modules/shared/status-metadata'

interface Props {
  code: string | null | undefined
}

const props = defineProps<Props>()

const metadata = computed(() => getStatusMetadata(props.code))
</script>

<template>
  <Tag :severity="metadata.severity" rounded>
    <span class="app-status-tag__content">
      <i :class="metadata.icon" aria-hidden="true"></i>
      <span>{{ metadata.label }}</span>
    </span>
  </Tag>
</template>
