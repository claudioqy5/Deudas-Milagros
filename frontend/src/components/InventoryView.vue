<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { 
  Search, 
  PackagePlus, 
  Package, 
  Edit3, 
  Trash2, 
  Plus, 
  Minus,
  Infinity as InfinityIcon
} from 'lucide-vue-next';
import type { Product } from '../services/api';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

const emit = defineEmits<{
  (e: 'new-product'): void;
  (e: 'edit-product', product: Product): void;
}>();

const products = ref<Product[]>([]);
const categories = ref<string[]>([]);
const search = ref('');
const selectedCategory = ref('Todos');
const loading = ref(true);

const loadInventory = async () => {
  try {
    loading.value = true;
    const [pList, catList] = await Promise.all([
      api.getProducts(),
      api.getProductCategories()
    ]);
    products.value = pList;
    categories.value = ['Todos', ...catList];
  } catch (err) {
    console.error('Error loading inventory', err);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadInventory();
});

defineExpose({
  refresh: loadInventory
});

const filtered = computed(() => {
  let list = products.value;

  if (selectedCategory.value !== 'Todos') {
    list = list.filter(p => p.category === selectedCategory.value);
  }

  if (search.value.trim()) {
    const q = search.value.toLowerCase();
    list = list.filter(p => p.name.toLowerCase().includes(q));
  }

  return list;
});

const quickAdjustStock = async (product: Product, delta: number) => {
  if (product.isInfiniteStock) return;
  sounds.playClick();
  const newStock = Math.max(0, product.stock + delta);
  try {
    product.stock = newStock;
    await api.updateProduct(product.id, {
      ...product,
      stock: newStock
    });
  } catch (err) {
    console.error(err);
    await loadInventory();
  }
};

const deleteProduct = async (product: Product) => {
  if (!confirm(`¿Estás seguro de eliminar el producto "${product.name}"?`)) return;
  try {
    sounds.playClick();
    await api.deleteProduct(product.id);
    await loadInventory();
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
        <div style="font-size: 0.82rem; font-weight: 700; color: var(--emerald-600); text-transform: uppercase; letter-spacing: 0.05em; display: flex; align-items: center; gap: 6px;">
          <Package :size="15" />
          Catálogo Simplificado
        </div>
        <h2 style="font-family: var(--font-heading); font-size: 1.85rem; font-weight: 800; margin-top: 4px; color: var(--slate-900);">
          Inventario de Productos
        </h2>
        <p style="font-size: 0.85rem; color: var(--slate-600); margin-top: 2px;">
          Precios de venta al público en Soles (S/.) y disponibilidad para ventas al fiado
        </p>
      </div>

      <button class="btn btn-primary" @click="emit('new-product')">
        <PackagePlus :size="18" />
        <span>Nuevo Producto</span>
      </button>
    </div>

    <!-- Toolbar: Search + Categories -->
    <div style="display: flex; gap: 14px; margin-bottom: 20px; flex-wrap: wrap;">
      <div class="search-bar" style="flex: 1; min-width: 260px;">
        <Search class="search-icon" :size="18" />
        <input 
          v-model="search"
          type="text" 
          class="search-input" 
          placeholder="Buscar producto por nombre..." 
        />
      </div>

      <!-- Categories Filter -->
      <div style="display: flex; gap: 6px; overflow-x: auto;">
        <button 
          v-for="cat in categories" 
          :key="cat"
          class="btn btn-sm"
          :class="selectedCategory === cat ? 'btn-primary' : 'btn-secondary'"
          style="border-radius: 999px; padding: 6px 14px;"
          @click="selectedCategory = cat"
        >
          {{ cat }}
        </button>
      </div>
    </div>

    <!-- Products Table -->
    <div class="glass-card" style="padding: 12px 16px;">
      <div v-if="filtered.length === 0" style="text-align: center; padding: 40px; color: var(--slate-400);">
        No se encontraron productos con los criterios seleccionados.
      </div>

      <div v-else style="overflow-x: auto;">
        <table class="custom-table">
          <thead>
            <tr>
              <th>Producto</th>
              <th>Categoría</th>
              <th>Precio de Venta</th>
              <th>Disponibilidad / Stock</th>
              <th style="text-align: right;">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in filtered" :key="p.id">
              <td>
                <div style="font-weight: 700; color: var(--slate-900); font-size: 0.95rem;">{{ p.name }}</div>
                <div style="font-size: 0.75rem; color: var(--slate-500);">
                  Unidad: {{ p.unit }}
                </div>
              </td>

              <td>
                <span class="badge" style="background: var(--slate-100); color: var(--slate-700); border: 1px solid var(--border-subtle);">
                  {{ p.category }}
                </span>
              </td>

              <td style="font-family: var(--font-heading); font-size: 1.15rem; font-weight: 800; color: var(--emerald-600);">
                S/. {{ p.salePrice.toFixed(2) }}
              </td>

              <td>
                <!-- Infinite stock badge -->
                <span 
                  v-if="p.isInfiniteStock" 
                  class="badge badge-infinite"
                  style="font-size: 0.78rem; padding: 4px 10px;"
                >
                  <InfinityIcon :size="14" />
                  Stock Infinito
                </span>

                <!-- Manual stock controls -->
                <div v-else style="display: flex; align-items: center; gap: 8px;">
                  <button 
                    class="btn btn-secondary btn-sm" 
                    style="padding: 2px 6px; width: 22px; height: 22px;"
                    title="Restar 1 unidad"
                    @click="quickAdjustStock(p, -1)"
                  >
                    <Minus :size="11" />
                  </button>

                  <span 
                    style="font-family: var(--font-heading); font-weight: 700; font-size: 0.95rem; min-width: 40px; text-align: center; color: var(--slate-900);"
                  >
                    {{ p.stock }} {{ p.unit }}
                  </span>

                  <button 
                    class="btn btn-secondary btn-sm" 
                    style="padding: 2px 6px; width: 22px; height: 22px;"
                    title="Sumar 1 unidad"
                    @click="quickAdjustStock(p, 1)"
                  >
                    <Plus :size="11" />
                  </button>
                </div>
              </td>

              <td style="text-align: right;">
                <div style="display: flex; align-items: center; justify-content: flex-end; gap: 6px;">
                  <button 
                    class="btn btn-secondary btn-sm" 
                    title="Editar producto"
                    @click="emit('edit-product', p)"
                  >
                    <Edit3 :size="14" />
                  </button>
                  <button 
                    class="btn btn-secondary btn-sm" 
                    style="color: #b91c1c;"
                    title="Eliminar producto"
                    @click="deleteProduct(p)"
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
