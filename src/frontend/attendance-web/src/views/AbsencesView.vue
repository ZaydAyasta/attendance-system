<script setup lang="ts">
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import AppEmptyState from '@/components/state/AppEmptyState.vue'
</script>

<template>
  <section>
    <AppPageHeader
      title="Ausencias"
      description="Consulta y organiza la información de ausencias."
    />

    <div class="app-grid app-grid--two">
      <article class="app-surface">
        <h2 class="app-surface__title">Información disponible</h2>
        <ul class="app-feature-list">
          <li>
            <i class="pi pi-file-edit" aria-hidden="true"></i>
            <span>Revisa ausencias autorizadas de forma simple y ordenada.</span>
          </li>
          <li>
            <i class="pi pi-info-circle" aria-hidden="true"></i>
            <span>Usa mensajes claros para facilitar la revisión diaria.</span>
          </li>
        </ul>
      </article>

      <article class="app-surface">
        <h2 class="app-surface__title">Sin información disponible</h2>
        <AppEmptyState
          title="No hay ausencias registradas para este período."
          description="Aún no hay información para mostrar."
          action-label="Registrar ausencia"
        />
      </article>
    </div>
  </section>
</template>
