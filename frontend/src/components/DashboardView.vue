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
  ShoppingBag,
  ShieldAlert,
  Calendar
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

const summary = ref<DashboardSummary | null>(null);
const loading = ref(true);

const loadDashboard = async () => {
  try {
    loading.value = true;
    summary.value = await api.getDashboardSummary();
  } catch (err) {
    console.error('Error loading dashboard', err);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadDashboard();
});

defineExpose({
  refresh: loadDashboard
});

const chartData = computed(() => {
  if (!summary.value) return { labels: [], datasets: [] };
  const trend = summary.value.monthlyTrend || [];
  return {
    labels: trend.map(t => t.month),
    datasets: [
      {
        label: 'Total Fiado (S/.)',
        backgroundColor: '#f43f5e',
        borderRadius: 4,
        data: trend.map(t => t.totalFiado),
      },
      {
        label: 'Total Cobrado (S/.)',
        backgroundColor: '#059669',
        borderRadius: 4,
        data: trend.map(t => t.totalCobrado),
      }
    ]
  };
});

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'top' as const,
      labels: {
        color: '#475569',
        font: { family: 'Plus Jakarta Sans', size: 12, weight: 600 }
      }
    },
    tooltip: {
      backgroundColor: '#0f172a',
      titleColor: '#ffffff',
      bodyColor: '#f1f5f9',
      borderColor: '#e2e8f0',
      borderWidth: 1,
      padding: 10
    }
  },
  scales: {
    x: {
      grid: { color: '#f1f5f9' },
      ticks: { color: '#64748b', font: { family: 'Plus Jakarta Sans', size: 11 } }
    },
    y: {
      grid: { color: '#f1f5f9' },
      ticks: { color: '#64748b', font: { family: 'Plus Jakarta Sans', size: 11 } }
    }
  }
};

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
    <!-- Header -->
    <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 24px;">
      <div>
        <div style="font-size: 0.8rem; font-weight: 700; color: var(--emerald-600); text-transform: uppercase; letter-spacing: 0.05em;">
          Panel Financiero
        </div>
        <h2 style="font-family: var(--font-heading); font-size: 1.85rem; font-weight: 800; margin-top: 4px; color: var(--slate-900);">
          Resumen General de Deudas
        </h2>
      </div>

      <button class="btn btn-primary" @click="emit('quick-sale')">
        <ShoppingBag :size="18" />
        <span>Nueva Venta al Fiado</span>
      </button>
    </div>

    <!-- 4 Main KPI Cards -->
    <div class="kpi-grid">
      <!-- KPI 1: Falta por cobrar -->
      <div class="kpi-card pending">
        <div class="kpi-header">
          <span class="kpi-title">Falta por Cobrar</span>
          <div class="kpi-icon-wrap">
            <TrendingDown :size="20" />
          </div>
        </div>
        <div class="kpi-value">
          S/. {{ summary?.totalPendingDebt.toFixed(2) || '0.00' }}
        </div>
        <div class="kpi-footer">
          <span style="color: #dc2626; font-weight: 700;">{{ summary?.activeDebtorsCount || 0 }} clientes</span>
          <span>con saldo pendiente</span>
        </div>
      </div>

      <!-- KPI 2: Ya Cobrado -->
      <div class="kpi-card collected">
        <div class="kpi-header">
          <span class="kpi-title">Ya Cobrado (Abonos)</span>
          <div class="kpi-icon-wrap">
            <TrendingUp :size="20" />
          </div>
        </div>
        <div class="kpi-value">
          S/. {{ summary?.totalCollected.toFixed(2) || '0.00' }}
        </div>
        <div class="kpi-footer">
          <span style="color: #059669; font-weight: 700;">Dinero recuperado</span>
          <span>en tienda y canales digitales</span>
        </div>
      </div>

      <!-- KPI 3: Total Ventas Fiadas -->
      <div class="kpi-card sales">
        <div class="kpi-header">
          <span class="kpi-title">Total Ventas Fiadas</span>
          <div class="kpi-icon-wrap">
            <Receipt :size="20" />
          </div>
        </div>
        <div class="kpi-value">
          S/. {{ summary?.totalSalesFiadas.toFixed(2) || '0.00' }}
        </div>
        <div class="kpi-footer">
          <span>Volumen total de crédito acumulado</span>
        </div>
      </div>

      <!-- KPI 4: Clientes Morosos -->
      <div class="kpi-card debtors">
        <div class="kpi-header">
          <span class="kpi-title">Clientes Morosos</span>
          <div class="kpi-icon-wrap">
            <Users :size="20" />
          </div>
        </div>
        <div class="kpi-value">
          {{ summary?.activeDebtorsCount || 0 }} / {{ summary?.totalCustomersCount || 0 }}
        </div>
        <div class="kpi-footer">
          <span>{{ ((summary?.activeDebtorsCount || 0) / (summary?.totalCustomersCount || 1) * 100).toFixed(0) }}% del padrón con saldo</span>
        </div>
      </div>
    </div>

    <!-- Main Grid: Top Debtors + Chart -->
    <div style="display: grid; grid-template-columns: 1.15fr 0.85fr; gap: 20px; margin-bottom: 24px;">
      <!-- Top Debtors -->
      <div class="glass-card">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px;">
          <div>
            <h3 style="font-family: var(--font-heading); font-size: 1.2rem; font-weight: 700; color: var(--slate-900);">
              Ranking: Clientes que Más Deben
            </h3>
            <p style="font-size: 0.82rem; color: var(--slate-500);">
              Prioridad de cobranza con notificación detallada para WhatsApp
            </p>
          </div>
          <button class="btn btn-secondary btn-sm" @click="emit('navigate', 'debts')">
            <span>Ver Todos</span>
            <ArrowUpRight :size="14" />
          </button>
        </div>

        <div v-if="!summary?.topDebtors || summary.topDebtors.length === 0" style="text-align: center; padding: 40px; color: var(--slate-400);">
          No hay clientes con deudas pendientes actualmente.
        </div>

        <div v-else style="display: flex; flex-direction: column; gap: 10px;">
          <div 
            v-for="(debtor, index) in summary.topDebtors" 
            :key="debtor.id"
            style="display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; border-radius: var(--radius-md); background: var(--slate-50); border: 1px solid var(--border-subtle); transition: background-color 0.15s ease;"
          >
            <!-- Rank & Info -->
            <div style="display: flex; align-items: center; gap: 12px;">
              <div 
                style="width: 32px; height: 32px; border-radius: 8px; display: flex; align-items: center; justify-content: center; font-family: var(--font-heading); font-weight: 800; font-size: 0.88rem;"
                :style="{
                  background: index === 0 ? '#dc2626' : index === 1 ? '#ea580c' : '#e2e8f0',
                  color: index < 2 ? '#ffffff' : 'var(--slate-700)'
                }"
              >
                #{{ index + 1 }}
              </div>
              <div>
                <div style="font-weight: 700; font-size: 0.95rem; color: var(--slate-900);">
                  {{ debtor.name }}
                </div>
                <div style="font-size: 0.78rem; color: var(--slate-500); display: flex; align-items: center; gap: 8px; margin-top: 1px;">
                  <span style="display: flex; align-items: center; gap: 4px;">
                    <Phone :size="12" />
                    {{ debtor.phone }}
                  </span>
                  <span>•</span>
                  <span>Límite: S/. {{ debtor.creditLimit }}</span>
                </div>
              </div>
            </div>

            <!-- Amount & Actions -->
            <div style="display: flex; align-items: center; gap: 14px;">
              <div style="text-align: right;">
                <div style="font-size: 0.7rem; color: #dc2626; font-weight: 700; text-transform: uppercase;">Debe</div>
                <div style="font-family: var(--font-heading); font-size: 1.25rem; font-weight: 800; color: #dc2626;">
                  S/. {{ debtor.currentDebt.toFixed(2) }}
                </div>
              </div>

              <div style="display: flex; gap: 6px;">
                <button 
                  class="btn btn-wsp btn-sm" 
                  title="Enviar cobranza por WhatsApp"
                  @click="handleWspQuick(debtor)"
                >
                  <MessageSquare :size="14" />
                  <span>Cobrar</span>
                </button>
                <button 
                  class="btn btn-secondary btn-sm" 
                  title="Registrar abono de dinero"
                  @click="handlePaymentQuick(debtor)"
                >
                  <Wallet :size="14" />
                  <span>Abonar</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Financial Chart -->
      <div class="glass-card" style="display: flex; flex-direction: column;">
        <div style="margin-bottom: 14px;">
          <h3 style="font-family: var(--font-heading); font-size: 1.2rem; font-weight: 700; color: var(--slate-900);">
            Evolución: Fiado vs Cobrado
          </h3>
          <p style="font-size: 0.82rem; color: var(--slate-500);">
            Comparativa de los últimos 6 meses
          </p>
        </div>

        <div style="flex: 1; min-height: 240px; position: relative;">
          <Bar :data="chartData" :options="chartOptions" />
        </div>
      </div>
    </div>

    <!-- Recent Activity -->
    <div class="glass-card">
      <div style="margin-bottom: 14px;">
        <h3 style="font-family: var(--font-heading); font-size: 1.15rem; font-weight: 700; color: var(--slate-900);">
          Movimientos Recientes en Tienda
        </h3>
        <p style="font-size: 0.8rem; color: var(--slate-500);">
          Últimas ventas fiadas y abonos registrados en la bodega
        </p>
      </div>

      <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 12px;">
        <div 
          v-for="act in summary?.recentActivity || []" 
          :key="act.id"
          style="display: flex; align-items: center; gap: 12px; padding: 12px 14px; border-radius: var(--radius-md); background: var(--slate-50); border: 1px solid var(--border-subtle);"
        >
          <div 
            style="width: 36px; height: 36px; border-radius: 8px; display: flex; align-items: center; justify-content: center;"
            :style="{
              background: act.type === 'SALE' ? '#fee2e2' : '#d1fae5',
              color: act.type === 'SALE' ? '#dc2626' : '#059669'
            }"
          >
            <Receipt v-if="act.type === 'SALE'" :size="18" />
            <Wallet v-else :size="18" />
          </div>

          <div style="flex: 1; overflow: hidden;">
            <div style="font-weight: 600; font-size: 0.88rem; color: var(--slate-900); text-overflow: ellipsis; white-space: nowrap; overflow: hidden;">
              {{ act.customerName }}
            </div>
            <div style="font-size: 0.76rem; color: var(--slate-500); text-overflow: ellipsis; white-space: nowrap; overflow: hidden;">
              {{ act.description }}
            </div>
          </div>

          <div style="text-align: right;">
            <div 
              style="font-family: var(--font-heading); font-weight: 800; font-size: 1rem;"
              :style="{ color: act.type === 'SALE' ? '#dc2626' : '#059669' }"
            >
              {{ act.type === 'SALE' ? '-' : '+' }} S/. {{ act.amount.toFixed(2) }}
            </div>
            <div style="font-size: 0.72rem; color: var(--slate-400);">
              {{ new Date(act.date).toLocaleDateString('es-PE', { day: '2-digit', month: 'short' }) }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
