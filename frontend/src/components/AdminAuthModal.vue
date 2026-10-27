<script setup lang="ts">
import { ref } from 'vue';
import { ShieldAlert, Lock, Eye, EyeOff, X } from 'lucide-vue-next';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'authenticated'): void;
}>();

const password = ref('');
const showPassword = ref(false);
const loading = ref(false);
const error = ref('');

const handleVerify = async () => {
  if (!password.value.trim()) {
    error.value = 'Por favor ingrese la contraseña de administrador';
    return;
  }

  try {
    loading.value = true;
    error.value = '';
    await api.verifyAdminPassword(password.value);
    sounds.playCashRegister();
    emit('authenticated');
  } catch (err: any) {
    sounds.playClick();
    error.value = err.message || 'Contraseña incorrecta. Este espacio es exclusivo del administrador.';
    password.value = '';
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <div class="modal-overlay" @click.self="emit('close')">
    <div class="modal-content admin-auth-modal" style="max-width: 440px;">
      <!-- Header -->
      <div class="modal-header">
        <div style="display: flex; align-items: center; gap: 10px;">
          <div class="admin-auth-icon-wrap">
            <ShieldAlert :size="20" />
          </div>
          <div>
            <h3 style="font-size: 1.15rem; font-weight: 800; color: var(--slate-900);">Acceso de Administrador</h3>
            <div style="font-size: 0.76rem; color: #dc2626; font-weight: 700;">
              Exclusivo para el Administrador
            </div>
          </div>
        </div>
        <button class="btn btn-secondary btn-sm" style="border-radius: 50%; width: 32px; height: 32px; padding: 0;" @click="emit('close')">
          <X :size="16" />
        </button>
      </div>

      <!-- Body -->
      <div class="modal-body" style="display: flex; flex-direction: column; gap: 16px;">
        <!-- Notice box -->
        <div class="admin-auth-notice">
          <Lock :size="16" class="notice-lock-icon" />
          <div class="notice-text">
            <strong>Espacio exclusivo del administrador</strong>
            <p>Para gestionar trabajadores, personal y accesos a la tienda, debe confirmar su contraseña de administrador.</p>
          </div>
        </div>

        <!-- Error alert -->
        <div v-if="error" class="admin-auth-error">
          <span>{{ error }}</span>
        </div>

        <!-- Password input -->
        <div class="form-group" style="margin-bottom: 0;">
          <label class="form-label" style="font-weight: 700; color: var(--slate-800);">
            Contraseña de Administrador:
          </label>
          <div style="position: relative;">
            <Lock :size="16" style="position: absolute; left: 14px; top: 13px; color: var(--slate-400);" />
            <input 
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              class="form-input"
              placeholder="Ingresa la contraseña..."
              style="padding-left: 40px; padding-right: 40px;"
              autofocus
              @keydown.enter="handleVerify"
            />
            <button 
              type="button" 
              class="btn-toggle-eye" 
              title="Mostrar / ocultar contraseña"
              @click="showPassword = !showPassword"
            >
              <EyeOff v-if="showPassword" :size="16" />
              <Eye v-else :size="16" />
            </button>
          </div>
        </div>
      </div>

      <!-- Footer Actions -->
      <div class="modal-footer" style="justify-content: flex-end; gap: 10px;">
        <button class="btn btn-secondary btn-sm" :disabled="loading" @click="emit('close')">
          Cancelar
        </button>
        <button class="btn btn-primary btn-sm" :disabled="loading" @click="handleVerify">
          <span v-if="loading">Verificando...</span>
          <span v-else>Confirmar y Entrar</span>
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.admin-auth-icon-wrap {
  width: 38px;
  height: 38px;
  border-radius: 10px;
  background: #fee2e2;
  color: #dc2626;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.admin-auth-notice {
  display: flex;
  gap: 12px;
  background: #fffbeb;
  border: 1px solid #fde68a;
  border-radius: var(--radius-md);
  padding: 12px 14px;
}

.notice-lock-icon {
  color: #d97706;
  flex-shrink: 0;
  margin-top: 2px;
}

.notice-text strong {
  display: block;
  font-size: 0.84rem;
  color: #92400e;
  margin-bottom: 2px;
}

.notice-text p {
  font-size: 0.77rem;
  color: #b45309;
  line-height: 1.35;
  margin: 0;
}

.admin-auth-error {
  padding: 10px 14px;
  border-radius: var(--radius-sm);
  background: #fee2e2;
  border: 1px solid #fecdd3;
  color: #b91c1c;
  font-size: 0.82rem;
  font-weight: 600;
}

.btn-toggle-eye {
  position: absolute;
  right: 12px;
  top: 10px;
  background: transparent;
  border: none;
  color: var(--slate-400);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2px;
}

.btn-toggle-eye:hover {
  color: var(--slate-700);
}
</style>
