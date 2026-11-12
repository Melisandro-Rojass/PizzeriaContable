using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;

namespace PizzeriaContable.Tests;

public class RegistrosImportadosQ3Tests
{
    [Fact]
    public async Task EstadoResultados_IncluyeVentasYGastosImportadosEnElPeriodo()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Ventas.AddRange(
            new Venta
            {
                NumeroComprobante = "CSV-Q3-2026-20260701",
                Fecha = new DateTime(2026, 7, 1),
                Subtotal = 100m,
                Impuesto = 15m,
                Total = 115m,
                Estado = "Completada",
                Observaciones = $"{Venta.CsvQ3ObservacionesPrefix} | Tipo: Real"
            },
            new Venta
            {
                NumeroComprobante = "CSV-Q3-2026-20260630",
                Fecha = new DateTime(2026, 6, 30),
                Total = 100m,
                Estado = "Completada",
                Observaciones = $"{Venta.CsvQ3ObservacionesPrefix} | Tipo: Real"
            });
        context.Gastos.Add(new Gasto
        {
            CategoriaGasto = "Otros",
            Descripcion = "Gastos de caja chica - resumen diario",
            FechaGasto = new DateTime(2026, 7, 1),
            FechaPago = new DateTime(2026, 7, 1),
            Monto = 14988m,
            MetodoPago = "Efectivo",
            Estado = "Pagado",
            Observaciones = $"{Gasto.CsvQ3ObservacionesPrefix} | Caja chica"
        });
        await context.SaveChangesAsync();

        var service = new ContabilidadService(context);
        var report = await service.ObtenerEstadoResultadosAsync(
            new DateTime(2026, 7, 1),
            new DateTime(2026, 9, 30));

        Assert.Equal(100m, report.TotalIngresos);
        Assert.Equal(14988m, report.TotalGastosOperativos);
        Assert.Equal(100m, report.IngresosImportados);
        Assert.Equal(14988m, report.GastosImportados);
        Assert.Contains(report.DetalleIngresos, item => item.Monto == 100m);
        Assert.Contains(report.DetalleGastos, item => item.Monto == 14988m);
    }

    [Fact]
    public async Task EstadoResultados_NoDuplicaRegistrosImportadosContabilizados()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var caja = new CuentaContable
        {
            Codigo = "1.1.01.01",
            Nombre = "Caja General",
            TipoCuenta = "Activo",
            Naturaleza = "Deudora"
        };
        var ingresos = new CuentaContable
        {
            Codigo = "4.1.01",
            Nombre = "Ingresos por ventas",
            TipoCuenta = "Ingreso",
            Naturaleza = "Acreedora"
        };
        var gastos = new CuentaContable
        {
            Codigo = "6.1.99",
            Nombre = "Otros gastos",
            TipoCuenta = "Gasto",
            Naturaleza = "Deudora"
        };
        var iva = new CuentaContable
        {
            Codigo = "2.1.03.01",
            Nombre = "IVA por pagar",
            TipoCuenta = "Pasivo",
            Naturaleza = "Acreedora"
        };
        context.CuentasContables.AddRange(caja, ingresos, gastos, iva);
        await context.SaveChangesAsync();

        var venta = new Venta
        {
            NumeroComprobante = "CSV-Q3-2026-20260701",
            Fecha = new DateTime(2026, 7, 1),
            Subtotal = 100m,
            Impuesto = 15m,
            Total = 115m,
            Estado = "Completada",
            Observaciones = $"{Venta.CsvQ3ObservacionesPrefix} | Tipo: Real"
        };
        var gasto = new Gasto
        {
            CategoriaGasto = "Otros",
            Descripcion = "Gastos de caja chica - resumen diario",
            FechaGasto = new DateTime(2026, 7, 1),
            Monto = 14988m,
            MetodoPago = "Efectivo",
            Estado = "Pagado",
            Observaciones = $"{Gasto.CsvQ3ObservacionesPrefix} | Caja chica"
        };
        context.Ventas.Add(venta);
        context.Gastos.Add(gasto);
        await context.SaveChangesAsync();

        context.AsientosContables.AddRange(
            new AsientoContable
            {
                NumeroAsiento = "AS-2026-00001",
                Fecha = new DateTime(2026, 7, 1),
                Concepto = "Venta diaria importada Q3 2026",
                TipoOrigen = "Venta",
                DocumentoOrigenId = venta.Id,
                Detalles =
                {
                    new AsientoDetalle { CuentaContableId = caja.Id, Debe = 115m },
                    new AsientoDetalle { CuentaContableId = ingresos.Id, Haber = 100m },
                    new AsientoDetalle { CuentaContableId = iva.Id, Haber = 15m }
                }
            },
            new AsientoContable
            {
                NumeroAsiento = "AS-2026-00002",
                Fecha = new DateTime(2026, 7, 1),
                Concepto = "Gasto diario importado Q3 2026",
                TipoOrigen = "Gasto",
                DocumentoOrigenId = gasto.Id,
                Detalles =
                {
                    new AsientoDetalle { CuentaContableId = gastos.Id, Debe = 14988m },
                    new AsientoDetalle { CuentaContableId = caja.Id, Haber = 14988m }
                }
            });
        await context.SaveChangesAsync();

        var report = await new ContabilidadService(context).ObtenerEstadoResultadosAsync(
            new DateTime(2026, 7, 1),
            new DateTime(2026, 9, 30));

        Assert.Equal(100m, report.TotalIngresos);
        Assert.Equal(14988m, report.TotalGastosOperativos);
        Assert.Equal(0m, report.IngresosImportados);
        Assert.Equal(0m, report.GastosImportados);
    }

    [Fact]
    public void RegistrosQ3Importados_SeIdentificanPorPrefijoDeObservacion()
    {
        var venta = new Venta
        {
            Observaciones = $"{Venta.CsvQ3ObservacionesPrefix} | Tipo: Estimado"
        };
        var gasto = new Gasto
        {
            Observaciones = $"{Gasto.CsvQ3ObservacionesPrefix} | Caja chica"
        };

        Assert.True(venta.EsRegistroConsolidadoImportado);
        Assert.True(gasto.EsRegistroConsolidadoImportado);
        Assert.False(new Venta { Observaciones = "Venta POS normal" }.EsRegistroConsolidadoImportado);
        Assert.False(new Gasto { Observaciones = "Gasto normal" }.EsRegistroConsolidadoImportado);
    }
}
