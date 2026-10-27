<script setup lang="ts">
import { ref } from 'vue';
import { 
  X, 
  Receipt, 
  Calendar 
} from 'lucide-vue-next';
import type { CustomerStatement } from '../services/api';

const props = defineProps<{
  statement: CustomerStatement | null;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'open-whatsapp', statement: CustomerStatement): void;
  (e: 'open-payment', customer: any): void;
}>();

const activeTab = ref<'sales' | 'payments'>('sales');
</script>

<template>
  <div v-if="statement" class="modal-overlay" @click.self="emit('close')">
    <div class="modal-content" style="max-width: 700px;">
      <!-- Header -->
      <div class="modal-header">
        <div style="display: flex; align-items: center; gap: 10px;">
          <div style="width: 38px; height: 38px; border-radius: 10px; background: var(--indigo-100); display: flex; align-items: center; justify-content: center; color: var(--indigo-600);">
            <Receipt :size="19" />
          </div>
          <div>
            <h3>Estado de Cuenta: {{ statement.customer.name }}</h3>
            <div style="font-size: 0.78rem; color: var(--slate-500);">
              Tel: {{ statement.customer.phone }} | DNI: {{ statement.customer.documentNumber || 'No reg.' }}
            </div>
          </div>
        </div>
        <button class="btn btn-secondary btn-sm" style="border-radius: 50%; width: 32px; height: 32px; padding: 0;" @click="emit('close')">
          <X :size="16" />
        </button>
      </div>

      <!-- Body -->
      <div class="modal-body" style="display: flex; flex-direction: column; gap: 16px;">
        <!-- Top Stats Row -->
        <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px;">
          <div style="padding: 12px; border-radius: var(--radius-md); background: var(--slate-50); border: 1px solid var(--border-subtle);">
            <div style="font-size: 0.7rem; text-transform: uppercase; color: var(--slate-500); font-weight: 700;">Total Fiado Histórico</div>
            <div style="font-family: var(--font-heading); font-size: 1.25rem; font-weight: 700; color: var(--slate-900); margin-top: 3px;">
              S/. {{ statement.totalSalesAmount.toFixed(2) }}
            </div>
          </div>
          <div style="padding: 12px; border-radius: var(--radius-md); background: #f0fdf4; border: 1px solid #bbf7d0;">
            <div style="font-size: 0.7rem; text-transform: uppercase; color: #166534; font-weight: 700;">Total Abonado</div>
            <div style="font-family: var(--font-heading); font-size: 1.25rem; font-weight: 700; color: #15803d; margin-top: 3px;">
              S/. {{ statement.totalPaymentsAmount.toFixed(2) }}
            </div>
          </div>
          <div style="padding: 12px; border-radius: var(--radius-md); background: #fef2f2; border: 1px solid #fecdd3;">
            <div style="font-size: 0.7rem; text-transform: uppercase; color: #991b1b; font-weight: 700;">Saldo Pendiente</div>
            <div style="font-family: var(--font-heading); font-size: 1.25rem; font-weight: 800; color: #dc2626; margin-top: 3px;">
              S/. {{ statement.balanceDue.toFixed(2) }}
            </div>
          </div>
        </div>

        <!-- Navigation Tabs -->
        <div style="display: flex; gap: 6px; border-bottom: 1px solid var(--border-subtle); padding-bottom: 6px;">
          <button 
            class="btn btn-sm" 
            :class="activeTab === 'sales' ? 'btn-primary' : 'btn-secondary'"
            @click="activeTab = 'sales'"
          >
            <Receipt :size="14" />
            <span>Ventas al Fiado ({{ statement.sales.length }})</span>
          </button>
          <button 
            class="btn btn-sm" 
            :class="activeTab === 'payments' ? 'btn-primary' : 'btn-secondary'"
            @click="activeTab = 'payments'"
          >
            <Wallet :size="14" />
            <span>Abonos Recibidos ({{ statement.payments.length }})</span>
          </button>
        </div>

        <!-- Sales Tab Content -->
        <div v-if="activeTab === 'sales'" style="display: flex; flex-direction: column; gap: 8px; max-height: 300px; overflow-y: auto;">
          <div v-if="statement.sales.length === 0" style="text-align: center; padding: 30px; color: var(--slate-400);">
            No hay registros de ventas fiadas para este cliente.
          </div>
          <div 
            v-for="sale in statement.sales" 
            :key="sale.id"
            style="background: var(--slate-50); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 12px;"
          >
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
              <div style="display: flex; align-items: center; gap: 6px;">
                <span style="font-weight: 700; color: var(--slate-900); font-size: 0.88rem;">{{ sale.saleCode }}</span>
                <span 
                  class="badge" 
                  :class="{
                    'badge-paid': sale.status === 'Paid',
                    'badge-partial': sale.status === 'PartiallyPaid',
                    'badge-debt': sale.status === 'Pending'
                  }"
                >
                  {{ sale.status === 'Paid' ? 'Cancelado' : sale.status === 'PartiallyPaid' ? 'Parcial' : 'Pendiente' }}
                </span>
              </div>
              <div style="font-size: 0.78rem; color: var(--slate-500); display: flex; align-items: center; gap: 4px;">
                <Calendar :size="12" />
                {{ new Date(sale.date).toLocaleDateString('es-PE', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }) }}
              </div>
            </div>

            <!-- Items -->
            <div style="background: #ffffff; border-radius: var(--radius-sm); border: 1px solid var(--border-subtle); padding: 6px 10px; margin-bottom: 6px;">
              <div 
                v-for="(item, idx) in sale.items" 
                :key="idx"
                style="display: flex; justify-content: space-between; font-size: 0.82rem; padding: 2px 0;"
              >
                <span style="color: var(--slate-800);">
                  {{ item.quantity }}x {{ item.productName }} <span style="color: var(--slate-400); font-size: 0.74rem;">(S/. {{ item.unitPrice.toFixed(2) }} c/u)</span>
                </span>
                <span style="font-weight: 600; color: var(--slate-900);">S/. {{ item.subtotal.toFixed(2) }}</span>
              </div>
            </div>

            <div style="display: flex; justify-content: space-between; align-items: center; font-size: 0.82rem; border-top: 1px dashed var(--border-subtle); padding-top: 6px;">
              <span style="color: var(--slate-500);">
                Total: S/. {{ sale.totalAmount.toFixed(2) }}
                <span v-if="sale.initialPayment > 0" style="color: var(--emerald-600); margin-left: 6px;">(Adelanto: S/. {{ sale.initialPayment.toFixed(2) }})</span>
              </span>
              <span style="font-weight: 700; color: sale.remainingDebt > 0 ? '#dc2626' : '#059669';">
                Resta: S/. {{ sale.remainingDebt.toFixed(2) }}
              </span>
            </div>
          </div>
        </div>

        <!-- Payments Tab Content -->
        <div v-if="activeTab === 'payments'" style="display: flex; flex-direction: column; gap: 8px; max-height: 300px; overflow-y: auto;">
          <div v-if="statement.payments.length === 0" style="text-align: center; padding: 30px; color: var(--slate-400);">
            No hay registros de abonos para este cliente.
          </div>
          <div 
            v-for="p in statement.payments" 
            :key="p.id"
            style="background: var(--slate-50); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 10px 14px; display: flex; justify-content: space-between; align-items: center;"
          >
            <div>
              <div style="font-weight: 600; font-size: 0.9rem; color: var(--slate-900);">
                Abono recibido vía <span style="color: var(--emerald-600);">{{ p.paymentMethod }}</span>
              </div>
              <div style="font-size: 0.76rem; color: var(--slate-500); margin-top: 1px;">
                {{ new Date(p.date).toLocaleDateString('es-PE', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }) }}
                <span v-if="p.reference"> | Ref: {{ p.reference }}</span>
                <span v-if="p.notes"> | {{ p.notes }}</span>
              </div>
            </div>
            <div style="font-family: var(--font-heading); font-size: 1.2rem; font-weight: 800; color: #059669;">
              + S/. {{ p.amount.toFixed(2) }}
            </div>
          </div>
        </div>
      </div>

      <!-- Footer Actions -->
      <div class="modal-footer" style="justify-content: flex-end;">
        <button class="btn btn-secondary btn-sm" @click="emit('close')">
          Cerrar
        </button>
      </div>
    </div>
  </div>
</template>
