<script setup lang="ts">
import { ref, watch } from 'vue';
import { 
  X, 
  PackagePlus, 
  Save, 
  Infinity as InfinityIcon, 
  Upload, 
  Link as LinkIcon, 
  Trash2, 
  Image as ImageIcon,
  CheckCircle2,
  Boxes,
  Plus
} from 'lucide-vue-next';
import type { Product, ProductPack } from '../services/api';
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

// Packs State
const packs = ref<ProductPack[]>([]);

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
    imageUrl.value = p.imageUrl || '';
    packs.value = p.packs ? JSON.parse(JSON.stringify(p.packs)) : [];
  } else {
    name.value = '';
    category.value = 'Abarrotes';
    salePrice.value = '';
    isInfiniteStock.value = true;
    stock.value = 0;
    unit.value = 'und';
    imageUrl.value = '';
    packs.value = [];
  }
  error.value = '';
}, { immediate: true });

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

// Pack operations
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
  if (!name.value.trim()) {
    error.value = 'El nombre del producto es obligatorio';
    return;
  }
  const sPrice = Number(salePrice.value);
  if (!sPrice || sPrice <= 0) {
    error.value = 'El precio de venta debe ser mayor a 0';
    return;
  }

  // Validate packs if any
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

  try {
    loading.value = true;
    error.value = '';

    const cleanPacks = packs.value.map(pk => ({
      id: pk.id || ('pk_' + Date.now()),
      name: pk.name.trim(),
      unitsCount: Number(pk.unitsCount) || 1,
      price: Number(pk.price) || 0,
      imageUrl: pk.imageUrl?.trim() || undefined
    }));

    const payload = {
      name: name.value.trim(),
      category: category.value,
      salePrice: sPrice,
      isInfiniteStock: isInfiniteStock.value,
      stock: isInfiniteStock.value ? 0 : (Number(stock.value) || 0),
      unit: unit.value,
      imageUrl: imageUrl.value.trim() || undefined,
      packs: cleanPacks
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
    <div class="modal-content" style="max-width: 580px; max-height: 90vh; display: flex; flex-direction: column;">
      <!-- Header -->
      <div class="modal-header">
        <div style="display: flex; align-items: center; gap: 10px;">
          <div style="width: 38px; height: 38px; border-radius: 10px; background: var(--emerald-100); color: var(--emerald-600); display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
            <PackagePlus :size="19" />
          </div>
          <div>
            <h3>{{ productToEdit ? 'Editar Producto' : 'Nuevo Producto' }}</h3>
            <div style="font-size: 0.78rem; color: var(--slate-500);">
              Catálogo de venta al fiado, packs y bodega
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

        <!-- Product Image Section -->
        <div class="form-group" style="background: var(--slate-50); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 14px; margin-bottom: 18px;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; flex-wrap: wrap; gap: 6px;">
            <label class="form-label" style="margin-bottom: 0; display: flex; align-items: center; gap: 6px;">
              <ImageIcon :size="16" style="color: var(--emerald-600);" />
              <span>Foto Principal del Producto</span>
            </label>

            <!-- Mode selector tabs -->
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

          <!-- Hidden file input -->
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
              <div style="font-size: 0.82rem; font-weight: 600; color: var(--emerald-700); display: flex; align-items: center; gap: 4px;">
                <CheckCircle2 :size="14" />
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

          <!-- No image selected yet -->
          <div v-else>
            <div 
              v-if="imageInputMode === 'file'"
              style="border: 2px dashed #cbd5e1; border-radius: var(--radius-md); padding: 16px; text-align: center; background: #ffffff; cursor: pointer; transition: all 0.2s ease;"
              @click="triggerFileInput"
            >
              <div style="width: 40px; height: 40px; border-radius: 50%; background: var(--emerald-50); color: var(--emerald-600); margin: 0 auto 8px; display: flex; align-items: center; justify-content: center;">
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

        <div class="form-group">
          <label class="form-label">Nombre del Producto / Marca: *</label>
          <input 
            v-model="name" 
            type="text" 
            class="form-input" 
            placeholder="Ej: Agua Mineral San Mateo 600ml" 
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
              <option value="botella">Botella</option>
              <option value="lata">Lata</option>
              <option value="bolsa">Bolsa</option>
              <option value="paquete">Paquete</option>
              <option value="kg">Kilo (kg)</option>
              <option value="barra">Barra</option>
            </select>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label">Precio Unitario de Venta al Público (S/.): *</label>
          <div style="position: relative;">
            <span style="position: absolute; left: 14px; top: 12px; font-weight: 700; color: var(--emerald-600); font-size: 1rem;">S/.</span>
            <input 
              v-model="salePrice" 
              type="number" 
              step="0.1" 
              class="form-input" 
              placeholder="2.00" 
              style="padding-left: 44px; font-weight: 700; font-size: 1.15rem; color: var(--emerald-600);" 
            />
          </div>
        </div>

        <!-- ============================================== -->
        <!-- PACKS / PRESENTACIONES SECTION (Packs x6, etc.) -->
        <!-- ============================================== -->
        <div class="form-group" style="background: #f8fafc; border: 1.5px solid #e2e8f0; border-radius: var(--radius-md); padding: 16px; margin-bottom: 18px;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; flex-wrap: wrap; gap: 8px;">
            <div>
              <label class="form-label" style="margin-bottom: 0; display: flex; align-items: center; gap: 6px;">
                <Boxes :size="17" style="color: var(--indigo-600);" />
                <span style="font-size: 0.92rem; font-weight: 700; color: var(--slate-900);">Packs & Promociones</span>
              </label>
              <div style="font-size: 0.75rem; color: var(--slate-500); margin-top: 2px;">
                Arma combos (ej: "Sixpack agua", "Caja x24") con su propio precio y foto
              </div>
            </div>

            <button 
              type="button" 
              class="btn btn-secondary btn-sm" 
              style="background: #eef2ff; border-color: #c7d2fe; color: #4338ca; font-weight: 600; padding: 6px 12px; display: flex; align-items: center; gap: 4px;"
              @click="addPack"
            >
              <Plus :size="15" />
              <span>Agregar Pack</span>
            </button>
          </div>

          <div v-if="packs.length === 0" style="padding: 16px; text-align: center; border: 1.5px dashed #cbd5e1; border-radius: var(--radius-md); background: #ffffff; color: var(--slate-500); font-size: 0.82rem;">
            <div>¿Deseas vender este producto en paquetes?</div>
            <div style="font-size: 0.74rem; color: var(--slate-400); margin-top: 3px;">
              Ejemplo: 6 botellas de agua como "Sixpack agua" con precio y foto especial.
            </div>
            <button 
              type="button" 
              class="btn btn-secondary btn-sm" 
              style="margin-top: 10px; font-size: 0.78rem;" 
              @click="addPack"
            >
              <Plus :size="13" />
              <span>Crear primer Pack</span>
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
                  Pack #{{ index + 1 }}
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
                  <label class="form-label" style="font-size: 0.74rem; margin-bottom: 4px;">Nombre del Pack: *</label>
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
                  <div style="display: flex; align-items: center; gap: 4px;">
                    <input 
                      v-model="pack.unitsCount" 
                      type="number" 
                      min="1" 
                      class="form-input" 
                      placeholder="6" 
                      style="font-size: 0.86rem; padding: 8px 10px;"
                    />
                  </div>
                </div>

                <div class="form-group" style="flex: 1.3; margin-bottom: 0;">
                  <label class="form-label" style="font-size: 0.74rem; margin-bottom: 4px;">Precio Pack (S/.): *</label>
                  <div style="position: relative;">
                    <span style="position: absolute; left: 10px; top: 8px; font-weight: 700; color: var(--emerald-600); font-size: 0.85rem;">S/.</span>
                    <input 
                      v-model="pack.price" 
                      type="number" 
                      step="0.1" 
                      class="form-input" 
                      placeholder="10.00" 
                      style="padding-left: 32px; font-size: 0.95rem; font-weight: 700; color: var(--emerald-600); padding: 8px 8px 8px 32px;"
                    />
                  </div>
                </div>
              </div>

              <!-- Pack Image / Photo -->
              <div style="display: flex; align-items: center; gap: 10px; padding-top: 10px; border-top: 1px dashed #e2e8f0; flex-wrap: wrap;">
                <div style="width: 44px; height: 44px; border-radius: 6px; overflow: hidden; background: #f1f5f9; border: 1px solid #cbd5e1; display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
                  <img v-if="pack.imageUrl" :src="pack.imageUrl" alt="Foto pack" style="width: 100%; height: 100%; object-fit: cover;" />
                  <ImageIcon v-else :size="18" style="color: #94a3b8;" />
                </div>

                <div style="flex: 1; min-width: 180px;">
                  <input 
                    v-model="pack.imageUrl" 
                    type="url" 
                    class="form-input" 
                    placeholder="URL de foto propia del pack (o sube una foto)" 
                    style="font-size: 0.78rem; padding: 6px 10px;"
                  />
                </div>

                <div style="display: flex; gap: 6px;">
                  <input 
                    :id="'pack_file_' + index" 
                    type="file" 
                    accept="image/*" 
                    style="display: none;" 
                    @change="handlePackFileChange($event, index)" 
                  />
                  <label 
                    :for="'pack_file_' + index" 
                    class="btn btn-secondary btn-sm" 
                    style="cursor: pointer; padding: 6px 10px; font-size: 0.76rem; margin-bottom: 0;"
                    title="Subir foto del pack desde el dispositivo"
                  >
                    <Upload :size="13" />
                    <span>Subir Foto</span>
                  </label>

                  <button 
                    v-if="pack.imageUrl"
                    type="button"
                    class="btn btn-secondary btn-sm"
                    style="color: #b91c1c; padding: 6px 8px;"
                    title="Quitar foto del pack"
                    @click="pack.imageUrl = ''"
                  >
                    <X :size="13" />
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Stock Option: Infinite Stock Toggle -->
        <div style="background: var(--slate-50); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 14px; margin-bottom: 8px;">
          <label style="display: flex; align-items: center; gap: 10px; cursor: pointer;">
            <input 
              v-model="isInfiniteStock" 
              type="checkbox" 
              style="width: 20px; height: 20px; accent-color: #059669; cursor: pointer;"
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

          <!-- Show manual stock input if infinite stock is disabled -->
          <div v-if="!isInfiniteStock" style="margin-top: 14px; padding-top: 12px; border-top: 1px dashed var(--border-subtle);">
            <label class="form-label">Cantidad en Stock (en {{ unit }}):</label>
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
      <div class="modal-footer" style="padding: 14px 24px;">
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
