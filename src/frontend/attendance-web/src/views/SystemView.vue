<script setup lang="ts">
import AppPageHeader from '@/components/app/AppPageHeader.vue'
</script>

<template>
  <section>
    <AppPageHeader
      title="Sistema"
      description="Prepara los puntos de marcación para que el personal registre su asistencia."
    />

    <div class="system-guide">
      <article class="app-surface system-guide__hero">
        <div>
          <p class="system-guide__eyebrow">Operación de TI</p>
          <h2 class="system-guide__title">El QR se prepara desde cada checkpoint</h2>
          <p class="system-guide__description">
            Revisa que el punto esté activo, muestra su QR y mantenlo disponible para los empleados.
          </p>
        </div>
        <RouterLink class="system-guide__cta" to="/checkpoints">
          Abrir checkpoints
          <i class="pi pi-arrow-right" aria-hidden="true" />
        </RouterLink>
      </article>

      <article class="app-surface">
        <h2 class="app-surface__title">Antes de iniciar</h2>
        <ol class="system-guide__steps">
          <li>
            <span class="system-guide__step-number">1</span>
            <div>
              <strong>Verifica el checkpoint</strong>
              <p>Confirma que el punto correcto esté activo y tenga un nombre reconocible.</p>
            </div>
          </li>
          <li>
            <span class="system-guide__step-number">2</span>
            <div>
              <strong>Muestra el código QR</strong>
              <p>Ábrelo desde el checkpoint para que el personal pueda escanearlo.</p>
            </div>
          </li>
          <li>
            <span class="system-guide__step-number">3</span>
            <div>
              <strong>Renueva el QR si hace falta</strong>
              <p>Los QR dinámicos vencen en 30 segundos y se actualizan automáticamente.</p>
            </div>
          </li>
        </ol>
      </article>

      <article class="app-surface system-guide__scope">
        <h2 class="app-surface__title">Tu alcance</h2>
        <p class="app-surface__description">
          Como miembro de TI puedes crear, editar, activar o desactivar checkpoints y operar sus QR.
          La gestión de empleados, usuarios, calendario y reportes corresponde a Administración.
        </p>
      </article>
    </div>
  </section>
</template>
