<script setup lang="ts">
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import QRCodeVue from 'qrcode.vue'
import { computed, reactive, ref, watch } from 'vue'
import { z } from 'zod'
import { useToast } from 'primevue/usetoast'
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import AppLoadingState from '@/components/state/AppLoadingState.vue'
import AppEmptyState from '@/components/state/AppEmptyState.vue'
import AppErrorState from '@/components/state/AppErrorState.vue'
import { createCheckpoint, getCheckpointQr, listCheckpoints, setCheckpointStatus, updateCheckpoint } from '@/modules/checkpoints/checkpoints.service'
import type { Checkpoint, CheckpointQr, CheckpointQrMode, CheckpointType } from '@/modules/checkpoints/checkpoints.types'
import { getUserFriendlyApiError } from '@/services/api-errors'

const toast = useToast()
const items = ref<Checkpoint[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const visible = ref(false)
const saving = ref(false)
const selected = ref<Checkpoint | null>(null)
const statusTarget = ref<Checkpoint | null>(null)
const formError = ref('')
const qrTarget = ref<Checkpoint | null>(null)
const qr = ref<CheckpointQr | null>(null)
const qrError = ref('')
const refreshingQr = ref(false)
const timer = ref<number | undefined>()
const now = ref(Date.now())

const form = reactive<{ code: string; name: string; type: CheckpointType; qrMode: CheckpointQrMode }>({
  code: '',
  name: '',
  type: 'EntryExit',
  qrMode: 'Dynamic',
})

const schema = z.object({
  code: z.string().trim().min(1, 'Ingresa el código.').max(60),
  name: z.string().trim().min(1, 'Ingresa el nombre.').max(160),
  type: z.enum(['EntryExit', 'Cafeteria', 'General']),
  qrMode: z.enum(['Dynamic', 'Static']),
})

const isDynamicQr = computed(() => qrTarget.value?.qrMode === 'Dynamic')
const secondsLeft = computed(() => qr.value?.expiresAt
  ? Math.max(0, Math.ceil((new Date(qr.value.expiresAt).getTime() - now.value) / 1000))
  : 0)

function qrModeLabel(mode: CheckpointQrMode) {
  return mode === 'Dynamic' ? 'Dinámico' : 'Estático'
}

function checkpointTypeLabel(type: CheckpointType) {
  return ({ EntryExit: 'Entrada / salida', Cafeteria: 'Comedor', General: 'General' })[type]
}

watch(() => form.type, (type) => {
  if (type === 'General') form.qrMode = 'Static'
})

async function load() {
  loading.value = true
  try {
    items.value = await listCheckpoints()
    error.value = null
  } catch (exception) {
    error.value = getUserFriendlyApiError(exception)
  } finally {
    loading.value = false
  }
}

function openCreate() {
  selected.value = null
  Object.assign(form, { code: '', name: '', type: 'EntryExit', qrMode: 'Dynamic' })
  formError.value = ''
  visible.value = true
}

function edit(item: Checkpoint) {
  selected.value = item
  Object.assign(form, { code: item.code, name: item.name, type: item.type, qrMode: item.qrMode })
  formError.value = ''
  visible.value = true
}

async function save() {
  const valid = schema.safeParse(form)
  if (!valid.success) {
    formError.value = valid.error.issues[0]?.message ?? ''
    return
  }

  saving.value = true
  try {
    if (selected.value) await updateCheckpoint(selected.value.id, { ...valid.data, version: selected.value.version })
    else await createCheckpoint(valid.data)
    visible.value = false
    toast.add({ severity: 'success', summary: 'Listo', detail: selected.value ? 'Checkpoint actualizado.' : 'Checkpoint creado.', life: 3000 })
    await load()
  } catch (exception) {
    formError.value = getUserFriendlyApiError(exception)
  } finally {
    saving.value = false
  }
}

async function changeStatus() {
  if (!statusTarget.value) return
  try {
    await setCheckpointStatus(statusTarget.value.id, !statusTarget.value.isActive, statusTarget.value.version)
    statusTarget.value = null
    await load()
  } catch (exception) {
    toast.add({ severity: 'error', summary: 'No se pudo actualizar', detail: getUserFriendlyApiError(exception), life: 4000 })
  }
}

function stopQrTimer() {
  if (timer.value) window.clearInterval(timer.value)
  timer.value = undefined
}

async function refreshQr() {
  if (!qrTarget.value) return
  refreshingQr.value = true
  try {
    qr.value = await getCheckpointQr(qrTarget.value.id)
    now.value = Date.now()
    qrError.value = ''
  } catch (exception) {
    qrError.value = getUserFriendlyApiError(exception)
  } finally {
    refreshingQr.value = false
  }
}

async function showQr(item: Checkpoint) {
  stopQrTimer()
  qrTarget.value = item
  qr.value = null
  await refreshQr()
  if (item.qrMode === 'Dynamic') {
    timer.value = window.setInterval(() => {
      now.value = Date.now()
      if (secondsLeft.value <= 5) void refreshQr()
    }, 1000)
  }
}

function closeQr() {
  stopQrTimer()
  qrTarget.value = null
  qr.value = null
}

void load()
</script>

<template>
  <section>
    <AppPageHeader title="Checkpoints" description="Gestiona los puntos físicos de captura." action-label="Crear checkpoint" action-icon="pi pi-plus" @action="openCreate" />
    <AppLoadingState v-if="loading" label="Cargando checkpoints..." />
    <AppErrorState v-else-if="error" title="No pudimos cargar los checkpoints." :description="error" @retry="load" />
    <AppEmptyState v-else-if="!items.length" title="No hay checkpoints." description="Crea el primer punto de captura." action-label="Crear checkpoint" @action="openCreate" />
    <div v-else class="absence-list">
      <DataTable :value="items" class="absence-table">
        <Column header="Código" field="code" />
        <Column header="Nombre" field="name" />
        <Column header="Tipo"><template #body="{ data }">{{ checkpointTypeLabel(data.type) }}</template></Column>
        <Column header="QR"><template #body="{ data }">{{ qrModeLabel(data.qrMode) }}</template></Column>
        <Column header="Estado"><template #body="{ data }">{{ data.isActive ? 'Activo' : 'Desactivado' }}</template></Column>
        <Column header="Acciones"><template #body="{ data }"><Button label="Mostrar QR" text @click="showQr(data)" /><Button label="Editar" text @click="edit(data)" /><Button :label="data.isActive ? 'Desactivar' : 'Activar'" text @click="statusTarget = data" /></template></Column>
      </DataTable>
      <div class="absence-cards">
        <article v-for="item in items" :key="item.id" class="app-surface absence-card">
          <strong>{{ item.name }}</strong>
          <span>Código: {{ item.code }}</span>
          <span>Tipo: {{ checkpointTypeLabel(item.type) }}</span>
          <span>QR: {{ qrModeLabel(item.qrMode) }}</span>
          <span>Estado: {{ item.isActive ? 'Activo' : 'Desactivado' }}</span>
          <div class="absence-card__actions"><Button label="Mostrar QR" text @click="showQr(item)" /><Button label="Editar" text @click="edit(item)" /></div>
        </article>
      </div>
    </div>

    <Dialog :visible="visible" modal class="app-form-dialog" :header="selected ? 'Editar checkpoint' : 'Crear checkpoint'" @update:visible="visible = false">
      <form class="absence-form" @submit.prevent="save">
        <label>Código<input v-model="form.code" /></label>
        <label>Nombre<input v-model="form.name" /></label>
        <label>Tipo<select v-model="form.type"><option value="EntryExit">Entrada / salida</option><option value="Cafeteria">Comedor</option><option value="General">General: entrada, almuerzo, salida y horas extra</option></select></label>
        <label>Modo del QR<select v-model="form.qrMode" :disabled="form.type === 'General'"><option value="Dynamic">Dinámico</option><option value="Static">Estático</option></select></label>
        <p class="checkpoint-qr-mode-help">{{ form.type === 'General' ? 'Los checkpoints generales usan un único QR estático compartido.' : form.qrMode === 'Dynamic' ? 'El QR se renueva automáticamente para limitar su vigencia.' : 'Se crea un único QR permanente para este checkpoint.' }}</p>
        <p v-if="formError">{{ formError }}</p>
        <div class="absence-card__actions"><Button label="Cancelar" text type="button" @click="visible = false" /><Button :label="selected ? 'Guardar cambios' : 'Crear checkpoint'" type="submit" :loading="saving" /></div>
      </form>
    </Dialog>

    <Dialog :visible="Boolean(statusTarget)" modal header="Cambiar estado" @update:visible="statusTarget = null"><p>Se actualizará el estado del checkpoint.</p><template #footer><Button label="Volver" text @click="statusTarget = null" /><Button :label="statusTarget?.isActive ? 'Desactivar' : 'Activar'" @click="changeStatus" /></template></Dialog>
    <Dialog :visible="Boolean(qrTarget)" modal class="checkpoint-qr-dialog" :header="`QR — ${qrTarget?.name ?? ''}`" @update:visible="closeQr">
      <p v-if="isDynamicQr">El código se actualiza automáticamente. Vence en {{ secondsLeft }} s.</p>
      <p v-else>Este QR es estático y no cambia.</p>
      <div v-if="qr" class="checkpoint-qr"><QRCodeVue :value="qr.token" :size="280" level="M" /></div>
      <p v-if="refreshingQr">Actualizando...</p>
      <p v-if="qrError">{{ qrError }}</p>
    </Dialog>
  </section>
</template>
