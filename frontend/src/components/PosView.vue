<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { 
  Search, 
  Plus, 
  Minus, 
  Trash2, 
  UserPlus, 
  ShoppingCart, 
  Package, 
  AlertCircle,
  MessageSquare,
  Infinity as InfinityIcon,
  CheckCircle2,
  Boxes,
  Link2,
  User,
  X,
  ChevronDown
} from 'lucide-vue-next';
import confetti from 'canvas-confetti';
import type { Customer, Product, ProductPack, CustomerStatement } from '../services/api';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

const emit = defineEmits<{
  (e: 'new-customer-request'): void;
  (e: 'sale-completed'): void;
  (e: 'open-whatsapp', statement: CustomerStatement): void;
  (e: 'cart-updated', data: { count: number; total: number }): void;
}>();

const customers = ref<Customer[]>([]);
const products = ref<Product[]>([]);
const categories = ref<string[]>([]);

const selectedCustomer = ref<Customer | null>(null);
const customerSearch = ref('');
const showCustomerDropdown = ref(false);

const productSearch = ref('');
const selectedCategory = ref('Todos');

// Cart Drawer state & Toast notice
const isCartDrawerOpen = ref(false);
const lastAddedItemNotice = ref<{ name: string; price: number; timer?: any } | null>(null);

interface CartItem {
  id: string;
  product: Product;
  pack?: ProductPack;
  displayName: string;
  displayImage?: string;
  quantity: number;
  unitPrice: number;
  unitsDeductedPerItem: number;
}

const cart = ref<CartItem[]>([]);
const initialPayment = ref<number | ''>('');
const notes = ref('');
const submitting = ref(false);
const errorMsg = ref('');
const successMsg = ref('');
const lastCreatedSaleCustomer = ref<Customer | null>(null);

const cartTotalQuantity = computed(() => {
  return cart.value.reduce((sum, item) => sum + item.quantity, 0);
});

const totalAmount = computed(() => {
  return cart.value.reduce((sum, item) => sum + (item.quantity * item.unitPrice), 0);
});

const remainingDebt = computed(() => {
  const init = Number(initialPayment.value) || 0;
  return Math.max(0, totalAmount.value - init);
});

const projectedDebt = computed(() => {
  if (!selectedCustomer.value) return 0;
  return selectedCustomer.value.currentDebt + remainingDebt.value;
});

const isOverLimit = computed(() => {
  if (!selectedCustomer.value || !selectedCustomer.value.creditLimit) return false;
  return projectedDebt.value > selectedCustomer.value.creditLimit;
});

const notifyCartUpdate = () => {
  emit('cart-updated', {
    count: cartTotalQuantity.value,
    total: totalAmount.value
  });
};

const openCart = () => {
  isCartDrawerOpen.value = true;
};

const closeCart = () => {
  isCartDrawerOpen.value = false;
};

const loadData = async () => {
  try {
    const [cList, pList, catList] = await Promise.all([
      api.getCustomers(),
      api.getProducts(),
      api.getProductCategories()
    ]);
    customers.value = cList;
    products.value = pList;
    categories.value = ['Todos', ...catList];
  } catch (err) {
    console.error('Error loading POS data', err);
  }
};

onMounted(() => {
  loadData();
});

defineExpose({
  refresh: loadData,
  selectCustomerById: (id: string) => {
    const found = customers.value.find(c => c.id === id);
    if (found) selectCustomer(found);
  },
  openCart,
  closeCart
});

const filteredCustomers = computed(() => {
  if (!customerSearch.value.trim()) return customers.value.slice(0, 8);
  const q = customerSearch.value.toLowerCase();
  return customers.value.filter(c => 
    c.name.toLowerCase().includes(q) || 
    c.phone.includes(q) || 
    (c.documentNumber && c.documentNumber.includes(q))
  );
});

const selectCustomer = (c: Customer) => {
  sounds.playClick();
  selectedCustomer.value = c;
  customerSearch.value = c.name;
  showCustomerDropdown.value = false;
};

const clearSelectedCustomer = () => {
  selectedCustomer.value = null;
  customerSearch.value = '';
};

interface PosCardItem {
  id: string;
  product: Product;
  pack?: ProductPack;
  displayName: string;
  category: string;
  price: number;
  imageUrl?: string;
  isInfiniteStock: boolean;
  stockText: string;
  isCombo: boolean;
  comboItems?: any[];
  isPack: boolean;
  packUnitsCount?: number;
}

const displayProducts = computed<PosCardItem[]>(() => {
  const list: PosCardItem[] = [];

  for (const prod of products.value) {
    // 1. Tarjeta base para la unidad individual del producto
    list.push({
      id: prod.id,
      product: prod,
      displayName: prod.name,
      category: prod.category,
      price: prod.salePrice,
      imageUrl: prod.imageUrl,
      isInfiniteStock: prod.isInfiniteStock,
      stockText: prod.isInfiniteStock ? 'Ilimitado' : `${prod.stock} ${prod.unit}`,
      isCombo: !!prod.isCombo,
      comboItems: prod.comboItems,
      isPack: false
    });

    // 2. Tarjetas independientes para cada pack/presentación (Sixpack, Caja, etc.)
    if (prod.packs && prod.packs.length > 0) {
      for (const pk of prod.packs) {
        let packStockText = 'Ilimitado';
        if (!prod.isInfiniteStock) {
          const packsAvailable = pk.unitsCount > 0 ? Math.floor(prod.stock / pk.unitsCount) : 0;
          packStockText = `${packsAvailable} ${packsAvailable === 1 ? 'pack' : 'packs'}`;
        }

        list.push({
          id: `${prod.id}__${pk.id}`,
          product: prod,
          pack: pk,
          displayName: `${prod.name} (${pk.name})`,
          category: prod.category,
          price: pk.price,
          imageUrl: pk.imageUrl || prod.imageUrl,
          isInfiniteStock: prod.isInfiniteStock,
          stockText: packStockText,
          isCombo: false,
          isPack: true,
          packUnitsCount: pk.unitsCount
        });
      }
    }
  }

  return list.filter(item => {
    let matchesCat = false;
    if (selectedCategory.value === 'Todos') {
      matchesCat = true;
    } else if (selectedCategory.value === 'Combos y Packs') {
      matchesCat = item.isPack || item.isCombo || item.category === 'Combos y Packs';
    } else {
      matchesCat = item.category === selectedCategory.value;
    }

    const q = productSearch.value.trim().toLowerCase();
    const matchesSearch = !q || 
      item.displayName.toLowerCase().includes(q) ||
      (item.pack && item.pack.name.toLowerCase().includes(q)) ||
      item.product.name.toLowerCase().includes(q);

    return matchesCat && matchesSearch;
  });
});

const addToCart = (product: Product, pack?: ProductPack) => {
  sounds.playBeep();
  const itemId = pack ? `${product.id}__${pack.id}` : product.id;
  const existing = cart.value.find(i => i.id === itemId);
  if (existing) {
    existing.quantity += 1;
  } else {
    cart.value.push({
      id: itemId,
      product,
      pack,
      displayName: pack ? `${product.name} (${pack.name})` : product.name,
      displayImage: pack?.imageUrl || product.imageUrl,
      quantity: 1,
      unitPrice: pack ? pack.price : product.salePrice,
      unitsDeductedPerItem: pack ? pack.unitsCount : 1
    });
  }

  // Avisito / Floating Toast
  if (lastAddedItemNotice.value?.timer) clearTimeout(lastAddedItemNotice.value.timer);
  const itemName = pack ? `${product.name} (${pack.name})` : product.name;
  const itemPrice = pack ? pack.price : product.salePrice;
  const timer = setTimeout(() => {
    lastAddedItemNotice.value = null;
  }, 2200);
  lastAddedItemNotice.value = { name: itemName, price: itemPrice, timer };

  notifyCartUpdate();
};

const updateQty = (item: CartItem, delta: number) => {
  sounds.playClick();
  item.quantity += delta;
  if (item.quantity <= 0) {
    removeFromCart(item);
  } else {
    notifyCartUpdate();
  }
};

const removeFromCart = (item: CartItem) => {
  sounds.playClick();
  cart.value = cart.value.filter(i => i.id !== item.id);
  notifyCartUpdate();
};

const clearCart = () => {
  sounds.playClick();
  cart.value = [];
  initialPayment.value = '';
  notes.value = '';
  errorMsg.value = '';
  successMsg.value = '';
  notifyCartUpdate();
};

const submitCreditSale = async () => {
  if (!selectedCustomer.value) {
    errorMsg.value = 'Por favor selecciona el cliente que llevará los productos al fiado.';
    return;
  }
  if (cart.value.length === 0) {
    errorMsg.value = 'El carrito está vacío. Agrega productos para registrar la venta.';
    return;
  }

  try {
    submitting.value = true;
    errorMsg.value = '';

    const payload = {
      customerId: selectedCustomer.value.id,
      items: cart.value.map(i => ({
        productId: i.product.id,
        productName: i.displayName,
        quantity: i.quantity,
        unitPrice: i.unitPrice,
        unitsDeducted: i.quantity * i.unitsDeductedPerItem
      })),
      initialPayment: Number(initialPayment.value) || 0,
      notes: notes.value || undefined
    };

    const createdSale = await api.createCreditSale(payload);

    sounds.playCashRegister();
    confetti({
      particleCount: 80,
      spread: 60,
      origin: { y: 0.6 }
    });

    lastCreatedSaleCustomer.value = selectedCustomer.value;
    successMsg.value = `¡Venta al fiado ${createdSale.saleCode} registrada con éxito para ${selectedCustomer.value.name}!`;

    await loadData();
    cart.value = [];
    initialPayment.value = '';
    notes.value = '';
    notifyCartUpdate();

    emit('sale-completed');
  } catch (err: any) {
    errorMsg.value = err.message || 'Error al procesar la venta al fiado';
  } finally {
    submitting.value = false;
  }
};

const sendWspTicket = async () => {
  if (!lastCreatedSaleCustomer.value) return;
  try {
    const statement = await api.getCustomerStatement(lastCreatedSaleCustomer.value.id);
    emit('open-whatsapp', statement);
  } catch (e) {
    console.error(e);
  }
};
</script>

<template>
  <div class="page-container pos-page-container">
    <!-- Floating Avisito Toast when item is added -->
    <Transition name="toast-slide">
      <div 
        v-if="lastAddedItemNotice" 
        class="pos-added-toast"
        @click="openCart"
      >
        <div class="toast-icon-circle">
          <CheckCircle2 :size="15" />
        </div>
        <div class="toast-content">
          <span class="toast-name">Agregado: <strong>{{ lastAddedItemNotice.name }}</strong></span>
          <span class="toast-action">Ver Carrito ({{ cartTotalQuantity }}) →</span>
        </div>
      </div>
    </Transition>

    <!-- MAIN POS CONTAINER: 2 Columns on Desktop, Single Column + Drawer on Mobile -->
    <div class="pos-main-container">
      <!-- LEFT COLUMN: HEADER + PRODUCTS -->
      <div class="pos-products-column">
        <!-- Top Header with Title and Category Dropdown -->
        <div class="pos-top-header">
          <div class="pos-title-wrap">
            <h2 class="pos-main-title">Punto de Venta al Fiado</h2>
            <p class="pos-sub-title">
              Toca cualquier producto para agregarlo al carrito y luego cárgalo al cliente
            </p>
          </div>

          <!-- Category Select Dropdown moved beside title -->
          <div class="pos-category-select-wrapper">
            <Boxes :size="15" class="pos-category-icon" />
            <select 
              v-model="selectedCategory" 
              class="pos-category-select-input"
              title="Filtrar por categoría"
            >
              <option v-for="cat in categories" :key="cat" :value="cat">
                {{ cat === 'Todos' ? 'Todas las Categorías' : cat }}
              </option>
            </select>
            <ChevronDown :size="13" class="pos-category-chevron" />
          </div>
        </div>

        <!-- PRODUCTS PANEL -->
        <div class="pos-fullscreen-panel glass-card">
          <!-- Search Input -->
          <div class="search-bar pos-products-search">
        <Search class="search-icon" :size="16" />
        <input 
          v-model="productSearch" 
          type="text" 
          class="search-input" 
          placeholder="Buscar producto por nombre..." 
        />
        <button 
          v-if="productSearch" 
          class="btn-clear-search-icon"
          title="Borrar búsqueda"
          @click="productSearch = ''"
        >
          ✕
        </button>
      </div>

      <!-- Full-Width Products Grid -->
      <div class="pos-fullscreen-grid">
        <div 
          v-for="item in displayProducts" 
          :key="item.id"
          class="pos-card-item"
          :class="{ 'is-pack-card': item.isPack }"
          @click="addToCart(item.product, item.pack)"
        >
          <!-- Product Image Preview / Banner -->
          <div class="pos-item-img-wrap">
            <img 
              v-if="item.imageUrl" 
              :src="item.imageUrl" 
              :alt="item.displayName"
              class="pos-item-img"
            />
            <div v-else class="pos-item-placeholder">
              <Boxes v-if="item.isPack" :size="30" />
              <Package v-else :size="30" />
            </div>

            <!-- Category Tag Overlay (Top Left) -->
            <span 
              v-if="item.category" 
              class="pos-category-tag"
            >
              {{ item.category }}
            </span>

            <!-- Stock Tag Overlay (Top Right) -->
            <span 
              v-if="item.isInfiniteStock"
              class="pos-stock-tag infinite"
            >
              <InfinityIcon :size="11" />
              Ilimitado
            </span>
            <span 
              v-else
              class="pos-stock-tag normal"
            >
              {{ item.stockText }}
            </span>
          </div>

          <!-- Product Details -->
          <div class="pos-item-info">
            <div>
              <!-- Combo or Pack Tag if applicable -->
              <div v-if="item.isCombo || item.isPack" class="pos-item-top-row">
                <span v-if="item.isCombo" class="pos-item-combo-badge">
                  <Link2 :size="10" />
                  Pack Fusionado
                </span>
                <span v-else-if="item.isPack" class="pos-item-pack-badge">
                  <Boxes :size="11" />
                  {{ item.pack?.name }} (x{{ item.packUnitsCount }})
                </span>
              </div>

              <!-- Name -->
              <div class="pos-item-title" :title="item.displayName">
                {{ item.product.name }}
              </div>

              <!-- Combo components preview -->
              <div 
                v-if="item.isCombo && item.comboItems && item.comboItems.length > 0"
                class="pos-combo-components-box"
              >
                <span class="combo-inc-label">Incluye: </span>
                <span>{{ item.comboItems.map((c: any) => (c.quantity > 1 ? `${c.quantity}x ` : '') + c.productName).join(' + ') }}</span>
              </div>
            </div>

            <!-- Price & Add Button -->
            <div class="pos-item-price-row">
              <div>
                <div class="pos-item-price">
                  S/. {{ item.price.toFixed(2) }}
                </div>
                <div 
                  v-if="item.isCombo && item.comboItems && item.comboItems.length > 0"
                  class="pos-item-crossed-price"
                >
                  S/. {{ item.comboItems.reduce((acc: number, c: any) => acc + (c.quantity * c.unitPrice), 0).toFixed(2) }} sep.
                </div>
                <div 
                  v-else-if="item.isPack && item.packUnitsCount && item.product.salePrice"
                  class="pos-item-crossed-price"
                >
                  S/. {{ (item.packUnitsCount * item.product.salePrice).toFixed(2) }} sep.
                </div>
              </div>

              <div class="pos-item-add-btn">
                <Plus :size="15" />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>

    <!-- SLIDE-OVER DRAWER FOR DETALLE DEL FIADO & CLIENTE -->
    <Transition name="drawer-fade">
      <div 
        v-if="isCartDrawerOpen" 
        class="cart-drawer-backdrop" 
        @click="closeCart"
      ></div>
    </Transition>

    <aside 
      class="cart-drawer" 
      :class="{ 'is-open': isCartDrawerOpen }"
    >
      <!-- Drawer Header -->
      <div class="cart-drawer-header">
        <div class="cart-drawer-title-wrap">
          <div class="cart-drawer-icon-wrap">
            <ShoppingCart :size="19" />
          </div>
          <div>
            <h3 class="cart-drawer-title">Detalle del Fiado</h3>
            
          </div>
        </div>
        <button class="cart-drawer-close-btn" title="Cerrar y seguir agregando" @click="closeCart">
          <X :size="18" />
        </button>
      </div>

      <!-- Drawer Body -->
      <div class="cart-drawer-body">
        <!-- 1. Customer Selection Section -->
        <div class="drawer-box customer-select-box">
          <div class="drawer-box-header">
            <User :size="15" />
            <span>1. Cliente para el Fiado</span>
          </div>

          <!-- Customer Selected Card -->
          <div v-if="selectedCustomer" class="drawer-customer-selected">
            <div class="drawer-cust-avatar">
              {{ selectedCustomer.name.charAt(0).toUpperCase() }}
            </div>
            <div class="drawer-cust-details">
              <div class="drawer-cust-top">
                <strong>{{ selectedCustomer.name }}</strong>
                <span class="badge" :class="selectedCustomer.currentDebt > 0 ? 'badge-debt' : 'badge-paid'">
                  {{ selectedCustomer.currentDebt > 0 ? `Debe S/. ${selectedCustomer.currentDebt.toFixed(2)}` : 'Al día' }}
                </span>
              </div>
              <div class="drawer-cust-sub">
                <span>📱 {{ selectedCustomer.phone }}</span>
              </div>
            </div>
            <button class="btn btn-secondary btn-xs btn-change-cust" @click="clearSelectedCustomer">
              Cambiar
            </button>
          </div>

          <!-- Customer Search Field if none selected -->
          <div v-else class="drawer-cust-search-wrap">
            <div class="drawer-cust-input-row">
              <div class="search-bar" style="flex: 1;">
                <Search class="search-icon" :size="15" />
                <input 
                  v-model="customerSearch"
                  type="text" 
                  class="search-input" 
                  placeholder="Buscar cliente (Nombre, DNI o Celular)..."
                  @focus="showCustomerDropdown = true"
                />
              </div>
              <button 
                class="btn btn-secondary btn-sm" 
                title="Registrar nuevo cliente"
                @click="emit('new-customer-request')"
              >
                <UserPlus :size="15" />
                <span>Nuevo</span>
              </button>
            </div>

            <!-- Dropdown autocomplete -->
            <div 
              v-if="showCustomerDropdown && filteredCustomers.length > 0 && !selectedCustomer"
              class="drawer-cust-dropdown"
            >
              <div 
                v-for="c in filteredCustomers" 
                :key="c.id"
                class="drawer-cust-drop-item"
                @mousedown="selectCustomer(c)"
              >
                <div>
                  <div class="drop-name">{{ c.name }}</div>
                  <div class="drop-meta">Cel: {{ c.phone }} | DNI: {{ c.documentNumber || 'Sin DNI' }}</div>
                </div>
                <div class="drop-debt" :style="{ color: c.currentDebt > 0 ? '#dc2626' : '#059669' }">
                  S/. {{ c.currentDebt.toFixed(2) }}
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 2. Products List in Cart -->
        <div class="drawer-box cart-products-box">
          <div class="drawer-box-header justify-between">
            <div style="display: flex; align-items: center; gap: 6px;">
              <Package :size="15" />
              <span>2. Productos Seleccionados</span>
            </div>
            <button 
              v-if="cart.length > 0"
              class="btn-text-clear"
              @click="clearCart"
            >
              Vaciar carrito
            </button>
          </div>

          <!-- Empty cart state -->
          <div v-if="cart.length === 0" class="drawer-empty-state">
            <Package :size="34" class="drawer-empty-icon" />
            <div class="drawer-empty-title">El carrito está vacío</div>
            <div class="drawer-empty-sub">Toca los productos en la pantalla para agregarlos aquí</div>
          </div>

          <!-- Cart list -->
          <div v-else class="drawer-items-list">
            <div 
              v-for="item in cart" 
              :key="item.id"
              class="drawer-item-card"
            >
              <div class="drawer-item-thumbnail">
                <img v-if="item.displayImage" :src="item.displayImage" :alt="item.displayName" />
                <Package v-else :size="16" />
              </div>

              <div class="drawer-item-meta">
                <div class="drawer-item-prod-name">{{ item.displayName }}</div>
                <div class="drawer-item-unit-info">
                  <span>S/. {{ item.unitPrice.toFixed(2) }} c/u</span>
                  <span v-if="item.pack" class="tag-chip pack">Pack x{{ item.pack.unitsCount }}</span>
                  <span v-if="item.product.isCombo" class="tag-chip combo">Pack Fusionado</span>
                </div>
              </div>

              <!-- Quantity Controls -->
              <div class="drawer-qty-pill">
                <button class="btn-qty-action" @click="updateQty(item, -1)">
                  <Minus :size="11" />
                </button>
                <span class="qty-counter">{{ item.quantity }}</span>
                <button class="btn-qty-action" @click="updateQty(item, 1)">
                  <Plus :size="11" />
                </button>
              </div>

              <!-- Item Subtotal -->
              <div class="drawer-item-subtotal">
                S/. {{ (item.quantity * item.unitPrice).toFixed(2) }}
              </div>

              <!-- Delete -->
              <button class="btn-item-delete" title="Quitar producto" @click="removeFromCart(item)">
                <Trash2 :size="13" />
              </button>
            </div>
          </div>
        </div>

        <!-- 3. Initial Payment & Notes -->
        <div v-if="cart.length > 0" class="drawer-box payment-box">
          <div class="payment-adelanto-row">
            <label class="payment-label">¿Dejó algún adelanto? (S/.):</label>
            <input 
              v-model="initialPayment" 
              type="number" 
              step="0.5" 
              class="form-input payment-input" 
              placeholder="0.00" 
            />
          </div>

          <div style="margin-top: 6px;">
            <input 
              v-model="notes" 
              type="text" 
              class="form-input form-input-sm" 
              placeholder="Nota o motivo (ej: paga el sábado)" 
            />
          </div>
        </div>

        <!-- Error / Success messages inside drawer -->
        <div v-if="errorMsg" class="drawer-alert error">
          <AlertCircle :size="15" />
          <span>{{ errorMsg }}</span>
        </div>

        <div v-if="successMsg" class="drawer-alert success">
          <CheckCircle2 :size="16" />
          <div style="flex: 1;">
            <div>{{ successMsg }}</div>
            <button class="btn btn-wsp btn-sm btn-wsp-drawer" @click="sendWspTicket">
              <MessageSquare :size="14" />
              <span>Enviar Detalle a su WhatsApp</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Drawer Footer -->
      <div class="cart-drawer-footer">
        <div class="drawer-totals-card">
          <div class="drawer-subtotal-row">
            <span>Subtotal de productos:</span>
            <span>S/. {{ totalAmount.toFixed(2) }}</span>
          </div>
          <div v-if="Number(initialPayment) > 0" class="drawer-subtotal-row adelanto">
            <span>Adelanto pagado:</span>
            <span>- S/. {{ Number(initialPayment).toFixed(2) }}</span>
          </div>
          <div class="drawer-main-total-row">
            <span class="main-total-label">Total a Cargar al Fiado:</span>
            <span class="main-total-amount">S/. {{ remainingDebt.toFixed(2) }}</span>
          </div>
          <div v-if="selectedCustomer" class="drawer-projected-note">
            Deuda total del cliente pasará a: <strong>S/. {{ projectedDebt.toFixed(2) }}</strong>
          </div>
        </div>

        <!-- Action Button -->
        <button 
          class="btn btn-primary btn-submit-fiado"
          :disabled="submitting || cart.length === 0 || !selectedCustomer"
          @click="submitCreditSale"
        >
          <span v-if="submitting">Registrando venta...</span>
          <span v-else-if="!selectedCustomer">1. Selecciona un cliente arriba</span>
          <span v-else-if="cart.length === 0">Agrega productos al carrito</span>
          <span v-else>Confirmar Venta al Fiado (S/. {{ remainingDebt.toFixed(2) }})</span>
        </button>

        <button class="btn btn-secondary btn-sm btn-keep-shopping" @click="closeCart">
          ← Seguir Agregando Productos
        </button>
      </div>
    </aside>
  </div>
</div>
</template>

<style scoped>
.pos-products-column {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-width: 0;
}

.pos-top-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 14px;
  flex-wrap: wrap;
  margin-bottom: 0;
}

.pos-title-wrap {
  flex: 1;
  min-width: 240px;
}

.pos-category-select-wrapper {
  position: relative;
  display: inline-flex;
  align-items: center;
  background: #ffffff;
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: 8px 14px;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.05);
  transition: all 0.2s ease;
  min-height: 42px;
}

.pos-category-select-wrapper:hover {
  border-color: var(--slate-400);
}

.pos-category-select-wrapper:focus-within {
  border-color: #0f172a;
  box-shadow: 0 0 0 2px rgba(15, 23, 42, 0.08);
}

.pos-category-icon {
  color: var(--slate-500);
  pointer-events: none;
  margin-right: 8px;
  flex-shrink: 0;
}

.pos-category-select-input {
  border: none;
  background: transparent;
  font-family: var(--font-body);
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--slate-800);
  outline: none;
  cursor: pointer;
  padding-right: 22px;
  appearance: none;
  -webkit-appearance: none;
  -moz-appearance: none;
  min-width: 160px;
}

.pos-category-chevron {
  position: absolute;
  right: 12px;
  color: var(--slate-400);
  pointer-events: none;
}

@media (min-width: 1025px) {
  .cart-drawer {
    top: 16px !important;
    height: calc(100vh - 36px) !important;
  }

  .pos-fullscreen-panel {
    min-height: calc(100vh - 110px);
  }
}

/* Compact & Proportional Cart Drawer styles */
.cart-drawer {
  display: flex;
  flex-direction: column;
}

.cart-drawer-header {
  padding: 10px 14px;
  flex-shrink: 0;
}

.cart-drawer-icon-wrap {
  width: 32px;
  height: 32px;
  border-radius: 8px;
}

.cart-drawer-title {
  font-size: 1.05rem;
}

.cart-drawer-sub {
  font-size: 0.72rem;
}

.cart-drawer-body {
  flex: 1 1 0%;
  min-height: 0;
  overflow-y: auto;
  padding: 8px 12px;
  gap: 8px;
}

.drawer-box {
  padding: 8px 10px;
}

.drawer-box-header {
  margin-bottom: 5px;
  font-size: 0.72rem;
}

.drawer-customer-selected {
  padding: 6px 10px;
  gap: 8px;
}

.drawer-cust-avatar {
  width: 30px;
  height: 30px;
  font-size: 0.85rem;
}

.drawer-cust-top strong {
  font-size: 0.86rem;
}

.drawer-cust-sub {
  font-size: 0.72rem;
}

.customer-select-box {
  flex-shrink: 0;
}

.cart-products-box {
  flex: 1 1 0%;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.drawer-empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
}

.drawer-items-list {
  flex: 1 1 0%;
  min-height: 0;
  overflow-y: auto;
  gap: 6px;
  padding-right: 2px;
}

.drawer-item-row {
  padding: 5px 8px;
  gap: 6px;
}

.drawer-item-thumbnail {
  width: 28px;
  height: 28px;
}

.drawer-item-prod-name {
  font-size: 0.82rem;
}

.payment-box {
  padding: 7px 10px;
  flex-shrink: 0;
}

.payment-label {
  font-size: 0.75rem;
}

.payment-input {
  height: 28px;
  padding: 2px 8px;
  font-size: 0.82rem;
  max-width: 90px;
}

.form-input-sm {
  height: 28px;
  padding: 2px 8px;
  font-size: 0.76rem;
}

.cart-drawer-footer {
  padding: 10px 14px;
  gap: 6px;
  flex-shrink: 0;
}

.drawer-totals-card {
  gap: 2px;
}

.drawer-subtotal-row {
  font-size: 0.76rem;
}

.drawer-main-total-row {
  padding-top: 4px;
  margin-top: 2px;
}

.main-total-label {
  font-size: 0.82rem;
}

.main-total-amount {
  font-size: 1.2rem;
}

.drawer-projected-note {
  font-size: 0.70rem;
  margin-top: 1px;
}

.btn-submit-fiado {
  padding: 9px 12px;
  font-size: 0.86rem;
}

@media (max-width: 1024px) {
  .pos-top-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 10px;
  }

  .pos-category-select-wrapper {
    width: 100%;
  }

  .pos-category-select-input {
    width: 100%;
  }
}
</style>
