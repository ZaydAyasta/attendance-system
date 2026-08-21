<script setup lang="ts">
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import AppEmptyState from '@/components/state/AppEmptyState.vue'
</script>

<template>
  <section>
    <AppPageHeader
      title="Asignaciones"
      description="Consulta asignaciones registradas por empleado."
    />

    <div class="app-grid app-grid--two">
      <article class="app-surface">
        <h2 class="app-surface__title">Información disponible</h2>
        <ul class="app-feature-list">
          <li>
            <i class="pi pi-briefcase" aria-hidden="true"></i>
            <span>Revisa asignaciones por fecha y empleado.</span>
          </li>
          <li>
            <i class="pi pi-comment" aria-hidden="true"></i>
            <span>Consulta observaciones registradas cuando existan.</span>
          </li>
        </ul>
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
