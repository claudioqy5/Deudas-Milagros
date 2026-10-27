<script setup lang="ts">
import { ref } from 'vue';
import { 
  X, 
  Wallet, 
  CheckCircle2, 
  Smartphone, 
  Banknote, 
  CreditCard
} from 'lucide-vue-next';
import confetti from 'canvas-confetti';
import type { Customer } from '../services/api';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

const props = defineProps<{
  customer: Customer | null;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'payment-success'): void;
}>();

const amount = ref<number | ''>('');
const paymentMethod = ref('Efectivo');
const reference = ref('');
const notes = ref('');
const loading = ref(false);
const error = ref('');

const methods = [
  { id: 'Efectivo', label: 'Efectivo', icon: Banknote, color: '#059669' },
  { id: 'Yape', label: 'Yape', icon: Smartphone, color: '#7c3aed' },
  { id: 'Plin', label: 'Plin', icon: Smartphone, color: '#0284c7' },
  { id: 'Transferencia', label: 'Transferencia', icon: CreditCard, color: '#d97706' }
];

const setAmount = (val: number) => {
  sounds.playClick();
  amount.value = Math.min(val, props.customer ? props.customer.currentDebt : val);
};

const payFull = () => {
  if (props.customer) {
    setAmount(props.customer.currentDebt);
  }
};

const submitPayment = async () => {
  if (!props.customer) return;
  const numAmount = Number(amount.value);
  if (!numAmount || numAmount <= 0) {
    error.value = 'Por favor ingrese un monto válido';
    return;
  }

  try {
    loading.value = true;
    error.value = '';
    
    await api.createPayment({
      customerId: props.customer.id,
      amount: numAmount,
      paymentMethod: paymentMethod.value,
      reference: reference.value || undefined,
      notes: notes.value || undefined
    });

    sounds.playCashRegister();
    confetti({
      particleCount: 70,
      spread: 60,
      origin: { y: 0.6 }
    });

    emit('payment-success');
    emit('close');
  } catch (err: any) {
    error.value = err.message || 'Error al procesar el abono';
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <div v-if="customer" class="modal-overlay" @click.self="emit('close')">
    <div class="modal-content" style="max-width: 40%;">
      <!-- Header -->
      <div class="modal-header">
        <div style="display: flex; align-items: center; gap: 10px;">
          <div style="width: 38px; height: 38px; border-radius: 10px; background: var(--emerald-100); display: flex; align-items: center; justify-content: center; color: var(--emerald-600);">
            <Wallet :size="19" />
          </div>
          <div>
            <h3>Registrar Abono / Pago</h3>
            <div style="font-size: 0.78rem; color: var(--slate-500);">
              Amortizar deuda del cliente
            </div>
          </div>
        </div>
        <button class="btn btn-secondary btn-sm" style="border-radius: 50%; width: 32px; height: 32px; padding: 0;" @click="emit('close')">
          <X :size="16" />
        </button>
      </div>

      <!-- Body -->
      <div class="modal-body" style="display: flex; flex-direction: column; gap: 16px;">
        <div v-if="error" style="padding: 10px 14px; border-radius: var(--radius-sm); background: #fee2e2; border: 1px solid #fecdd3; color: #b91c1c; font-size: 0.85rem;">
          {{ error }}
        </div>

        <!-- Customer Summary -->
        <div style="padding: 12px 16px; border-radius: var(--radius-md); background: var(--slate-50); border: 1px solid var(--border-subtle); display: flex; justify-content: space-between; align-items: center;">
          <div>
            <div style="font-weight: 700; font-size: 1rem; color: var(--slate-900);">{{ customer.name }}</div>
            <div style="font-size: 0.8rem; color: var(--slate-500);">Tel: {{ customer.phone }}</div>
          </div>
          <div style="text-align: right;">
            <div style="font-size: 0.7rem; text-transform: uppercase; color: #dc2626; font-weight: 700;">Deuda Actual</div>
            <div style="font-family: var(--font-heading); font-size: 1.4rem; font-weight: 800; color: #dc2626;">
              S/. {{ customer.currentDebt.toFixed(2) }}
            </div>
          </div>
        </div>

        <!-- Amount Input -->
        <div class="form-group" style="margin-bottom: 0;">
          <label class="form-label">Monto a abonar (S/.):</label>
          <div style="position: relative;">
            <span style="position: absolute; left: 14px; top: 10px; font-weight: 700; color: var(--slate-400); font-size: 1.05rem;">S/.</span>
            <input 
              v-model="amount" 
              type="number" 
              step="0.5" 
              class="form-input" 
              placeholder="0.00" 
              style="padding-left: 46px; font-family: var(--font-heading); font-size: 1.3rem; font-weight: 700; color: var(--emerald-600);"
              @keyup.enter="submitPayment"
            />
          </div>

          <!-- Quick Presets -->
          <div style="display: flex; gap: 6px; margin-top: 8px; flex-wrap: wrap;">
            <button 
              type="button" 
              class="btn btn-secondary btn-sm" 
              style="background: #ecfdf5; border-color: #a7f3d0; color: #065f46; font-weight: 700;"
              @click="payFull"
            >
              Pagar Todo (S/. {{ customer.currentDebt.toFixed(2) }})
            </button>
            <button 
              v-for="p in [10, 20, 50, 100]" 
              :key="p" 
              type="button" 
              class="btn btn-secondary btn-sm"
              @click="setAmount(p)"
            >
              +S/. {{ p }}
            </button>
          </div>
        </div>

        <!-- Payment Method Selector -->
        <div class="form-group" style="margin-bottom: 0;">
          <label class="form-label">Método de pago:</label>
          <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px;">
            <button 
              v-for="m in methods" 
              :key="m.id"
              type="button" 
              class="btn btn-secondary" 
              :style="{
                flexDirection: 'column', 
                padding: '10px 6px', 
                gap: '4px',
                borderColor: paymentMethod === m.id ? m.color : '#cbd5e1',
                background: paymentMethod === m.id ? `${m.color}15` : '#ffffff',
                color: paymentMethod === m.id ? m.color : 'var(--slate-700)'
              }"
              @click="paymentMethod = m.id"
            >
              <component :is="m.icon" :size="18" :style="{ color: m.color }" />
              <span style="font-size: 0.78rem; font-weight: 600;">{{ m.label }}</span>
            </button>
          </div>
        </div>

        <!-- Optional Reference / Notes -->
        <div class="form-row">
          <div class="form-group" style="margin-bottom: 0;">
            <label class="form-label">Nro. de Operación (opcional):</label>
            <input 
              v-model="reference" 
              type="text" 
              class="form-input" 
              placeholder="Ej: Op-49208" 
              style="font-size: 0.85rem;"
            />
          </div>
          <div class="form-group" style="margin-bottom: 0;">
            <label class="form-label">Nota o Detalle:</label>
            <input 
              v-model="notes" 
              type="text" 
              class="form-input" 
              placeholder="Ej: Pagó en tienda" 
              style="font-size: 0.85rem;"
            />
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="modal-footer">
        <button class="btn btn-secondary" :disabled="loading" @click="emit('close')">
          Cancelar
        </button>
        <button 
          class="btn btn-primary" 
          :disabled="loading"
          @click="submitPayment"
        >
          <CheckCircle2 :size="17" />
          <span>{{ loading ? 'Guardando...' : 'Confirmar Abono' }}</span>
        </button>
      </div>
    </div>
  </div>
</template>
