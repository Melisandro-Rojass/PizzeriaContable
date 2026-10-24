using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Services;
using Xunit;

namespace PizzeriaContable.Tests;

public class CajaYArqueoTests
{
    private CajaService CrearServicio(out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        context = new ApplicationDbContext(options);
        return new CajaService(context);
    }

    [Fact]
    public async Task ArqueoCaja_ExcluyePagosConTarjeta_DelEfectivoFisicoEsperado()
    {
        // Arrange: Abrir caja con C$ 1,000 de fondo
        var service = CrearServicio(out var context);
        var sesion = await service.AbrirCajaAsync(1000m, "cajero1");

        // Act: Registrar una venta en efectivo de C$ 500 y una venta con tarjeta de C$ 800
        await service.ActualizarTotalesPorVentaAsync(sesion.Id, "Efectivo", 500m);
        await service.ActualizarTotalesPorVentaAsync(sesion.Id, "Tarjeta", 800m);

        // Recargar sesión
        var sesionActualizada = await context.CajasSesiones.FindAsync(sesion.Id);

        // Assert:
        // Total Efectivo: 500
        // Total Tarjeta: 800
        // Efectivo Físico Esperado en gaveta: 1000 (fondo) + 500 (venta efectivo) = 1500 (NO 2300)
        Assert.NotNull(sesionActualizada);
        Assert.Equal(500m, sesionActualizada.TotalVentasEfectivo);
        Assert.Equal(800m, sesionActualizada.TotalVentasTarjeta);
        Assert.Equal(1500m, sesionActualizada.EfectivoEsperado);
    }

    [Fact]
    public async Task CerrarCaja_ConFaltante_CalculaDiferenciaNegativa()
    {
        // Arrange: Abrir caja con 1,000, venta efectivo 400 => esperado 1,400
        var service = CrearServicio(out _);
        var sesion = await service.AbrirCajaAsync(1000m, "cajero1");
        await service.ActualizarTotalesPorVentaAsync(sesion.Id, "Efectivo", 400m);

        // Act: En el arqueo físico solo hay C$ 1,350 (faltante de C$ 50)
        var sesionCerrada = await service.CerrarCajaAsync(sesion.Id, 1350m, "Faltaron C$ 50 en arqueo", "cajero1");

        // Assert
        Assert.Equal("Cerrada", sesionCerrada.Estado);
        Assert.Equal(1400m, sesionCerrada.EfectivoEsperado);
        Assert.Equal(1350m, sesionCerrada.EfectivoContado);
        Assert.Equal(-50m, sesionCerrada.Diferencia);
    }
}
