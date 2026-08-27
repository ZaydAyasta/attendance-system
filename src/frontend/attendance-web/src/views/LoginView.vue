<script setup lang="ts">
import axios from 'axios'
import Button from 'primevue/button'
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const form = reactive({ usernameOrEmail: '', password: '' })
const error = ref('')

async function submit(): Promise<void> {
  error.value = ''
  try {
    await authStore.login(form.usernameOrEmail.trim(), form.password)
    await router.replace(typeof route.query.redirect === 'string' ? route.query.redirect : '/')
  } catch (exception) {
    error.value = axios.isAxiosError(exception) && exception.response?.status === 401
      ? 'Usuario o contraseña incorrectos.'
      : 'No pudimos iniciar sesión. Inténtalo nuevamente.'
  }
}
</script>

<template>
  <main class="login-page">
    <section class="login-card app-surface" aria-labelledby="login-title">
      <p class="login-card__eyebrow">Sistema de Asistencia</p>
      <h1 id="login-title">Iniciar sesión</h1>
      <p>Ingresa con tu cuenta corporativa del sistema.</p>
      <form class="absence-form" @submit.prevent="submit">
        <label>Usuario o correo<input v-model="form.usernameOrEmail" autocomplete="username" required /></label>
        <label>Contraseña<input v-model="form.password" type="password" autocomplete="current-password" required /></label>
        <p v-if="error" class="login-card__error" role="alert">{{ error }}</p>
        <Button label="Iniciar sesión" type="submit" :loading="authStore.loading" :disabled="authStore.loading" />
      </form>
    </section>
  </main>
</template>
