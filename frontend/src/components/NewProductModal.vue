<script setup lang="ts">
import { ref, watch, computed, onMounted } from 'vue';
import { 
  X, 
  PackagePlus, 
  Package,
  Save, 
  Infinity as InfinityIcon, 
  Upload, 
  Link as LinkIcon, 
  Trash2, 
  Image as ImageIcon,
  CheckCircle2,
  Boxes,
  Plus,
  Minus,
  Link2,
  Sparkles,
  Info
} from 'lucide-vue-next';
import type { Product, ProductPack, ComboItem } from '../services/api';
import { api } from '../services/api';
import { sounds } from '../utils/sound';
import { compressImageFile } from '../utils/image';

const props = defineProps<{
  show: boolean;
  productToEdit?: Product | null;
  categories: string[];
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'product-saved', product: Product): void;
}>();

// Product Type: single vs combo
const productType = ref<'single' | 'combo'>('single');

const name = ref('');
const category = ref('Abarrotes');
const salePrice = ref<number | ''>('');
const isInfiniteStock = ref(true);
const stock = ref<number | ''>(0);
const unit = ref('und');
const imageUrl = ref('');
const imageInputMode = ref<'file' | 'url'>('file');
const isProcessingImage = ref(false);
const fileInput = ref<HTMLInputElement | null>(null);

// Packs State (for single product variants, e.g. sixpack)
const packs = ref<ProductPack[]>([]);

// Combo / Fused Pack State
const comboItems = ref<ComboItem[]>([]);
const availableProducts = ref<Product[]>([]);
const selectedComponentId = ref('');
const componentQuantity = ref<number>(1);

const loading = ref(false);
const error = ref('');

const defaultCategories = ['Abarrotes', 'Lácteos', 'Bebidas', 'Huevos y Pan', 'Limpieza', 'Snacks', 'Embutidos', 'Combos y Packs', 'Otros'];

// Load available products for combo selector
const loadAvailableProducts = async () => {
  try {
    const list = await api.getProducts();
    // Exclude currently editing product to avoid self-reference
    availableProducts.value = list.filter(p => !props.productToEdit || p.id !== props.productToEdit.id);
  } catch (err) {
    console.error('Error loading products for combo', err);
  }
};

watch(() => props.show, (isOpen) => {
  if (isOpen) {
    loadAvailableProducts();
  }
});

watch(() => props.productToEdit, (p) => {
  if (p) {
    productType.value = p.isCombo ? 'combo' : 'single';
    name.value = p.name;
    category.value = p.category;
    salePrice.value = p.salePrice;
    isInfiniteStock.value = p.isInfiniteStock ?? true;
    stock.value = p.stock || 0;
    unit.value = p.unit;
    imageUrl.value = p.imageUrl || '';
    packs.value = p.packs ? JSON.parse(JSON.stringify(p.packs)) : [];
    comboItems.value = p.comboItems ? JSON.parse(JSON.stringify(p.comboItems)) : [];
  } else {
    productType.value = 'single';
    name.value = '';
    category.value = 'Abarrotes';
    salePrice.value = '';
    isInfiniteStock.value = true;
    stock.value = 0;
    unit.value = 'und';
    imageUrl.value = '';
    packs.value = [];
    comboItems.value = [];
  }
  error.value = '';
  selectedComponentId.value = '';
  componentQuantity.value = 1;
}, { immediate: true });

const setProductTypeCombo = () => {
  sounds.playClick();
  productType.value = 'combo';
  if (!category.value || category.value === 'Abarrotes') {
    category.value = 'Combos y Packs';
  }
  unit.value = 'pack';
  isInfiniteStock.value = true;
};

const setProductTypeSingle = () => {
  sounds.playClick();
  productType.value = 'single';
  if (category.value === 'Combos y Packs') {
    category.value = 'Abarrotes';
  }
  unit.value = 'und';
};

const triggerFileInput = () => {
  fileInput.value?.click();
};

const handleFileChange = async (e: Event) => {
  const target = e.target as HTMLInputElement;
  const file = target.files?.[0];
  if (!file) return;

  try {
    isProcessingImage.value = true;
    error.value = '';
    const optimizedBase64 = await compressImageFile(file, 600, 600, 0.82);
    imageUrl.value = optimizedBase64;
    sounds.playClick();
  } catch (err: any) {
    error.value = err.message || 'Error al procesar la imagen seleccionada';
  } finally {
    isProcessingImage.value = false;
    if (target) target.value = '';
  }
};

const removeImage = () => {
  imageUrl.value = '';
  sounds.playClick();
};

// Combo / Fused Pack Operations
const comboRegularTotal = computed(() => {
  return comboItems.value.reduce((acc, it) => acc + (it.quantity * it.unitPrice), 0);
});

const savingsAmount = computed(() => {
  const sPrice = Number(salePrice.value) || 0;
  if (!sPrice || sPrice >= comboRegularTotal.value) return 0;
  return comboRegularTotal.value - sPrice;
});

const savingsPercentage = computed(() => {
  if (comboRegularTotal.value <= 0 || savingsAmount.value <= 0) return 0;
  return Math.round((savingsAmount.value / comboRegularTotal.value) * 100);
});

const addComponentToCombo = () => {
  if (!selectedComponentId.value) return;
  const prod = availableProducts.value.find(p => p.id === selectedComponentId.value);
  if (!prod) return;

  sounds.playClick();
  const existing = comboItems.value.find(ci => ci.productId === prod.id);
  const qty = Number(componentQuantity.value) || 1;

  if (existing) {
    existing.quantity += qty;
  } else {
    comboItems.value.push({
      productId: prod.id,
      productName: prod.name,
      quantity: qty,
      unitPrice: prod.salePrice
    });
  }

  // Auto-generate name if empty or starts with "Pack "
  if (!name.value || name.value.startsWith('Pack ')) {
    name.value = 'Pack ' + comboItems.value
      .map(c => (c.quantity > 1 ? `${c.quantity}x ` : '') + c.productName)
      .join(' + ');
  }

  // Auto-assign image from first component if no custom image set
  if (!imageUrl.value && prod.imageUrl) {
    imageUrl.value = prod.imageUrl;
  }

  // Reset selector
  selectedComponentId.value = '';
  componentQuantity.value = 1;
};

const removeComboItem = (index: number) => {
  sounds.playClick();
  comboItems.value.splice(index, 1);
  if (comboItems.value.length === 0) {
    name.value = '';
  } else if (name.value.startsWith('Pack ')) {
    name.value = 'Pack ' + comboItems.value
      .map(c => (c.quantity > 1 ? `${c.quantity}x ` : '') + c.productName)
      .join(' + ');
  }
};

const updateComboItemQty = (item: ComboItem, delta: number) => {
  sounds.playClick();
  item.quantity = Math.max(1, item.quantity + delta);
};

// Pack operations (single product variants)
const addPack = () => {
  sounds.playClick();
  packs.value.push({
    id: 'pack_' + Date.now() + '_' + Math.random().toString(36).substring(2, 6),
    name: '',
    unitsCount: 6,
    price: 0,
    imageUrl: ''
  });
};

const removePack = (index: number) => {
  sounds.playClick();
  packs.value.splice(index, 1);
};

const handlePackFileChange = async (e: Event, index: number) => {
  const target = e.target as HTMLInputElement;
  const file = target.files?.[0];
  if (!file) return;

  try {
    const optimized = await compressImageFile(file, 500, 500, 0.80);
    if (packs.value[index]) {
      packs.value[index].imageUrl = optimized;
    }
    sounds.playClick();
  } catch (err: any) {
    error.value = 'Error al procesar foto del pack: ' + (err.message || '');
  } finally {
    if (target) target.value = '';
  }
};

const saveProduct = async () => {
  if (productType.value === 'combo' && comboItems.value.length < 2) {
    error.value = 'Un pack fusionado debe incluir al menos 2 productos componentes (ej: galleta + gaseosa).';
    return;
  }

  if (!name.value.trim()) {
    error.value = 'El nombre del producto o pack es obligatorio';
    return;
  }

  const sPrice = Number(salePrice.value);
  if (!sPrice || sPrice <= 0) {
    error.value = 'El precio de venta debe ser mayor a 0';
    return;
  }

  // Validate packs if in single product mode
  if (productType.value === 'single') {
    for (let i = 0; i < packs.value.length; i++) {
      const pk = packs.value[i];
      if (!pk.name.trim()) {
        error.value = `El nombre del Pack #${i + 1} es obligatorio (ej: Sixpack agua)`;
        return;
      }
      if (Number(pk.price) <= 0) {
        error.value = `El precio del Pack #${i + 1} ("${pk.name}") debe ser mayor a 0`;
        return;
      }
      if (Number(pk.unitsCount) <= 0) {
        error.value = `Las unidades del Pack #${i + 1} deben ser al menos 1`;
        return;
      }
    }
  }

  try {
    loading.value = true;
    error.value = '';

    const cleanPacks = productType.value === 'single' ? packs.value.map(pk => ({
      id: pk.id || ('pk_' + Date.now()),
      name: pk.name.trim(),
      unitsCount: Number(pk.unitsCount) || 1,
      price: Number(pk.price) || 0,
      imageUrl: pk.imageUrl?.trim() || undefined
    })) : [];

    const isComboMode = productType.value === 'combo';

    const payload = {
      name: name.value.trim(),
      category: category.value,
      salePrice: sPrice,
      isInfiniteStock: isComboMode ? true : isInfiniteStock.value,
      stock: isComboMode ? 0 : (isInfiniteStock.value ? 0 : (Number(stock.value) || 0)),
      unit: isComboMode ? 'pack' : unit.value,
      imageUrl: imageUrl.value.trim() || undefined,
      packs: cleanPacks,
      isCombo: isComboMode,
      comboItems: isComboMode ? comboItems.value : []
    };

    let saved: Product;
    if (props.productToEdit) {
      saved = await api.updateProduct(props.productToEdit.id, payload);
    } else {
      saved = await api.createProduct(payload);
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
    <div class="modal-content" style="max-width: 600px; max-height: 90vh; display: flex; flex-direction: column;">
      <!-- Header -->
      <div class="modal-header">
        <div style="display: flex; align-items: center; gap: 10px;">
          <div style="width: 38px; height: 38px; border-radius: 10px; background: #f1f5f9; color: var(--slate-900); display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
            <PackagePlus v-if="productType === 'single'" :size="19" />
            <Link2 v-else :size="19" style="color: #4f46e5;" />
          </div>
          <div>
            <h3>{{ productToEdit ? (productType === 'combo' ? 'Editar Pack Fusionado' : 'Editar Producto') : (productType === 'combo' ? 'Nuevo Pack Fusionado' : 'Nuevo Producto') }}</h3>
            <div style="font-size: 0.78rem; color: var(--slate-500);">
              {{ productType === 'combo' ? 'Crea un combo fusionando 2 o más productos con precio especial' : 'Catálogo de venta al fiado, packs y bodega' }}
            </div>
          </div>
        </div>
        <button class="btn btn-secondary btn-sm" style="border-radius: 50%; width: 34px; height: 34px; padding: 0;" @click="emit('close')">
          <X :size="16" />
        </button>
      </div>

      <!-- Body (scrollable) -->
      <div class="modal-body" style="overflow-y: auto; flex: 1; padding: 18px 24px;">
        <div v-if="error" style="padding: 10px 14px; border-radius: var(--radius-sm); background: #fee2e2; border: 1px solid #fecdd3; color: #b91c1c; font-size: 0.85rem; margin-bottom: 16px;">
          {{ error }}
        </div>

        <!-- Product Type Selector (Single vs Fused Combo) -->
        <div style="display: flex; gap: 6px; background: var(--slate-100); padding: 4px; border-radius: var(--radius-md); margin-bottom: 20px; border: 1px solid var(--border-subtle);">
          <button 
            type="button"
            style="flex: 1; padding: 8px 12px; border-radius: 8px; border: none; font-size: 0.88rem; font-weight: 700; cursor: pointer; transition: all 0.2s ease; display: flex; align-items: center; justify-content: center; gap: 6px; font-family: var(--font-body);"
            :style="productType === 'single' ? 'background: #0f172a; color: #ffffff; box-shadow: 0 1px 3px rgba(0,0,0,0.12);' : 'background: transparent; color: var(--slate-600);'"
            @click="setProductTypeSingle"
          >
            <Package :size="16" />
            <span>Producto Simple</span>
          </button>

          <button 
            type="button"
            style="flex: 1; padding: 8px 12px; border-radius: 8px; border: none; font-size: 0.88rem; font-weight: 700; cursor: pointer; transition: all 0.2s ease; display: flex; align-items: center; justify-content: center; gap: 6px; font-family: var(--font-body);"
            :style="productType === 'combo' ? 'background: #0f172a; color: #ffffff; box-shadow: 0 1px 3px rgba(0,0,0,0.12);' : 'background: transparent; color: var(--slate-600);'"
            @click="setProductTypeCombo"
          >
            <Link2 :size="16" />
            <span>Pack Fusionado (Combo)</span>
          </button>
        </div>

        <!-- ============================================== -->
        <!-- FUSED COMBO BUILDER SECTION                     -->
        <!-- ============================================== -->
        <div v-if="productType === 'combo'" style="background: #f8faff; border: 1.5px solid #c7d2fe; border-radius: var(--radius-md); padding: 16px; margin-bottom: 20px;">
          <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px;">
            <div style="display: flex; align-items: center; gap: 8px;">
              <span style="width: 26px; height: 26px; border-radius: 6px; background: #e0e7ff; color: #4338ca; display: flex; align-items: center; justify-content: center;">
                <Boxes :size="15" />
              </span>
              <div>
                <div style="font-weight: 700; font-size: 0.92rem; color: #1e1b4b;">Productos Incluidos en el Pack</div>
                <div style="font-size: 0.74rem; color: #6366f1;">Ejemplo: 1 Galleta Soda + 1 Gaseosa</div>
              </div>
            </div>
            <span style="font-size: 0.75rem; font-weight: 700; background: #e0e7ff; color: #4338ca; padding: 3px 8px; border-radius: 6px;">
              {{ comboItems.length }} producto(s)
            </span>
          </div>

          <!-- Component Selector Row -->
          <div style="display: flex; gap: 8px; margin-bottom: 14px; flex-wrap: wrap;">
            <div style="flex: 2; min-width: 200px;">
              <select v-model="selectedComponentId" class="form-select" style="font-size: 0.85rem; padding: 8px 12px;">
                <option value="" disabled>Selecciona un producto del catálogo...</option>
                <option v-for="p in availableProducts.filter(p => !p.isCombo)" :key="p.id" :value="p.id">
                  {{ p.name }} (S/. {{ p.salePrice.toFixed(2) }})
                </option>
              </select>
            </div>

            <div style="width: 80px;">
              <input 
                v-model="componentQuantity" 
                type="number" 
                min="1" 
                class="form-input" 
                placeholder="Cant" 
                style="font-size: 0.85rem; padding: 8px 10px; text-align: center;"
              />
            </div>

            <button 
              type="button" 
              class="btn btn-secondary" 
              style="font-size: 0.84rem; padding: 8px 14px; background: #4f46e5; color: #ffffff; border-color: #4338ca; font-weight: 600;"
              :disabled="!selectedComponentId"
              @click="addComponentToCombo"
            >
              <Plus :size="15" />
              <span>Añadir</span>
            </button>
          </div>

          <!-- Empty combo items message -->
          <div v-if="comboItems.length === 0" style="padding: 16px; text-align: center; border: 1.5px dashed #c7d2fe; border-radius: 8px; background: #ffffff; color: #6366f1; font-size: 0.82rem;">
            <div>Aún no has agregado productos a este pack.</div>
            <div style="font-size: 0.74rem; color: var(--slate-500); margin-top: 4px;">
              Selecciona arriba los productos (ej: galleta y gaseosa) y presiona "Añadir".
            </div>
          </div>

          <!-- List of Added Components -->
          <div v-else style="display: flex; flex-direction: column; gap: 8px; margin-bottom: 14px;">
            <div 
              v-for="(item, idx) in comboItems" 
              :key="item.productId"
              style="display: flex; align-items: center; justify-content: space-between; padding: 10px 12px; background: #ffffff; border: 1px solid #e0e7ff; border-radius: 8px;"
            >
              <div style="flex: 1; min-width: 0;">
                <div style="font-weight: 700; font-size: 0.88rem; color: var(--slate-900); text-overflow: ellipsis; white-space: nowrap; overflow: hidden;">
                  {{ item.productName }}
                </div>
                <div style="font-size: 0.74rem; color: var(--slate-500); margin-top: 2px;">
                  Precio unitario individual: S/. {{ item.unitPrice.toFixed(2) }}
                </div>
              </div>

              <!-- Quantity Controls -->
              <div style="display: flex; align-items: center; gap: 10px;">
                <div style="display: flex; align-items: center; gap: 4px; background: var(--slate-100); padding: 2px 6px; border-radius: 6px;">
                  <button 
                    type="button" 
                    style="border: none; background: transparent; cursor: pointer; width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; color: var(--slate-600);"
                    @click="updateComboItemQty(item, -1)"
                  >
                    <Minus :size="12" />
                  </button>
                  <span style="font-size: 0.86rem; font-weight: 800; min-width: 20px; text-align: center; color: var(--slate-900);">
                    {{ item.quantity }}
                  </span>
                  <button 
                    type="button" 
                    style="border: none; background: transparent; cursor: pointer; width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; color: var(--slate-600);"
                    @click="updateComboItemQty(item, 1)"
                  >
                    <Plus :size="12" />
                  </button>
                </div>

                <div style="font-family: var(--font-heading); font-weight: 800; font-size: 0.95rem; color: var(--slate-800); min-width: 65px; text-align: right;">
                  S/. {{ (item.quantity * item.unitPrice).toFixed(2) }}
                </div>

                <button 
                  type="button" 
                  class="btn btn-secondary btn-sm" 
                  style="color: #b91c1c; border-color: #fecdd3; padding: 4px;"
                  title="Quitar del pack"
                  @click="removeComboItem(idx)"
                >
                  <Trash2 :size="13" />
                </button>
              </div>
            </div>
          </div>

          <!-- Total Calculation & Price Box -->
          <div style="background: #ffffff; border: 1px solid #e0e7ff; border-radius: 8px; padding: 12px 14px;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; font-size: 0.84rem; color: var(--slate-600);">
              <span>Suma si se compran por separado:</span>
              <strong style="font-size: 1rem; color: var(--slate-800);">S/. {{ comboRegularTotal.toFixed(2) }}</strong>
            </div>

            <div v-if="savingsAmount > 0" style="display: flex; align-items: center; gap: 6px; padding: 8px 10px; background: #ecfdf5; border: 1px solid #a7f3d0; border-radius: 6px; font-size: 0.82rem; color: #065f46; font-weight: 700;">
              <Sparkles :size="15" style="color: #059669;" />
              <span>¡El cliente ahorra S/. {{ savingsAmount.toFixed(2) }} ({{ savingsPercentage }}% menos) que comprando por separado!</span>
            </div>
          </div>
        </div>

        <!-- Image Section -->
        <div class="form-group" style="background: var(--slate-50); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 14px; margin-bottom: 18px;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; flex-wrap: wrap; gap: 6px;">
            <label class="form-label" style="margin-bottom: 0; display: flex; align-items: center; gap: 6px;">
              <ImageIcon :size="16" style="color: var(--slate-600);" />
              <span>Foto del {{ productType === 'combo' ? 'Pack Fusionado' : 'Producto' }}</span>
            </label>

            <div style="display: flex; gap: 4px; background: #e2e8f0; padding: 2px; border-radius: 6px;">
              <button 
                type="button"
                class="btn btn-sm" 
                :style="imageInputMode === 'file' ? 'background: #fff; color: var(--slate-900); font-size: 0.75rem; padding: 4px 9px; box-shadow: 0 1px 2px rgba(0,0,0,0.06);' : 'background: transparent; color: var(--slate-600); font-size: 0.75rem; padding: 4px 9px; border: none;'"
                @click="imageInputMode = 'file'"
              >
                <Upload :size="12" style="margin-right: 4px;" />
                Subir Foto
              </button>
              <button 
                type="button"
                class="btn btn-sm" 
                :style="imageInputMode === 'url' ? 'background: #fff; color: var(--slate-900); font-size: 0.75rem; padding: 4px 9px; box-shadow: 0 1px 2px rgba(0,0,0,0.06);' : 'background: transparent; color: var(--slate-600); font-size: 0.75rem; padding: 4px 9px; border: none;'"
                @click="imageInputMode = 'url'"
              >
                <LinkIcon :size="12" style="margin-right: 4px;" />
                Pegar URL
              </button>
            </div>
          </div>

          <input 
            ref="fileInput" 
            type="file" 
            accept="image/*" 
            style="display: none;" 
            @change="handleFileChange" 
          />

          <!-- Image Preview Area -->
          <div v-if="imageUrl" style="display: flex; align-items: center; gap: 14px; padding: 10px; background: #ffffff; border: 1px solid var(--border-subtle); border-radius: var(--radius-md);">
            <div style="width: 64px; height: 64px; border-radius: 8px; overflow: hidden; background: #f8fafc; border: 1px solid #e2e8f0; display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
              <img :src="imageUrl" alt="Vista previa" style="width: 100%; height: 100%; object-fit: cover;" />
            </div>
            <div style="flex: 1; min-width: 0;">
              <div style="font-size: 0.82rem; font-weight: 600; color: var(--slate-800); display: flex; align-items: center; gap: 4px;">
                <CheckCircle2 :size="14" style="color: #4f46e5;" />
                Imagen asignada
              </div>
              <div style="font-size: 0.72rem; color: var(--slate-500); margin-top: 2px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                {{ imageUrl.startsWith('data:') ? 'Foto optimizada lista para guardar' : imageUrl }}
              </div>
            </div>
            <div style="display: flex; gap: 6px;">
              <button 
                type="button"
                class="btn btn-secondary btn-sm" 
                title="Cambiar imagen"
                @click="imageInputMode === 'file' ? triggerFileInput() : null"
              >
                Cambiar
              </button>
              <button 
                type="button"
                class="btn btn-secondary btn-sm" 
                style="color: #b91c1c; border-color: #fecdd3;" 
                title="Quitar imagen"
                @click="removeImage"
              >
                <Trash2 :size="14" />
              </button>
            </div>
          </div>

          <div v-else>
            <div 
              v-if="imageInputMode === 'file'"
              style="border: 2px dashed #cbd5e1; border-radius: var(--radius-md); padding: 16px; text-align: center; background: #ffffff; cursor: pointer; transition: all 0.2s ease;"
              @click="triggerFileInput"
            >
              <div style="width: 40px; height: 40px; border-radius: 50%; background: var(--slate-100); color: var(--slate-700); margin: 0 auto 8px; display: flex; align-items: center; justify-content: center;">
                <Upload :size="18" />
              </div>
              <div style="font-size: 0.86rem; font-weight: 600; color: var(--slate-800);">
                {{ isProcessingImage ? 'Procesando imagen...' : 'Seleccionar foto desde tu tablet, celular o PC' }}
              </div>
              <div style="font-size: 0.74rem; color: var(--slate-500); margin-top: 2px;">
                JPG, PNG, WEBP (se optimiza automáticamente)
              </div>
            </div>

            <div v-else style="display: flex; gap: 8px;">
              <input 
                v-model="imageUrl" 
                type="url" 
                class="form-input" 
                placeholder="https://ejemplo.com/foto-producto.jpg" 
                style="font-size: 0.85rem;"
              />
            </div>
          </div>
        </div>

        <!-- Name Field -->
        <div class="form-group">
          <label class="form-label">
            {{ productType === 'combo' ? 'Nombre del Pack Fusionado: *' : 'Nombre del Producto / Marca: *' }}
          </label>
          <input 
            v-model="name" 
            type="text" 
            class="form-input" 
            :placeholder="productType === 'combo' ? 'Ej: Pack Galleta Soda + Gaseosa Inka Kola' : 'Ej: Agua Mineral San Mateo 600ml'" 
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

          <div v-if="productType === 'single'" class="form-group">
            <label class="form-label">Unidad de Medida:</label>
            <select v-model="unit" class="form-select">
              <option value="und">Unidad (und)</option>
              <option value="botella">Botella</option>
              <option value="lata">Lata</option>
              <option value="bolsa">Bolsa</option>
              <option value="paquete">Paquete</option>
              <option value="kg">Kilo (kg)</option>
              <option value="barra">Barra</option>
            </select>
          </div>
        </div>

        <!-- Sale Price -->
        <div class="form-group">
          <label class="form-label">
            {{ productType === 'combo' ? 'Precio Especial de Venta del Pack (S/.): *' : 'Precio Unitario de Venta al Público (S/.): *' }}
          </label>
          <div style="position: relative;">
            <span style="position: absolute; left: 14px; top: 12px; font-weight: 700; color: var(--slate-700); font-size: 1rem;">S/.</span>
            <input 
              v-model="salePrice" 
              type="number" 
              step="0.1" 
              class="form-input" 
              :placeholder="productType === 'combo' ? 'Ej: 3.50 (menos que separado)' : '2.00'" 
              style="padding-left: 44px; font-weight: 700; font-size: 1.15rem; color: var(--slate-900);" 
            />
          </div>
          <div v-if="productType === 'combo'" style="font-size: 0.74rem; color: var(--slate-500); margin-top: 4px;">
            Este es el precio final que se cobrará al vender el pack junto.
          </div>
        </div>

        <!-- Single Product: Packs / Presentations -->
        <div v-if="productType === 'single'" class="form-group" style="background: #f8fafc; border: 1.5px solid #e2e8f0; border-radius: var(--radius-md); padding: 16px; margin-bottom: 18px;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; flex-wrap: wrap; gap: 8px;">
            <div>
              <label class="form-label" style="margin-bottom: 0; display: flex; align-items: center; gap: 6px;">
                <Boxes :size="17" style="color: var(--indigo-600);" />
                <span style="font-size: 0.92rem; font-weight: 700; color: var(--slate-900);">Presentaciones en Paquete (Mismo producto)</span>
              </label>
              <div style="font-size: 0.75rem; color: var(--slate-500); margin-top: 2px;">
                Ejemplo: "Sixpack agua", "Caja x24" con precio especial por cantidad
              </div>
            </div>

            <button 
              type="button" 
              class="btn btn-secondary btn-sm" 
              style="background: #eef2ff; border-color: #c7d2fe; color: #4338ca; font-weight: 600; padding: 6px 12px; display: flex; align-items: center; gap: 4px;"
              @click="addPack"
            >
              <Plus :size="15" />
              <span>Agregar Presentación</span>
            </button>
          </div>

          <div v-if="packs.length === 0" style="padding: 16px; text-align: center; border: 1.5px dashed #cbd5e1; border-radius: var(--radius-md); background: #ffffff; color: var(--slate-500); font-size: 0.82rem;">
            <div>¿Deseas vender este producto en paquetes de varias unidades?</div>
            <div style="font-size: 0.74rem; color: var(--slate-400); margin-top: 3px;">
              Ejemplo: 6 botellas de agua como "Sixpack" con precio promocional.
            </div>
            <button 
              type="button" 
              class="btn btn-secondary btn-sm" 
              style="margin-top: 10px; font-size: 0.78rem;" 
              @click="addPack"
            >
              <Plus :size="13" />
              <span>Crear presentación x unidades</span>
            </button>
          </div>

          <div v-else style="display: flex; flex-direction: column; gap: 12px;">
            <div 
              v-for="(pack, index) in packs" 
              :key="pack.id" 
              style="background: #ffffff; border: 1px solid #cbd5e1; border-radius: var(--radius-md); padding: 14px; box-shadow: 0 1px 3px rgba(0,0,0,0.04);"
            >
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
                <span style="font-size: 0.74rem; font-weight: 700; color: #4338ca; background: #e0e7ff; padding: 2px 10px; border-radius: 999px; display: flex; align-items: center; gap: 4px;">
                  <Boxes :size="12" />
                  Presentación #{{ index + 1 }}
                </span>
                <button 
                  type="button" 
                  class="btn btn-secondary btn-sm" 
                  style="color: #b91c1c; border-color: #fecdd3; padding: 3px 8px;"
                  title="Eliminar este pack"
                  @click="removePack(index)"
                >
                  <Trash2 :size="13" />
                  <span style="font-size: 0.74rem; margin-left: 2px;">Quitar</span>
                </button>
              </div>

              <div class="form-row" style="margin-bottom: 10px; gap: 10px;">
                <div class="form-group" style="flex: 2; margin-bottom: 0;">
                  <label class="form-label" style="font-size: 0.74rem; margin-bottom: 4px;">Nombre: *</label>
                  <input 
                    v-model="pack.name" 
                    type="text" 
                    class="form-input" 
                    placeholder="Ej: Sixpack agua / Caja x24" 
                    style="font-size: 0.86rem; padding: 8px 12px;"
                  />
                </div>

                <div class="form-group" style="flex: 1; margin-bottom: 0;">
                  <label class="form-label" style="font-size: 0.74rem; margin-bottom: 4px;">Equivale a:</label>
                  <input 
                    v-model="pack.unitsCount" 
                    type="number" 
                    min="1" 
                    class="form-input" 
                    placeholder="6" 
                    style="font-size: 0.86rem; padding: 8px 10px;"
                  />
                </div>

                <div class="form-group" style="flex: 1.3; margin-bottom: 0;">
                  <label class="form-label" style="font-size: 0.74rem; margin-bottom: 4px;">Precio (S/.): *</label>
                  <div style="position: relative;">
                    <span style="position: absolute; left: 10px; top: 8px; font-weight: 700; color: var(--slate-700); font-size: 0.85rem;">S/.</span>
                    <input 
                      v-model="pack.price" 
                      type="number" 
                      step="0.1" 
                      class="form-input" 
                      placeholder="10.00" 
                      style="padding-left: 32px; font-size: 0.95rem; font-weight: 700; color: var(--slate-900); padding: 8px 8px 8px 32px;"
                    />
                  </div>
                </div>
              </div>

              <!-- Pack Custom Image -->
              <div style="display: flex; align-items: center; gap: 10px; padding-top: 8px; border-top: 1px dashed #e2e8f0;">
                <div 
                  v-if="pack.imageUrl"
                  style="position: relative; width: 44px; height: 44px; border-radius: 6px; overflow: hidden; border: 1px solid #cbd5e1; flex-shrink: 0;"
                >
                  <img :src="pack.imageUrl" alt="Foto pack" style="width: 100%; height: 100%; object-fit: cover;" />
                  <button 
                    type="button" 
                    style="position: absolute; top: 1px; right: 1px; background: rgba(0,0,0,0.65); color: #ffffff; border: none; border-radius: 3px; width: 16px; height: 16px; font-size: 10px; display: flex; align-items: center; justify-content: center; cursor: pointer;"
                    title="Quitar foto"
                    @click="pack.imageUrl = ''"
                  >
                    ✕
                  </button>
                </div>
                <div v-else style="width: 44px; height: 44px; border-radius: 6px; background: #f1f5f9; border: 1px dashed #94a3b8; display: flex; align-items: center; justify-content: center; color: #94a3b8; flex-shrink: 0;">
                  <ImageIcon :size="20" />
                </div>

                <div style="flex: 1;">
                  <label 
                    class="btn btn-secondary btn-sm" 
                    style="display: inline-flex; align-items: center; gap: 5px; cursor: pointer; padding: 4px 10px; font-size: 0.76rem;"
                  >
                    <Upload :size="13" />
                    <span>{{ pack.imageUrl ? 'Cambiar foto del pack' : 'Subir foto para este pack (opcional)' }}</span>
                    <input 
                      type="file" 
                      accept="image/*" 
                      style="display: none;" 
                      @change="(e) => handlePackFileChange(e, index)"
                    />
                  </label>
                  <div style="font-size: 0.72rem; color: var(--slate-400); margin-top: 2px;">
                    {{ pack.imageUrl ? 'Foto personalizada asignada al pack' : 'Si no subes una, se usará la foto del producto principal.' }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Stock Option -->
        <div style="background: var(--slate-50); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 14px; margin-bottom: 8px;">
          <template v-if="productType === 'combo'">
            <div style="display: flex; align-items: center; gap: 10px;">
              <span style="width: 32px; height: 32px; border-radius: 8px; background: #e0e7ff; color: #4338ca; display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
                <Info :size="16" />
              </span>
              <div>
                <div style="font-weight: 700; font-size: 0.88rem; color: var(--slate-900);">Control de Stock Automático</div>
                <div style="font-size: 0.76rem; color: var(--slate-500); margin-top: 2px;">
                  Al vender este pack, el sistema descontará automáticamente el stock de cada uno de sus productos componentes (ej: galleta y gaseosa).
                </div>
              </div>
            </div>
          </template>

          <template v-else>
            <label style="display: flex; align-items: center; gap: 10px; cursor: pointer;">
              <input 
                v-model="isInfiniteStock" 
                type="checkbox" 
                style="width: 20px; height: 20px; accent-color: #0f172a; cursor: pointer;"
              />
              <div>
                <div style="font-weight: 700; font-size: 0.9rem; color: var(--slate-900); display: flex; align-items: center; gap: 6px;">
                  <InfinityIcon :size="16" style="color: var(--indigo-600);" />
                  Stock Infinito / Ilimitado
                </div>
                <div style="font-size: 0.76rem; color: var(--slate-500); margin-top: 2px;">
                  Recomendado para bodega diaria sin control manual de inventario.
                </div>
              </div>
            </label>

            <div v-if="!isInfiniteStock" style="margin-top: 14px; padding-top: 12px; border-top: 1px dashed var(--border-subtle);">
              <label class="form-label">Cantidad en Stock (en {{ unit }}):</label>
              <input 
                v-model="stock" 
                type="number" 
                class="form-input" 
                placeholder="0" 
              />
            </div>
          </template>
        </div>
      </div>

      <!-- Footer -->
      <div class="modal-footer" style="padding: 14px 24px;">
        <button class="btn btn-secondary" :disabled="loading" @click="emit('close')">
          Cancelar
        </button>
        <button class="btn btn-primary" :disabled="loading" @click="saveProduct">
          <Save :size="16" />
          <span>{{ loading ? 'Guardando...' : (productToEdit ? 'Actualizar' : (productType === 'combo' ? 'Crear Pack Fusionado' : 'Guardar Producto')) }}</span>
        </button>
      </div>
    </div>
  </div>
</template>
