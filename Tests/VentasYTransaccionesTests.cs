using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;
using Xunit;

namespace PizzeriaContable.Tests;

public class VentasYTransaccionesTests
{
    private ApplicationDbContext CrearContextoEnMemoria()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        // Sembrar datos básicos para pruebas de venta
        context.Configuraciones.Add(new ConfiguracionEmpresa
        {
            NombreComercial = "Pizzería La Tradicional",
            TasaIvaPorcentaje = 15.00m,
            MonedaPrincipal = "NIO",
            SimboloMoneda = "C$"
        });

        // Cuentas contables necesarias
        context.CuentasContables.AddRange(
            new CuentaContable { Codigo = "1.1.01.01", Nombre = "Caja General", TipoCuenta = "Activo", Naturaleza = "Deudora", PermiteMovimiento = true },
            new CuentaContable { Codigo = "1.1.02.01", Nombre = "Banco Cuenta Corriente", TipoCuenta = "Activo", Naturaleza = "Deudora", PermiteMovimiento = true },
            new CuentaContable { Codigo = "4.1.01", Nombre = "Ventas de Pizzas y Alimentos", TipoCuenta = "Ingreso", Naturaleza = "Acreedora", PermiteMovimiento = true },
            new CuentaContable { Codigo = "2.1.03.01", Nombre = "IVA Débito Fiscal", TipoCuenta = "Pasivo", Naturaleza = "Acreedora", PermiteMovimiento = true }
        );

        // Categoría y Productos
        var cat = new Categoria { Nombre = "Pizzas", Descripcion = "Pizzas de prueba", Activo = true };
        context.Categorias.Add(cat);
        context.SaveChanges();

        context.Productos.AddRange(
            new Producto { Id = 1, Codigo = "PIZ-PEP-01", Nombre = "Pizza Pepperoni Grande", Precio = 390.00m, CategoriaId = cat.Id, Activo = true },
            new Producto { Id = 2, Codigo = "BEB-GAS-01", Nombre = "Gaseosa 1.5L", Precio = 58.50m, CategoriaId = cat.Id, Activo = true },
            new Producto { Id = 3, Codigo = "PIZ-DSC-01", Nombre = "Producto Descontinuado", Precio = 100.00m, CategoriaId = cat.Id, Activo = false }
        );

        // Turno de caja abierta
        context.CajasSesiones.Add(new CajaSesion
        {
            Id = 1,
            MontoInicial = 1000m,
            Estado = "Abierta",
            FechaApertura = DateTime.Now,
            UsuarioId = "cajero1"
        });

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task CrearVentaTransaccional_CalculaMontosYPersisteTodoCorrectamente()
    {
        // Arrange
        var context = CrearContextoEnMemoria();
        var invService = new InventarioService(context);
        var contService = new ContabilidadService(context);
        var cajaService = new CajaService(context);
        var ventaService = new VentaService(context, invService, contService, cajaService);

        // Subtotal: 390.00 + 58.50 = 448.50. IVA (15%): 67.28. Total: 515.78
        var venta = new Venta
        {
            ClienteNombre = "Carlos Gutiérrez",
            Descuento = 0
        };

        var detalles = new List<VentaDetalle>
        {
            new VentaDetalle { ProductoId = 1, Cantidad = 1, PrecioUnitario = 390.00m, Descuento = 0 },
            new VentaDetalle { ProductoId = 2, Cantidad = 1, PrecioUnitario = 58.50m, Descuento = 0 }
        };

        decimal subtotal = 448.50m;
        decimal iva = Math.Round(subtotal * 0.15m, 2); // 67.28
        decimal total = subtotal + iva; // 515.78

        var pagos = new List<VentaPago>
        {
            new VentaPago { MetodoPago = "Efectivo", Monto = total }
        };

        // Act
        var ventaResult = await ventaService.CrearVentaTransaccionalAsync(venta, detalles, pagos, "cajero1");

        // Assert
        Assert.NotNull(ventaResult);
        Assert.True(ventaResult.Id > 0);
        Assert.Equal("Completada", ventaResult.Estado);
        Assert.Equal(subtotal, ventaResult.Subtotal);
        Assert.Equal(iva, ventaResult.Impuesto);
        Assert.Equal(total, ventaResult.Total);
        Assert.StartsWith("TCK-", ventaResult.NumeroComprobante);

        // Verificar que los detalles y pagos se persistieron
        var detallesGuardados = await context.VentaDetalles.Where(d => d.VentaId == ventaResult.Id).ToListAsync();
        Assert.Equal(2, detallesGuardados.Count);

        var pagosGuardados = await context.VentaPagos.Where(p => p.VentaId == ventaResult.Id).ToListAsync();
        Assert.Single(pagosGuardados);

        // Verificar que la caja sumó el efectivo
        var caja = await context.CajasSesiones.FindAsync(1);
        Assert.NotNull(caja);
        Assert.Equal(total, caja.TotalVentasEfectivo);

        // Verificar que se generó el asiento contable
        var asiento = await context.AsientosContables
            .Include(a => a.Detalles)
            .FirstOrDefaultAsync(a => a.TipoOrigen == "Venta" && a.DocumentoOrigenId == ventaResult.Id);
        Assert.NotNull(asiento);
        Assert.True(asiento.Contabilizado);

        // Regla partida doble en el asiento de venta
        decimal sumaDebe = asiento.Detalles.Sum(d => d.Debe);
        decimal sumaHaber = asiento.Detalles.Sum(d => d.Haber);
        Assert.Equal(sumaDebe, sumaHaber);

        // Auditoría registrada
        var auditoria = await context.Auditorias.FirstOrDefaultAsync(a => a.Entidad == "Venta" && a.EntidadId == ventaResult.Id.ToString());
        Assert.NotNull(auditoria);
        Assert.Equal("Crear", auditoria.Accion);
    }

    [Fact]
    public async Task CrearVentaTransaccional_PrevieneManipulacionDePrecios_UsaPrecioOficialDelServidor()
    {
        // Arrange
        var context = CrearContextoEnMemoria();
        var invService = new InventarioService(context);
        var contService = new ContabilidadService(context);
        var cajaService = new CajaService(context);
        var ventaService = new VentaService(context, invService, contService, cajaService);

        var venta = new Venta { ClienteNombre = "Cliente Malicioso" };
        
        // Cliente intenta enviar precioUnitario = 1.00 en lugar del precio oficial 390.00
        var detalles = new List<VentaDetalle>
        {
            new VentaDetalle { ProductoId = 1, Cantidad = 1, PrecioUnitario = 1.00m, Descuento = 0 }
        };

        // Si el cliente intenta pagar solo 1.15 basándose en su precio alterado
        var pagosAlterados = new List<VentaPago>
        {
            new VentaPago { MetodoPago = "Efectivo", Monto = 1.15m }
        };

        // Act & Assert: El servidor debe forzar el precio oficial 390.00 y rechazar el pago insuficiente
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ventaService.CrearVentaTransaccionalAsync(venta, detalles, pagosAlterados, "cajero1"));

        Assert.Contains("no coincide", ex.Message);
    }

    [Fact]
    public async Task CrearVentaTransaccional_ProductoInactivo_EsRechazado()
    {
        // Arrange
        var context = CrearContextoEnMemoria();
        var invService = new InventarioService(context);
        var contService = new ContabilidadService(context);
        var cajaService = new CajaService(context);
        var ventaService = new VentaService(context, invService, contService, cajaService);

        var venta = new Venta { ClienteNombre = "Cliente Test" };
        var detalles = new List<VentaDetalle>
        {
            new VentaDetalle { ProductoId = 3, Cantidad = 1, Descuento = 0 } // Producto inactivo
        };
        var pagos = new List<VentaPago>
        {
            new VentaPago { MetodoPago = "Efectivo", Monto = 115m }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ventaService.CrearVentaTransaccionalAsync(venta, detalles, pagos, "cajero1"));

        Assert.Contains("inactivo", ex.Message);
    }

    [Fact]
    public async Task CrearVentaTransaccional_MetodoDePagoInvalido_EsRechazado()
    {
        // Arrange
        var context = CrearContextoEnMemoria();
        var invService = new InventarioService(context);
        var contService = new ContabilidadService(context);
        var cajaService = new CajaService(context);
        var ventaService = new VentaService(context, invService, contService, cajaService);

        var venta = new Venta { ClienteNombre = "Cliente Test" };
        var detalles = new List<VentaDetalle>
        {
            new VentaDetalle { ProductoId = 1, Cantidad = 1, Descuento = 0 }
        };
        var pagos = new List<VentaPago>
        {
            new VentaPago { MetodoPago = "CriptomonedaFicticia", Monto = 448.50m }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            ventaService.CrearVentaTransaccionalAsync(venta, detalles, pagos, "cajero1"));

        Assert.Contains("no es válido", ex.Message);
    }

    [Fact]
    public async Task CrearVentaTransaccional_PagoMixtoTarjetaYEfectivo_ActualizaCorrectamente()
    {
        // Arrange
        var context = CrearContextoEnMemoria();
        var invService = new InventarioService(context);
        var contService = new ContabilidadService(context);
        var cajaService = new CajaService(context);
        var ventaService = new VentaService(context, invService, contService, cajaService);

        var venta = new Venta { ClienteNombre = "Cliente Mixto" };
        var detalles = new List<VentaDetalle>
        {
            new VentaDetalle { ProductoId = 1, Cantidad = 1, Descuento = 0 } // 390.00 + 15% IVA = 448.50
        };

        // Pago mixto: 200 Efectivo + 248.50 Tarjeta
        var pagos = new List<VentaPago>
        {
            new VentaPago { MetodoPago = "Efectivo", Monto = 200.00m },
            new VentaPago { MetodoPago = "Tarjeta", Monto = 248.50m, Referencia = "AUTH-987654" }
        };

        // Act
        var ventaResult = await ventaService.CrearVentaTransaccionalAsync(venta, detalles, pagos, "cajero1");

        // Assert
        Assert.Equal(448.50m, ventaResult.Total);
        var caja = await context.CajasSesiones.FindAsync(1);
        Assert.NotNull(caja);
        Assert.Equal(200.00m, caja.TotalVentasEfectivo);
        Assert.Equal(248.50m, caja.TotalVentasTarjeta);
    }

    [Fact]
    public async Task ContabilidadService_CrearAsientoAutomatico_ParticipaEnTransaccionSinLanzarExcepcion()
    {
        // Arrange
        var context = CrearContextoEnMemoria();
        var contService = new ContabilidadService(context);

        var apuntes = new List<(string codigoCuenta, decimal debe, decimal haber, string? detalle)>
        {
            ("1.1.01.01", 100m, 0m, "Ingreso de prueba"),
            ("4.1.01", 0m, 100m, "Venta de prueba")
        };

        // Act & Assert: Crear asiento debe ejecutarse sin excepción (y si existiera una transacción externa, no debe abrir anidada)
        var asiento = await contService.CrearAsientoAutomaticoAsync(
            "Asiento Transaccional de Prueba",
            "Venta",
            999,
            apuntes,
            "admin");

        Assert.NotNull(asiento);
        Assert.True(asiento.Id > 0);
        Assert.StartsWith("AS-", asiento.NumeroAsiento);
    }

    [Fact]
    public async Task CrearVentaTransaccional_FalloEnPago_NoDejaRegistrosHuerfanos()
    {
        // Arrange
        var context = CrearContextoEnMemoria();
        var invService = new InventarioService(context);
        var contService = new ContabilidadService(context);
        var cajaService = new CajaService(context);
        var ventaService = new VentaService(context, invService, contService, cajaService);

        int totalVentasInicial = await context.Ventas.CountAsync();
        int totalDetallesInicial = await context.VentaDetalles.CountAsync();

        var venta = new Venta { ClienteNombre = "Cliente Fallido" };
        var detalles = new List<VentaDetalle>
        {
            new VentaDetalle { ProductoId = 1, Cantidad = 1, Descuento = 0 }
        };
        // Pago incorrecto que provocará excepción de validación
        var pagos = new List<VentaPago>
        {
            new VentaPago { MetodoPago = "Efectivo", Monto = 50.00m }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ventaService.CrearVentaTransaccionalAsync(venta, detalles, pagos, "cajero1"));

        // Verificar que no quedaron ventas ni detalles huérfanos
        int totalVentasFinal = await context.Ventas.CountAsync();
        int totalDetallesFinal = await context.VentaDetalles.CountAsync();

        Assert.Equal(totalVentasInicial, totalVentasFinal);
        Assert.Equal(totalDetallesInicial, totalDetallesFinal);
    }
}
