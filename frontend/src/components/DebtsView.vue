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

// Calcula la quincena actual del mes:
// - Del día 1 al 15: rango del 1 al 15
// - Del día 16 en adelante: rango del 16 al último día del mes (28, 29, 30 o 31)
const getDefaultQuincenaRange = () => {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth();
  const day = now.getDate();

  const pad = (n: number) => String(n).padStart(2, '0');
  const monthStr = pad(month + 1);

  if (day <= 15) {
    return {
      startDate: `${year}-${monthStr}-01`,
      endDate: `${year}-${monthStr}-15`
    };
  } else {
    const lastDay = new Date(year, month + 1, 0).getDate();
    return {
      startDate: `${year}-${monthStr}-16`,
      endDate: `${year}-${monthStr}-${pad(lastDay)}`
    };
  }
};

const defaultRange = getDefaultQuincenaRange();
const startDate = ref(defaultRange.startDate);
const endDate = ref(defaultRange.endDate);

const isQuincenaActive = computed(() => {
  const q = getDefaultQuincenaRange();
  return startDate.value === q.startDate && endDate.value === q.endDate;
});

const clearDates = () => {
  sounds.playClick();
  startDate.value = '';
  endDate.value = '';
};

const resetToQuincena = () => {
  sounds.playClick();
  const q = getDefaultQuincenaRange();
  startDate.value = q.startDate;
  endDate.value = q.endDate;
};

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

  // Filtro por rango de fechas de último fiado (Desde - Hasta)
  if (startDate.value || endDate.value) {
    list = list.filter(c => {
      if (!c.lastSaleDate) return false;
      const d = new Date(c.lastSaleDate);
      const saleDate = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
      if (startDate.value && saleDate < startDate.value) return false;
      if (endDate.value && saleDate > endDate.value) return false;
      return true;
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

const highestDebtAmount = computed(() => {
  if (filteredDebtors.value.length === 0) return 0;
  return Math.max(...filteredDebtors.value.map(d => d.currentDebt));
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
      <div>
        <div style="display: flex; align-items: center; gap: 10px; flex-wrap: wrap;">
          <h2 class="pos-main-title">Cobranza y Control de Deudas</h2>
          <span class="debts-badge-title">
            <AlertTriangle :size="14" />
            <span>{{ filteredDebtors.length }} deudores</span>
          </span>
        </div>
        <p class="pos-sub-title">
          Seguimiento de saldos pendientes, registro de abonos y recordatorios por WhatsApp
        </p>
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
        <!-- Date Range Filter (Desde - Hasta) -->
        <div class="debts-date-range-box" title="Filtrar por rango de fechas de último fiado">
          <Calendar :size="14" class="debts-date-icon" />

          <div class="debts-date-field">
            <span class="debts-date-label">Desde:</span>
            <input 
              v-model="startDate" 
              type="date" 
              class="debts-date-picker"
              title="Fecha inicial del fiado"
            />
          </div>

          <span class="debts-date-range-sep">-</span>

          <div class="debts-date-field">
            <span class="debts-date-label">Hasta:</span>
            <input 
              v-model="endDate" 
              type="date" 
              class="debts-date-picker"
              title="Fecha final del fiado"
            />
          </div>

          <button 
            v-if="startDate || endDate" 
            class="debts-clear-date-btn" 
            title="Limpiar rango de fechas (mostrar todos)"
            @click="clearDates"
          >
            ✕
          </button>
        </div>

        <!-- Quincena Actual quick button (shows when not on default quincena) -->
        <button 
          v-if="!isQuincenaActive"
          class="btn btn-secondary btn-sm"
          style="height: 38px; font-size: 0.78rem; padding: 0 10px; font-weight: 700; color: #4338ca; white-space: nowrap;"
          title="Restablecer filtro a la quincena actual del mes"
          @click="resetToQuincena"
        >
          Quincena Actual
        </button>

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

    <!-- Debtors Table -->
    <div class="glass-card" style="padding: 12px 16px;">
      <div v-if="filteredDebtors.length === 0" style="text-align: center; padding: 40px; color: var(--slate-400);">
        <Receipt :size="40" style="color: var(--emerald-600); margin: 0 auto 10px;" />
        <h3 style="font-family: var(--font-heading); font-size: 1.15rem; font-weight: 700; color: var(--slate-900);">
          No hay deudas que mostrar
        </h3>
        <p style="color: var(--slate-500); font-size: 0.85rem; margin-top: 4px;">
          Todos tus clientes se encuentran al día o no coinciden con los filtros aplicados.
        </p>
      </div>

      <div v-else style="overflow-x: auto;">
        <table class="custom-table">
          <thead>
            <tr>
              <th style="width: 50px; text-align: center;">#</th>
              <th>Cliente</th>
              <th style="text-align: center;">Contacto / DNI</th>
              <th style="text-align: center;">Último Fiado</th>
              <th style="text-align: center;">Total Abonado</th>
              <th style="text-align: center;">Deuda Pendiente</th>
              <th style="text-align: center; width: 130px;">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(debtor, index) in filteredDebtors" :key="debtor.id">
              <!-- Ranking -->
              <td style="text-align: center;">
                <span 
                  style="display: inline-flex; align-items: center; justify-content: center; width: 28px; height: 28px; border-radius: 8px; font-weight: 800; font-size: 0.82rem; font-family: var(--font-heading);"
                  :style="{
                    background: sortOrder === 'desc' && index === 0 ? '#fee2e2' : sortOrder === 'desc' && index === 1 ? '#ffedd5' : '#f1f5f9',
                    color: sortOrder === 'desc' && index === 0 ? '#dc2626' : sortOrder === 'desc' && index === 1 ? '#ea580c' : '#475569'
                  }"
                >
                  #{{ index + 1 }}
                </span>
              </td>

              <!-- Cliente -->
              <td>
                <div style="display: flex; align-items: center; gap: 8px; flex-wrap: wrap;">
                  <span style="font-weight: 700; color: var(--slate-900);">{{ debtor.name }}</span>
                  <span 
                    v-if="debtor.currentDebt === highestDebtAmount && highestDebtAmount > 0" 
                    class="badge" 
                    style="background: #fee2e2; color: #b91c1c; border: 1px solid #fecdd3; font-size: 0.68rem; padding: 1px 6px;"
                  >
                    Mayor Deudor
                  </span>
                </div>
              </td>

              <!-- Contacto / DNI -->
              <td style="text-align: center;">
                <div style="display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 2px;">
                  <span v-if="debtor.phone" style="display: inline-flex; align-items: center; gap: 4px; color: #16a34a; font-weight: 600; font-size: 0.84rem;">
                    <Phone :size="12" />
                    {{ debtor.phone }}
                  </span>
                  <span v-else style="color: var(--slate-400); font-size: 0.82rem;">-</span>
                  <span v-if="debtor.documentNumber" style="color: var(--slate-500); font-size: 0.74rem;">
                    DNI: {{ debtor.documentNumber }}
                  </span>
                </div>
              </td>

              <!-- Último Fiado -->
              <td style="text-align: center; color: var(--slate-600); font-size: 0.84rem;">
                <span v-if="debtor.lastSaleDate">
                  {{ new Date(debtor.lastSaleDate).toLocaleDateString('es-PE') }}
                </span>
                <span v-else style="color: var(--slate-400);">-</span>
              </td>

              <!-- Total Abonado -->
              <td style="text-align: center; color: var(--slate-600); font-size: 0.88rem; font-weight: 600;">
                S/. {{ debtor.totalPaid.toFixed(2) }}
              </td>

              <!-- Deuda Pendiente -->
              <td style="text-align: center;">
                <span style="font-family: var(--font-heading); font-size: 1.05rem; font-weight: 800; color: #dc2626;">
                  S/. {{ debtor.currentDebt.toFixed(2) }}
                </span>
              </td>

              <!-- Acciones: Solo Iconos -->
              <td style="text-align: center;">
                <div style="display: flex; align-items: center; justify-content: center; gap: 6px;">
                  <!-- WSP -->
                  <button 
                    class="btn btn-wsp btn-sm" 
                    title="Enviar cobranza por WhatsApp"
                    style="padding: 7px 9px; min-width: 32px; height: 32px;"
                    @click="handleWhatsAppClick(debtor)"
                  >
                    <svg viewBox="0 0 24 24" width="15" height="15" fill="currentColor">
                      <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L0 24l6.335-1.662c1.743.95 3.71 1.452 5.715 1.452h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.414z"/>
                    </svg>
                  </button>

                  <!-- Abonar -->
                  <button 
                    class="btn btn-primary btn-sm" 
                    title="Registrar abono / pago"
                    style="padding: 7px 9px; min-width: 32px; height: 32px;"
                    @click="handlePaymentClick(debtor)"
                  >
                    <Wallet :size="15" />
                  </button>

                  <!-- Estado -->
                  <button 
                    class="btn btn-secondary btn-sm" 
                    title="Ver estado de cuenta completo"
                    style="padding: 7px 9px; min-width: 32px; height: 32px;"
                    @click="handleStatementClick(debtor)"
                  >
                    <Eye :size="15" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
.debts-date-range-box {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: var(--radius-md);
  padding: 0 10px;
  height: 38px;
  position: relative;
  transition: all 0.15s ease;
}

.debts-date-range-box:focus-within {
  border-color: #0f172a;
  box-shadow: 0 0 0 2px rgba(15, 23, 42, 0.08);
}

.debts-date-field {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.debts-date-label {
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--slate-500);
  text-transform: uppercase;
  letter-spacing: 0.02em;
}

.debts-date-range-sep {
  color: var(--slate-300);
  font-weight: 700;
  font-size: 0.85rem;
}

@media (max-width: 768px) {
  .debts-date-range-box {
    width: 100%;
    justify-content: space-between;
    height: auto;
    padding: 6px 10px;
    gap: 6px;
    flex-wrap: wrap;
  }
}
</style>
