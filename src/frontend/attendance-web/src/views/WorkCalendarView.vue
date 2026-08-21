<script setup lang="ts">
import AppPageHeader from '@/components/app/AppPageHeader.vue'
</script>

<template>
  <section>
    <AppPageHeader
      title="Calendario laboral"
      description="Consulta la información del calendario laboral."
    />

    <div class="app-grid app-grid--two">
      <article class="app-surface">
        <h2 class="app-surface__title">Información disponible</h2>
        <ul class="app-feature-list">
          <li>
            <i class="pi pi-calendar" aria-hidden="true"></i>
            <span>Consulta días laborables, no laborables y feriados.</span>
          </li>
          <li>
            <i class="pi pi-users" aria-hidden="true"></i>
            <span>Revisa la información disponible de manera simple.</span>
          </li>
        </ul>
      </article>

      <article class="app-surface">
        <h2 class="app-surface__title">Disponibilidad</h2>
        <p class="app-surface__description">
          Esta sección estará disponible próximamente.
        </p>
      </article>
    </div>
  </section>
</template>
