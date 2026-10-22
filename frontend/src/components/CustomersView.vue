<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { 
  Search, 
  UserPlus, 
  Phone, 
  MessageSquare, 
  Wallet, 
  Receipt, 
  Edit3, 
  Trash2, 
  Users
} from 'lucide-vue-next';
import type { Customer, CustomerStatement } from '../services/api';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

const emit = defineEmits<{
  (e: 'new-customer'): void;
  (e: 'edit-customer', customer: Customer): void;
  (e: 'open-whatsapp', statement: CustomerStatement): void;
  (e: 'open-payment', customer: Customer): void;
  (e: 'open-statement', statement: CustomerStatement): void;
}>();

const customers = ref<Customer[]>([]);
const search = ref('');
const loading = ref(true);

const loadCustomers = async () => {
  try {
    loading.value = true;
    customers.value = await api.getCustomers();
  } catch (err) {
    console.error('Error loading customers', err);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadCustomers();
});

defineExpose({
  refresh: loadCustomers
});

const filtered = computed(() => {
  if (!search.value.trim()) return customers.value;
  const q = search.value.toLowerCase();
  return customers.value.filter(c => 
    c.name.toLowerCase().includes(q) || 
    c.phone.includes(q) || 
    (c.documentNumber && c.documentNumber.includes(q))
  );
});

const handleWhatsAppClick = async (c: Customer) => {
  sounds.playClick();
  try {
    const statement = await api.getCustomerStatement(c.id);
    emit('open-whatsapp', statement);
  } catch (err) {
    console.error(err);
  }
};

const handleStatementClick = async (c: Customer) => {
  sounds.playClick();
  try {
    const statement = await api.getCustomerStatement(c.id);
    emit('open-statement', statement);
  } catch (err) {
    console.error(err);
  }
};

const deleteCustomer = async (c: Customer) => {
  if (c.currentDebt > 0) {
    alert(`No puedes eliminar a ${c.name} porque aún tiene una deuda activa de S/. ${c.currentDebt.toFixed(2)}.`);
    return;
  }
  if (!confirm(`¿Estás seguro de eliminar al cliente ${c.name}?`)) return;

  try {
    await api.deleteCustomer(c.id);
    sounds.playClick();
    await loadCustomers();
  } catch (err: any) {
    alert(err.message || 'Error al eliminar');
  }
};
</script>

<template>
  <div class="page-container">
    <!-- Header -->
    <div style="display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 24px;">
      <div>
        <div style="font-size: 0.8rem; font-weight: 700; color: var(--emerald-600); text-transform: uppercase; letter-spacing: 0.05em; display: flex; align-items: center; gap: 6px;">
          <Users :size="15" />
          Directorio de Clientes
        </div>
        <h2 style="font-family: var(--font-heading); font-size: 1.85rem; font-weight: 800; margin-top: 4px; color: var(--slate-900);">
          Padrón de Clientes de la Bodega
        </h2>
        <p style="font-size: 0.85rem; color: var(--slate-600); margin-top: 2px;">
          Gestiona los datos de contacto, historial crediticio y deudas individuales
        </p>
      </div>

      <button class="btn btn-primary" @click="emit('new-customer')">
        <UserPlus :size="18" />
        <span>Registrar Nuevo Cliente</span>
      </button>
    </div>

    <!-- Toolbar: Search -->
    <div style="margin-bottom: 20px;">
      <div class="search-bar" style="max-width: 460px;">
        <Search class="search-icon" :size="18" />
        <input 
          v-model="search"
          type="text" 
          class="search-input" 
          placeholder="Buscar cliente por nombre, teléfono o DNI..." 
        />
      </div>
    </div>

    <!-- Table -->
    <div class="glass-card" style="padding: 12px 16px;">
      <div v-if="filtered.length === 0" style="text-align: center; padding: 40px; color: var(--slate-400);">
        No se encontraron clientes registrados con ese criterio.
      </div>

      <div v-else style="overflow-x: auto;">
        <table class="custom-table">
          <thead>
            <tr>
              <th>Cliente</th>
              <th>Contacto / DNI</th>
              <th>Dirección</th>
              <th>Total Fiado</th>
              <th>Abonos</th>
              <th>Deuda Actual</th>
              <th style="text-align: right;">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in filtered" :key="c.id">
              <td>
                <div style="display: flex; align-items: center; gap: 10px;">
                  <div style="width: 36px; height: 36px; border-radius: 8px; background: var(--emerald-100); color: var(--emerald-600); display: flex; align-items: center; justify-content: center; font-weight: 800; font-family: var(--font-heading);">
                    {{ c.name.charAt(0) }}
                  </div>
                  <div>
                    <div style="font-weight: 700; color: var(--slate-900); font-size: 0.94rem;">{{ c.name }}</div>
                    <div style="font-size: 0.74rem; color: var(--slate-400);">
                      Límite: S/. {{ c.creditLimit }}
                    </div>
                  </div>
                </div>
              </td>

              <td>
                <div style="display: flex; align-items: center; gap: 6px; font-weight: 600; color: #16a34a; font-size: 0.88rem;">
                  <Phone :size="13" />
                  <span>{{ c.phone }}</span>
                </div>
                <div v-if="c.documentNumber" style="font-size: 0.76rem; color: var(--slate-400); margin-top: 2px;">
                  DNI: {{ c.documentNumber }}
                </div>
              </td>

              <td style="color: var(--slate-600); font-size: 0.85rem; max-width: 180px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                {{ c.address || '—' }}
              </td>

              <td style="font-family: var(--font-heading); font-weight: 700; color: var(--slate-800);">
                S/. {{ c.totalPurchased.toFixed(2) }}
              </td>

              <td style="font-family: var(--font-heading); font-weight: 700; color: var(--emerald-600);">
                S/. {{ c.totalPaid.toFixed(2) }}
              </td>

              <td>
                <div style="display: flex; align-items: center; gap: 8px;">
                  <span 
                    style="font-family: var(--font-heading); font-size: 1.15rem; font-weight: 800;"
                    :style="{ color: c.currentDebt > 0 ? '#dc2626' : '#059669' }"
                  >
                    S/. {{ c.currentDebt.toFixed(2) }}
                  </span>
                  <span 
                    class="badge" 
                    :class="c.currentDebt > 0 ? 'badge-debt' : 'badge-paid'"
                    style="font-size: 0.7rem;"
                  >
                    {{ c.currentDebt > 0 ? 'Debe' : 'Al Día' }}
                  </span>
                </div>
              </td>

              <td style="text-align: right;">
                <div style="display: flex; align-items: center; justify-content: flex-end; gap: 6px;">
                  <button 
                    v-if="c.currentDebt > 0"
                    class="btn btn-wsp btn-sm" 
                    title="Mandar cobranza por WhatsApp"
                    @click="handleWhatsAppClick(c)"
                  >
                    <MessageSquare :size="14" />
                    <span>WSP</span>
                  </button>

                  <button 
                    v-if="c.currentDebt > 0"
                    class="btn btn-primary btn-sm" 
                    title="Registrar abono"
                    @click="emit('open-payment', c)"
                  >
                    <Wallet :size="14" />
                  </button>

                  <button 
                    class="btn btn-secondary btn-sm" 
                    title="Ver estado de cuenta completo"
                    @click="handleStatementClick(c)"
                  >
                    <Receipt :size="14" />
                  </button>

                  <button 
                    class="btn btn-secondary btn-sm" 
                    title="Editar datos del cliente"
                    @click="emit('edit-customer', c)"
                  >
                    <Edit3 :size="14" />
                  </button>

                  <button 
                    class="btn btn-secondary btn-sm" 
                    style="color: #b91c1c;"
                    title="Eliminar cliente"
                    :disabled="c.currentDebt > 0"
                    @click="deleteCustomer(c)"
                  >
                    <Trash2 :size="14" />
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
