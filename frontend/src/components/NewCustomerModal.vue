<script setup lang="ts">
import { ref, watch } from 'vue';
import { X, UserPlus, Save, Phone, MapPin, CreditCard } from 'lucide-vue-next';
import type { Customer } from '../services/api';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

const props = defineProps<{
  show: boolean;
  customerToEdit?: Customer | null;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'customer-saved', customer: Customer): void;
}>();

const name = ref('');
const phone = ref('');
const documentNumber = ref('');
const address = ref('');
const creditLimit = ref(300);
const notes = ref('');
const loading = ref(false);
const error = ref('');

watch(() => props.customerToEdit, (c) => {
  if (c) {
    name.value = c.name;
    phone.value = c.phone;
    documentNumber.value = c.documentNumber || '';
    address.value = c.address || '';
    creditLimit.value = c.creditLimit || 300;
    notes.value = c.notes || '';
  } else {
    name.value = '';
    phone.value = '';
    documentNumber.value = '';
    address.value = '';
    creditLimit.value = 300;
    notes.value = '';
  }
  error.value = '';
}, { immediate: true });

const saveCustomer = async () => {
  if (!name.value.trim()) {
    error.value = 'El nombre del cliente es obligatorio';
    return;
  }
  if (!phone.value.trim()) {
    error.value = 'El teléfono o celular es obligatorio para enviar mensajes por WhatsApp';
    return;
  }

  try {
    loading.value = true;
    error.value = '';

    let saved: Customer;
    if (props.customerToEdit) {
      saved = await api.updateCustomer(props.customerToEdit.id, {
        name: name.value.trim(),
        phone: phone.value.trim(),
        documentNumber: documentNumber.value.trim(),
        address: address.value.trim(),
        creditLimit: Number(creditLimit.value) || 300,
        notes: notes.value.trim()
      });
    } else {
      saved = await api.createCustomer({
        name: name.value.trim(),
        phone: phone.value.trim(),
        documentNumber: documentNumber.value.trim(),
        address: address.value.trim(),
        creditLimit: Number(creditLimit.value) || 300,
        notes: notes.value.trim()
      });
    }

    sounds.playBeep();
    emit('customer-saved', saved);
    emit('close');
  } catch (err: any) {
    error.value = err.message || 'Error al guardar cliente';
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <div v-if="show" class="modal-overlay" @click.self="emit('close')">
    <div class="modal-content" style="max-width: 520px;">
      <!-- Header -->
      <div class="modal-header">
        <div style="display: flex; align-items: center; gap: 10px;">
          <div style="width: 38px; height: 38px; border-radius: 10px; background: var(--emerald-100); display: flex; align-items: center; justify-content: center; color: var(--emerald-600);">
            <UserPlus :size="19" />
          </div>
          <div>
            <h3>{{ customerToEdit ? 'Editar Cliente' : 'Registrar Nuevo Cliente' }}</h3>
            <div style="font-size: 0.78rem; color: var(--slate-500);">
              Permite vender al fiado y enviar cobranzas por WhatsApp
            </div>
          </div>
        </div>
        <button class="btn btn-secondary btn-sm" style="border-radius: 50%; width: 32px; height: 32px; padding: 0;" @click="emit('close')">
          <X :size="16" />
        </button>
      </div>

      <!-- Body -->
      <div class="modal-body">
        <div v-if="error" style="padding: 10px 14px; border-radius: var(--radius-sm); background: #fee2e2; border: 1px solid #fecdd3; color: #b91c1c; font-size: 0.85rem; margin-bottom: 16px;">
          {{ error }}
        </div>

        <div class="form-group">
          <label class="form-label">Nombre Completo del Cliente: *</label>
          <input 
            v-model="name" 
            type="text" 
            class="form-input" 
            placeholder="Ej: Rosa María Paredes" 
            autofocus 
          />
        </div>

        <div class="form-row">
          <div class="form-group">
            <label class="form-label">Teléfono / WhatsApp: *</label>
            <div style="position: relative;">
              <Phone :size="16" style="position: absolute; left: 14px; top: 12px; color: var(--slate-400);" />
              <input 
                v-model="phone" 
                type="text" 
                class="form-input" 
                placeholder="Ej: 987654321" 
                style="padding-left: 40px;" 
              />
            </div>
            <div style="font-size: 0.73rem; color: var(--emerald-600); margin-top: 4px;">
              Requerido para la cobranza por WhatsApp
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">DNI o Documento (opcional):</label>
            <div style="position: relative;">
              <CreditCard :size="16" style="position: absolute; left: 14px; top: 12px; color: var(--slate-400);" />
              <input 
                v-model="documentNumber" 
                type="text" 
                class="form-input" 
                placeholder="Ej: 45871236" 
                style="padding-left: 40px;" 
              />
            </div>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label class="form-label">Dirección / Referencia:</label>
            <div style="position: relative;">
              <MapPin :size="16" style="position: absolute; left: 14px; top: 12px; color: var(--slate-400);" />
              <input 
                v-model="address" 
                type="text" 
                class="form-input" 
                placeholder="Ej: Jr. Las Gardenias 342" 
                style="padding-left: 40px;" 
              />
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Límite de Crédito sugerido (S/.):</label>
            <input 
              v-model="creditLimit" 
              type="number" 
              step="50" 
              class="form-input" 
              placeholder="300" 
            />
          </div>
        </div>

        <div class="form-group" style="margin-bottom: 0;">
          <label class="form-label">Notas / Observaciones:</label>
          <textarea 
            v-model="notes" 
            class="form-textarea" 
            rows="2" 
            placeholder="Ej: Vecino de la esquina, paga cada quincena"
          ></textarea>
        </div>
      </div>

      <!-- Footer -->
      <div class="modal-footer">
        <button class="btn btn-secondary" :disabled="loading" @click="emit('close')">
          Cancelar
        </button>
        <button class="btn btn-primary" :disabled="loading" @click="saveCustomer">
          <Save :size="16" />
          <span>{{ loading ? 'Guardando...' : (customerToEdit ? 'Actualizar' : 'Guardar Cliente') }}</span>
        </button>
      </div>
    </div>
  </div>
</template>
