<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { 
  Search, 
  Wallet, 
  Receipt, 
  Phone, 
  AlertTriangle,
  Eye,
  Calendar,
  ArrowUpDown
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
const sortOrder = ref<'desc' | 'asc'>('desc');
const filterDate = ref('');
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

  // Filtro por fecha de último fiado
  if (filterDate.value) {
    list = list.filter(c => {
      if (!c.lastSaleDate) return false;
      const saleDate = new Date(c.lastSaleDate).toISOString().split('T')[0];
      return saleDate === filterDate.value;
    });
  }

  return list.sort((a, b) => {
    if (sortOrder.value === 'asc') {
      return a.currentDebt - b.currentDebt;
    }
    return b.currentDebt - a.currentDebt;
  });
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
    <!-- Header with Accumulated Debt Card -->
    <div class="debts-header-row">
      <div class="debts-badge-title">
        <AlertTriangle :size="15" />
        <span>Módulo de Cobranzas y Morosidad ({{ filteredDebtors.length }} deudores)</span>
      </div>

      <div class="debts-total-card">
        <span class="total-label">Deuda Acumulada Filtrada</span>
        <span class="total-amount">S/. {{ totalDebt.toFixed(2) }}</span>
      </div>
    </div>

    <!-- Toolbar: Search + Date Filter + Sort Order Button in single row -->
    <div class="debts-toolbar-row">
      <div class="search-bar debts-search-bar">
        <Search class="search-icon" :size="16" />
        <input 
          v-model="search"
          type="text" 
          class="search-input" 
          placeholder="Buscar deudor por nombre, celular o DNI..." 
        />
        <button 
          v-if="search" 
          class="btn-clear-search-icon" 
          title="Borrar búsqueda"
          @click="search = ''"
        >
          ✕
        </button>
      </div>

      <div class="debts-filters-group">
        <!-- Date Filter -->
        <div class="debts-date-box" title="Filtrar por fecha de último fiado">
          <Calendar :size="14" class="debts-date-icon" />
          <input 
            v-model="filterDate" 
            type="date" 
            class="debts-date-picker"
          />
          <button 
            v-if="filterDate" 
            class="debts-clear-date-btn" 
            title="Quitar filtro de fecha"
            @click="filterDate = ''"
          >
            ✕
          </button>
        </div>

        <!-- Sort Order Toggle -->
        <button 
          class="btn btn-secondary debts-sort-btn"
          :title="sortOrder === 'desc' ? 'Orden actual: Mayor a menor. Clic para cambiar a menor a mayor.' : 'Orden actual: Menor a mayor. Clic para cambiar a mayor a menor.'"
          @click="sortOrder = sortOrder === 'desc' ? 'asc' : 'desc'"
        >
          <ArrowUpDown :size="14" />
          <span>{{ sortOrder === 'desc' ? 'Mayor a Menor' : 'Menor a Mayor' }}</span>
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
        class="glass-card debtor-row-card" 
        :style="{
          borderColor: index === 0 ? '#fca5a5' : 'var(--border-subtle)',
          background: index === 0 ? '#fffbfa' : '#ffffff'
        }"
      >
        <!-- Left: Rank Badge + Customer Info -->
        <div class="debtor-info-col">
          <!-- Rank Number -->
          <div 
            style="width: 40px; height: 40px; border-radius: 10px; display: flex; align-items: center; justify-content: center; font-family: var(--font-heading); font-weight: 800; font-size: 1rem; flex-shrink: 0;"
            :style="{
              background: index === 0 ? '#dc2626' : index === 1 ? '#ea580c' : '#f1f5f9',
              color: index < 2 ? '#ffffff' : 'var(--slate-700)'
            }"
          >
            #{{ index + 1 }}
          </div>

          <div>
            <div style="display: flex; align-items: center; gap: 10px; flex-wrap: wrap;">
              <span style="font-weight: 700; font-size: 1.05rem; color: var(--slate-900);">{{ debtor.name }}</span>
              <span v-if="index === 0" class="badge" style="background: #fee2e2; color: #b91c1c; border: 1px solid #fecdd3; font-size: 0.72rem;">
                Mayor Deudor
              </span>
            </div>

            <div style="font-size: 0.8rem; color: var(--slate-500); display: flex; align-items: center; gap: 10px; margin-top: 4px; flex-wrap: wrap;">
              <span style="display: flex; align-items: center; gap: 4px; color: #16a34a; font-weight: 600;">
                <Phone :size="13" />
                {{ debtor.phone }}
              </span>
              <span v-if="debtor.documentNumber">•</span>
              <span v-if="debtor.documentNumber">DNI: {{ debtor.documentNumber }}</span>
              <span v-if="debtor.lastSaleDate">
                • Último fiado: {{ new Date(debtor.lastSaleDate).toLocaleDateString('es-PE') }}
              </span>
            </div>
          </div>
        </div>

        <!-- Center: Current Debt Amount -->
        <div class="debtor-amount-col">
          <div style="font-size: 0.7rem; text-transform: uppercase; color: #dc2626; font-weight: 700;">
            Deuda Pendiente
          </div>
          <div style="font-family: var(--font-heading); font-size: 1.7rem; font-weight: 800; color: #dc2626; letter-spacing: -0.02em; line-height: 1.2;">
            S/. {{ debtor.currentDebt.toFixed(2) }}
          </div>
          <div style="font-size: 0.74rem; color: var(--slate-400); margin-top: 1px;">
            Total abonado: S/. {{ debtor.totalPaid.toFixed(2) }}
          </div>
        </div>

        <!-- Right: Action Buttons -->
        <div class="debtor-actions-col">
          <button 
            class="btn btn-wsp" 
            title="Enviar mensaje de cobranza por WhatsApp con detalle exacto"
            @click="handleWhatsAppClick(debtor)"
          >
            <svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor">
              <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L0 24l6.335-1.662c1.743.95 3.71 1.452 5.715 1.452h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.414z"/>
            </svg>
            <span>WSP</span>
          </button>

          <button 
            class="btn btn-primary" 
            title="Registrar pago o abono"
            @click="handlePaymentClick(debtor)"
          >
            <Wallet :size="16" />
            <span>Abonar</span>
          </button>

          <button 
            class="btn btn-secondary" 
            title="Ver estado de cuenta completo"
            @click="handleStatementClick(debtor)"
          >
            <Eye :size="16" />
            <span>Estado</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
