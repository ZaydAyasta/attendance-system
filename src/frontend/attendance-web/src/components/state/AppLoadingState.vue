<script setup lang="ts">
import ProgressSpinner from 'primevue/progressspinner'

interface Props {
  centered?: boolean
  label?: string
}

withDefaults(defineProps<Props>(), {
  centered: true,
  label: 'Cargando información...',
})
</script>

<template>
  <section class="app-state" :class="{ 'app-state--centered': centered }" aria-live="polite">
    <ProgressSpinner
      style="width: 2.75rem; height: 2.75rem"
      strokeWidth="4"
      fill="transparent"
      aria-label="Cargando"
    />
    <p class="app-state__description">{{ label }}</p>
  </section>
</template>
