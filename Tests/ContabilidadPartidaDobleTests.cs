using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;
using PizzeriaContable.ViewModels;
using Xunit;

namespace PizzeriaContable.Tests;

public class ContabilidadPartidaDobleTests
{
    private ContabilidadService CrearServicio(out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        context = new ApplicationDbContext(options);
        return new ContabilidadService(context);
    }

    [Fact]
    public void ValidarAsientoEquilibrado_ConSumasIguales_RetornaTrue()
    {
        // Arrange
        var service = CrearServicio(out _);
        var detalles = new List<AsientoDetalle>
        {
            new AsientoDetalle { Debe = 1500.00m, Haber = 0m },
            new AsientoDetalle { Debe = 0m, Haber = 1304.35m },
            new AsientoDetalle { Debe = 0m, Haber = 195.65m }
        };

        // Act
        bool esValido = service.ValidarAsientoEquilibrado(detalles);

        // Assert
        Assert.True(esValido);
    }

    [Fact]
    public void ValidarAsientoEquilibrado_ConSumasDesiguales_RetornaFalse()
    {
        // Arrange
        var service = CrearServicio(out _);
        var detalles = new List<AsientoDetalle>
        {
            new AsientoDetalle { Debe = 1500.00m, Haber = 0m },
            new AsientoDetalle { Debe = 0m, Haber = 1400.00m }
        };

        // Act
        bool esValido = service.ValidarAsientoEquilibrado(detalles);

        // Assert
        Assert.False(esValido);
    }

    [Fact]
    public void ValidarAsientoEquilibrado_ConMenosDeDosLineas_RetornaFalse()
    {
        // Arrange
        var service = CrearServicio(out _);
        var detalles = new List<AsientoDetalle>
        {
            new AsientoDetalle { Debe = 1000m, Haber = 0m }
        };

        // Act
        bool esValido = service.ValidarAsientoEquilibrado(detalles);

        // Assert
        Assert.False(esValido);
    }

    [Fact]
    public async Task CrearAsientoAutomaticoAsync_PrevieneDobleContabilizacion_ParaMismoDocumento()
    {
        // Arrange
        var service = CrearServicio(out var context);
        await service.InicializarCatalogoCuentasAsync();

        var apuntes = new List<(string codigo, decimal debe, decimal haber, string? detalle)>
        {
            ("1.1.01.01", 1000m, 0m, "Cobro venta"),
            ("4.1.01", 0m, 1000m, "Ingreso pizza")
        };

        // Act 1: Primer asiento debe funcionar correctamente
        var asiento1 = await service.CrearAsientoAutomaticoAsync("Venta #1", "Venta", 999, apuntes, "Admin");
        Assert.NotNull(asiento1);

        // Act 2 & Assert: Intentar contabilizar el mismo documento origen nuevamente debe lanzar excepción
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CrearAsientoAutomaticoAsync("Venta #1 duplicada", "Venta", 999, apuntes, "Admin"));
    }

    [Fact]
    public async Task CrearAsientoAutomaticoAsync_ConImportesNegativos_LanzaExcepcion()
    {
        // Arrange
        var service = CrearServicio(out _);
        await service.InicializarCatalogoCuentasAsync();

        var apuntes = new List<(string codigo, decimal debe, decimal haber, string? detalle)>
        {
            ("1.1.01.01", -500m, 0m, "Importe negativo inválido"),
            ("4.1.01", 0m, -500m, "Importe negativo inválido")
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CrearAsientoAutomaticoAsync("Asiento inválido", "Manual", null, apuntes, "Admin"));
    }

    [Fact]
    public async Task CrearAsientoAutomaticoAsync_ConImportesSimultaneosEnDebeYHaber_LanzaExcepcion()
    {
        // Arrange
        var service = CrearServicio(out _);
        await service.InicializarCatalogoCuentasAsync();

        var apuntes = new List<(string codigo, decimal debe, decimal haber, string? detalle)>
        {
            ("1.1.01.01", 500m, 200m, "Importes simultáneos prohibidos"),
            ("4.1.01", 0m, 300m, "Contrapartida")
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CrearAsientoAutomaticoAsync("Asiento simultáneo", "Manual", null, apuntes, "Admin"));
    }

    [Fact]
    public async Task RegistrarVentaContadoEjemploAsync_GeneraAsientoDidacticoC1000YCuadraCuentasT()
    {
        // Arrange
        var service = CrearServicio(out var context);
        await service.InicializarCatalogoCuentasAsync();

        // Act
        var asiento = await service.RegistrarVentaContadoEjemploAsync("Docente");

        // Assert
        Assert.NotNull(asiento);
        Assert.True(asiento.Contabilizado);
        Assert.False(asiento.Anulado);
        Assert.Equal(2, asiento.Detalles.Count);

        var lineaCaja = asiento.Detalles.First(d => d.Debe > 0);
        var lineaIngresos = asiento.Detalles.First(d => d.Haber > 0);

        Assert.Equal(1000.00m, lineaCaja.Debe);
        Assert.Equal(0.00m, lineaCaja.Haber);
        Assert.Equal(1000.00m, lineaIngresos.Haber);
        Assert.Equal(0.00m, lineaIngresos.Debe);

        // Verificar en Cuentas T
        var resumenT = await service.ObtenerResumenCuentasTAsync(null, null, null, null);
        Assert.True(resumenT.PartidaDobleEquilibrada);
        Assert.Equal(1000.00m, resumenT.TotalDebeGeneral);
        Assert.Equal(1000.00m, resumenT.TotalHaberGeneral);

        var cajaT = resumenT.Cuentas.First(c => c.Codigo == "1.1.01.01");
        Assert.Equal(1000.00m, cajaT.TotalDebe);
        Assert.Equal(0.00m, cajaT.TotalHaber);
        Assert.Equal(1000.00m, cajaT.SaldoFinal); // Saldo Deudor de Activo

        var ingresosT = resumenT.Cuentas.First(c => c.Codigo == "4.1.01");
        Assert.Equal(0.00m, ingresosT.TotalDebe);
        Assert.Equal(1000.00m, ingresosT.TotalHaber);
        Assert.Equal(1000.00m, ingresosT.SaldoFinal); // Saldo Acreedor de Ingreso
    }

    [Fact]
    public async Task AnularAsientoAsync_MarcaAsientoComoAnuladoYPreservaTrazabilidad()
    {
        // Arrange
        var service = CrearServicio(out var context);
        await service.InicializarCatalogoCuentasAsync();

        var asiento = await service.RegistrarVentaContadoEjemploAsync("UsuarioPrueba");
        int asientoId = asiento.Id;

        // Act
        bool anulado = await service.AnularAsientoAsync(asientoId, "Error en comprobante didáctico", "Admin");

        // Assert
        Assert.True(anulado);
        var asientoDb = await context.AsientosContables.FindAsync(asientoId);
        Assert.NotNull(asientoDb);
        Assert.True(asientoDb.Anulado);
        Assert.NotNull(asientoDb.FechaAnulacion);
        Assert.Equal("Error en comprobante didáctico", asientoDb.MotivoAnulacion);

        // Una vez anulado, ya no debe sumar en el balance ni en Cuentas T
        var resumenT = await service.ObtenerResumenCuentasTAsync(null, null, null, null);
        Assert.Equal(0m, resumenT.TotalDebeGeneral);
        Assert.Equal(0m, resumenT.TotalHaberGeneral);
    }
}
