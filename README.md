# BodegaPOS & Control de Deudas (Fiados)

Sistema web integral desarrollado para la gestión de ventas al fiado, inventario, clientes, ranking de deudas, cobranzas automáticas por WhatsApp y panel financiero en tiempo real.

## Tecnologías Utilizadas

- **Frontend:** Vue 3 (Vite, Composition API, Lucide Icons, Chart.js, Canvas Confetti)
- **Backend:** ASP.NET Core 9.0 Web API (C#)
- **Base de Datos:** MongoDB
- **Contenedores:** Docker & Docker Compose
- **Moneda:** Soles Peruanos (S/.)

## Características Principales

1. **Dashboard Financiero:**
   - Visualización en vivo de lo que ya se cobró y lo que falta por cobrar.
   - Gráfico de barras comparativo de ventas fiadas vs. cobros mensuales.
   - Ranking de clientes que más deben con alertas visuales.
   - Registro de movimientos recientes.

2. **Cobranza Rápida por WhatsApp:**
   - Botón directo para enviar la relación completa de la deuda al WhatsApp del cliente.
   - Desglose cronológico detallado: fecha y hora de cada compra, productos llevados, cantidades, precios unitarios y subtotales.
   - Abonos registrados, saldo pendiente exacto y datos de pago digital (Yape / Plin).

3. **POS de Ventas al Fiado:**
   - Búsqueda ágil de cliente con validación de límite de crédito y deuda actual en vivo.
   - Catálogo de productos organizado por categorías.
   - Carrito interactivo con opción de registrar pago inicial / adelanto.
   - Soporte para **Stock Infinito** en productos de rotación continua.

4. **Gestión de Clientes:**
   - Padrón completo de clientes con teléfono validado para WhatsApp, DNI, dirección y notas.
   - Estado de cuenta detallado con historial de compras y pagos.

5. **Inventario Simplificado:**
   - Precios de venta al público en Soles (S/.).
   - Opción de stock infinito para evitar actualizar inventario tras cada venta.

6. **Seguridad y Control de Acceso por Roles:**
   - Pantalla de inicio de sesión (Login).
   - Rol **Administrador (`admin`):** Acceso total y gestión de usuarios/trabajadores.
   - Rol **Trabajador (`Worker`):** Operaciones de venta, cobranza y abonos (sin acceso al módulo de administración de usuarios).

## Credenciales Predeterminadas

- **Administrador:** `admin` / `admin123`
- **Trabajador:** `cajero` / `cajero123`

## Despliegue con Docker

Para levantar todo el stack (MongoDB + Backend + Frontend) con un solo comando:

```bash
docker compose up --build -d
```
