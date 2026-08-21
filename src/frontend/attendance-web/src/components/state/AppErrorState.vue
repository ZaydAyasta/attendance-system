<script setup lang="ts">
import Button from 'primevue/button'

interface Props {
  description?: string
  retryLabel?: string
  title?: string
}

withDefaults(defineProps<Props>(), {
  description: 'Vuelve a intentarlo en unos segundos.',
  retryLabel: 'Volver a intentar',
  title: 'No pudimos cargar la información.',
})

const emit = defineEmits<{
  retry: []
}>()
</script>

<template>
  <section class="app-state app-state--centered" role="alert">
    <span class="app-state__icon" aria-hidden="true">
      <i class="pi pi-exclamation-triangle"></i>
    </span>
    <h2 class="app-state__title">{{ title }}</h2>
    <p class="app-state__description">{{ description }}</p>
    <Button :label="retryLabel" severity="secondary" @click="emit('retry')" />
  </section>
</template>
