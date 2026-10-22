using BodegaDebts.Api.Controllers;
using BodegaDebts.Api.Models;
using MongoDB.Driver;

namespace BodegaDebts.Api.Services;

public static class DataSeeder
{
    public static async Task SeedAsync(MongoDbService db)
    {
        // 1. Seed Users (Admin & Worker)
        var userCount = await db.Users.CountDocumentsAsync(_ => true);
        if (userCount == 0)
        {
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = AuthController.HashPassword("admin123"),
                FullName = "Don Pepe (Administrador)",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var workerUser = new User
            {
                Username = "cajero",
                PasswordHash = AuthController.HashPassword("cajero123"),
                FullName = "Juan Cajero",
                Role = "Worker",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await db.Users.InsertManyAsync(new[] { adminUser, workerUser });
            Console.WriteLine("[DataSeeder] Initial users seeded (admin / cajero)");
        }

        // 2. Seed Products (Simplified schema with infinite stock)
        var productCount = await db.Products.CountDocumentsAsync(_ => true);
        if (productCount == 0)
        {
            var products = new List<Product>
            {
                new() { Name = "Arroz Costeño Extra 1kg", Category = "Abarrotes", SalePrice = 4.80m, IsInfiniteStock = true, Stock = 0, Unit = "bolsa" },
                new() { Name = "Aceite Primor Clásico 900ml", Category = "Abarrotes", SalePrice = 8.90m, IsInfiniteStock = true, Stock = 0, Unit = "botella" },
                new() { Name = "Azúcar Rubia Cartavio 1kg", Category = "Abarrotes", SalePrice = 4.20m, IsInfiniteStock = true, Stock = 0, Unit = "bolsa" },
                new() { Name = "Fideos Don Vittorio Spaghetti 450g", Category = "Abarrotes", SalePrice = 3.20m, IsInfiniteStock = true, Stock = 0, Unit = "paquete" },
                new() { Name = "Leche Gloria Etiqueta Azul 400g", Category = "Lácteos", SalePrice = 4.30m, IsInfiniteStock = true, Stock = 0, Unit = "lata" },
                new() { Name = "Yogurt Gloria Fresa 1L", Category = "Lácteos", SalePrice = 7.00m, IsInfiniteStock = true, Stock = 0, Unit = "botella" },
                new() { Name = "Mantequilla Gloria con Sal 200g", Category = "Lácteos", SalePrice = 6.80m, IsInfiniteStock = true, Stock = 0, Unit = "barra" },
                new() { Name = "Queso Fresco Andino kg", Category = "Lácteos", SalePrice = 26.00m, IsInfiniteStock = true, Stock = 0, Unit = "kg" },
                new() { Name = "Inca Kola Sin Azúcar 500ml", Category = "Bebidas", SalePrice = 2.80m, IsInfiniteStock = true, Stock = 0, Unit = "botella" },
                new() { Name = "Coca Cola Original 1.5L", Category = "Bebidas", SalePrice = 7.00m, IsInfiniteStock = true, Stock = 0, Unit = "botella" },
                new() { Name = "Agua Mineral San Mateo 600ml", Category = "Bebidas", SalePrice = 2.00m, IsInfiniteStock = true, Stock = 0, Unit = "botella" },
                new() { Name = "Cerveza Cristal 650ml", Category = "Bebidas", SalePrice = 7.50m, IsInfiniteStock = true, Stock = 0, Unit = "botella" },
                new() { Name = "Huevos Pardos Frescos (x6)", Category = "Huevos y Pan", SalePrice = 4.80m, IsInfiniteStock = true, Stock = 0, Unit = "paquete" },
                new() { Name = "Pan Francés Artesanal (x10)", Category = "Huevos y Pan", SalePrice = 3.00m, IsInfiniteStock = true, Stock = 0, Unit = "bolsa" },
                new() { Name = "Detergente Bolívar Flores 800g", Category = "Limpieza", SalePrice = 8.00m, IsInfiniteStock = true, Stock = 0, Unit = "bolsa" },
                new() { Name = "Papel Higiénico Suave 4 rollos", Category = "Limpieza", SalePrice = 5.50m, IsInfiniteStock = true, Stock = 0, Unit = "paquete" },
                new() { Name = "Galletas Casino Fresa 6pk", Category = "Snacks", SalePrice = 4.50m, IsInfiniteStock = true, Stock = 0, Unit = "paquete" },
                new() { Name = "Papas Lays Clásicas 160g", Category = "Snacks", SalePrice = 6.50m, IsInfiniteStock = true, Stock = 0, Unit = "bolsa" }
            };

            await db.Products.InsertManyAsync(products);
        }

        // 3. Customers seed
        var customerCount = await db.Customers.CountDocumentsAsync(_ => true);
        if (customerCount == 0)
        {
            var productsList = await db.Products.Find(_ => true).ToListAsync();
            var leche = productsList.FirstOrDefault(p => p.Name.Contains("Leche")) ?? productsList[0];
            var arroz = productsList.FirstOrDefault(p => p.Name.Contains("Arroz")) ?? productsList[0];
            var aceite = productsList.FirstOrDefault(p => p.Name.Contains("Aceite")) ?? productsList[1];
            var huevos = productsList.FirstOrDefault(p => p.Name.Contains("Huevos")) ?? productsList[2];
            var gaseosa = productsList.FirstOrDefault(p => p.Name.Contains("Inca")) ?? productsList[3];

            var c1 = new Customer
            {
                Name = "Rosa María Paredes",
                Phone = "987654321",
                DocumentNumber = "45871236",
                Address = "Jr. Las Gardenias 342, Mz. B Lt. 12",
                CreditLimit = 350m,
                Notes = "Vecina del 2do piso. Paga los sábados en quincena.",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };

            var c2 = new Customer
            {
                Name = "Carlos Quispe Huamán",
                Phone = "993412587",
                DocumentNumber = "71245896",
                Address = "Av. Santa Rosa 1105",
                CreditLimit = 500m,
                Notes = "Taxista, suele pasar en las noches.",
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            };

            var c3 = new Customer
            {
                Name = "Gladys Huanca Soto",
                Phone = "945123987",
                DocumentNumber = "09456123",
                Address = "Pasaje Los Olivos 118",
                CreditLimit = 250m,
                Notes = "Cliente puntual por Yape.",
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            };

            var c4 = new Customer
            {
                Name = "Manuel Becerra Rivas",
                Phone = "978456123",
                DocumentNumber = "40123987",
                Address = "Calle Bolognesi 410",
                CreditLimit = 400m,
                Notes = "Tiene taller mecánico al costado.",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            var c5 = new Customer
            {
                Name = "Elena Flores Castillo",
                Phone = "963852741",
                DocumentNumber = "48963251",
                Address = "Jr. Arequipa 560 int 4",
                CreditLimit = 200m,
                Notes = "Profesora de primaria.",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            await db.Customers.InsertManyAsync(new[] { c1, c2, c3, c4, c5 });

            var sale1 = new CreditSale
            {
                SaleCode = "FIA-2026-001",
                CustomerId = c1.Id!,
                CustomerName = c1.Name,
                CustomerPhone = c1.Phone,
                Date = DateTime.UtcNow.AddDays(-5),
                Items = new List<SaleItem>
                {
                    new() { ProductId = leche.Id!, ProductName = leche.Name, Quantity = 3, UnitPrice = leche.SalePrice, Subtotal = 3 * leche.SalePrice },
                    new() { ProductId = arroz.Id!, ProductName = arroz.Name, Quantity = 2, UnitPrice = arroz.SalePrice, Subtotal = 2 * arroz.SalePrice }
                },
                TotalAmount = (3 * leche.SalePrice) + (2 * arroz.SalePrice),
                InitialPayment = 0,
                RemainingDebt = (3 * leche.SalePrice) + (2 * arroz.SalePrice),
                Status = "Pending"
            };

            var sale2 = new CreditSale
            {
                SaleCode = "FIA-2026-002",
                CustomerId = c1.Id!,
                CustomerName = c1.Name,
                CustomerPhone = c1.Phone,
                Date = DateTime.UtcNow.AddDays(-2),
                Items = new List<SaleItem>
                {
                    new() { ProductId = aceite.Id!, ProductName = aceite.Name, Quantity = 1, UnitPrice = aceite.SalePrice, Subtotal = aceite.SalePrice },
                    new() { ProductId = huevos.Id!, ProductName = huevos.Name, Quantity = 2, UnitPrice = huevos.SalePrice, Subtotal = 2 * huevos.SalePrice }
                },
                TotalAmount = aceite.SalePrice + (2 * huevos.SalePrice),
                InitialPayment = 5.00m,
                RemainingDebt = (aceite.SalePrice + (2 * huevos.SalePrice)) - 5.00m,
                Status = "PartiallyPaid"
            };

            var sale3 = new CreditSale
            {
                SaleCode = "FIA-2026-003",
                CustomerId = c2.Id!,
                CustomerName = c2.Name,
                CustomerPhone = c2.Phone,
                Date = DateTime.UtcNow.AddDays(-7),
                Items = new List<SaleItem>
                {
                    new() { ProductId = aceite.Id!, ProductName = aceite.Name, Quantity = 2, UnitPrice = aceite.SalePrice, Subtotal = 2 * aceite.SalePrice },
                    new() { ProductId = arroz.Id!, ProductName = arroz.Name, Quantity = 5, UnitPrice = arroz.SalePrice, Subtotal = 5 * arroz.SalePrice },
                    new() { ProductId = gaseosa.Id!, ProductName = gaseosa.Name, Quantity = 4, UnitPrice = gaseosa.SalePrice, Subtotal = 4 * gaseosa.SalePrice }
                },
                TotalAmount = (2 * aceite.SalePrice) + (5 * arroz.SalePrice) + (4 * gaseosa.SalePrice),
                InitialPayment = 0,
                RemainingDebt = (2 * aceite.SalePrice) + (5 * arroz.SalePrice) + (4 * gaseosa.SalePrice),
                Status = "Pending"
            };

            var sale4 = new CreditSale
            {
                SaleCode = "FIA-2026-004",
                CustomerId = c2.Id!,
                CustomerName = c2.Name,
                CustomerPhone = c2.Phone,
                Date = DateTime.UtcNow.AddDays(-1),
                Items = new List<SaleItem>
                {
                    new() { ProductId = leche.Id!, ProductName = leche.Name, Quantity = 6, UnitPrice = leche.SalePrice, Subtotal = 6 * leche.SalePrice },
                    new() { ProductId = huevos.Id!, ProductName = huevos.Name, Quantity = 3, UnitPrice = huevos.SalePrice, Subtotal = 3 * huevos.SalePrice }
                },
                TotalAmount = (6 * leche.SalePrice) + (3 * huevos.SalePrice),
                InitialPayment = 0,
                RemainingDebt = (6 * leche.SalePrice) + (3 * huevos.SalePrice),
                Status = "Pending"
            };

            var sale5 = new CreditSale
            {
                SaleCode = "FIA-2026-005",
                CustomerId = c3.Id!,
                CustomerName = c3.Name,
                CustomerPhone = c3.Phone,
                Date = DateTime.UtcNow.AddDays(-3),
                Items = new List<SaleItem>
                {
                    new() { ProductId = arroz.Id!, ProductName = arroz.Name, Quantity = 2, UnitPrice = arroz.SalePrice, Subtotal = 2 * arroz.SalePrice },
                    new() { ProductId = huevos.Id!, ProductName = huevos.Name, Quantity = 1, UnitPrice = huevos.SalePrice, Subtotal = huevos.SalePrice }
                },
                TotalAmount = (2 * arroz.SalePrice) + huevos.SalePrice,
                InitialPayment = 0,
                RemainingDebt = (2 * arroz.SalePrice) + huevos.SalePrice,
                Status = "Pending"
            };

            var sale6 = new CreditSale
            {
                SaleCode = "FIA-2026-006",
                CustomerId = c4.Id!,
                CustomerName = c4.Name,
                CustomerPhone = c4.Phone,
                Date = DateTime.UtcNow.AddDays(-4),
                Items = new List<SaleItem>
                {
                    new() { ProductId = aceite.Id!, ProductName = aceite.Name, Quantity = 3, UnitPrice = aceite.SalePrice, Subtotal = 3 * aceite.SalePrice }
                },
                TotalAmount = 3 * aceite.SalePrice,
                InitialPayment = 0,
                RemainingDebt = 3 * aceite.SalePrice,
                Status = "Pending"
            };

            await db.CreditSales.InsertManyAsync(new[] { sale1, sale2, sale3, sale4, sale5, sale6 });

            var pay1 = new Payment
            {
                CustomerId = c1.Id!,
                CustomerName = c1.Name,
                Amount = 15.00m,
                PaymentMethod = "Yape",
                Reference = "Op-492019",
                Date = DateTime.UtcNow.AddDays(-3),
                Notes = "Abono por Yape de Rosa"
            };

            var pay2 = new Payment
            {
                CustomerId = c2.Id!,
                CustomerName = c2.Name,
                Amount = 25.00m,
                PaymentMethod = "Efectivo",
                Reference = "Caja-1",
                Date = DateTime.UtcNow.AddDays(-2),
                Notes = "Dejó 25 soles en efectivo en la tarde"
            };

            var pay3 = new Payment
            {
                CustomerId = c3.Id!,
                CustomerName = c3.Name,
                Amount = 10.00m,
                PaymentMethod = "Plin",
                Reference = "Plin-88219",
                Date = DateTime.UtcNow.AddDays(-1),
                Notes = "Abono parcial de Gladys"
            };

            await db.Payments.InsertManyAsync(new[] { pay1, pay2, pay3 });

            var allSales = await db.CreditSales.Find(_ => true).ToListAsync();
            var allPayments = await db.Payments.Find(_ => true).ToListAsync();

            foreach (var customer in new[] { c1, c2, c3, c4, c5 })
            {
                var custSales = allSales.Where(s => s.CustomerId == customer.Id).ToList();
                var custPayments = allPayments.Where(p => p.CustomerId == customer.Id).ToList();

                var totalSales = custSales.Sum(s => s.TotalAmount);
                var totalPaid = custPayments.Sum(p => p.Amount) + custSales.Sum(s => s.InitialPayment);
                var currentDebt = Math.Max(0, totalSales - totalPaid);

                customer.TotalPurchased = totalSales;
                customer.TotalPaid = totalPaid;
                customer.CurrentDebt = currentDebt;
                customer.LastSaleDate = custSales.OrderByDescending(s => s.Date).FirstOrDefault()?.Date;
                customer.LastPaymentDate = custPayments.OrderByDescending(p => p.Date).FirstOrDefault()?.Date;

                await db.Customers.ReplaceOneAsync(c => c.Id == customer.Id, customer);
            }
        }
    }
}
