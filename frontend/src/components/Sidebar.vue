<script setup lang="ts">
import { ref, computed } from 'vue';
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
  (e: 'expanded-change', isExpanded: boolean): void;
}>();

const isPinned = ref(localStorage.getItem('bodega_sidebar_pinned') === 'true');
const isOpen = ref(false); // Explicit open state for click / touch
const isMouseHovered = ref(false);
const ignoreHover = ref(false); // Blocks hover re-activation right after a selection

const isVisuallyOpen = computed(() => {
  return isPinned.value || isOpen.value || isMouseHovered.value;
});

const togglePin = () => {
  sounds.playClick();
  isPinned.value = !isPinned.value;
  localStorage.setItem('bodega_sidebar_pinned', String(isPinned.value));
  emit('toggle-pin', isPinned.value);
};

// Emit initial state
emit('toggle-pin', isPinned.value);

const openSidebar = () => {
  if (!isVisuallyOpen.value) {
    sounds.playClick();
    isOpen.value = true;
    emit('expanded-change', true);
  }
};

const closeSidebar = () => {
  isOpen.value = false;
  isMouseHovered.value = false;
  isPinned.value = false;
  localStorage.setItem('bodega_sidebar_pinned', 'false');
  emit('expanded-change', false);
  emit('toggle-pin', false);

  // Block mouse hover re-activation for 400ms so the sidebar actually collapses
  ignoreHover.value = true;
  setTimeout(() => { ignoreHover.value = false; }, 400);
};

const handleNav = (view: string, e?: Event) => {
  if (e) e.stopPropagation();

  // If the sidebar is closed: FIRST TAP/CLICK OPENS IT
  if (!isVisuallyOpen.value) {
    openSidebar();
    return;
  }

  // Sidebar is open: navigate and immediately close
  sounds.playClick();
  emit('navigate', view);
  emit('close-mobile');
  closeSidebar();
};

const handleQuickSale = (e?: Event) => {
  if (e) e.stopPropagation();

  if (!isVisuallyOpen.value) {
    openSidebar();
    return;
  }

  sounds.playClick();
  emit('quick-sale');
  emit('close-mobile');
  closeSidebar();
};

const onPointerEnter = (e: PointerEvent) => {
  // Only trigger for real mouse pointer, never touch — and only when not ignoring
  if (e.pointerType === 'mouse' && !ignoreHover.value) {
    isMouseHovered.value = true;
  }
};

const onPointerLeave = (e: PointerEvent) => {
  if (e.pointerType === 'mouse') {
    isMouseHovered.value = false;
  }
  ignoreHover.value = false;
};

const toggleBrand = (e?: Event) => {
  if (e) e.stopPropagation();
  sounds.playClick();
  if (isVisuallyOpen.value) {
    closeSidebar();
  } else {
    openSidebar();
  }
};

defineExpose({
  closeTouchExpanded: closeSidebar,
  closeSidebar
});

const handleLogout = (e?: Event) => {
  if (e) e.stopPropagation();
  sounds.playClick();
  closeSidebar();
  emit('logout');
};
</script>

<template>
  <aside 
    class="sidebar" 
    :class="{ 
      'is-pinned': isPinned, 
      'is-open': isVisuallyOpen,
      'is-mobile-open': isMobileOpen
    }"
    @pointerenter="onPointerEnter"
    @pointerleave="onPointerLeave"
    @click="openSidebar"
  >
    <!-- Brand Header -->
    <div class="brand-section" style="cursor: pointer;" @click.stop="toggleBrand">
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
        @click.stop="togglePin"
      >
        <Pin v-if="!isPinned" :size="15" />
        <PinOff v-else :size="15" />
      </button>

      <!-- Mobile/Tablet Close Button -->
      <button 
        class="mobile-sidebar-close" 
        title="Cerrar Menú"
        @click.stop="emit('close-mobile')"
      >
        <X :size="19" />
      </button>
    </div>

    <!-- Quick Action Button -->
    <div class="sidebar-quick-action" style="margin-bottom: 16px;">
      <button 
        class="btn btn-primary quick-sale-btn" 
        title="Vender al Fiado"
        @click.stop="(e) => handleQuickSale(e)"
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
        @click.stop="(e) => handleNav('dashboard', e)"
      >
        <LayoutDashboard class="nav-icon" :size="19" />
        <span class="sidebar-text">Inicio</span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'pos' }"
        title="Punto de venta de fiados"
        @click.stop="(e) => handleNav('pos', e)"
      >
        <ShoppingCart class="nav-icon" :size="19" />
        <span class="sidebar-text">POS de Fiados</span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'debts' }"
        title="Deudas y cobranzas pendientes"
        @click.stop="(e) => handleNav('debts', e)"
      >
        <Receipt class="nav-icon" :size="19" />
        <span class="sidebar-text">Cobranza</span>
        
        <!-- Expanded badge -->
        <span v-if="debtorsCount > 0" class="nav-badge">{{ debtorsCount }}</span>
        <!-- Compact indicator dot -->
        <span v-if="debtorsCount > 0" class="nav-dot"></span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'customers' }"
        title="Directorio de clientes"
        @click.stop="(e) => handleNav('customers', e)"
      >
        <Users class="nav-icon" :size="19" />
        <span class="sidebar-text">Clientes</span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'inventory' }"
        title="Catálogo e inventario de productos"
        @click.stop="(e) => handleNav('inventory', e)"
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

      <!-- Section: Usuarios / Trabajadores (Protegido por clave de administrador) -->
      <div 
        class="nav-item" 
        :class="{ active: currentView === 'users' }"
        title="Usuarios y personal (Exclusivo Administrador)"
        @click.stop="(e) => handleNav('users', e)"
      >
        <UserCheck class="nav-icon" :size="19" />
        <span class="sidebar-text">Trabajadores</span>
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
