<script setup lang="ts">
import { ref, computed } from 'vue';
import { 
  X, 
  Send, 
  Copy, 
  Check, 
  Phone, 
  MessageSquare, 
  ExternalLink,
  ShieldCheck
} from 'lucide-vue-next';
import type { CustomerStatement } from '../services/api';
import { sounds } from '../utils/sound';

const props = defineProps<{
  statement: CustomerStatement | null;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
}>();

const copied = ref(false);
const customNote = ref('');

const finalMessage = computed(() => {
  if (!props.statement) return '';
  if (!customNote.value.trim()) return props.statement.whatsAppMessage;
  
  return `${props.statement.whatsAppMessage}\n\nNota adicional: ${customNote.value.trim()}`;
});

const finalWhatsAppUrl = computed(() => {
  if (!props.statement) return '';
  const cleanPhone = props.statement.customer.phone.replace(/\D/g, '');
  const phoneWithCountry = cleanPhone.length === 9 && cleanPhone.startsWith('9') ? `51${cleanPhone}` : cleanPhone;
  return `https://wa.me/${phoneWithCountry}?text=${encodeURIComponent(finalMessage.value)}`;
});

const copyMessage = async () => {
  if (!finalMessage.value) return;
  sounds.playClick();
  await navigator.clipboard.writeText(finalMessage.value);
  copied.value = true;
  setTimeout(() => {
    copied.value = false;
  }, 2500);
};

const openWhatsApp = () => {
  sounds.playCashRegister();
  window.open(finalWhatsAppUrl.value, '_blank');
};
</script>

<template>
  <div v-if="statement" class="modal-overlay" @click.self="emit('close')">
    <div class="modal-content" style="max-width: 660px;">
      <!-- Header -->
      <div class="modal-header" style="background: #f0fdf4; border-bottom-color: #bbf7d0;">
        <div style="display: flex; align-items: center; gap: 10px;">
          <div style="width: 38px; height: 38px; border-radius: 10px; background: #16a34a; display: flex; align-items: center; justify-content: center; color: white;">
            <MessageSquare :size="20" />
          </div>
          <div>
            <h3 style="color: var(--slate-900); margin-bottom: 2px;">Cobranza por WhatsApp</h3>
            <div style="font-size: 0.78rem; color: #15803d; display: flex; align-items: center; gap: 5px;">
              <ShieldCheck :size="13" />
              Notificación detallada con desglose de productos y fechas
            </div>
          </div>
        </div>
        <button 
          class="btn btn-secondary btn-sm" 
          style="border-radius: 50%; width: 32px; height: 32px; padding: 0;"
          @click="emit('close')"
        >
          <X :size="16" />
        </button>
      </div>

      <!-- Body -->
      <div class="modal-body" style="display: flex; flex-direction: column; gap: 16px;">
        <!-- Customer Info Banner -->
        <div style="display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; border-radius: var(--radius-md); background: var(--slate-50); border: 1px solid var(--border-subtle);">
          <div>
            <div style="font-size: 1rem; font-weight: 700; color: var(--slate-900);">{{ statement.customer.name }}</div>
            <div style="font-size: 0.82rem; color: var(--slate-500); display: flex; align-items: center; gap: 6px; margin-top: 2px;">
              <Phone :size="13" />
              <span>{{ statement.customer.phone || 'Sin número registrado' }}</span>
            </div>
          </div>
          <div style="text-align: right;">
            <div style="font-size: 0.72rem; text-transform: uppercase; color: #dc2626; font-weight: 700;">Deuda Total Pendiente</div>
            <div style="font-family: var(--font-heading); font-size: 1.45rem; font-weight: 800; color: #dc2626;">
              S/. {{ statement.balanceDue.toFixed(2) }}
            </div>
          </div>
        </div>

        <!-- Sales Breakdown Quick List -->
        <div>
          <div style="font-size: 0.78rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; color: var(--slate-500); margin-bottom: 6px; display: flex; align-items: center; justify-content: space-between;">
            <span>Productos y fechas incluidos en la notificación</span>
            <span style="color: #059669;">{{ statement.sales.filter(s => s.status !== 'Paid').length }} compra(s) pendientes</span>
          </div>

          <div style="max-height: 120px; overflow-y: auto; background: var(--slate-50); border-radius: var(--radius-md); border: 1px solid var(--border-subtle); padding: 8px 12px;">
            <div 
              v-for="sale in statement.sales.filter(s => s.status !== 'Paid')" 
              :key="sale.id"
              style="padding: 5px 0; border-bottom: 1px dashed var(--border-subtle); font-size: 0.82rem; display: flex; justify-content: space-between; align-items: center;"
            >
              <div>
                <div style="color: var(--slate-900); font-weight: 600;">
                  {{ new Date(sale.date).toLocaleDateString('es-PE', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }) }}
                </div>
                <div style="font-size: 0.75rem; color: var(--slate-500);">
                  {{ sale.items.map(i => `${i.quantity}x ${i.productName}`).join(', ') }}
                </div>
              </div>
              <div style="font-weight: 700; color: #dc2626; font-family: var(--font-heading);">
                S/. {{ sale.remainingDebt.toFixed(2) }}
              </div>
            </div>
          </div>
        </div>

        <!-- Optional custom note -->
        <div class="form-group" style="margin-bottom: 0;">
          <label class="form-label" style="font-size: 0.8rem;">Agregar recordatorio o nota extra (opcional):</label>
          <input 
            v-model="customNote"
            type="text" 
            class="form-input" 
            placeholder="Ej: Te esperamos hasta las 9pm / Puedes transferir al mismo Yape..."
            style="font-size: 0.85rem; padding: 8px 12px;"
          />
        </div>

        <!-- Message Preview -->
        <div>
          <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 6px;">
            <span style="font-size: 0.78rem; font-weight: 700; color: var(--slate-500); text-transform: uppercase;">Vista Previa del Mensaje</span>
            <button 
              class="btn btn-secondary btn-sm" 
              style="padding: 3px 8px; font-size: 0.75rem;"
              @click="copyMessage"
            >
              <Check v-if="copied" :size="13" style="color: var(--emerald-600);" />
              <Copy v-else :size="13" />
              <span>{{ copied ? 'Copiado' : 'Copiar Texto' }}</span>
            </button>
          </div>
          <div class="wsp-preview-box">
            {{ finalMessage }}
          </div>
        </div>
      </div>

      <!-- Footer Buttons -->
      <div class="modal-footer">
        <button class="btn btn-secondary" @click="emit('close')">
          Cerrar
        </button>
        <button 
          class="btn btn-wsp" 
          style="padding: 10px 20px; font-size: 0.92rem;"
          @click="openWhatsApp"
        >
          <Send :size="17" />
          <span>Enviar a WhatsApp</span>
          <ExternalLink :size="14" />
        </button>
      </div>
    </div>
  </div>
</template>
