export interface User {
  id: string;
  username: string;
  fullName: string;
  role: 'Admin' | 'Worker';
  isActive: boolean;
  createdAt: string;
  lastLogin?: string;
}

export interface LoginResponse {
  token: string;
  id: string;
  username: string;
  fullName: string;
  role: 'Admin' | 'Worker';
}

export interface Customer {
  id: string;
  name: string;
  phone: string;
  documentNumber?: string;
  address?: string;
  creditLimit: number;
  currentDebt: number;
  totalPurchased: number;
  totalPaid: number;
  notes?: string;
  isActive: boolean;
  lastSaleDate?: string;
  lastPaymentDate?: string;
  createdAt: string;
}

export interface ProductPack {
  id: string;
  name: string; // e.g. "Sixpack agua", "Caja x24"
  unitsCount: number; // e.g. 6
  price: number; // e.g. 10.00
  imageUrl?: string;
}

export interface ComboItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface Product {
  id: string;
  name: string;
  category: string;
  salePrice: number;
  stock: number;
  isInfiniteStock: boolean;
  unit: string;
  imageUrl?: string;
  packs?: ProductPack[];
  isCombo?: boolean;
  comboItems?: ComboItem[];
  isActive: boolean;
}

export interface SaleItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface CreditSale {
  id: string;
  saleCode: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  items: SaleItem[];
  totalAmount: number;
  initialPayment: number;
  remainingDebt: number;
  status: 'Pending' | 'PartiallyPaid' | 'Paid';
  date: string;
  notes?: string;
}

export interface Payment {
  id: string;
  customerId: string;
  customerName: string;
  amount: number;
  paymentMethod: string;
  reference?: string;
  notes?: string;
  date: string;
}

export interface MonthlyMetric {
  month: string;
  totalFiado: number;
  totalCobrado: number;
}

export interface RecentActivity {
  type: 'SALE' | 'PAYMENT';
  id: string;
  customerName: string;
  amount: number;
  date: string;
  description: string;
}

export interface StoreSettings {
  storeName: string;
  ownerName: string;
  phone: string;
  yapeNumber: string;
  plinNumber: string;
  currencySymbol: string;
}

export interface DashboardSummary {
  totalPendingDebt: number;
  totalCollected: number;
  totalSalesFiadas: number;
  activeDebtorsCount: number;
  totalCustomersCount: number;
  totalProductsCount: number;
  lowStockProductsCount: number;
  topDebtors: Customer[];
  monthlyTrend: MonthlyMetric[];
  recentActivity: RecentActivity[];
  store: StoreSettings;
  period?: string;
  periodLabel?: string;
  globalPendingDebt?: number;
  periodSalesCount?: number;
  periodPaymentsCount?: number;
}

export interface CustomerStatement {
  customer: Customer;
  sales: CreditSale[];
  payments: Payment[];
  totalSalesAmount: number;
  totalPaymentsAmount: number;
  balanceDue: number;
  whatsAppMessage: string;
  whatsAppUrl: string;
}

const API_BASE = import.meta.env.VITE_API_URL || '/api';

// Auth State Helper
const USER_KEY = 'bodega_auth_user';

export const authState = {
  getUser(): LoginResponse | null {
    const data = localStorage.getItem(USER_KEY);
    if (!data) return null;
    try {
      return JSON.parse(data);
    } catch {
      return null;
    }
  },
  setUser(user: LoginResponse) {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },
  clearUser() {
    localStorage.removeItem(USER_KEY);
  },
  isAuthenticated(): boolean {
    return !!this.getUser();
  },
  isAdmin(): boolean {
    const u = this.getUser();
    return u?.role === 'Admin';
  }
};

export const api = {
  // Auth
  async login(username: string, password: string): Promise<LoginResponse> {
    const res = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Credenciales incorrectas');
    }
    const data: LoginResponse = await res.json();
    authState.setUser(data);
    return data;
  },

  logout() {
    authState.clearUser();
  },

  async verifyAdminPassword(password: string): Promise<boolean> {
    try {
      const res = await fetch(`${API_BASE}/auth/verify-admin`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ password })
      });
      if (res.ok) return true;
    } catch {
      // fallback
    }

    const current = authState.getUser();
    const usernamesToTry: string[] = [];
    if (current && current.role === 'Admin') {
      usernamesToTry.push(current.username);
    }
    if (!usernamesToTry.includes('admin')) {
      usernamesToTry.push('admin');
    }

    for (const u of usernamesToTry) {
      try {
        const res = await fetch(`${API_BASE}/auth/login`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ username: u, password })
        });
        if (res.ok) {
          const data = await res.json();
          if (data.role === 'Admin') {
            return true;
          }
        }
      } catch {
        // continue
      }
    }
    throw new Error('Contraseña incorrecta. Este espacio es exclusivo del administrador.');
  },

  // Users Management (Workers)
  async getUsers(): Promise<User[]> {
    const res = await fetch(`${API_BASE}/users`);
    if (!res.ok) throw new Error('Error al cargar lista de usuarios');
    return res.json();
  },

  async createUser(user: { username: string; password: string; fullName: string; role: string }): Promise<User> {
    const res = await fetch(`${API_BASE}/users`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(user)
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Error al crear usuario');
    }
    return res.json();
  },

  async updateUser(id: string, user: { fullName?: string; role?: string; password?: string; isActive?: boolean }): Promise<User> {
    const res = await fetch(`${API_BASE}/users/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(user)
    });
    if (!res.ok) throw new Error('Error al actualizar usuario');
    return res.json();
  },

  async deleteUser(id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/users/${id}`, { method: 'DELETE' });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Error al eliminar usuario');
    }
  },

  // Dashboard
  async getDashboardSummary(params?: {
    period?: 'day' | 'week' | 'month' | 'year' | 'all';
    date?: string;
    startDate?: string;
    endDate?: string;
  }): Promise<DashboardSummary> {
    const query = new URLSearchParams();
    if (params?.period) query.append('period', params.period);
    if (params?.date) query.append('date', params.date);
    if (params?.startDate) query.append('startDate', params.startDate);
    if (params?.endDate) query.append('endDate', params.endDate);
    const qs = query.toString() ? `?${query.toString()}` : '';
    const res = await fetch(`${API_BASE}/dashboard/summary${qs}`);
    if (!res.ok) throw new Error('Error al cargar métricas del dashboard');
    return res.json();
  },

  // Customers
  async getCustomers(search = '', onlyDebtors = false): Promise<Customer[]> {
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    if (onlyDebtors) params.append('onlyDebtors', 'true');
    const res = await fetch(`${API_BASE}/customers?${params.toString()}`);
    if (!res.ok) throw new Error('Error al cargar clientes');
    return res.json();
  },

  async getTopDebtors(limit = 10): Promise<Customer[]> {
    const res = await fetch(`${API_BASE}/customers/top-debtors?limit=${limit}`);
    if (!res.ok) throw new Error('Error al cargar deudores');
    return res.json();
  },

  async getCustomerById(id: string): Promise<Customer> {
    const res = await fetch(`${API_BASE}/customers/${id}`);
    if (!res.ok) throw new Error('Error al obtener cliente');
    return res.json();
  },

  async createCustomer(customer: Partial<Customer>): Promise<Customer> {
    const res = await fetch(`${API_BASE}/customers`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(customer),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Error al crear cliente');
    }
    return res.json();
  },

  async updateCustomer(id: string, customer: Partial<Customer>): Promise<Customer> {
    const res = await fetch(`${API_BASE}/customers/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(customer),
    });
    if (!res.ok) throw new Error('Error al actualizar cliente');
    return res.json();
  },

  async deleteCustomer(id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/customers/${id}`, { method: 'DELETE' });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Error al eliminar cliente');
    }
  },

  async getCustomerStatement(id: string): Promise<CustomerStatement> {
    const res = await fetch(`${API_BASE}/customers/${id}/statement`);
    if (!res.ok) throw new Error('Error al obtener estado de cuenta');
    return res.json();
  },

  // Products
  async getProducts(search = '', category = 'Todos'): Promise<Product[]> {
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    if (category && category !== 'Todos') params.append('category', category);
    const res = await fetch(`${API_BASE}/products?${params.toString()}`);
    if (!res.ok) throw new Error('Error al cargar productos');
    return res.json();
  },

  async getProductCategories(): Promise<string[]> {
    const res = await fetch(`${API_BASE}/products/categories`);
    if (!res.ok) throw new Error('Error al cargar categorías');
    return res.json();
  },

  async createProduct(product: Partial<Product>): Promise<Product> {
    const res = await fetch(`${API_BASE}/products`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(product),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Error al crear producto');
    }
    return res.json();
  },

  async updateProduct(id: string, product: Partial<Product>): Promise<Product> {
    const res = await fetch(`${API_BASE}/products/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(product),
    });
    if (!res.ok) throw new Error('Error al actualizar producto');
    return res.json();
  },

  async deleteProduct(id: string): Promise<void> {
    const res = await fetch(`${API_BASE}/products/${id}`, { method: 'DELETE' });
    if (!res.ok) throw new Error('Error al eliminar producto');
  },

  // Credit Sales
  async createCreditSale(data: {
    customerId: string;
    items: { 
      productId: string; 
      productName?: string;
      quantity: number; 
      unitPrice?: number;
      unitsDeducted?: number;
    }[];
    initialPayment?: number;
    notes?: string;
  }): Promise<CreditSale> {
    const res = await fetch(`${API_BASE}/creditSales`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Error al registrar venta al fiado');
    }
    return res.json();
  },

  async getCreditSales(customerId?: string, status?: string): Promise<CreditSale[]> {
    const params = new URLSearchParams();
    if (customerId) params.append('customerId', customerId);
    if (status) params.append('status', status);
    const res = await fetch(`${API_BASE}/creditSales?${params.toString()}`);
    if (!res.ok) throw new Error('Error al cargar ventas fiadas');
    return res.json();
  },

  // Payments
  async createPayment(data: {
    customerId: string;
    amount: number;
    paymentMethod: string;
    reference?: string;
    notes?: string;
  }): Promise<any> {
    const res = await fetch(`${API_BASE}/payments`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Error al registrar abono');
    }
    return res.json();
  },

  async getPayments(customerId?: string): Promise<Payment[]> {
    const params = new URLSearchParams();
    if (customerId) params.append('customerId', customerId);
    const res = await fetch(`${API_BASE}/payments?${params.toString()}`);
    if (!res.ok) throw new Error('Error al cargar abonos');
    return res.json();
  },
};
