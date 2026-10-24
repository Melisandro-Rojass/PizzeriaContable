using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;
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
}
