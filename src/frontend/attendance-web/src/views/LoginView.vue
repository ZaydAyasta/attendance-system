<script setup lang="ts">
import axios from 'axios'
import Button from 'primevue/button'
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import brandLogo from '@/assets/image-1.png'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const form = reactive({ usernameOrEmail: '', password: '', rememberMe: true })
const error = ref('')
const passwordVisible = ref(false)

async function submit(): Promise<void> {
  error.value = ''
  try {
    await authStore.login(form.usernameOrEmail.trim(), form.password, form.rememberMe)
    await router.replace(typeof route.query.redirect === 'string' ? route.query.redirect : '/')
  } catch (exception) {
    error.value = axios.isAxiosError(exception) && exception.response?.status === 401
      ? 'Usuario o contraseña incorrectos.'
      : axios.isAxiosError(exception) && exception.response?.status === 429
        ? 'Demasiados intentos. Espera un minuto antes de volver a intentarlo.'
        : 'No pudimos iniciar sesión. Inténtalo nuevamente.'
  }
}
</script>

<template>
  <main class="login-page">
    <section class="login-card" aria-labelledby="login-title">
      <div class="login-card__brand-panel">
        <img :src="brandLogo" alt="Nakama" class="login-card__logo" />
        <div>
          <p class="login-card__eyebrow">Sistema de asistencia</p>
          <p class="login-card__brand-copy">Una operación clara empieza con un registro confiable.</p>
        </div>
      </div>
      <div class="login-card__form-panel">
        <div>
          <p class="login-card__eyebrow">Acceso seguro</p>
          <h1 id="login-title">Iniciar sesión</h1>
          <p class="login-card__intro">Ingresa con tu cuenta corporativa para continuar.</p>
        </div>
        <form class="absence-form" @submit.prevent="submit">
          <label>Usuario o correo<input v-model="form.usernameOrEmail" autocomplete="username" required /></label>
          <label>
            Contraseña
            <span class="login-card__password-field">
              <input
                v-model="form.password"
                :type="passwordVisible ? 'text' : 'password'"
                autocomplete="current-password"
                required
              />
              <button
                class="login-card__password-toggle"
                type="button"
                :aria-label="passwordVisible ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                :aria-pressed="passwordVisible"
                :title="passwordVisible ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                @click="passwordVisible = !passwordVisible"
              >
                <i :class="passwordVisible ? 'pi pi-eye-slash' : 'pi pi-eye'" aria-hidden="true" />
              </button>
            </span>
          </label>
          <label class="login-card__remember"><input v-model="form.rememberMe" type="checkbox" /> Mantener sesión iniciada</label>
          <p v-if="error" class="login-card__error" role="alert">{{ error }}</p>
          <Button label="Iniciar sesión" type="submit" :loading="authStore.loading" :disabled="authStore.loading" />
        </form>
      </div>
    </section>
  </main>
</template>
