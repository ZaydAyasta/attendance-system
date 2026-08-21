<script setup lang="ts">
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import AppEmptyState from '@/components/state/AppEmptyState.vue'
</script>

<template>
  <section>
    <AppPageHeader
      title="Reportes"
      description="Revisa los reportes disponibles del sistema."
    />

    <div class="app-grid app-grid--two">
      <article class="app-surface">
        <h2 class="app-surface__title">Disponibilidad</h2>
        <p class="app-surface__description">
          Esta sección estará disponible próximamente.
        </p>
      </article>

      <article class="app-surface">
        <h2 class="app-surface__title">Sin información disponible</h2>
        <AppEmptyState
          title="Aún no hay información para mostrar."
          description="Esta sección estará disponible próximamente."
        />
      </article>
    </div>
  </section>
</template>
