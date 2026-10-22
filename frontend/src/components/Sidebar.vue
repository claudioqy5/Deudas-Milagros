<script setup lang="ts">
import { ref } from 'vue';
import { 
  LayoutDashboard, 
  ShoppingCart, 
  Receipt, 
  Users, 
  Package, 
  Store,
  UserCheck,
  PlusCircle,
  LogOut,
  ShieldCheck,
  User as UserIcon,
  Pin,
  PinOff,
  X
} from 'lucide-vue-next';
import { sounds } from '../utils/sound';
import type { LoginResponse } from '../services/api';

const props = defineProps<{
  currentView: string;
  debtorsCount: number;
  lowStockCount: number;
  currentUser: LoginResponse | null;
  isMobileOpen?: boolean;
}>();

const emit = defineEmits<{
  (e: 'navigate', view: string): void;
  (e: 'quick-sale'): void;
  (e: 'logout'): void;
  (e: 'toggle-pin', isPinned: boolean): void;
  (e: 'close-mobile'): void;
}>();

const isHovered = ref(false);
const isPinned = ref(localStorage.getItem('bodega_sidebar_pinned') === 'true');

const togglePin = () => {
  sounds.playClick();
  isPinned.value = !isPinned.value;
  localStorage.setItem('bodega_sidebar_pinned', String(isPinned.value));
  emit('toggle-pin', isPinned.value);
};

// Emit initial state
emit('toggle-pin', isPinned.value);

const handleNav = (view: string) => {
  sounds.playClick();
  emit('navigate', view);
  emit('close-mobile');
};

const handleQuickSale = () => {
  sounds.playClick();
  emit('quick-sale');
  emit('close-mobile');
};

const handleLogout = () => {
  sounds.playClick();
  emit('logout');
};
</script>

<template>
  <aside 
    class="sidebar" 
    :class="{ 
      'is-pinned': isPinned, 
      'is-hovered': isHovered,
      'is-mobile-open': isMobileOpen
    }"
    @mouseenter="isHovered = true"
    @mouseleave="isHovered = false"
  >
    <!-- Brand Header -->
    <div class="brand-section">
      <div class="brand-icon" title="BodegaPOS">
        <Store :size="22" />
      </div>
      <div class="brand-info">
        <h1>BodegaPOS</h1>
        <span>
          <span class="brand-badge"></span>
          Control de Fiados
        </span>
      </div>

      <!-- Pin/Unpin Toggle Button (shown on desktop when expanded) -->
      <button 
        class="pin-btn" 
        :class="{ active: isPinned }"
        :title="isPinned ? 'Desanclar barra (modo auto-ocultar al quitar cursor)' : 'Fijar barra abierta'"
        @click="togglePin"
      >
        <Pin v-if="!isPinned" :size="15" />
        <PinOff v-else :size="15" />
      </button>

      <!-- Mobile/Tablet Close Button -->
      <button 
        class="mobile-sidebar-close" 
        title="Cerrar Menú"
        @click="emit('close-mobile')"
      >
        <X :size="19" />
      </button>
    </div>

    <!-- Quick Action Button -->
    <div class="sidebar-quick-action" style="margin-bottom: 16px;">
      <button 
        class="btn btn-primary quick-sale-btn" 
        title="Vender al Fiado"
        @click="handleQuickSale"
      >
        <PlusCircle class="quick-sale-icon" :size="19" />
        <span class="sidebar-text">Vender al Fiado</span>
      </button>
    </div>

    <!-- Navigation Menu -->
    <nav class="nav-menu">
      <div 
        class="nav-item" 
        :class="{ active: currentView === 'dashboard' }"
        title="Dashboard general"
        @click="handleNav('dashboard')"
      >
        <LayoutDashboard class="nav-icon" :size="19" />
        <span class="sidebar-text">Dashboard</span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'pos' }"
        title="Punto de venta de fiados"
        @click="handleNav('pos')"
      >
        <ShoppingCart class="nav-icon" :size="19" />
        <span class="sidebar-text">POS de Fiados</span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'debts' }"
        title="Deudas y cobranzas pendientes"
        @click="handleNav('debts')"
      >
        <Receipt class="nav-icon" :size="19" />
        <span class="sidebar-text">Deudas & Cobranzas</span>
        
        <!-- Expanded badge -->
        <span v-if="debtorsCount > 0" class="nav-badge">{{ debtorsCount }}</span>
        <!-- Compact indicator dot -->
        <span v-if="debtorsCount > 0" class="nav-dot"></span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'customers' }"
        title="Directorio de clientes"
        @click="handleNav('customers')"
      >
        <Users class="nav-icon" :size="19" />
        <span class="sidebar-text">Lista de Clientes</span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'inventory' }"
        title="Catálogo e inventario de productos"
        @click="handleNav('inventory')"
      >
        <Package class="nav-icon" :size="19" />
        <span class="sidebar-text">Inventario</span>

        <!-- Expanded badge -->
        <span 
          v-if="lowStockCount > 0" 
          class="nav-badge" 
          style="background: #fef3c7; color: #92400e; border-color: #fde68a;"
        >
          {{ lowStockCount }} bajo
        </span>
        <!-- Compact indicator dot -->
        <span v-if="lowStockCount > 0" class="nav-dot" style="background-color: #f59e0b;"></span>
      </div>

      <!-- Admin-Only Section: Usuarios / Trabajadores -->
      <div 
        v-if="currentUser?.role === 'Admin'"
        class="nav-item" 
        :class="{ active: currentView === 'users' }"
        title="Usuarios y personal"
        @click="handleNav('users')"
      >
        <UserCheck class="nav-icon" :size="19" />
        <span class="sidebar-text">Usuarios & Personal</span>
      </div>
    </nav>

    <!-- Sidebar Footer: Current User & Logout -->
    <div class="sidebar-footer">
      <div class="user-pill" :title="currentUser?.fullName || currentUser?.username">
        <div class="user-pill-avatar">
          <ShieldCheck v-if="currentUser?.role === 'Admin'" :size="17" />
          <UserIcon v-else :size="17" />
        </div>
        <div class="user-pill-info">
          <div class="user-pill-name">{{ currentUser?.fullName || currentUser?.username || 'Usuario' }}</div>
          <div class="user-pill-role">{{ currentUser?.role === 'Admin' ? 'Administrador' : 'Trabajador' }}</div>
        </div>
        <button 
          class="user-logout-btn" 
          title="Cerrar Sesión"
          @click="handleLogout"
        >
          <LogOut :size="16" />
        </button>
      </div>
    </div>
  </aside>
</template>
