<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { Menu, Store, PlusCircle, ShoppingCart } from 'lucide-vue-next';
import LoginView from './components/LoginView.vue';
import Sidebar from './components/Sidebar.vue';
import DashboardView from './components/DashboardView.vue';
import PosView from './components/PosView.vue';
import DebtsView from './components/DebtsView.vue';
import CustomersView from './components/CustomersView.vue';
import InventoryView from './components/InventoryView.vue';
import UsersView from './components/UsersView.vue';

import WhatsAppModal from './components/WhatsAppModal.vue';
import PaymentModal from './components/PaymentModal.vue';
import CustomerStatementModal from './components/CustomerStatementModal.vue';
import NewCustomerModal from './components/NewCustomerModal.vue';
import NewProductModal from './components/NewProductModal.vue';

import type { Customer, Product, CustomerStatement, DashboardSummary, LoginResponse } from './services/api';
import { api, authState } from './services/api';

const currentUser = ref<LoginResponse | null>(null);
const currentView = ref<'dashboard' | 'pos' | 'debts' | 'customers' | 'inventory' | 'users'>('dashboard');
const isSidebarPinned = ref(localStorage.getItem('bodega_sidebar_pinned') === 'true');
const isMobileSidebarOpen = ref(false);

const getViewTitle = (view: string) => {
  switch (view) {
    case 'dashboard': return 'Dashboard';
    case 'pos': return 'POS de Fiados';
    case 'debts': return 'Deudas & Cobranzas';
    case 'customers': return 'Lista de Clientes';
    case 'inventory': return 'Inventario';
    case 'users': return 'Usuarios & Personal';
    default: return '';
  }
};

const handleTogglePin = (pinned: boolean) => {
  isSidebarPinned.value = pinned;
};

// Global Stats for badges
const summary = ref<DashboardSummary | null>(null);
const categories = ref<string[]>([]);

// Modals State
const showWhatsAppModal = ref(false);
const activeStatement = ref<CustomerStatement | null>(null);

const showPaymentModal = ref(false);
const activePaymentCustomer = ref<Customer | null>(null);

const showStatementModal = ref(false);
const activeFullStatement = ref<CustomerStatement | null>(null);

const showNewCustomerModal = ref(false);
const customerToEdit = ref<Customer | null>(null);

const showNewProductModal = ref(false);
const productToEdit = ref<Product | null>(null);

// Component Refs
const dashboardRef = ref<any>(null);
const posRef = ref<any>(null);
const debtsRef = ref<any>(null);
const customersRef = ref<any>(null);
const inventoryRef = ref<any>(null);

const checkAuth = () => {
  currentUser.value = authState.getUser();
};

const onLoginSuccess = () => {
  checkAuth();
  currentView.value = 'dashboard';
  loadGlobalStats();
};

const onLogout = () => {
  api.logout();
  currentUser.value = null;
};

const loadGlobalStats = async () => {
  if (!currentUser.value) return;
  try {
    const [dash, cats] = await Promise.all([
      api.getDashboardSummary(),
      api.getProductCategories()
    ]);
    summary.value = dash;
    categories.value = cats;
  } catch (err) {
    console.error('Error loading global stats', err);
  }
};

onMounted(() => {
  checkAuth();
  if (currentUser.value) {
    loadGlobalStats();
  }
});

const handleNavigate = (view: string) => {
  // Protect users section from workers
  if (view === 'users' && currentUser.value?.role !== 'Admin') {
    currentView.value = 'dashboard';
    isMobileSidebarOpen.value = false;
    return;
  }
  currentView.value = view as any;
  isMobileSidebarOpen.value = false;
};

const refreshAll = async () => {
  await loadGlobalStats();
  if (dashboardRef.value?.refresh) dashboardRef.value.refresh();
  if (posRef.value?.refresh) posRef.value.refresh();
  if (debtsRef.value?.refresh) debtsRef.value.refresh();
  if (customersRef.value?.refresh) customersRef.value.refresh();
  if (inventoryRef.value?.refresh) inventoryRef.value.refresh();
};

// Handlers
const handleWhatsAppOpen = (statement: CustomerStatement) => {
  activeStatement.value = statement;
  showWhatsAppModal.value = true;
};

const handlePaymentOpen = (customer: Customer) => {
  activePaymentCustomer.value = customer;
  showPaymentModal.value = true;
};

const handleStatementOpen = (statement: CustomerStatement) => {
  activeFullStatement.value = statement;
  showStatementModal.value = true;
};

const handleNewCustomer = () => {
  customerToEdit.value = null;
  showNewCustomerModal.value = true;
};

const handleEditCustomer = (c: Customer) => {
  customerToEdit.value = c;
  showNewCustomerModal.value = true;
};

const handleNewProduct = () => {
  productToEdit.value = null;
  showNewProductModal.value = true;
};

const handleEditProduct = (p: Product) => {
  productToEdit.value = p;
  showNewProductModal.value = true;
};

const handleQuickSale = () => {
  currentView.value = 'pos';
  isMobileSidebarOpen.value = false;
};

// POS Shopping Cart State for Topbar Badge Notice
const posCartCount = ref(0);
const posCartTotal = ref(0);
const posCartBump = ref(false);

const handlePosCartUpdate = (data: { count: number; total: number }) => {
  posCartCount.value = data.count;
  posCartTotal.value = data.total;
  posCartBump.value = true;
  setTimeout(() => {
    posCartBump.value = false;
  }, 450);
};

const handleTopCartClick = () => {
  if (currentView.value === 'pos') {
    if (posRef.value?.openCart) {
      posRef.value.openCart();
    }
  } else {
    handleQuickSale();
  }
};

const handleCustomerSaved = async () => {
  await refreshAll();
};

const handleProductSaved = async () => {
  await refreshAll();
};
</script>

<template>
  <!-- If not logged in, show Login Screen -->
  <LoginView v-if="!currentUser" @login-success="onLoginSuccess" />

  <!-- Main System if authenticated -->
  <div v-else class="app-container">
    <!-- Backdrop for tablet/mobile sidebar drawer -->
    <div 
      v-if="isMobileSidebarOpen" 
      class="sidebar-backdrop" 
      @click="isMobileSidebarOpen = false"
    ></div>

    <!-- Sidebar Navigation -->
    <Sidebar 
      :current-view="currentView"
      :debtors-count="summary?.activeDebtorsCount || 0"
      :low-stock-count="summary?.lowStockProductsCount || 0"
      :current-user="currentUser"
      :is-mobile-open="isMobileSidebarOpen"
      @navigate="handleNavigate"
      @quick-sale="handleQuickSale"
      @logout="onLogout"
      @toggle-pin="handleTogglePin"
      @close-mobile="isMobileSidebarOpen = false"
    />

    <!-- Main Content Area -->
    <main class="main-wrapper" :class="{ 'sidebar-pinned': isSidebarPinned }">
      <!-- Tablet/Mobile Top Navigation Bar -->
      <header class="tablet-topbar">
        <button class="tablet-menu-btn" title="Abrir Menú" @click="isMobileSidebarOpen = true">
          <Menu :size="22" />
        </button>
        <div class="tablet-brand">
          <div class="tablet-brand-icon">
            <Store :size="16" />
          </div>
          <span class="tablet-brand-name">BodegaPOS</span>
          <span class="tablet-view-tag">• {{ getViewTitle(currentView) }}</span>
        </div>
        <!-- If in POS, show interactive shopping cart button with live count & total -->
        <button 
          v-if="currentView === 'pos'"
          class="btn btn-primary btn-sm tablet-quick-btn pos-topbar-cart-btn"
          :class="{ 'has-items': posCartCount > 0, 'cart-bump': posCartBump }"
          title="Ver detalle del fiado y cliente"
          @click="handleTopCartClick"
        >
          <div class="cart-icon-wrapper">
            <ShoppingCart :size="16" />
            <span v-if="posCartCount > 0" class="topbar-cart-badge">{{ posCartCount }}</span>
          </div>
          <span class="cart-label">Carrito</span>
          <span v-if="posCartCount > 0" class="topbar-cart-total">• S/. {{ posCartTotal.toFixed(2) }}</span>
        </button>
        <button 
          v-else
          class="btn btn-primary btn-sm tablet-quick-btn" 
          @click="handleQuickSale"
        >
          <PlusCircle :size="15" />
          <span>Vender</span>
        </button>
      </header>

      <DashboardView 
        v-if="currentView === 'dashboard'" 
        ref="dashboardRef"
        @navigate="handleNavigate"
        @open-whatsapp="handleWhatsAppOpen"
        @open-payment="handlePaymentOpen"
        @quick-sale="handleQuickSale"
      />

      <PosView 
        v-else-if="currentView === 'pos'" 
        ref="posRef"
        @new-customer-request="handleNewCustomer"
        @sale-completed="refreshAll"
        @open-whatsapp="handleWhatsAppOpen"
        @cart-updated="handlePosCartUpdate"
      />

      <DebtsView 
        v-else-if="currentView === 'debts'" 
        ref="debtsRef"
        @open-whatsapp="handleWhatsAppOpen"
        @open-payment="handlePaymentOpen"
        @open-statement="handleStatementOpen"
      />

      <CustomersView 
        v-else-if="currentView === 'customers'" 
        ref="customersRef"
        @new-customer="handleNewCustomer"
        @edit-customer="handleEditCustomer"
        @open-whatsapp="handleWhatsAppOpen"
        @open-payment="handlePaymentOpen"
        @open-statement="handleStatementOpen"
      />

      <InventoryView 
        v-else-if="currentView === 'inventory'" 
        ref="inventoryRef"
        @new-product="handleNewProduct"
        @edit-product="handleEditProduct"
      />

      <UsersView 
        v-else-if="currentView === 'users' && currentUser?.role === 'Admin'" 
      />
    </main>

    <!-- Modals -->
    <WhatsAppModal 
      :statement="activeStatement"
      @close="showWhatsAppModal = false; activeStatement = null"
    />

    <PaymentModal 
      :customer="activePaymentCustomer"
      @close="showPaymentModal = false; activePaymentCustomer = null"
      @payment-success="refreshAll"
    />

    <CustomerStatementModal 
      :statement="activeFullStatement"
      @close="showStatementModal = false; activeFullStatement = null"
      @open-whatsapp="stmt => { showStatementModal = false; handleWhatsAppOpen(stmt); }"
      @open-payment="cust => { showStatementModal = false; handlePaymentOpen(cust); }"
    />

    <NewCustomerModal 
      :show="showNewCustomerModal"
      :customer-to-edit="customerToEdit"
      @close="showNewCustomerModal = false; customerToEdit = null"
      @customer-saved="handleCustomerSaved"
    />

    <NewProductModal 
      :show="showNewProductModal"
      :product-to-edit="productToEdit"
      :categories="categories"
      @close="showNewProductModal = false; productToEdit = null"
      @product-saved="handleProductSaved"
    />
  </div>
</template>
