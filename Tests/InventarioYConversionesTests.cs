using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Services;
using Xunit;

namespace PizzeriaContable.Tests;

public class InventarioYConversionesTests
{
    private InventarioService CrearServicio()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        return new InventarioService(context);
    }

    [Fact]
    public void ConvertirUnidades_GramosAKilogramos_RetornaValorCorrecto()
    {
        // Arrange
        var service = CrearServicio();

        // Act
        decimal resultado = service.ConvertirUnidades(250m, "g", "kg");

        // Assert: 250 gramos = 0.250 kg
        Assert.Equal(0.25m, resultado);
    }

    [Fact]
    public void ConvertirUnidades_KilogramosAGramos_RetornaValorCorrecto()
    {
        // Arrange
        var service = CrearServicio();

        // Act
        decimal resultado = service.ConvertirUnidades(1.5m, "kg", "g");

        // Assert: 1.5 kg = 1500 g
        Assert.Equal(1500m, resultado);
    }

    [Fact]
    public void ConvertirUnidades_MililitrosALitros_RetornaValorCorrecto()
    {
        // Arrange
        var service = CrearServicio();

        // Act
        decimal resultado = service.ConvertirUnidades(750m, "ml", "L");

        // Assert: 750 ml = 0.75 L
        Assert.Equal(0.75m, resultado);
    }

    [Fact]
    public void ConvertirUnidades_MismaUnidad_RetornaMismaCantidad()
    {
        // Arrange
        var service = CrearServicio();

        // Act
        decimal resultado = service.ConvertirUnidades(10m, "unidad", "unidad");

        // Assert
        Assert.Equal(10m, resultado);
    }

    [Fact]
    public void ConvertirUnidades_UnidadesIncompatibles_LanzaExcepcion()
    {
        // Arrange
        var service = CrearServicio();

        // Act & Assert: Convertir kg a litros sin densidad debe fallar explícitamente
        Assert.Throws<InvalidOperationException>(() =>
            service.ConvertirUnidades(5m, "kg", "ml"));
    }
}
