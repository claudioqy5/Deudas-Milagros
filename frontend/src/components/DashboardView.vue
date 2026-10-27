<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { 
  TrendingDown, 
  TrendingUp, 
  Receipt, 
  Users, 
  Phone, 
  MessageSquare, 
  Wallet, 
  ArrowUpRight,
  Calendar,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  Sparkles,
  RotateCcw,
  CalendarDays
} from 'lucide-vue-next';
import { Bar } from 'vue-chartjs';
import { 
  Chart as ChartJS, 
  Title, 
  Tooltip, 
  Legend, 
  BarElement, 
  CategoryScale, 
  LinearScale 
} from 'chart.js';
import type { DashboardSummary, Customer, CustomerStatement } from '../services/api';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale);

const emit = defineEmits<{
  (e: 'navigate', view: string): void;
  (e: 'open-whatsapp', statement: CustomerStatement): void;
  (e: 'open-payment', customer: Customer): void;
  (e: 'quick-sale'): void;
}>();

type PeriodType = 'day' | 'week' | 'month' | 'year' | 'all';

// State
const selectedPeriod = ref<PeriodType>('day');
const summary = ref<DashboardSummary | null>(null);
const loading = ref(true);

// Helper to get today's local date YYYY-MM-DD
const getTodayStr = () => {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const selectedDate = ref<string>(getTodayStr());

// Computed for month picker (YYYY-MM)
const selectedMonth = computed({
  get: () => selectedDate.value.substring(0, 7),
  set: (val: string) => {
    if (val) {
      selectedDate.value = `${val}-01`;
      loadDashboard();
    }
  }
});

const selectedYear = computed(() => {
  return selectedDate.value.substring(0, 4);
});

// Check if currently viewing "today" / current period
const isCurrentPeriod = computed(() => {
  const today = getTodayStr();
  if (selectedPeriod.value === 'day') {
    return selectedDate.value === today;
  }
  if (selectedPeriod.value === 'week') {
    const { monday, sunday } = getWeekRange(selectedDate.value);
    const todayDate = new Date();
    return todayDate >= monday && todayDate <= sunday;
  }
  if (selectedPeriod.value === 'month') {
    return selectedDate.value.substring(0, 7) === today.substring(0, 7);
  }
  if (selectedPeriod.value === 'year') {
    return selectedDate.value.substring(0, 4) === today.substring(0, 4);
  }
  return true;
});

// Calculate Monday to Sunday range
const getWeekRange = (dateStr: string) => {
  const [y, m, d] = dateStr.split('-').map(Number);
  const base = new Date(y, m - 1, d, 12, 0, 0);
  const day = base.getDay();
  const diff = (day + 6) % 7; // days since Monday
  const monday = new Date(base);
  monday.setDate(base.getDate() - diff);
  monday.setHours(0, 0, 0, 0);

  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  sunday.setHours(23, 59, 59, 999);

  return { monday, sunday };
};

// Formatted display label for active period
const activePeriodDisplay = computed(() => {
  if (selectedPeriod.value === 'all') {
    return 'Histórico Completo';
  }
  const [y, m, d] = selectedDate.value.split('-').map(Number);
  const dt = new Date(y, m - 1, d);

  if (selectedPeriod.value === 'day') {
    const today = getTodayStr();
    const isToday = selectedDate.value === today;
    const formatted = dt.toLocaleDateString('es-PE', { 
      weekday: 'long', 
      day: 'numeric', 
      month: 'long', 
      year: 'numeric' 
    });
    const capitalized = formatted.charAt(0).toUpperCase() + formatted.slice(1);
    return isToday ? `Hoy (${capitalized})` : capitalized;
  }

  if (selectedPeriod.value === 'week') {
    const { monday, sunday } = getWeekRange(selectedDate.value);
    const mStr = monday.toLocaleDateString('es-PE', { day: '2-digit', month: 'short' });
    const sStr = sunday.toLocaleDateString('es-PE', { day: '2-digit', month: 'short', year: 'numeric' });
    return `Semana del ${mStr} al ${sStr}`;
  }

  if (selectedPeriod.value === 'month') {
    const mName = dt.toLocaleDateString('es-PE', { month: 'long', year: 'numeric' });
    return mName.charAt(0).toUpperCase() + mName.slice(1);
  }

  if (selectedPeriod.value === 'year') {
    return `Año ${y}`;
  }

  return '';
});

// Load Dashboard with exact local bounds converted to UTC
const loadDashboard = async () => {
  try {
    loading.value = true;
    const period = selectedPeriod.value;

    if (period === 'all') {
      summary.value = await api.getDashboardSummary({ period: 'all' });
      return;
    }

    const [y, m, d] = selectedDate.value.split('-').map(Number);

    if (period === 'day') {
      const start = new Date(y, m - 1, d, 0, 0, 0, 0);
      const end = new Date(y, m - 1, d, 23, 59, 59, 999);
      summary.value = await api.getDashboardSummary({
        period: 'day',
        date: selectedDate.value,
        startDate: start.toISOString(),
        endDate: end.toISOString()
      });
    } else if (period === 'week') {
      const { monday, sunday } = getWeekRange(selectedDate.value);
      summary.value = await api.getDashboardSummary({
        period: 'week',
        date: selectedDate.value,
        startDate: monday.toISOString(),
        endDate: sunday.toISOString()
      });
    } else if (period === 'month') {
      const start = new Date(y, m - 1, 1, 0, 0, 0, 0);
      const end = new Date(y, m, 0, 23, 59, 59, 999);
      summary.value = await api.getDashboardSummary({
        period: 'month',
        date: selectedDate.value,
        startDate: start.toISOString(),
        endDate: end.toISOString()
      });
    } else if (period === 'year') {
      const start = new Date(y, 0, 1, 0, 0, 0, 0);
      const end = new Date(y, 11, 31, 23, 59, 59, 999);
      summary.value = await api.getDashboardSummary({
        period: 'year',
        date: selectedDate.value,
        startDate: start.toISOString(),
        endDate: end.toISOString()
      });
    }
  } catch (err) {
    console.error('Error loading dashboard', err);
  } finally {
    loading.value = false;
  }
};

const setPeriod = (p: PeriodType) => {
  sounds.playClick();
  selectedPeriod.value = p;
  loadDashboard();
};

const goToCurrentPeriod = () => {
  sounds.playClick();
  selectedDate.value = getTodayStr();
  loadDashboard();
};

const navigatePeriod = (direction: -1 | 1) => {
  sounds.playClick();
  const [y, m, d] = selectedDate.value.split('-').map(Number);
  const current = new Date(y, m - 1, d, 12, 0, 0);

  if (selectedPeriod.value === 'day') {
    current.setDate(current.getDate() + direction);
  } else if (selectedPeriod.value === 'week') {
    current.setDate(current.getDate() + direction * 7);
  } else if (selectedPeriod.value === 'month') {
    current.setMonth(current.getMonth() + direction);
  } else if (selectedPeriod.value === 'year') {
    current.setFullYear(current.getFullYear() + direction);
  }

  const ny = current.getFullYear();
  const nm = String(current.getMonth() + 1).padStart(2, '0');
  const nd = String(current.getDate()).padStart(2, '0');
  selectedDate.value = `${ny}-${nm}-${nd}`;
  loadDashboard();
};

const onDateChange = () => {
  sounds.playClick();
  loadDashboard();
};

onMounted(() => {
  loadDashboard();
});

defineExpose({
  refresh: loadDashboard
});

// Dynamic Chart Data
const chartData = computed(() => {
  if (!summary.value) return { labels: [], datasets: [] };
  const trend = summary.value.monthlyTrend || [];
  return {
    labels: trend.map(t => t.month),
    datasets: [
      {
        label: 'Total Fiado (S/.)',
        backgroundColor: '#f43f5e',
        borderRadius: 3,
        barPercentage: 0.8,
        categoryPercentage: 0.8,
        data: trend.map(t => t.totalFiado),
      },
      {
        label: 'Total Cobrado (S/.)',
        backgroundColor: '#059669',
        borderRadius: 3,
        barPercentage: 0.8,
        categoryPercentage: 0.8,
        data: trend.map(t => t.totalCobrado),
      }
    ]
  };
});

const chartTitle = computed(() => {
  switch (selectedPeriod.value) {
    case 'day': return 'Evolución por Horas: Fiado vs Cobrado';
    case 'week': return 'Evolución Semanal: Fiado vs Cobrado';
    case 'month': return 'Evolución Mensual: Fiado vs Cobrado';
    case 'year': return 'Evolución Anual: Fiado vs Cobrado';
    default: return 'Evolución Histórica: Fiado vs Cobrado';
  }
});

const rankingTitle = computed(() => {
  switch (selectedPeriod.value) {
    case 'day': return 'Clientes con Fiados del Día';
    case 'week': return 'Clientes con Fiados de la Semana';
    case 'month': return 'Clientes con Fiados del Mes';
    case 'year': return 'Clientes con Fiados del Año';
    default: return 'Ranking: Clientes que Más Deben';
  }
});

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  layout: {
    padding: {
      left: 0,
      right: 4,
      top: 0,
      bottom: 0
    }
  },
  plugins: {
    legend: {
      position: 'top' as const,
      labels: {
        color: '#475569',
        boxWidth: 12,
        padding: 8,
        font: { family: 'Plus Jakarta Sans', size: 11, weight: 600 }
      }
    },
    tooltip: {
      backgroundColor: '#0f172a',
      titleColor: '#ffffff',
      bodyColor: '#f1f5f9',
      borderColor: '#e2e8f0',
      borderWidth: 1,
      padding: 8
    }
  },
  scales: {
    x: {
      grid: { display: false },
      ticks: { 
        color: '#64748b', 
        font: { family: 'Plus Jakarta Sans', size: selectedPeriod.value === 'day' ? 10 : 11 },
        maxRotation: 0,
        autoSkip: false
      }
    },
    y: {
      grid: { color: '#f1f5f9' },
      ticks: { 
        color: '#64748b', 
        font: { family: 'Plus Jakarta Sans', size: 10.5 },
        maxTicksLimit: 6
      }
    }
  }
}));

const handleWspQuick = async (customer: Customer) => {
  sounds.playClick();
  try {
    const statement = await api.getCustomerStatement(customer.id);
    emit('open-whatsapp', statement);
  } catch (e) {
    console.error(e);
  }
};

const handlePaymentQuick = (customer: Customer) => {
  sounds.playClick();
  emit('open-payment', customer);
};
</script>

<template>
  <div class="page-container">
    <!-- Top Header Row: Title & Date Filters Side-by-Side -->
    <div class="dashboard-top-header-row">
      <div class="dashboard-title-block">
        <h2 class="pos-main-title">Panel Principal y Balance</h2>
        <p class="pos-sub-title">
          Resumen general y estadísticas financieras
        </p>
      </div>

      <!-- Filter Bar (Dia, Semana, Mes, Año, Todo) -->
      <div class="dashboard-filter-bar">
      

      <!-- Context Controls: Date Picker & Quick Navigation -->
      <div class="filter-controls-group">
        <!-- Day Controls -->
        <template v-if="selectedPeriod === 'day'">
          <button class="nav-icon-btn" title="Día anterior" @click="navigatePeriod(-1)">
            <ChevronLeft :size="16" />
          </button>

          <div class="date-input-pill" title="Haz clic para seleccionar cualquier fecha">
            <Calendar :size="16" style="color: var(--slate-500);" />
            <input 
              type="date" 
              v-model="selectedDate" 
              @change="onDateChange" 
              class="date-picker-native" 
            />
          </div>

          <button class="nav-icon-btn" title="Día siguiente" @click="navigatePeriod(1)">
            <ChevronRight :size="16" />
          </button>

          <button 
            class="btn-preset-quick" 
            :class="{ active: isCurrentPeriod }"
            @click="goToCurrentPeriod"
          >
            Hoy
          </button>
        </template>

        <!-- Week Controls -->
        <template v-if="selectedPeriod === 'week'">
          <button class="nav-icon-btn" title="Semana anterior" @click="navigatePeriod(-1)">
            <ChevronLeft :size="16" />
          </button>

          <div class="date-input-pill">
            <Calendar :size="16" style="color: var(--emerald-600);" />
            <span style="font-size: 0.86rem; color: var(--slate-800);">{{ activePeriodDisplay }}</span>
          </div>

          <button class="nav-icon-btn" title="Semana siguiente" @click="navigatePeriod(1)">
            <ChevronRight :size="16" />
          </button>

          <button 
            class="btn-preset-quick" 
            :class="{ active: isCurrentPeriod }"
            @click="goToCurrentPeriod"
          >
            Esta Semana
          </button>
        </template>

        <!-- Month Controls -->
        <template v-if="selectedPeriod === 'month'">
          <button class="nav-icon-btn" title="Mes anterior" @click="navigatePeriod(-1)">
            <ChevronLeft :size="16" />
          </button>

          <div class="date-input-pill" title="Haz clic para elegir mes">
            <Calendar :size="16" style="color: var(--slate-500);" />
            <input 
              type="month" 
              v-model="selectedMonth" 
              class="date-picker-native" 
            />
          </div>

          <button class="nav-icon-btn" title="Mes siguiente" @click="navigatePeriod(1)">
            <ChevronRight :size="16" />
          </button>

          <button 
            class="btn-preset-quick" 
            :class="{ active: isCurrentPeriod }"
            @click="goToCurrentPeriod"
          >
            Este Mes
          </button>
        </template>

        <!-- Year Controls -->
        <template v-if="selectedPeriod === 'year'">
          <button class="nav-icon-btn" title="Año anterior" @click="navigatePeriod(-1)">
            <ChevronLeft :size="16" />
          </button>

          <div class="date-input-pill">
            <Calendar :size="16" style="color: var(--slate-500);" />
            <span style="font-size: 0.88rem; font-weight: 700; color: var(--slate-800);">Año {{ selectedYear }}</span>
          </div>

          <button class="nav-icon-btn" title="Año siguiente" @click="navigatePeriod(1)">
            <ChevronRight :size="16" />
          </button>

          <button 
            class="btn-preset-quick" 
            :class="{ active: isCurrentPeriod }"
            @click="goToCurrentPeriod"
          >
            Este Año
          </button>
        </template>

        <!-- All Controls -->
        <template v-if="selectedPeriod === 'all'">
          <div class="active-period-badge">
            <Sparkles :size="15" />
            <span>Mostrando todo el historial</span>
          </div>
        </template>
      </div>
      <!-- Dropdown: Period Selector -->
      <div class="period-select-wrapper">
        <CalendarDays :size="15" class="period-select-icon" />
        <select 
          :value="selectedPeriod" 
          @change="(e) => setPeriod((e.target as HTMLSelectElement).value as PeriodType)"
          class="period-select-input"
          title="Seleccionar periodo de análisis"
        >
          <option value="day">Día</option>
          <option value="week">Semana</option>
          <option value="month">Mes</option>
          <option value="year">Año</option>
          <option value="all">Todo</option>
        </select>
        <ChevronDown :size="13" class="period-select-chevron" />
      </div>
    </div>
  </div>

  <!-- Active Filter Summary Banner -->
    <div 
      v-if="selectedPeriod !== 'all'"
      class="dashboard-filter-banner"
    >
      <div class="dashboard-filter-banner-label">
        <span class="filter-banner-dot"></span>
        <span>Filtrado por: <strong style="color: var(--slate-900);">{{ activePeriodDisplay }}</strong></span>
      </div>
      <div class="dashboard-filter-banner-stats">
        <span class="stat-badge sales-badge">{{ summary?.periodSalesCount ?? 0 }} ventas al fiado</span>
        <span class="stat-separator">•</span>
        <span class="stat-badge payments-badge">{{ summary?.periodPaymentsCount ?? 0 }} abonos registrados</span>
      </div>
    </div>

    <!-- 4 Main KPI Cards -->
    <div class="kpi-grid">
      <!-- KPI 1: Falta por cobrar -->
      <div class="kpi-card pending">
        <div class="kpi-header">
          <span class="kpi-title">
            {{ selectedPeriod === 'all' ? 'Falta por Cobrar Total' : 'Por Cobrar del Periodo' }}
          </span>
          <div class="kpi-icon-wrap">
            <TrendingDown :size="16" />
          </div>
        </div>
        <div class="kpi-value">
          S/. {{ summary?.totalPendingDebt.toFixed(2) || '0.00' }}
        </div>
        <div class="kpi-footer" style="flex-direction: column; gap: 2px; align-items: flex-start;">
          <div style="display: flex; gap: 4px; align-items: center; flex-wrap: wrap;">
            <span style="color: #dc2626; font-weight: 700;">{{ summary?.activeDebtorsCount || 0 }} clientes</span>
            <span>con saldo</span>
          </div>
          <div v-if="selectedPeriod !== 'all' && summary?.globalPendingDebt" style="font-size: 0.68rem; color: var(--slate-400);">
            Acumulado: S/. {{ summary.globalPendingDebt.toFixed(2) }}
          </div>
        </div>
      </div>

      <!-- KPI 2: Ya Cobrado -->
      <div class="kpi-card collected">
        <div class="kpi-header">
          <span class="kpi-title">Ya Cobrado (Abonos)</span>
          <div class="kpi-icon-wrap">
            <TrendingUp :size="16" />
          </div>
        </div>
        <div class="kpi-value">
          S/. {{ summary?.totalCollected.toFixed(2) || '0.00' }}
        </div>
        <div class="kpi-footer">
          <span style="color: #059669; font-weight: 700;">Recuperado</span>
          <span>en el periodo</span>
        </div>
      </div>

      <!-- KPI 3: Total Ventas Fiadas -->
      <div class="kpi-card sales">
        <div class="kpi-header">
          <span class="kpi-title">Total Ventas Fiadas</span>
          <div class="kpi-icon-wrap">
            <Receipt :size="16" />
          </div>
        </div>
        <div class="kpi-value">
          S/. {{ summary?.totalSalesFiadas.toFixed(2) || '0.00' }}
        </div>
        <div class="kpi-footer">
          <span>{{ summary?.periodSalesCount ?? 0 }} ventas al crédito</span>
        </div>
      </div>

      <!-- KPI 4: Clientes del Periodo / Morosos -->
      <div class="kpi-card debtors">
        <div class="kpi-header">
          <span class="kpi-title">
            {{ selectedPeriod === 'all' ? 'Padrón Deudores' : 'Clientes Fiaron' }}
          </span>
          <div class="kpi-icon-wrap">
            <Users :size="16" />
          </div>
        </div>
        <div class="kpi-value">
          {{ summary?.activeDebtorsCount || 0 }}
          <span v-if="selectedPeriod === 'all'" style="font-size: 0.95rem; color: var(--slate-400); font-weight: 600;">
            / {{ summary?.totalCustomersCount || 0 }}
          </span>
        </div>
        <div class="kpi-footer">
          <span v-if="selectedPeriod === 'all'">
            {{ ((summary?.activeDebtorsCount || 0) / (summary?.totalCustomersCount || 1) * 100).toFixed(0) }}% con saldo activo
          </span>
          <span v-else>
            compraron al fiado en fecha
          </span>
        </div>
      </div>
    </div>

    <!-- Main Grid: Top Debtors + Chart -->
    <div class="dashboard-main-grid">
      <!-- Top Debtors Card -->
      <div class="glass-card">
        <div class="card-header-flex">
          <div class="card-title-wrap">
            <h3 class="card-title">
              {{ rankingTitle }}
            </h3>
            <p class="card-subtitle">
              {{ selectedPeriod === 'all' ? 'Clientes con mayor deuda pendiente acumulada' : 'Compras al fiado y saldos generados en este periodo' }}
            </p>
          </div>
          <button class="btn btn-secondary btn-sm" @click="emit('navigate', 'debts')">
            <span>Ver Todos</span>
            <ArrowUpRight :size="14" />
          </button>
        </div>

        <!-- Empty state -->
        <div 
          v-if="!summary?.topDebtors || summary.topDebtors.length === 0" 
          style="text-align: center; padding: 40px 20px; color: var(--slate-400); background: var(--slate-50); border-radius: var(--radius-md); border: 1px dashed var(--border-subtle);"
        >
          <div style="font-size: 0.92rem; font-weight: 600; color: var(--slate-600); margin-bottom: 6px;">
            No se registraron ventas al fiado en este periodo
          </div>
          <div style="font-size: 0.8rem; margin-bottom: 12px;">
            Puedes cambiar de día o ver el ranking global de deudas de la tienda.
          </div>
          <button class="btn btn-secondary btn-sm" style="margin: 0 auto;" @click="setPeriod('all')">
            <span>Ver Ranking General</span>
          </button>
        </div>

        <!-- List -->
        <div v-else class="debtor-list-container">
          <div 
            v-for="(debtor, index) in summary.topDebtors" 
            :key="debtor.id"
            class="debtor-item-row"
          >
            <!-- Rank & Info -->
            <div class="debtor-item-info">
              <div 
                class="debtor-rank-badge"
                :style="{
                  background: index === 0 ? '#dc2626' : index === 1 ? '#ea580c' : '#e2e8f0',
                  color: index < 2 ? '#ffffff' : 'var(--slate-700)'
                }"
              >
                #{{ index + 1 }}
              </div>
              <div class="debtor-details">
                <div class="debtor-name">
                  {{ debtor.name }}
                </div>
                <div v-if="debtor.phone" class="debtor-meta">
                  <span class="debtor-phone">
                    <Phone :size="12" />
                    {{ debtor.phone }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Amount -->
            <div class="debtor-item-summary">
              <div class="debtor-amount-box">
                <div class="debtor-amount-label">
                  {{ selectedPeriod === 'all' ? 'Debe' : 'Pendiente' }}
                </div>
                <div class="debtor-amount-val">
                  S/. {{ debtor.currentDebt.toFixed(2) }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Financial Chart -->
      <div class="glass-card chart-card">
        <div style="margin-bottom: 14px;">
          <h3 style="font-family: var(--font-heading); font-size: 1.2rem; font-weight: 700; color: var(--slate-900);">
            {{ chartTitle }}
          </h3>
          <p style="font-size: 0.82rem; color: var(--slate-500);">
            {{ activePeriodDisplay }}
          </p>
        </div>

        <div class="chart-container-box">
          <Bar :data="chartData" :options="chartOptions" />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.dashboard-top-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 14px;
  flex-wrap: wrap;
}

.dashboard-title-block {
  min-width: 240px;
}

.dashboard-top-header-row .dashboard-filter-bar {
  margin-bottom: 0;
}

.period-select-wrapper {
  display: inline-flex;
  align-items: center;
  position: relative;
  background: var(--slate-100);
  border: 1px solid var(--border-subtle);
  border-radius: 6px;
  padding: 0 8px;
  height: 32px;
  transition: all 0.15s ease;
}

.period-select-wrapper:hover {
  background: var(--slate-200);
  border-color: var(--slate-300);
}

.period-select-wrapper:focus-within {
  border-color: #0f172a;
  background: #ffffff;
  box-shadow: 0 0 0 2px rgba(15, 23, 42, 0.08);
}

.period-select-icon {
  color: var(--slate-600);
  pointer-events: none;
  margin-right: 5px;
  flex-shrink: 0;
}

.period-select-input {
  border: none;
  background: transparent;
  font-family: var(--font-body);
  font-size: 0.82rem;
  font-weight: 700;
  color: var(--slate-900);
  outline: none;
  cursor: pointer;
  padding-right: 18px;
  appearance: none;
  -webkit-appearance: none;
  -moz-appearance: none;
}

.period-select-chevron {
  position: absolute;
  right: 6px;
  color: var(--slate-500);
  pointer-events: none;
}

.dashboard-main-grid {
  grid-template-columns: minmax(320px, 0.72fr) 1.28fr;
}

.debtor-item-row {
  padding: 8px 12px;
  gap: 10px;
}

.debtor-name {
  font-size: 0.9rem;
  line-height: 1.2;
}

.debtor-meta {
  margin-top: 2px;
  font-size: 0.75rem;
}

@media (max-width: 1024px) {
  .dashboard-main-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 900px) {
  .dashboard-top-header-row {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }

  .dashboard-top-header-row .dashboard-filter-bar {
    width: 100%;
  }
}
</style>
