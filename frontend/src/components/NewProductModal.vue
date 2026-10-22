<script setup lang="ts">
import { ref, watch } from 'vue';
import { X, PackagePlus, Save, Infinity as InfinityIcon } from 'lucide-vue-next';
import type { Product } from '../services/api';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

const props = defineProps<{
  show: boolean;
  productToEdit?: Product | null;
  categories: string[];
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'product-saved', product: Product): void;
}>();

const name = ref('');
const category = ref('Abarrotes');
const salePrice = ref<number | ''>('');
const isInfiniteStock = ref(true);
const stock = ref<number | ''>(0);
const unit = ref('und');
const loading = ref(false);
const error = ref('');

const defaultCategories = ['Abarrotes', 'Lácteos', 'Bebidas', 'Huevos y Pan', 'Limpieza', 'Snacks', 'Embutidos', 'Otros'];

watch(() => props.productToEdit, (p) => {
  if (p) {
    name.value = p.name;
    category.value = p.category;
    salePrice.value = p.salePrice;
    isInfiniteStock.value = p.isInfiniteStock ?? true;
    stock.value = p.stock || 0;
    unit.value = p.unit;
  } else {
    name.value = '';
    category.value = 'Abarrotes';
    salePrice.value = '';
    isInfiniteStock.value = true;
    stock.value = 0;
    unit.value = 'und';
  }
  error.value = '';
}, { immediate: true });

const saveProduct = async () => {
  if (!name.value.trim()) {
    error.value = 'El nombre del producto es obligatorio';
    return;
  }
  const sPrice = Number(salePrice.value);
  if (!sPrice || sPrice <= 0) {
    error.value = 'El precio de venta debe ser mayor a 0';
    return;
  }

  try {
    loading.value = true;
    error.value = '';

    let saved: Product;
    if (props.productToEdit) {
      saved = await api.updateProduct(props.productToEdit.id, {
        name: name.value.trim(),
        category: category.value,
        salePrice: sPrice,
        isInfiniteStock: isInfiniteStock.value,
        stock: isInfiniteStock.value ? 0 : (Number(stock.value) || 0),
        unit: unit.value
      });
    } else {
      saved = await api.createProduct({
        name: name.value.trim(),
        category: category.value,
        salePrice: sPrice,
        isInfiniteStock: isInfiniteStock.value,
        stock: isInfiniteStock.value ? 0 : (Number(stock.value) || 0),
        unit: unit.value
      });
    }

    sounds.playBeep();
    emit('product-saved', saved);
    emit('close');
  } catch (err: any) {
    error.value = err.message || 'Error al guardar el producto';
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <div v-if="show" class="modal-overlay" @click.self="emit('close')">
    <div class="modal-content" style="max-width: 500px;">
      <!-- Header -->
      <div class="modal-header">
        <div style="display: flex; align-items: center; gap: 10px;">
          <div style="width: 38px; height: 38px; border-radius: 10px; background: var(--emerald-100); color: var(--emerald-600); display: flex; align-items: center; justify-content: center;">
            <PackagePlus :size="19" />
          </div>
          <div>
            <h3>{{ productToEdit ? 'Editar Producto' : 'Nuevo Producto' }}</h3>
            <div style="font-size: 0.78rem; color: var(--slate-500);">
              Catálogo de venta al fiado
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
          <label class="form-label">Nombre del Producto / Marca: *</label>
          <input 
            v-model="name" 
            type="text" 
            class="form-input" 
            placeholder="Ej: Leche Gloria Etiqueta Azul 400g" 
            autofocus 
          />
        </div>

        <div class="form-row">
          <div class="form-group">
            <label class="form-label">Categoría:</label>
            <select v-model="category" class="form-select">
              <option v-for="cat in (categories.length ? categories.filter(c => c !== 'Todos') : defaultCategories)" :key="cat" :value="cat">
                {{ cat }}
              </option>
            </select>
          </div>

          <div class="form-group">
            <label class="form-label">Unidad de Medida:</label>
            <select v-model="unit" class="form-select">
              <option value="und">Unidad (und)</option>
              <option value="bolsa">Bolsa</option>
              <option value="botella">Botella</option>
              <option value="lata">Lata</option>
              <option value="paquete">Paquete</option>
              <option value="kg">Kilo (kg)</option>
              <option value="barra">Barra</option>
            </select>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label">Precio de Venta al Público (S/.): *</label>
          <div style="position: relative;">
            <span style="position: absolute; left: 14px; top: 11px; font-weight: 700; color: var(--emerald-600); font-size: 1rem;">S/.</span>
            <input 
              v-model="salePrice" 
              type="number" 
              step="0.1" 
              class="form-input" 
              placeholder="4.50" 
              style="padding-left: 44px; font-weight: 700; font-size: 1.1rem; color: var(--emerald-600);" 
            />
          </div>
        </div>

        <!-- Stock Option: Infinite Stock Toggle -->
        <div style="background: var(--slate-50); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 14px; margin-bottom: 8px;">
          <label style="display: flex; align-items: center; gap: 10px; cursor: pointer;">
            <input 
              v-model="isInfiniteStock" 
              type="checkbox" 
              style="width: 18px; height: 18px; accent-color: #059669; cursor: pointer;"
            />
            <div>
              <div style="font-weight: 700; font-size: 0.9rem; color: var(--slate-900); display: flex; align-items: center; gap: 6px;">
                <InfinityIcon :size="16" style="color: var(--indigo-600);" />
                Stock Infinito / Ilimitado
              </div>
              <div style="font-size: 0.76rem; color: var(--slate-500); margin-top: 2px;">
                Recomendado para no tener que actualizar el stock manualmente tras cada venta.
              </div>
            </div>
          </label>

          <!-- Show manual stock input if infinite stock is disabled -->
          <div v-if="!isInfiniteStock" style="margin-top: 14px; padding-top: 12px; border-top: 1px dashed var(--border-subtle);">
            <label class="form-label">Cantidad en Stock:</label>
            <input 
              v-model="stock" 
              type="number" 
              class="form-input" 
              placeholder="0" 
            />
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="modal-footer">
        <button class="btn btn-secondary" :disabled="loading" @click="emit('close')">
          Cancelar
        </button>
        <button class="btn btn-primary" :disabled="loading" @click="saveProduct">
          <Save :size="16" />
          <span>{{ loading ? 'Guardando...' : (productToEdit ? 'Actualizar' : 'Guardar Producto') }}</span>
        </button>
      </div>
    </div>
  </div>
</template>
