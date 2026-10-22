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
  ArrowDown
} from 'lucide-vue-next';
import confetti from 'canvas-confetti';
import type { Customer, Product, ProductPack, CustomerStatement } from '../services/api';
import { api } from '../services/api';
import { sounds } from '../utils/sound';

const emit = defineEmits<{
  (e: 'new-customer-request'): void;
  (e: 'sale-completed'): void;
  (e: 'open-whatsapp', statement: CustomerStatement): void;
}>();

const customers = ref<Customer[]>([]);
const products = ref<Product[]>([]);
const categories = ref<string[]>([]);

const selectedCustomer = ref<Customer | null>(null);
const customerSearch = ref('');
const showCustomerDropdown = ref(false);

const productSearch = ref('');
const selectedCategory = ref('Todos');

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

const ticketRef = ref<HTMLElement | null>(null);

const scrollToCart = () => {
  ticketRef.value?.scrollIntoView({ behavior: 'smooth' });
};

const cart = ref<CartItem[]>([]);
const initialPayment = ref<number | ''>('');
const notes = ref('');
const submitting = ref(false);
const errorMsg = ref('');
const successMsg = ref('');
const lastCreatedSaleCustomer = ref<Customer | null>(null);

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
  }
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

const filteredProducts = computed(() => {
  return products.value.filter(p => {
    const matchesCat = selectedCategory.value === 'Todos' || p.category === selectedCategory.value;
    const matchesSearch = !productSearch.value.trim() || 
      p.name.toLowerCase().includes(productSearch.value.toLowerCase());
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
};

const updateQty = (item: CartItem, delta: number) => {
  sounds.playClick();
  item.quantity += delta;
  if (item.quantity <= 0) {
    removeFromCart(item);
  }
};

const removeFromCart = (item: CartItem) => {
  sounds.playClick();
  cart.value = cart.value.filter(i => i.id !== item.id);
};

const clearCart = () => {
  sounds.playClick();
  cart.value = [];
  initialPayment.value = '';
  notes.value = '';
  errorMsg.value = '';
  successMsg.value = '';
};

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
    successMsg.value = `Venta al fiado ${createdSale.saleCode} registrada con éxito para ${selectedCustomer.value.name}.`;

    await loadData();
    cart.value = [];
    initialPayment.value = '';
    notes.value = '';

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
  <div class="page-container" style="padding-top: 20px;">
    <!-- Top Header -->
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
      <div>
        <h2 style="font-family: var(--font-heading); font-size: 1.6rem; font-weight: 800; color: var(--slate-900);">
          Punto de Venta al Fiado
        </h2>
        <p style="font-size: 0.85rem; color: var(--slate-500);">
          Selecciona al cliente, agrega los productos y carga la deuda a su cuenta
        </p>
      </div>

      <!-- Customer Selector Box -->
      <div style="position: relative; width: 440px;">
        <div style="display: flex; gap: 8px;">
          <div class="search-bar" style="flex: 1;">
            <Search class="search-icon" :size="18" />
            <input 
              v-model="customerSearch"
              type="text" 
              class="search-input" 
              placeholder="Buscar cliente (Nombre, DNI o Celular)..."
              @focus="showCustomerDropdown = true"
            />
          </div>
          <button 
            class="btn btn-secondary" 
            title="Registrar nuevo cliente"
            style="padding: 10px 14px;"
            @click="emit('new-customer-request')"
          >
            <UserPlus :size="17" />
            <span>Nuevo</span>
          </button>
        </div>

        <!-- Dropdown Results -->
        <div 
          v-if="showCustomerDropdown && filteredCustomers.length > 0 && !selectedCustomer"
          style="position: absolute; top: calc(100% + 6px); left: 0; right: 0; background: #ffffff; border: 1px solid var(--border-subtle); border-radius: var(--radius-md); box-shadow: var(--shadow-lg); z-index: 50; max-height: 280px; overflow-y: auto;"
        >
          <div 
            v-for="c in filteredCustomers" 
            :key="c.id"
            style="padding: 10px 14px; border-bottom: 1px solid var(--border-subtle); cursor: pointer; display: flex; justify-content: space-between; align-items: center; transition: background 0.15s ease;"
            @mousedown="selectCustomer(c)"
          >
            <div>
              <div style="font-weight: 600; font-size: 0.92rem; color: var(--slate-900);">{{ c.name }}</div>
              <div style="font-size: 0.78rem; color: var(--slate-500);">Cel: {{ c.phone }} | DNI: {{ c.documentNumber || 'Sin DNI' }}</div>
            </div>
            <div style="text-align: right;">
              <div style="font-size: 0.7rem; color: var(--slate-400);">Deuda:</div>
              <div style="font-family: var(--font-heading); font-weight: 700; font-size: 0.95rem;" :style="{ color: c.currentDebt > 0 ? '#dc2626' : '#059669' }">
                S/. {{ c.currentDebt.toFixed(2) }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Banner for Selected Customer -->
    <div 
      v-if="selectedCustomer" 
      style="margin-bottom: 20px; padding: 14px 18px; border-radius: var(--radius-md); background: #f0fdf4; border: 1px solid #bbf7d0; display: flex; justify-content: space-between; align-items: center;"
    >
      <div style="display: flex; align-items: center; gap: 14px;">
        <div style="width: 40px; height: 40px; border-radius: 10px; background: #dcfce7; color: #15803d; display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 1.1rem; font-family: var(--font-heading);">
          {{ selectedCustomer.name.charAt(0) }}
        </div>
        <div>
          <div style="display: flex; align-items: center; gap: 10px;">
            <span style="font-size: 1.05rem; font-weight: 700; color: var(--slate-900);">{{ selectedCustomer.name }}</span>
            <span class="badge" :class="selectedCustomer.currentDebt > 0 ? 'badge-debt' : 'badge-paid'">
              {{ selectedCustomer.currentDebt > 0 ? `Deuda: S/. ${selectedCustomer.currentDebt.toFixed(2)}` : 'Al día' }}
            </span>
            <span v-if="isOverLimit" class="badge" style="background: #fee2e2; color: #b91c1c; border: 1px solid #fecdd3;">
              <AlertCircle :size="13" /> Límite Excedido (S/. {{ selectedCustomer.creditLimit }})
            </span>
          </div>
          <div style="font-size: 0.8rem; color: var(--slate-600); margin-top: 2px;">
            WhatsApp: {{ selectedCustomer.phone }} | Dirección: {{ selectedCustomer.address || 'No registrada' }}
          </div>
        </div>
      </div>

      <button 
        class="btn btn-secondary btn-sm" 
        style="padding: 6px 12px; font-size: 0.8rem;"
        @click="clearSelectedCustomer"
      >
        Cambiar Cliente
      </button>
    </div>

    <!-- Main Layout: Product Grid + Ticket -->
    <div class="pos-main-layout">
      <!-- LEFT: Products Grid -->
      <div class="glass-card" style="padding: 20px;">
        <!-- Filters Row -->
        <div style="display: flex; flex-direction: column; gap: 12px; margin-bottom: 18px;">
          <div style="display: flex; gap: 6px; overflow-x: auto; padding-bottom: 2px;">
            <button 
              v-for="cat in categories" 
              :key="cat"
              class="btn btn-sm"
              :class="selectedCategory === cat ? 'btn-primary' : 'btn-secondary'"
              style="border-radius: 999px; padding: 5px 12px; font-size: 0.8rem;"
              @click="selectedCategory = cat"
            >
              {{ cat }}
            </button>
          </div>

          <div class="search-bar">
            <Search class="search-icon" :size="16" />
            <input 
              v-model="productSearch" 
              type="text" 
              class="search-input" 
              placeholder="Buscar producto por nombre..." 
            />
          </div>
        </div>

        <!-- Grid -->
        <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 12px; max-height: 520px; overflow-y: auto; padding-right: 4px;">
          <div 
            v-for="prod in filteredProducts" 
            :key="prod.id"
            class="pos-card-item"
            style="background: #ffffff; border: 1px solid var(--border-subtle); border-radius: var(--radius-md); overflow: hidden; display: flex; flex-direction: column; justify-content: space-between; cursor: pointer; transition: all 0.15s ease;"
            @click="addToCart(prod)"
          >
            <!-- Product Image Preview / Banner -->
            <div style="width: 100%; height: 100px; background: #f1f5f9; position: relative; overflow: hidden; display: flex; align-items: center; justify-content: center;">
              <img 
                v-if="prod.imageUrl" 
                :src="prod.imageUrl" 
                :alt="prod.name"
                style="width: 100%; height: 100%; object-fit: cover; transition: transform 0.25s ease;" 
              />
              <div v-else style="display: flex; flex-direction: column; align-items: center; color: #94a3b8;">
                <Package :size="28" />
              </div>

              <!-- Stock Tag Overlay -->
              <span 
                v-if="prod.isInfiniteStock"
                style="position: absolute; top: 6px; right: 6px; font-size: 0.68rem; font-weight: 700; color: #4338ca; background: rgba(224, 231, 255, 0.92); backdrop-filter: blur(4px); padding: 2px 6px; border-radius: 4px; display: flex; align-items: center; gap: 3px; box-shadow: 0 1px 3px rgba(0,0,0,0.08);"
              >
                <InfinityIcon :size="11" />
                Ilimitado
              </span>
              <span 
                v-else
                style="position: absolute; top: 6px; right: 6px; font-size: 0.68rem; font-weight: 700; color: #065f46; background: rgba(209, 250, 229, 0.92); backdrop-filter: blur(4px); padding: 2px 6px; border-radius: 4px; box-shadow: 0 1px 3px rgba(0,0,0,0.08);"
              >
                {{ prod.stock }} {{ prod.unit }}
              </span>
            </div>

            <div style="padding: 12px; display: flex; flex-direction: column; flex: 1; justify-content: space-between;">
              <div>
                <!-- Category Tag -->
                <div style="font-size: 0.70rem; color: var(--slate-500); font-weight: 600; text-transform: uppercase; margin-bottom: 4px;">
                  {{ prod.category }}
                </div>

                <!-- Name -->
                <div style="font-weight: 600; font-size: 0.88rem; color: var(--slate-900); margin-bottom: 10px; line-height: 1.3; min-height: 36px; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;">
                  {{ prod.name }}
                </div>
              </div>

              <!-- Price & Add -->
              <div style="display: flex; justify-content: space-between; align-items: center; border-top: 1px solid var(--border-subtle); padding-top: 8px;">
                <div style="font-family: var(--font-heading); font-size: 1.15rem; font-weight: 800; color: var(--emerald-600);">
                  S/. {{ prod.salePrice.toFixed(2) }}
                </div>
                <div style="width: 26px; height: 26px; border-radius: 6px; background: #d1fae5; color: #059669; display: flex; align-items: center; justify-content: center;">
                  <Plus :size="15" />
                </div>
              </div>
            </div>

            <!-- Pack options chips if product has packs -->
            <div 
              v-if="prod.packs && prod.packs.length > 0" 
              style="padding: 8px 12px 10px 12px; display: flex; flex-direction: column; gap: 5px; border-top: 1px dashed var(--border-subtle); background: #f8fafc;"
            >
              <div style="font-size: 0.67rem; font-weight: 700; color: #4338ca; text-transform: uppercase; display: flex; align-items: center; gap: 4px;">
                <Boxes :size="11" />
                Packs Disponibles:
              </div>
              <button 
                v-for="pk in prod.packs" 
                :key="pk.id"
                type="button"
                style="display: flex; align-items: center; justify-content: space-between; padding: 6px 8px; border-radius: 6px; border: 1px solid #c7d2fe; background: #eef2ff; color: #3730a3; cursor: pointer; text-align: left; transition: all 0.15s ease;"
                title="Agregar este pack al carrito"
                @click.stop="addToCart(prod, pk)"
              >
                <div style="display: flex; align-items: center; gap: 6px; min-width: 0; overflow: hidden;">
                  <img v-if="pk.imageUrl" :src="pk.imageUrl" style="width: 22px; height: 22px; border-radius: 4px; object-fit: cover; flex-shrink: 0;" />
                  <Boxes v-else :size="13" style="flex-shrink: 0;" />
                  <span style="font-size: 0.74rem; font-weight: 700; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                    {{ pk.name }}
                  </span>
                </div>
                <div style="font-size: 0.78rem; font-weight: 800; color: #4338ca; white-space: nowrap; margin-left: 4px;">
                  + S/. {{ pk.price.toFixed(2) }}
                </div>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- RIGHT: Interactive Ticket / Cart -->
      <div ref="ticketRef" class="glass-card" style="padding: 20px; display: flex; flex-direction: column; min-height: 560px;">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; border-bottom: 1px solid var(--border-subtle); padding-bottom: 12px;">
          <div style="display: flex; align-items: center; gap: 8px;">
            <ShoppingCart :size="19" style="color: var(--emerald-600);" />
            <h3 style="font-family: var(--font-heading); font-size: 1.15rem; font-weight: 700; color: var(--slate-900);">
              Detalle del Fiado
            </h3>
          </div>
          <button 
            v-if="cart.length > 0"
            class="btn btn-secondary btn-sm" 
            style="color: #b91c1c; border-color: #fecdd3; padding: 3px 8px; font-size: 0.75rem;"
            @click="clearCart"
          >
            Limpiar
          </button>
        </div>

        <!-- Success notification -->
        <div 
          v-if="successMsg" 
          style="padding: 12px; border-radius: var(--radius-md); background: #ecfdf5; border: 1px solid #a7f3d0; color: #065f46; font-size: 0.85rem; margin-bottom: 14px;"
        >
          <div>{{ successMsg }}</div>
          <button 
            class="btn btn-wsp btn-sm" 
            style="margin-top: 8px; width: 100%; justify-content: center;"
            @click="sendWspTicket"
          >
            <MessageSquare :size="15" />
            <span>Enviar Detalle a su WhatsApp</span>
          </button>
        </div>

        <!-- Error notification -->
        <div 
          v-if="errorMsg" 
          style="padding: 10px 14px; border-radius: var(--radius-md); background: #fee2e2; border: 1px solid #fecdd3; color: #b91c1c; font-size: 0.85rem; margin-bottom: 14px;"
        >
          {{ errorMsg }}
        </div>

        <!-- Cart Items List -->
        <div style="flex: 1; overflow-y: auto; max-height: 260px; display: flex; flex-direction: column; gap: 8px; margin-bottom: 14px;">
          <div v-if="cart.length === 0" style="text-align: center; padding: 40px 10px; color: var(--slate-400);">
            <Package :size="36" style="margin: 0 auto 8px; opacity: 0.3;" />
            <div style="font-size: 0.85rem; color: var(--slate-600);">El carrito está vacío</div>
            <div style="font-size: 0.75rem; margin-top: 2px;">Haz clic en cualquier producto para agregarlo</div>
          </div>

          <div 
            v-for="item in cart" 
            :key="item.id"
            style="background: var(--slate-50); border: 1px solid var(--border-subtle); border-radius: var(--radius-sm); padding: 8px 10px; display: flex; justify-content: space-between; align-items: center; gap: 8px;"
          >
            <div style="width: 36px; height: 36px; border-radius: 6px; overflow: hidden; background: #e2e8f0; display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
              <img v-if="item.displayImage" :src="item.displayImage" :alt="item.displayName" style="width: 100%; height: 100%; object-fit: cover;" />
              <Package v-else :size="16" style="color: #64748b;" />
            </div>

            <div style="flex: 1; overflow: hidden; padding-right: 8px;">
              <div style="font-weight: 600; font-size: 0.88rem; color: var(--slate-900); white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
                {{ item.displayName }}
              </div>
              <div style="font-size: 0.74rem; color: var(--slate-500); display: flex; align-items: center; gap: 6px;">
                <span>S/. {{ item.unitPrice.toFixed(2) }} c/u</span>
                <span v-if="item.pack" style="font-size: 0.68rem; color: #4338ca; background: #e0e7ff; padding: 1px 5px; border-radius: 3px; font-weight: 600;">
                  Pack x{{ item.pack.unitsCount }}
                </span>
              </div>
            </div>

            <!-- Qty controls -->
            <div style="display: flex; align-items: center; gap: 6px;">
              <button 
                class="btn btn-secondary btn-sm" 
                style="padding: 2px 5px; width: 22px; height: 22px;"
                @click="updateQty(item, -1)"
              >
                <Minus :size="11" />
              </button>
              <span style="font-weight: 700; font-size: 0.9rem; min-width: 18px; text-align: center;">
                {{ item.quantity }}
              </span>
              <button 
                class="btn btn-secondary btn-sm" 
                style="padding: 2px 5px; width: 22px; height: 22px;"
                @click="updateQty(item, 1)"
              >
                <Plus :size="11" />
              </button>
            </div>

            <!-- Subtotal -->
            <div style="width: 65px; text-align: right; font-weight: 700; font-family: var(--font-heading); color: var(--slate-900); font-size: 0.95rem;">
              S/. {{ (item.quantity * item.unitPrice).toFixed(2) }}
            </div>

            <!-- Remove -->
            <button 
              class="btn btn-secondary btn-sm" 
              style="padding: 4px; border: none; color: var(--slate-400); margin-left: 4px;"
              @click="removeFromCart(item)"
            >
              <Trash2 :size="13" />
            </button>
          </div>
        </div>

        <!-- Initial Payment (Adelanto) & Notes -->
        <div style="background: var(--slate-50); border-radius: var(--radius-md); padding: 12px; margin-bottom: 14px; border: 1px solid var(--border-subtle);">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
            <label style="font-size: 0.8rem; font-weight: 600; color: var(--slate-700);">
              ¿Dejó algún adelanto / pago inicial? (S/.):
            </label>
            <input 
              v-model="initialPayment" 
              type="number" 
              step="0.5" 
              class="form-input" 
              placeholder="0.00" 
              style="width: 90px; padding: 5px 8px; font-size: 0.85rem; text-align: right;" 
            />
          </div>

          <div>
            <input 
              v-model="notes" 
              type="text" 
              class="form-input" 
              placeholder="Nota o motivo (ej: paga el fin de semana)" 
              style="padding: 6px 10px; font-size: 0.8rem;" 
            />
          </div>
        </div>

        <!-- Total Breakdown -->
        <div style="border-top: 1px dashed var(--border-subtle); padding-top: 10px; margin-bottom: 14px;">
          <div style="display: flex; justify-content: space-between; font-size: 0.82rem; color: var(--slate-600); margin-bottom: 3px;">
            <span>Subtotal de productos:</span>
            <span>S/. {{ totalAmount.toFixed(2) }}</span>
          </div>
          <div v-if="Number(initialPayment) > 0" style="display: flex; justify-content: space-between; font-size: 0.82rem; color: var(--emerald-600); margin-bottom: 3px;">
            <span>Adelanto entregado:</span>
            <span>- S/. {{ Number(initialPayment).toFixed(2) }}</span>
          </div>
          <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 6px;">
            <span style="font-weight: 700; font-size: 1rem; color: var(--slate-900);">Total a Cargar al Fiado:</span>
            <span style="font-family: var(--font-heading); font-size: 1.5rem; font-weight: 800; color: #dc2626;">
              S/. {{ remainingDebt.toFixed(2) }}
            </span>
          </div>

          <div v-if="selectedCustomer" style="font-size: 0.74rem; color: var(--slate-500); text-align: right; margin-top: 2px;">
            Deuda total del cliente pasará a: <b style="color: #dc2626;">S/. {{ projectedDebt.toFixed(2) }}</b>
          </div>
        </div>

        <!-- Action Button -->
        <button 
          class="btn btn-primary btn-lg" 
          style="width: 100%; justify-content: center;"
          :disabled="submitting || cart.length === 0 || !selectedCustomer"
          @click="submitCreditSale"
        >
          <span>{{ submitting ? 'Registrando...' : 'Registrar Venta al Fiado' }}</span>
        </button>
      </div>
    </div>

    <!-- Tablet Floating Checkout Bar -->
    <div v-if="cart.length > 0" class="tablet-bottom-cart-bar">
      <div style="display: flex; align-items: center; gap: 10px;">
        <div style="width: 38px; height: 38px; border-radius: 10px; background: #d1fae5; color: #065f46; display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
          <ShoppingCart :size="19" />
        </div>
        <div>
          <div style="font-size: 0.72rem; color: var(--slate-500); font-weight: 600;">
            {{ cart.reduce((acc, i) => acc + i.quantity, 0) }} productos en carrito
          </div>
          <div style="font-size: 1.15rem; font-weight: 800; color: #059669; font-family: var(--font-heading); line-height: 1.1;">
            S/. {{ totalAmount.toFixed(2) }}
          </div>
        </div>
      </div>
      <button class="btn btn-primary" style="padding: 10px 18px; font-weight: 700;" @click="scrollToCart">
        <span>Ver Ticket & Cobrar</span>
        <ArrowDown :size="16" />
      </button>
    </div>
  </div>
</template>
