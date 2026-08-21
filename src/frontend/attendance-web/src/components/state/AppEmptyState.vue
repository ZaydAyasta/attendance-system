<script setup lang="ts">
import Button from 'primevue/button'

interface Props {
  actionLabel?: string
  description: string
  title?: string
}

withDefaults(defineProps<Props>(), {
  title: 'No hay información para mostrar.',
})

const emit = defineEmits<{
  action: []
}>()
</script>

<template>
  <section class="app-state app-state--centered">
    <span class="app-state__icon" aria-hidden="true">
      <i class="pi pi-inbox"></i>
    </span>
    <h2 class="app-state__title">{{ title }}</h2>
    <p class="app-state__description">{{ description }}</p>
    <Button v-if="actionLabel" :label="actionLabel" text @click="emit('action')" />
  </section>
</template>
