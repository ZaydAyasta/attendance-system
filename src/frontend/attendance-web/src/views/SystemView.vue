<script setup lang="ts">
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import AppErrorState from '@/components/state/AppErrorState.vue'
</script>

<template>
  <section>
    <AppPageHeader
      title="Sistema"
      description="Consulta la información general del sistema."
    />

    <div class="app-grid app-grid--two">
      <article class="app-surface">
        <h2 class="app-surface__title">Disponibilidad</h2>
        <p class="app-surface__description">
          Esta sección estará disponible próximamente.
        </p>
      </article>

      <article class="app-surface">
        <h2 class="app-surface__title">Problemas de carga</h2>
        <AppErrorState
          title="No pudimos obtener el diagnóstico."
          description="No pudimos cargar la información en este momento."
        />
      </article>
    </div>
  </section>
</template>
