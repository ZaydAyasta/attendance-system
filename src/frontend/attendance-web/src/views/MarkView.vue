<script setup lang="ts">
import Button from 'primevue/button'
import jsQR from 'jsqr'
import { onBeforeUnmount, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import { captureActionLabel, getTodayMarks, markCapture, resolveCapture } from '@/modules/attendance-capture/attendance-capture.service'
import type { CaptureAction, CaptureResolution, TodayMark } from '@/modules/attendance-capture/attendance-capture.types'
import { getUserFriendlyApiError } from '@/services/api-errors'

const toast = useToast()
const video = ref<HTMLVideoElement | null>(null)
const scannerError = ref('')
const scanning = ref(false)
const resolving = ref(false)
const marking = ref(false)
const token = ref('')
const resolution = ref<CaptureResolution | null>(null)
const marks = ref<TodayMark[]>([])
const isDevelopment = import.meta.env.DEV
const labels: Record<string, string> = {
  Entry: 'Entrada', LunchStart: 'Inicio de almuerzo', LunchEnd: 'Fin de almuerzo', Exit: 'Salida',
  CommissionExit: 'Salida a comisión', CommissionReturn: 'Regreso de comisión', OtherExit: 'Otra salida',
  OtherReturn: 'Retorno', OvertimeStart: 'Inicio de horas extra', OvertimeEnd: 'Fin de horas extra',
}

let cameraRequest: symbol | undefined
let stream: MediaStream | undefined
let scanTimer: number | undefined

async function loadMarks() {
  try {
    marks.value = await getTodayMarks()
  } catch (cause) {
    scannerError.value = getUserFriendlyApiError(cause)
  }
}

async function resolve(tokenValue: string) {
  if (!tokenValue || resolving.value) return
  resolving.value = true
  scannerError.value = ''
  try {
    token.value = tokenValue
    resolution.value = await resolveCapture(tokenValue)
  } catch (cause) {
    resolution.value = null
    scannerError.value = getUserFriendlyApiError(cause)
  } finally {
    resolving.value = false
  }
}

function stopCamera() {
  cameraRequest = undefined
  window.clearInterval(scanTimer)
  scanTimer = undefined
  stream?.getTracks().forEach(track => track.stop())
  stream = undefined
  if (video.value) video.value.srcObject = null
  scanning.value = false
}

async function startCamera() {
  const preview = video.value
  if (scanning.value || !preview) return
  scannerError.value = ''
  if (!navigator.mediaDevices?.getUserMedia) {
    scannerError.value = 'Tu navegador no permite usar la cámara.'
    return
  }

  const request = Symbol()
  cameraRequest = request
  scanning.value = true
  resolution.value = null
  token.value = ''
  try {
    const camera = await navigator.mediaDevices.getUserMedia({
      audio: false,
      video: { facingMode: { ideal: 'environment' }, width: { ideal: 960 } },
    })
    if (cameraRequest !== request) {
      camera.getTracks().forEach(track => track.stop())
      return
    }

    stream = camera
    preview.srcObject = camera
    await preview.play()
    if (cameraRequest !== request) return

    const canvas = document.createElement('canvas')
    const frame = canvas.getContext('2d', { willReadFrequently: true })
    if (!frame) throw new Error('No se pudo procesar el video.')
    scanTimer = window.setInterval(() => {
      if (preview.readyState < HTMLMediaElement.HAVE_CURRENT_DATA || !preview.videoWidth || !preview.videoHeight) return

      const scale = Math.min(1, 960 / Math.max(preview.videoWidth, preview.videoHeight))
      const width = Math.max(1, Math.round(preview.videoWidth * scale))
      const height = Math.max(1, Math.round(preview.videoHeight * scale))
      if (canvas.width !== width || canvas.height !== height) {
        canvas.width = width
        canvas.height = height
      }
      frame.drawImage(preview, 0, 0, width, height)
      const { data } = frame.getImageData(0, 0, width, height)
      const result = jsQR(data, width, height)
      if (result?.data) {
        stopCamera()
        void resolve(result.data)
      }
    }, 180)
  } catch {
    if (cameraRequest !== request) return
    stopCamera()
    scannerError.value = 'No se pudo abrir la cámara. Revisa el permiso e inténtalo nuevamente.'
  }
}

async function confirm(action: CaptureAction) {
  if (!token.value || marking.value) return
  marking.value = true
  try {
    const result = await markCapture(token.value, action)
    const time = new Intl.DateTimeFormat('es-PE', { timeStyle: 'short', timeZone: 'America/Lima' }).format(new Date(result.occurredAt))
    toast.add({ severity: 'success', summary: 'Marcación registrada', detail: `${result.message} ${time}`, life: 4000 })
    resolution.value = null
    token.value = ''
    await loadMarks()
  } catch (cause) {
    scannerError.value = getUserFriendlyApiError(cause)
  } finally {
    marking.value = false
  }
}

onBeforeUnmount(stopCamera)
void loadMarks()
</script>

<template><section class="mark-view"><AppPageHeader title="Marcar asistencia" description="Escanea el código QR del checkpoint."/><article class="app-surface mark-camera"><video ref="video" autoplay muted playsinline aria-label="Vista de cámara para escanear código QR"></video><p v-if="!scanning">Usa la cámara para leer el QR del checkpoint.</p><Button :label="scanning?'Detener cámara':'Abrir cámara'" :icon="scanning?'pi pi-stop':'pi pi-camera'" :disabled="!scanning && (resolving || marking)" @click="scanning?stopCamera():startCamera()"/></article><details v-if="isDevelopment" class="app-surface"><summary>Diagnóstico: pegar token QR</summary><form class="absence-form" @submit.prevent="resolve(token)"><input v-model="token" aria-label="Token QR de diagnóstico"/><Button label="Resolver token" type="submit" :loading="resolving"/></form></details><p v-if="scannerError" class="mark-error" role="alert">{{scannerError}}</p><article v-if="resolution" class="app-surface mark-resolution"><h2>{{resolution.checkpoint.name}}</h2><p>{{resolution.message}}</p><div class="mark-actions"><Button v-for="action in resolution.availableActions" :key="action" :label="captureActionLabel(action)" :loading="marking" :disabled="marking" @click="confirm(action)"/></div></article><article class="app-surface"><h2 class="app-surface__title">Marcaciones de hoy</h2><p v-if="!marks.length">Aún no registras marcaciones hoy.</p><ul v-else class="mark-history"><li v-for="mark in marks" :key="mark.id"><strong>{{labels[mark.type]??mark.type}}</strong><span>{{new Intl.DateTimeFormat('es-PE',{timeStyle:'short',timeZone:'America/Lima'}).format(new Date(mark.occurredAt))}}</span><small v-if="mark.checkpoint">{{mark.checkpoint.name}}</small></li></ul></article></section></template>
