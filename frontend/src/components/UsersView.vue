<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { 
  UserPlus, 
  UserCheck, 
  Shield, 
  Lock, 
  Edit3, 
  Trash2, 
  Search, 
  Clock, 
  X, 
  Save, 
  KeyRound,
  ShieldCheck,
  User as UserIcon
} from 'lucide-vue-next';
import type { User } from '../services/api';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

const users = ref<User[]>([]);
const search = ref('');
const loading = ref(true);

const showModal = ref(false);
const editingUser = ref<User | null>(null);

const formUsername = ref('');
const formPassword = ref('');
const formFullName = ref('');
const formRole = ref<'Admin' | 'Worker'>('Worker');
const modalError = ref('');
const modalLoading = ref(false);

const loadUsers = async () => {
  try {
    loading.value = true;
    users.value = await api.getUsers();
  } catch (err) {
    console.error('Error loading users', err);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadUsers();
});

const filtered = computed(() => {
  if (!search.value.trim()) return users.value;
  const q = search.value.toLowerCase();
  return users.value.filter(u => 
    u.username.toLowerCase().includes(q) || 
    u.fullName.toLowerCase().includes(q)
  );
});

const openNewUserModal = () => {
  sounds.playClick();
  editingUser.value = null;
  formUsername.value = '';
  formPassword.value = '';
  formFullName.value = '';
  formRole.value = 'Worker';
  modalError.value = '';
  showModal.value = true;
};

const openEditUserModal = (u: User) => {
  sounds.playClick();
  editingUser.value = u;
  formUsername.value = u.username;
  formPassword.value = ''; // empty means don't change
  formFullName.value = u.fullName;
  formRole.value = u.role;
  modalError.value = '';
  showModal.value = true;
};

const saveUser = async () => {
  modalError.value = '';

  if (!editingUser.value && !formUsername.value.trim()) {
    modalError.value = 'El nombre de usuario es obligatorio';
    return;
  }
  if (!editingUser.value && !formPassword.value.trim()) {
    modalError.value = 'La contraseña es obligatoria para nuevos usuarios';
    return;
  }

  try {
    modalLoading.value = true;

    if (editingUser.value) {
      await api.updateUser(editingUser.value.id, {
        fullName: formFullName.value.trim(),
        role: formRole.value,
        password: formPassword.value.trim() || undefined
      });
    } else {
      await api.createUser({
        username: formUsername.value.trim(),
        password: formPassword.value.trim(),
        fullName: formFullName.value.trim(),
        role: formRole.value
      });
    }

    sounds.playBeep();
    showModal.value = false;
    await loadUsers();
  } catch (err: any) {
    modalError.value = err.message || 'Error al guardar el usuario';
  } finally {
    modalLoading.value = false;
  }
};

const deleteUser = async (u: User) => {
  if (u.username === 'admin') {
    alert('No puedes eliminar el usuario administrador principal.');
    return;
  }
  if (!confirm(`¿Estás seguro de desactivar al usuario "${u.fullName || u.username}"?`)) return;

  try {
    sounds.playClick();
    await api.deleteUser(u.id);
    await loadUsers();
  } catch (err: any) {
    alert(err.message || 'Error al desactivar usuario');
  }
};
</script>

<template>
  <div class="page-container">
    <!-- Header -->
    <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 24px;">
      <div>
        <div style="font-size: 0.82rem; font-weight: 700; color: var(--emerald-600); text-transform: uppercase; letter-spacing: 0.05em; display: flex; align-items: center; gap: 6px;">
          <Shield :size="15" />
          Seguridad y Personal
        </div>
        <h2 style="font-family: var(--font-heading); font-size: 1.85rem; font-weight: 800; margin-top: 4px; color: var(--slate-900);">
          Gestión de Usuarios y Trabajadores
        </h2>
        <p style="font-size: 0.85rem; color: var(--slate-600); margin-top: 2px;">
          Crea accesos individuales para cajeros y personal de la bodega. (Sección exclusiva de Administrador)
        </p>
      </div>

      <button class="btn btn-primary" @click="openNewUserModal">
        <UserPlus :size="18" />
        <span>Nuevo Usuario</span>
      </button>
    </div>

    <!-- Search Toolbar -->
    <div style="margin-bottom: 20px;">
      <div class="search-bar" style="max-width: 440px;">
        <Search class="search-icon" :size="18" />
        <input 
          v-model="search"
          type="text" 
          class="search-input" 
          placeholder="Buscar usuario o trabajador por nombre..." 
        />
      </div>
    </div>

    <!-- Users Table -->
    <div class="glass-card" style="padding: 12px 16px;">
      <div v-if="filtered.length === 0" style="text-align: center; padding: 40px; color: var(--slate-400);">
        No se encontraron usuarios registrados con ese criterio.
      </div>

      <div v-else style="overflow-x: auto;">
        <table class="custom-table">
          <thead>
            <tr>
              <th>Usuario</th>
              <th>Nombre Completo</th>
              <th>Rol / Permisos</th>
              <th>Estado</th>
              <th>Fecha de Creación</th>
              <th>Último Acceso</th>
              <th style="text-align: right;">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="u in filtered" :key="u.id">
              <td>
                <div style="display: flex; align-items: center; gap: 10px;">
                  <div 
                    style="width: 36px; height: 36px; border-radius: 8px; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 0.88rem;"
                    :style="{
                      background: u.role === 'Admin' ? 'var(--emerald-100)' : 'var(--indigo-100)',
                      color: u.role === 'Admin' ? 'var(--emerald-600)' : 'var(--indigo-600)'
                    }"
                  >
                    {{ u.fullName.charAt(0) || u.username.charAt(0).toUpperCase() }}
                  </div>
                  <div>
                    <div style="font-weight: 700; color: var(--slate-900); font-size: 0.92rem;">
                      {{ u.username }}
                    </div>
                  </div>
                </div>
              </td>

              <td style="color: var(--slate-700); font-weight: 600;">
                {{ u.fullName }}
              </td>

              <td>
                <span 
                  class="badge" 
                  :style="{
                    background: u.role === 'Admin' ? '#d1fae5' : '#e0e7ff',
                    color: u.role === 'Admin' ? '#065f46' : '#3730a3',
                    border: u.role === 'Admin' ? '1px solid #a7f3d0' : '1px solid #c7d2fe'
                  }"
                >
                  <ShieldCheck v-if="u.role === 'Admin'" :size="13" />
                  <UserIcon v-else :size="13" />
                  {{ u.role === 'Admin' ? 'Administrador' : 'Trabajador / Cajero' }}
                </span>
              </td>

              <td>
                <span 
                  class="badge" 
                  :class="u.isActive ? 'badge-paid' : 'badge-debt'"
                >
                  {{ u.isActive ? 'Activo' : 'Inactivo' }}
                </span>
              </td>

              <td style="color: var(--slate-500); font-size: 0.82rem;">
                {{ new Date(u.createdAt).toLocaleDateString('es-PE', { day: '2-digit', month: 'short', year: 'numeric' }) }}
              </td>

              <td style="color: var(--slate-500); font-size: 0.82rem;">
                {{ u.lastLogin ? new Date(u.lastLogin).toLocaleDateString('es-PE', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }) : 'Nunca' }}
              </td>

              <td style="text-align: right;">
                <div style="display: flex; align-items: center; justify-content: flex-end; gap: 6px;">
                  <button 
                    class="btn btn-secondary btn-sm" 
                    title="Editar usuario o cambiar contraseña"
                    @click="openEditUserModal(u)"
                  >
                    <Edit3 :size="14" />
                  </button>

                  <button 
                    class="btn btn-secondary btn-sm" 
                    style="color: #b91c1c;"
                    title="Desactivar usuario"
                    :disabled="u.username === 'admin'"
                    @click="deleteUser(u)"
                  >
                    <Trash2 :size="14" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- User Modal (Create / Edit) -->
    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal-content" style="max-width: 500px;">
        <div class="modal-header">
          <div style="display: flex; align-items: center; gap: 10px;">
            <div style="width: 38px; height: 38px; border-radius: 10px; background: var(--emerald-100); color: var(--emerald-600); display: flex; align-items: center; justify-content: center;">
              <UserPlus :size="18" />
            </div>
            <div>
              <h3>{{ editingUser ? 'Editar Usuario' : 'Registrar Nuevo Usuario' }}</h3>
              <div style="font-size: 0.78rem; color: var(--slate-500);">
                Acceso al sistema para empleados de la bodega
              </div>
            </div>
          </div>
          <button class="btn btn-secondary btn-sm" style="border-radius: 50%; width: 32px; height: 32px; padding: 0;" @click="showModal = false">
            <X :size="16" />
          </button>
        </div>

        <div class="modal-body">
          <div v-if="modalError" style="padding: 10px 14px; border-radius: var(--radius-sm); background: #fee2e2; border: 1px solid #fecdd3; color: #b91c1c; font-size: 0.85rem; margin-bottom: 16px;">
            {{ modalError }}
          </div>

          <div class="form-group">
            <label class="form-label">Nombre Completo del Trabajador: *</label>
            <input 
              v-model="formFullName" 
              type="text" 
              class="form-input" 
              placeholder="Ej: Juan Pérez Morales" 
              autofocus 
            />
          </div>

          <div class="form-row">
            <div class="form-group">
              <label class="form-label">Nombre de Usuario (Login): *</label>
              <input 
                v-model="formUsername" 
                type="text" 
                class="form-input" 
                placeholder="Ej: juanp" 
                :disabled="!!editingUser"
              />
            </div>

            <div class="form-group">
              <label class="form-label">Rol del Usuario:</label>
              <select v-model="formRole" class="form-select">
                <option value="Worker">Trabajador (Cajero)</option>
                <option value="Admin">Administrador (Dueño)</option>
              </select>
            </div>
          </div>

          <div class="form-group" style="margin-bottom: 0;">
            <label class="form-label">
              {{ editingUser ? 'Nueva Contraseña (dejar en blanco para no cambiarla):' : 'Contraseña de Acceso: *' }}
            </label>
            <div style="position: relative;">
              <KeyRound :size="16" style="position: absolute; left: 14px; top: 12px; color: var(--slate-400);" />
              <input 
                v-model="formPassword" 
                type="password" 
                class="form-input" 
                placeholder="••••••••" 
                style="padding-left: 40px;" 
              />
            </div>
            <div v-if="!editingUser" style="font-size: 0.73rem; color: var(--slate-500); margin-top: 4px;">
              Mínimo 6 caracteres recomendados.
            </div>
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn btn-secondary" :disabled="modalLoading" @click="showModal = false">
            Cancelar
          </button>
          <button class="btn btn-primary" :disabled="modalLoading" @click="saveUser">
            <Save :size="16" />
            <span>{{ modalLoading ? 'Guardando...' : (editingUser ? 'Actualizar Usuario' : 'Crear Usuario') }}</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
