<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { 
  Search, 
  UserPlus, 
  Phone, 
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
  let list = customers.value;
  if (search.value.trim()) {
    const q = search.value.toLowerCase();
    list = list.filter(c => 
      c.name.toLowerCase().includes(q) || 
      c.phone.includes(q) || 
      (c.documentNumber && c.documentNumber.includes(q))
    );
  }
  return [...list].sort((a, b) => {
    const timeA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
    const timeB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
    if (timeA !== timeB) return timeB - timeA;
    return (b.id || '').localeCompare(a.id || '');
  });
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
        <h2 style="font-family: var(--font-heading); font-size: 1.85rem; font-weight: 800; margin-top: 4px; color: var(--slate-900);">
          Padrón de Clientes de la Bodega
        </h2>
        <p style="font-size: 0.85rem; color: var(--slate-600); margin-top: 2px;">
          Directorio de clientes, datos de contacto, direcciones y observaciones
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
              <th style="text-align: center;">Contacto / DNI</th>
              <th>Dirección</th>
              <th>Notas / Detalle</th>
              <th style="text-align: center;">Fecha Registro</th>
              <th style="text-align: center;">Estado</th>
              <th style="text-align: center; width: 110px;">Acciones</th>
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
                  </div>
                </div>
              </td>

              <td style="text-align: center;">
                <div style="display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 2px;">
                  <div style="display: flex; align-items: center; gap: 6px; font-weight: 600; color: #16a34a; font-size: 0.88rem;">
                    <Phone :size="13" />
                    <span>{{ c.phone }}</span>
                  </div>
                  <div v-if="c.documentNumber" style="font-size: 0.76rem; color: var(--slate-500); margin-top: 2px;">
                    DNI: {{ c.documentNumber }}
                  </div>
                </div>
              </td>

              <td style="color: var(--slate-700); font-size: 0.85rem; max-width: 200px;">
                {{ c.address || '—' }}
              </td>

              <td style="color: var(--slate-500); font-size: 0.82rem; max-width: 200px;">
                {{ c.notes || '—' }}
              </td>

              <td style="text-align: center; color: var(--slate-600); font-size: 0.84rem;">
                {{ c.createdAt ? new Date(c.createdAt).toLocaleDateString('es-PE') : '—' }}
              </td>

              <td style="text-align: center;">
                <span 
                  class="badge" 
                  :class="c.isActive !== false ? 'badge-paid' : 'badge-debt'"
                  style="font-size: 0.72rem; padding: 2px 8px;"
                >
                  {{ c.isActive !== false ? 'Activo' : 'Inactivo' }}
                </span>
              </td>

              <td style="text-align: center;">
                <div style="display: flex; align-items: center; justify-content: center; gap: 6px;">
                  <button 
                    class="btn btn-secondary btn-sm" 
                    title="Editar datos del cliente"
                    style="padding: 7px 9px; min-width: 32px; height: 32px;"
                    @click="emit('edit-customer', c)"
                  >
                    <Edit3 :size="14" />
                  </button>

                  <button 
                    class="btn btn-secondary btn-sm" 
                    style="color: #b91c1c; padding: 7px 9px; min-width: 32px; height: 32px;"
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
