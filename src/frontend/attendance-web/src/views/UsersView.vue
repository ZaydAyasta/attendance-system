<script setup lang="ts">
import Button from 'primevue/button'
import Column from 'primevue/column'
import DataTable from 'primevue/datatable'
import Dialog from 'primevue/dialog'
import Tag from 'primevue/tag'
import { computed, reactive, ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import AppEmptyState from '@/components/state/AppEmptyState.vue'
import AppErrorState from '@/components/state/AppErrorState.vue'
import AppLoadingState from '@/components/state/AppLoadingState.vue'
import AppPageHeader from '@/components/app/AppPageHeader.vue'
import { listActiveEmployees, type EmployeeOption } from '@/modules/employees/employees.service'
import { createIdentityUser, listIdentityUsers, resetIdentityUserPassword, setIdentityUserStatus, updateIdentityUser, type IdentityRole, type IdentityUser } from '@/modules/identity/users.service'
import { getUserFriendlyApiError } from '@/services/api-errors'

const toast = useToast()
const users = ref<IdentityUser[]>([])
const employees = ref<EmployeeOption[]>([])
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)
const formError = ref('')
const visible = ref(false)
const resetVisible = ref(false)
const statusVisible = ref(false)
const selected = ref<IdentityUser | null>(null)
const passwordVisible = ref(false)
const form = reactive({ username: '', email: '', password: '', role: 'User' as IdentityRole, employeeId: '' })
const resetPassword = ref('')
const resetPasswordVisible = ref(false)
const roleLabels: Record<IdentityRole, string> = { Admin: 'Administrador', User: 'Usuario', IT: 'TI' }
const passwordHelp = 'Mínimo 8 caracteres, con mayúscula, minúscula y número.'
const requiresEmployee = computed(() => form.role === 'User')
const availableEmployees = computed(() => employees.value.filter(employee => !users.value.some(user => user.employeeId === employee.id && user.id !== selected.value?.id)))

async function load(): Promise<void> {
  loading.value = true
  try {
    const [loadedUsers, loadedEmployees] = await Promise.all([listIdentityUsers(), listActiveEmployees()])
    users.value = loadedUsers
    employees.value = loadedEmployees
    error.value = null
  } catch (exception) {
    error.value = getUserFriendlyApiError(exception)
  } finally {
    loading.value = false
  }
}

function openCreate(): void {
  selected.value = null
  formError.value = ''
  passwordVisible.value = false
  Object.assign(form, { username: '', email: '', password: '', role: 'User', employeeId: '' })
  visible.value = true
}

function openEdit(user: IdentityUser): void {
  selected.value = user
  formError.value = ''
  Object.assign(form, { username: user.username, email: user.email ?? '', password: '', role: user.role, employeeId: user.employeeId ?? '' })
  visible.value = true
}

function validateForm(): string | null {
  if (!form.username.trim()) return 'Ingresa el usuario o correo.'
  if (!selected.value && !form.password) return 'Ingresa la contraseña inicial.'
  if (requiresEmployee.value && !form.employeeId) return 'Una cuenta Usuario debe tener un empleado asociado.'
  return null
}

async function save(): Promise<void> {
  formError.value = validateForm() ?? ''
  if (formError.value) return
  saving.value = true
  try {
    if (selected.value) {
      await updateIdentityUser(selected.value.id, { username: form.username.trim(), email: form.email.trim() || null, role: form.role })
    } else {
      await createIdentityUser({ username: form.username.trim(), email: form.email.trim() || null, password: form.password, role: form.role, employeeId: form.employeeId || null })
    }
    visible.value = false
    form.password = ''
    toast.add({ severity: 'success', summary: 'Listo', detail: selected.value ? 'Cuenta actualizada.' : 'Cuenta creada correctamente.', life: 3000 })
    await load()
  } catch (exception) {
    formError.value = getUserFriendlyApiError(exception)
  } finally {
    saving.value = false
  }
}

async function confirmStatus(): Promise<void> {
  if (!selected.value) return
  saving.value = true
  try {
    await setIdentityUserStatus(selected.value.id, !selected.value.isActive)
    statusVisible.value = false
    toast.add({ severity: 'success', summary: 'Estado actualizado', detail: 'La cuenta fue actualizada.', life: 3000 })
    await load()
  } catch (exception) {
    formError.value = getUserFriendlyApiError(exception)
  } finally {
    saving.value = false
  }
}

async function confirmReset(): Promise<void> {
  if (!selected.value) return
  if (!resetPassword.value) { formError.value = 'Ingresa la nueva contraseña.'; return }
  saving.value = true
  try {
    await resetIdentityUserPassword(selected.value.id, resetPassword.value)
    resetPassword.value = ''
    resetVisible.value = false
    toast.add({ severity: 'success', summary: 'Contraseña restablecida', detail: 'La contraseña anterior ya no es válida.', life: 3000 })
  } catch (exception) {
    formError.value = getUserFriendlyApiError(exception)
  } finally {
    saving.value = false
  }
}

function roleLabel(role: IdentityRole): string { return roleLabels[role] }
void load()
</script>

<template>
  <section>
    <AppPageHeader title="Usuarios del sistema" description="Administra las cuentas de acceso y sus permisos." action-label="Crear cuenta" action-icon="pi pi-plus" @action="openCreate" />
    <AppLoadingState v-if="loading" label="Cargando cuentas..." />
    <AppErrorState v-else-if="error" title="No pudimos cargar las cuentas." :description="error" @retry="load" />
    <AppEmptyState v-else-if="!users.length" title="No hay cuentas registradas." description="Crea la primera cuenta del sistema." action-label="Crear cuenta" @action="openCreate" />
    <div v-else class="absence-list">
      <DataTable :value="users" class="absence-table">
        <Column header="Usuario / correo"><template #body="{ data }"><strong>{{ data.username }}</strong><small>{{ data.email ?? 'Sin correo registrado' }}</small></template></Column>
        <Column header="Empleado vinculado"><template #body="{ data }">{{ data.employee ? `${data.employee.employeeCode} — ${data.employee.fullName}` : 'Sin vínculo' }}</template></Column>
        <Column header="Rol"><template #body="{ data }">{{ roleLabel(data.role) }}</template></Column>
        <Column header="Estado"><template #body="{ data }"><Tag :value="data.isActive ? 'Activa' : 'Desactivada'" :severity="data.isActive ? 'success' : 'secondary'" /></template></Column>
        <Column header="Acciones"><template #body="{ data }"><div class="absence-card__actions"><Button label="Editar" text @click="openEdit(data)" /><Button label="Restablecer contraseña" text @click="selected = data; formError = ''; resetPassword = ''; resetVisible = true" /><Button :label="data.isActive ? 'Desactivar' : 'Activar'" text @click="selected = data; formError = ''; statusVisible = true" /></div></template></Column>
      </DataTable>
      <div class="absence-cards">
        <article v-for="user in users" :key="user.id" class="app-surface absence-card"><strong>{{ user.username }}</strong><small>{{ user.email ?? 'Sin correo registrado' }}</small><span>Empleado: {{ user.employee ? `${user.employee.employeeCode} — ${user.employee.fullName}` : 'Sin vínculo' }}</span><span>Rol: {{ roleLabel(user.role) }}</span><Tag :value="user.isActive ? 'Activa' : 'Desactivada'" :severity="user.isActive ? 'success' : 'secondary'" /><div class="absence-card__actions"><Button label="Editar" text @click="openEdit(user)" /><Button label="Restablecer contraseña" text @click="selected = user; formError = ''; resetPassword = ''; resetVisible = true" /><Button :label="user.isActive ? 'Desactivar' : 'Activar'" text @click="selected = user; formError = ''; statusVisible = true" /></div></article>
      </div>
    </div>

    <Dialog :visible="visible" modal class="app-form-dialog" :header="selected ? 'Editar cuenta' : 'Crear cuenta'" @update:visible="visible = false">
      <form class="absence-form" @submit.prevent="save">
        <label>Usuario<input v-model="form.username" autocomplete="username" required /></label>
        <label>Correo electrónico<input v-model="form.email" type="email" autocomplete="email" /></label>
        <label>Rol<select v-model="form.role"><option value="Admin">Administrador</option><option value="User" :disabled="Boolean(selected && !selected.employeeId)">Usuario</option><option value="IT">TI</option></select></label>
        <label v-if="!selected">Empleado<select v-model="form.employeeId" :required="requiresEmployee"><option value="">{{ requiresEmployee ? 'Selecciona un empleado' : 'Sin vínculo laboral' }}</option><option v-for="employee in availableEmployees" :key="employee.id" :value="employee.id">{{ employee.employeeCode }} — {{ employee.fullName }}</option></select><small v-if="requiresEmployee">Obligatorio para una cuenta Usuario.</small></label>
        <p v-else-if="selected.employee" class="users-view__immutable">Empleado vinculado: {{ selected.employee.employeeCode }} — {{ selected.employee.fullName }}. El vínculo no se edita desde esta pantalla.</p>
        <template v-if="!selected"><label>Contraseña inicial<div class="users-view__password"><input v-model="form.password" :type="passwordVisible ? 'text' : 'password'" autocomplete="new-password" required /><Button :label="passwordVisible ? 'Ocultar' : 'Mostrar'" text type="button" @click="passwordVisible = !passwordVisible" /></div></label><small>{{ passwordHelp }}</small></template>
        <p v-if="formError" class="login-card__error" role="alert">{{ formError }}</p>
        <div class="absence-card__actions"><Button label="Cancelar" text type="button" @click="visible = false" /><Button :label="selected ? 'Guardar cambios' : 'Crear cuenta'" type="submit" :loading="saving" /></div>
      </form>
    </Dialog>

    <Dialog :visible="resetVisible" modal class="app-form-dialog" header="Restablecer contraseña" @update:visible="resetVisible = false">
      <p>La contraseña de esta cuenta será reemplazada.</p><label class="users-view__reset-label">Nueva contraseña<div class="users-view__password"><input v-model="resetPassword" :type="resetPasswordVisible ? 'text' : 'password'" autocomplete="new-password" /><Button :label="resetPasswordVisible ? 'Ocultar' : 'Mostrar'" text type="button" @click="resetPasswordVisible = !resetPasswordVisible" /></div></label><small>{{ passwordHelp }}</small><p v-if="formError" class="login-card__error" role="alert">{{ formError }}</p>
      <template #footer><Button label="Cancelar" text @click="resetVisible = false" /><Button label="Restablecer contraseña" :loading="saving" @click="confirmReset" /></template>
    </Dialog>
    <Dialog :visible="statusVisible" modal class="app-form-dialog" :header="selected?.isActive ? 'Desactivar cuenta' : 'Activar cuenta'" @update:visible="statusVisible = false"><p>{{ selected?.isActive ? 'La cuenta dejará de poder iniciar sesión y sus sesiones se invalidarán en pocos minutos.' : 'La cuenta podrá iniciar sesión nuevamente.' }}</p><p v-if="formError" class="login-card__error" role="alert">{{ formError }}</p><template #footer><Button label="Cancelar" text @click="statusVisible = false" /><Button :label="selected?.isActive ? 'Desactivar cuenta' : 'Activar cuenta'" :loading="saving" @click="confirmStatus" /></template></Dialog>
  </section>
</template>

<style scoped>
.users-view__password { display: flex; gap: .35rem; align-items: center; }.users-view__password input { min-width: 0; }.users-view__immutable { margin: 0; color: var(--app-muted); }.users-view__reset-label { display: grid; gap: .35rem; font-weight: 600; }
</style>
