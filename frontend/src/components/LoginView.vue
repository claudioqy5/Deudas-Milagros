<script setup lang="ts">
import { ref } from 'vue';
import { Store, Lock, User, ArrowRight, ShieldCheck, CheckCircle2 } from 'lucide-vue-next';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

const emit = defineEmits<{
  (e: 'login-success'): void;
}>();

const username = ref('admin');
const password = ref('admin123');
const loading = ref(false);
const error = ref('');

const handleLogin = async () => {
  if (!username.value.trim() || !password.value.trim()) {
    error.value = 'Por favor ingresa tu usuario y contraseña';
    return;
  }

  try {
    loading.value = true;
    error.value = '';
    await api.login(username.value.trim(), password.value.trim());
    sounds.playBeep();
    emit('login-success');
  } catch (err: any) {
    error.value = err.message || 'Error al iniciar sesión';
  } finally {
    loading.value = false;
  }
};

const setDemoUser = (u: string, p: string) => {
  sounds.playClick();
  username.value = u;
  password.value = p;
  error.value = '';
};
</script>

<template>
  <div style="min-height: 100vh; display: flex; align-items: center; justify-content: center; background: #f8fafc; padding: 20px;">
    <div style="max-width: 440px; width: 100%;">
      <!-- Card -->
      <div class="glass-card" style="padding: 36px 32px; border-radius: var(--radius-xl); box-shadow: var(--shadow-lg);">
        <!-- Brand Header -->
        <div style="text-align: center; margin-bottom: 28px;">
          <div style="width: 52px; height: 52px; border-radius: 14px; background: linear-gradient(135deg, #059669 0%, #047857 100%); color: #ffffff; display: flex; align-items: center; justify-content: center; margin: 0 auto 14px; box-shadow: 0 4px 12px rgba(5, 150, 105, 0.25);">
            <Store :size="28" />
          </div>
          <h1 style="font-family: var(--font-heading); font-size: 1.55rem; font-weight: 800; color: var(--slate-900);">
            BodegaPOS & Deudas
          </h1>
          <p style="font-size: 0.88rem; color: var(--slate-500); margin-top: 4px;">
            Inicia sesión para gestionar ventas al fiado y cobranzas
          </p>
        </div>

        <!-- Error alert -->
        <div v-if="error" style="padding: 10px 14px; border-radius: var(--radius-md); background: #fee2e2; border: 1px solid #fecdd3; color: #b91c1c; font-size: 0.85rem; margin-bottom: 18px;">
          {{ error }}
        </div>

        <!-- Form -->
        <form @submit.prevent="handleLogin" style="display: flex; flex-direction: column; gap: 16px;">
          <div class="form-group" style="margin-bottom: 0;">
            <label class="form-label">Usuario:</label>
            <div style="position: relative;">
              <User :size="17" style="position: absolute; left: 14px; top: 12px; color: var(--slate-400);" />
              <input 
                v-model="username" 
                type="text" 
                class="form-input" 
                placeholder="Nombre de usuario" 
                style="padding-left: 40px;" 
                autocomplete="username"
              />
            </div>
          </div>

          <div class="form-group" style="margin-bottom: 0;">
            <label class="form-label">Contraseña:</label>
            <div style="position: relative;">
              <Lock :size="17" style="position: absolute; left: 14px; top: 12px; color: var(--slate-400);" />
              <input 
                v-model="password" 
                type="password" 
                class="form-input" 
                placeholder="••••••••" 
                style="padding-left: 40px;" 
                autocomplete="current-password"
              />
            </div>
          </div>

          <button 
            type="submit" 
            class="btn btn-primary btn-lg" 
            style="width: 100%; margin-top: 8px; justify-content: center;"
            :disabled="loading"
          >
            <span>{{ loading ? 'Ingresando...' : 'Iniciar Sesión' }}</span>
            <ArrowRight :size="18" />
          </button>
        </form>

        <!-- Quick Demo Switcher -->
        <div style="margin-top: 26px; padding-top: 20px; border-top: 1px solid var(--border-subtle);">
          <div style="font-size: 0.76rem; font-weight: 700; color: var(--slate-400); text-transform: uppercase; letter-spacing: 0.05em; text-align: center; margin-bottom: 10px;">
            Acceso Rápido de Prueba
          </div>
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px;">
            <button 
              type="button" 
              class="btn btn-secondary btn-sm"
              style="font-size: 0.78rem; padding: 8px 6px; flex-direction: column; gap: 2px;"
              @click="setDemoUser('admin', 'admin123')"
            >
              <span style="font-weight: 700; color: var(--emerald-600);">Administrador</span>
              <span style="font-size: 0.7rem; color: var(--slate-400);">admin / admin123</span>
            </button>
            <button 
              type="button" 
              class="btn btn-secondary btn-sm"
              style="font-size: 0.78rem; padding: 8px 6px; flex-direction: column; gap: 2px;"
              @click="setDemoUser('cajero', 'cajero123')"
            >
              <span style="font-weight: 700; color: var(--indigo-600);">Trabajador</span>
              <span style="font-size: 0.7rem; color: var(--slate-400);">cajero / cajero123</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
