<script setup lang="ts">
import Button from 'primevue/button'

interface Props {
  actionIcon?: string
  actionLabel?: string
  description?: string
  title: string
}

defineProps<Props>()

const emit = defineEmits<{
  action: []
}>()
</script>

<template>
  <header class="app-page-header">
    <div class="app-page-header__copy">
      <h1 class="app-page-header__title">{{ title }}</h1>
      <p v-if="description" class="app-page-header__description">{{ description }}</p>
    </div>

    <Button
      v-if="actionLabel"
      :icon="actionIcon"
      :label="actionLabel"
      @click="emit('action')"
    />
  </header>
</template>
