using PizzeriaContable.Models;

namespace PizzeriaContable.ViewModels;

public class DashboardViewModel
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public decimal VentasHoy { get; set; }
    public int CantidadVentasHoy { get; set; }

    public decimal VentasMes { get; set; }
    public decimal ComprasPeriodo { get; set; }
    public decimal GastosPeriodo { get; set; }
    public decimal CostoVentasPeriodo { get; set; }

    public decimal MargenBruto => VentasMes - CostoVentasPeriodo;
    public decimal ResultadoEstimado => MargenBruto - GastosPeriodo;

    public decimal EfectivoEnCajaActual { get; set; }
    public bool HayCajaAbierta { get; set; }

    public decimal CuentasPorPagarPendientes { get; set; }
    public decimal CuentasPorCobrarPendientes { get; set; }

    public List<Ingrediente> IngredientesStockBajo { get; set; } = new();
    public List<Venta> UltimasVentas { get; set; } = new();

    public bool BaseDatosVacia => VentasMes == 0 && ComprasPeriodo == 0 && GastosPeriodo == 0;
}
