<script setup lang="ts">
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
  User as UserIcon
} from 'lucide-vue-next';
import { sounds } from '../utils/sound';
import type { LoginResponse } from '../services/api';

const props = defineProps<{
  currentView: string;
  debtorsCount: number;
  lowStockCount: number;
  currentUser: LoginResponse | null;
}>();

const emit = defineEmits<{
  (e: 'navigate', view: string): void;
  (e: 'quick-sale'): void;
  (e: 'logout'): void;
}>();

const handleNav = (view: string) => {
  sounds.playClick();
  emit('navigate', view);
};

const handleLogout = () => {
  sounds.playClick();
  emit('logout');
};
</script>

<template>
  <aside class="sidebar">
    <!-- Brand Header -->
    <div class="brand-section">
      <div class="brand-icon">
        <Store :size="22" />
      </div>
      <div class="brand-info">
        <h1>BodegaPOS</h1>
        <span>
          <span class="brand-badge"></span>
          Control de Fiados
        </span>
      </div>
    </div>

    <!-- Quick Action Button -->
    <div style="margin-bottom: 16px;">
      <button 
        class="btn btn-primary" 
        style="width: 100%; justify-content: flex-start; padding: 11px 14px; font-size: 0.9rem;"
        @click="emit('quick-sale')"
      >
        <PlusCircle :size="18" />
        <span>Vender al Fiado</span>
      </button>
    </div>

    <!-- Navigation Menu -->
    <nav class="nav-menu">
      <div 
        class="nav-item" 
        :class="{ active: currentView === 'dashboard' }"
        @click="handleNav('dashboard')"
      >
        <LayoutDashboard class="nav-icon" :size="19" />
        <span>Dashboard</span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'pos' }"
        @click="handleNav('pos')"
      >
        <ShoppingCart class="nav-icon" :size="19" />
        <span>POS de Fiados</span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'debts' }"
        @click="handleNav('debts')"
      >
        <Receipt class="nav-icon" :size="19" />
        <span>Deudas & Cobranzas</span>
        <span v-if="debtorsCount > 0" class="nav-badge">{{ debtorsCount }}</span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'customers' }"
        @click="handleNav('customers')"
      >
        <Users class="nav-icon" :size="19" />
        <span>Lista de Clientes</span>
      </div>

      <div 
        class="nav-item" 
        :class="{ active: currentView === 'inventory' }"
        @click="handleNav('inventory')"
      >
        <Package class="nav-icon" :size="19" />
        <span>Inventario</span>
        <span 
          v-if="lowStockCount > 0" 
          class="nav-badge" 
          style="background: #fef3c7; color: #92400e; border-color: #fde68a;"
        >
          {{ lowStockCount }} bajo
        </span>
      </div>

      <!-- Admin-Only Section: Usuarios / Trabajadores -->
      <div 
        v-if="currentUser?.role === 'Admin'"
        class="nav-item" 
        :class="{ active: currentView === 'users' }"
        @click="handleNav('users')"
      >
        <UserCheck class="nav-icon" :size="19" />
        <span>Usuarios & Personal</span>
      </div>
    </nav>

    <!-- Sidebar Footer: Current User & Logout -->
    <div class="sidebar-footer">
      <div class="user-pill">
        <div class="user-pill-avatar">
          <ShieldCheck v-if="currentUser?.role === 'Admin'" :size="16" />
          <UserIcon v-else :size="16" />
        </div>
        <div class="user-pill-info">
          <div class="user-pill-name">{{ currentUser?.fullName || currentUser?.username || 'Usuario' }}</div>
          <div class="user-pill-role">{{ currentUser?.role === 'Admin' ? 'Administrador' : 'Trabajador' }}</div>
        </div>
        <button 
          class="btn btn-secondary btn-sm" 
          style="padding: 6px; border: none; color: var(--slate-500);"
          title="Cerrar Sesión"
          @click="handleLogout"
        >
          <LogOut :size="16" />
        </button>
      </div>
    </div>
  </aside>
</template>
