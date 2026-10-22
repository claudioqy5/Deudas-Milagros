<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { 
  Search, 
  MessageSquare, 
  Wallet, 
  Receipt, 
  Phone, 
  AlertTriangle
} from 'lucide-vue-next';
import type { Customer, CustomerStatement } from '../services/api';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

const emit = defineEmits<{
  (e: 'open-whatsapp', statement: CustomerStatement): void;
  (e: 'open-payment', customer: Customer): void;
  (e: 'open-statement', statement: CustomerStatement): void;
}>();

const customers = ref<Customer[]>([]);
const search = ref('');
const filterDebtRange = ref<'all' | 'high' | 'medium'>('all');
const loading = ref(true);

const loadDebtors = async () => {
  try {
    loading.value = true;
    customers.value = await api.getCustomers('', true);
  } catch (err) {
    console.error('Error loading debtors', err);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadDebtors();
});

defineExpose({
  refresh: loadDebtors
});

const filteredDebtors = computed(() => {
  let list = customers.value.filter(c => c.currentDebt > 0);

  if (search.value.trim()) {
    const q = search.value.toLowerCase();
    list = list.filter(c => 
      c.name.toLowerCase().includes(q) || 
      c.phone.includes(q) || 
      (c.documentNumber && c.documentNumber.includes(q))
    );
  }

  if (filterDebtRange.value === 'high') {
    list = list.filter(c => c.currentDebt >= 50);
  } else if (filterDebtRange.value === 'medium') {
    list = list.filter(c => c.currentDebt < 50);
  }

  return list.sort((a, b) => b.currentDebt - a.currentDebt);
});

const totalDebt = computed(() => {
  return filteredDebtors.value.reduce((sum, c) => sum + c.currentDebt, 0);
});

const handleWhatsAppClick = async (customer: Customer) => {
  sounds.playClick();
  try {
    const statement = await api.getCustomerStatement(customer.id);
    emit('open-whatsapp', statement);
  } catch (err) {
    console.error(err);
  }
};

const handlePaymentClick = (customer: Customer) => {
  sounds.playClick();
  emit('open-payment', customer);
};

const handleStatementClick = async (customer: Customer) => {
  sounds.playClick();
  try {
    const statement = await api.getCustomerStatement(customer.id);
    emit('open-statement', statement);
  } catch (err) {
    console.error(err);
  }
};
</script>

<template>
  <div class="page-container">
    <!-- Header -->
    <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 24px;">
      <div>
        <div style="font-size: 0.8rem; font-weight: 700; color: #dc2626; text-transform: uppercase; letter-spacing: 0.05em; display: flex; align-items: center; gap: 6px;">
          <AlertTriangle :size="15" />
          Módulo de Cobranzas y Morosidad
        </div>
        <h2 style="font-family: var(--font-heading); font-size: 1.85rem; font-weight: 800; margin-top: 4px; color: var(--slate-900);">
          Ranking de Deudas Pendientes
        </h2>
        <p style="font-size: 0.85rem; color: var(--slate-600); margin-top: 2px;">
          Lista ordenada de mayor a menor deuda con botón rápido para enviar cobranza detallada por WhatsApp
        </p>
      </div>

      <div style="padding: 12px 20px; border-radius: var(--radius-md); background: #fef2f2; border: 1px solid #fecdd3; text-align: right;">
        <div style="font-size: 0.73rem; text-transform: uppercase; color: #b91c1c; font-weight: 700;">Deuda Acumulada Filtrada</div>
        <div style="font-family: var(--font-heading); font-size: 1.55rem; font-weight: 800; color: #dc2626;">
          S/. {{ totalDebt.toFixed(2) }}
        </div>
      </div>
    </div>

    <!-- Search & Filters Toolbar -->
    <div style="display: flex; gap: 14px; margin-bottom: 20px; flex-wrap: wrap;">
      <div class="search-bar" style="flex: 1; min-width: 280px;">
        <Search class="search-icon" :size="18" />
        <input 
          v-model="search"
          type="text" 
          class="search-input" 
          placeholder="Buscar deudor por nombre, celular o DNI..." 
        />
      </div>

      <div style="display: flex; gap: 6px;">
        <button 
          class="btn btn-sm"
          :class="filterDebtRange === 'all' ? 'btn-primary' : 'btn-secondary'"
          @click="filterDebtRange = 'all'"
        >
          Todos los Deudores ({{ customers.filter(c => c.currentDebt > 0).length }})
        </button>
        <button 
          class="btn btn-sm"
          :class="filterDebtRange === 'high' ? 'btn-primary' : 'btn-secondary'"
          @click="filterDebtRange = 'high'"
        >
          Mayor a S/. 50.00
        </button>
        <button 
          class="btn btn-sm"
          :class="filterDebtRange === 'medium' ? 'btn-primary' : 'btn-secondary'"
          @click="filterDebtRange = 'medium'"
        >
          Menor a S/. 50.00
        </button>
      </div>
    </div>

    <!-- Debtors List -->
    <div v-if="filteredDebtors.length === 0" class="glass-card" style="text-align: center; padding: 50px 20px;">
      <Receipt :size="44" style="color: var(--emerald-600); margin: 0 auto 10px;" />
      <h3 style="font-family: var(--font-heading); font-size: 1.25rem; font-weight: 700; color: var(--slate-900);">
        No hay deudas que mostrar
      </h3>
      <p style="color: var(--slate-500); font-size: 0.88rem; margin-top: 4px;">
        Todos tus clientes se encuentran al día o no coinciden con los filtros aplicados.
      </p>
    </div>

    <div v-else style="display: flex; flex-direction: column; gap: 12px;">
      <div 
        v-for="(debtor, index) in filteredDebtors" 
        :key="debtor.id"
        class="glass-card" 
        style="padding: 16px 20px; display: flex; align-items: center; justify-content: space-between; gap: 18px;"
        :style="{
          borderColor: index === 0 ? '#fca5a5' : 'var(--border-subtle)',
          background: index === 0 ? '#fffbfa' : '#ffffff'
        }"
      >
        <!-- Left: Rank Badge + Customer Info -->
        <div style="display: flex; align-items: center; gap: 14px; flex: 1; min-width: 260px;">
          <!-- Rank Number -->
          <div 
            style="width: 40px; height: 40px; border-radius: 10px; display: flex; align-items: center; justify-content: center; font-family: var(--font-heading); font-weight: 800; font-size: 1rem;"
            :style="{
              background: index === 0 ? '#dc2626' : index === 1 ? '#ea580c' : '#f1f5f9',
              color: index < 2 ? '#ffffff' : 'var(--slate-700)'
            }"
          >
            #{{ index + 1 }}
          </div>

          <div>
            <div style="display: flex; align-items: center; gap: 10px;">
              <span style="font-weight: 700; font-size: 1.05rem; color: var(--slate-900);">{{ debtor.name }}</span>
              <span v-if="index === 0" class="badge" style="background: #fee2e2; color: #b91c1c; border: 1px solid #fecdd3; font-size: 0.72rem;">
                Mayor Deudor
              </span>
            </div>

            <div style="font-size: 0.8rem; color: var(--slate-500); display: flex; align-items: center; gap: 10px; margin-top: 3px; flex-wrap: wrap;">
              <span style="display: flex; align-items: center; gap: 4px; color: #16a34a; font-weight: 600;">
                <Phone :size="13" />
                {{ debtor.phone }}
              </span>
              <span>•</span>
              <span v-if="debtor.documentNumber">DNI: {{ debtor.documentNumber }}</span>
              <span v-if="debtor.address">| Dir: {{ debtor.address }}</span>
              <span v-if="debtor.lastSaleDate">
                | Último fiado: {{ new Date(debtor.lastSaleDate).toLocaleDateString('es-PE') }}
              </span>
            </div>

            <!-- Credit limit bar -->
            <div style="margin-top: 6px; width: 200px;">
              <div style="display: flex; justify-content: space-between; font-size: 0.72rem; color: var(--slate-500); margin-bottom: 2px;">
                <span>Uso de crédito</span>
                <span>{{ ((debtor.currentDebt / (debtor.creditLimit || 1)) * 100).toFixed(0) }}% (Lím: S/. {{ debtor.creditLimit }})</span>
              </div>
              <div style="height: 5px; width: 100%; background: #e2e8f0; border-radius: 999px; overflow: hidden;">
                <div 
                  style="height: 100%; border-radius: 999px;"
                  :style="{
                    width: `${Math.min(100, (debtor.currentDebt / (debtor.creditLimit || 1)) * 100)}%`,
                    background: (debtor.currentDebt / (debtor.creditLimit || 1)) > 0.8 ? '#dc2626' : '#d97706'
                  }"
                ></div>
              </div>
            </div>
          </div>
        </div>

        <!-- Center: Current Debt Amount -->
        <div style="text-align: right; min-width: 120px;">
          <div style="font-size: 0.7rem; text-transform: uppercase; color: #dc2626; font-weight: 700;">
            Deuda Pendiente
          </div>
          <div style="font-family: var(--font-heading); font-size: 1.75rem; font-weight: 800; color: #dc2626; letter-spacing: -0.02em;">
            S/. {{ debtor.currentDebt.toFixed(2) }}
          </div>
          <div style="font-size: 0.74rem; color: var(--slate-400); margin-top: 1px;">
            Total abonado: S/. {{ debtor.totalPaid.toFixed(2) }}
          </div>
        </div>

        <!-- Right: Action Buttons -->
        <div style="display: flex; align-items: center; gap: 8px;">
          <button 
            class="btn btn-wsp" 
            style="padding: 9px 14px; font-size: 0.85rem;"
            title="Enviar mensaje de cobranza por WhatsApp con detalle exacto"
            @click="handleWhatsAppClick(debtor)"
          >
            <MessageSquare :size="16" />
            <span>Mandar WSP</span>
          </button>

          <button 
            class="btn btn-primary" 
            style="padding: 9px 14px; font-size: 0.85rem;"
            title="Registrar pago o abono"
            @click="handlePaymentClick(debtor)"
          >
            <Wallet :size="16" />
            <span>Abonar</span>
          </button>

          <button 
            class="btn btn-secondary" 
            style="padding: 9px 11px;"
            title="Ver desglose completo de compras y pagos"
            @click="handleStatementClick(debtor)"
          >
            <Receipt :size="16" />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
