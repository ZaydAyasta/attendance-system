<script setup lang="ts">
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import AppStatusTag from '@/components/app/AppStatusTag.vue'
import AppEmptyState from '@/components/state/AppEmptyState.vue'
import AppErrorState from '@/components/state/AppErrorState.vue'
</script>

<template>
  <section>
    <AppPageHeader
      title="Asistencia"
      description="Consulta estados de asistencia y marcaciones registradas."
    />

    <div class="app-grid app-grid--two">
      <article class="app-surface">
        <h2 class="app-surface__title">Estados disponibles</h2>
        <p class="app-surface__description">
          Los estados se muestran con etiquetas fáciles de reconocer.
        </p>

        <div class="app-status-row" style="margin-top: 1rem">
          <AppStatusTag code="Present" />
          <AppStatusTag code="Incomplete" />
          <AppStatusTag code="UnexcusedAbsence" />
          <AppStatusTag code="MedicalLeave" />
          <AppStatusTag code="NonWorkingDay" />
          <AppStatusTag code="MissingWorkCalendarDay" />
        </div>
      </article>

      <article class="app-surface">
        <h2 class="app-surface__title">Información destacada</h2>
        <ul class="app-feature-list">
          <li>
            <i class="pi pi-clock" aria-hidden="true"></i>
            <span>Asistencia diaria con un formato claro y fácil de revisar.</span>
          </li>
          <li>
            <i class="pi pi-id-card" aria-hidden="true"></i>
            <span>Estados resumidos para identificar rápidamente cada caso.</span>
          </li>
          <li>
            <i class="pi pi-mobile" aria-hidden="true"></i>
            <span>Consulta cómoda tanto en escritorio como en móvil.</span>
          </li>
        </ul>
      </article>

      <article class="app-surface">
        <h2 class="app-surface__title">Sin marcaciones</h2>
        <AppEmptyState
          title="Aún no hay marcaciones para mostrar."
          description="Aún no hay información para mostrar."
        />
      </article>

      <article class="app-surface">
        <h2 class="app-surface__title">Problemas de carga</h2>
        <AppErrorState
          description="No pudimos cargar la información en este momento."
        />
      </article>
    </div>
  </section>
</template>
