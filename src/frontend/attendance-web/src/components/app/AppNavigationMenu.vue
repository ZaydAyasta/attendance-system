<script setup lang="ts">
import type { NavigationItem } from '@/config/navigation'

interface Props {
  currentRoutePath: string
  items: NavigationItem[]
  showDescription?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  showDescription: false,
})

function isActiveRoute(route: string): boolean {
  if (route === '/') {
    return props.currentRoutePath === '/'
  }

  return props.currentRoutePath.startsWith(route)
}
</script>

<template>
  <div class="app-nav">
    <RouterLink
      v-for="item in items"
      :key="item.route"
      :to="item.route"
      class="app-nav__link"
      :class="{ 'app-nav__link--active': isActiveRoute(item.route) }"
    >
      <span class="app-nav__icon" aria-hidden="true">
        <i :class="item.icon"></i>
      </span>
      <span>
        <span class="app-nav__label">{{ item.label }}</span>
        <span v-if="props.showDescription" class="app-nav__description">{{ item.description }}</span>
      </span>
    </RouterLink>
  </div>
</template>
