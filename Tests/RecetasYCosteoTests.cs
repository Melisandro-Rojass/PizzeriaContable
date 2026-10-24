using PizzeriaContable.Models;
using Xunit;

namespace PizzeriaContable.Tests;

public class RecetasYCosteoTests
{
    [Fact]
    public void CalcularCostoTeorico_ConIngredientesVariados_CalculaCostoTotalExacto()
    {
        // Arrange: Pizza con harina, queso y salsa
        var ingHarina = new Ingrediente { Id = 1, Nombre = "Harina", CostoUnitario = 30.00m, UnidadMedida = "kg" };
        var ingQueso = new Ingrediente { Id = 2, Nombre = "Queso Mozzarella", CostoUnitario = 160.00m, UnidadMedida = "kg" };
        var ingSalsa = new Ingrediente { Id = 3, Nombre = "Salsa Tomate", CostoUnitario = 50.00m, UnidadMedida = "kg" };

        var receta = new Receta
        {
            Id = 1,
            ProductoId = 10,
            Rendimiento = 1,
            MermaPrevistaPorcentaje = 0m,
            Detalles = new List<RecetaDetalle>
            {
                new RecetaDetalle { Ingrediente = ingHarina, Cantidad = 0.250m, UnidadMedida = "kg" }, // 0.25 * 30 = 7.50
                new RecetaDetalle { Ingrediente = ingQueso, Cantidad = 0.200m, UnidadMedida = "kg" },   // 0.20 * 160 = 32.00
                new RecetaDetalle { Ingrediente = ingSalsa, Cantidad = 0.100m, UnidadMedida = "kg" }    // 0.10 * 50 = 5.00
            }
        };

        // Act
        decimal costoTeorico = receta.Detalles.Sum(d => d.Cantidad * (d.Ingrediente?.CostoUnitario ?? 0));

        // Assert: 7.50 + 32.00 + 5.00 = 44.50
        Assert.Equal(44.50m, costoTeorico);
    }

    [Fact]
    public void CalcularMargenBruto_ConPrecioVentaYCostos_CalculaPorcentajeCorrecto()
    {
        // Arrange
        decimal precioVenta = 250.00m;
        decimal costoReceta = 50.00m;

        // Act
        decimal utilidadBruta = precioVenta - costoReceta;
        decimal porcentajeMargen = Math.Round((utilidadBruta / precioVenta) * 100m, 2);

        // Assert: (200 / 250) * 100 = 80.00%
        Assert.Equal(200.00m, utilidadBruta);
        Assert.Equal(80.00m, porcentajeMargen);
    }
}
