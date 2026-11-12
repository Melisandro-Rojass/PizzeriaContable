namespace PizzeriaContable.ViewModels;

public class ResultadoCargaDto
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int ProductosRegistrados { get; set; }
    public int IngredientesRegistrados { get; set; }
    public int ProveedoresRegistrados { get; set; }
    public int RecetasRegistradas { get; set; }
    public int VentasRealesCargadas { get; set; }
    public int VentasEstimadasCargadas { get; set; }
    public int TotalVentasCargadas => VentasRealesCargadas + VentasEstimadasCargadas;
    public decimal MontoTotalVentasReales { get; set; }
    public decimal MontoTotalVentasEstimadas { get; set; }
    public decimal MontoTotalVentas => MontoTotalVentasReales + MontoTotalVentasEstimadas;
    public int ComprasCargadas { get; set; }
    public decimal MontoTotalCompras { get; set; }
    public int GastosCargados { get; set; }
    public decimal MontoTotalGastos { get; set; }
    public int AsientosContablesGenerados { get; set; }
    public bool BalanzaCuadrada { get; set; }
    public decimal TotalDebeBalanza { get; set; }
    public decimal TotalHaberBalanza { get; set; }
    public decimal DiferenciaBalanza => Math.Abs(TotalDebeBalanza - TotalHaberBalanza);
    public DateTime FechaEjecucion { get; set; } = DateTime.Now;
}

public class ResumenTrimestreDto
{
    public int VentasJulio { get; set; }
    public decimal TotalJulio { get; set; }
    public int VentasAgosto { get; set; }
    public decimal TotalAgosto { get; set; }
    public int VentasSeptiembre { get; set; }
    public decimal TotalSeptiembre { get; set; }
    public decimal TotalTrimestre => TotalJulio + TotalAgosto + TotalSeptiembre;

    public decimal ComprasJulio { get; set; }
    public decimal ComprasAgosto { get; set; }
    public decimal ComprasSeptiembre { get; set; }
    public decimal TotalComprasTrimestre => ComprasJulio + ComprasAgosto + ComprasSeptiembre;

    public decimal GastosJulio { get; set; }
    public decimal GastosAgosto { get; set; }
    public decimal GastosSeptiembre { get; set; }
    public decimal TotalGastosTrimestre => GastosJulio + GastosAgosto + GastosSeptiembre;

    public int TotalVentasReales { get; set; }
    public decimal MontoVentasReales { get; set; }
    public int TotalVentasEstimadas { get; set; }
    public decimal MontoVentasEstimadas { get; set; }

    public bool YaCargado { get; set; }
}

