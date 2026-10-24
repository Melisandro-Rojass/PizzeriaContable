using PizzeriaContable.Models;

namespace PizzeriaContable.Services;

public interface IContabilidadService
{
    Task<AsientoContable> CrearAsientoAutomaticoAsync(
        string concepto,
        string tipoOrigen,
        int? documentoId,
        List<(string codigoCuenta, decimal debe, decimal haber, string? detalle)> apuntes,
        string? usuario);

    bool ValidarAsientoEquilibrado(IEnumerable<AsientoDetalle> detalles);

    Task<List<AsientoContable>> ObtenerLibroDiarioAsync(DateTime? desde, DateTime? hasta);

    Task<(CuentaContable cuenta, List<AsientoDetalle> movimientos, decimal saldoDeudor, decimal saldoAcreedor)> 
        ObtenerLibroMayorAsync(int cuentaId, DateTime? desde, DateTime? hasta);

    Task<List<BalanceComprobacionItem>> ObtenerBalanceComprobacionAsync(DateTime? desde, DateTime? hasta);

    Task<EstadoResultadosViewModel> ObtenerEstadoResultadosAsync(DateTime desde, DateTime hasta);

    Task<BalanceGeneralViewModel> ObtenerBalanceGeneralAsync(DateTime fechaCorte);

    Task InicializarCatalogoCuentasAsync();
}

public class BalanceComprobacionItem
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string TipoCuenta { get; set; } = string.Empty;
    public string Naturaleza { get; set; } = string.Empty;
    public decimal TotalDebe { get; set; }
    public decimal TotalHaber { get; set; }
    public decimal SaldoDeudor { get; set; }
    public decimal SaldoAcreedor { get; set; }
}

public class EstadoResultadosViewModel
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public decimal TotalIngresos { get; set; }
    public decimal TotalCostosVentas { get; set; }
    public decimal MargenBruto => TotalIngresos - TotalCostosVentas;
    public decimal TotalGastosOperativos { get; set; }
    public decimal UtilidadOperativa => MargenBruto - TotalGastosOperativos;
    public List<(string Concepto, decimal Monto)> DetalleIngresos { get; set; } = new();
    public List<(string Concepto, decimal Monto)> DetalleGastos { get; set; } = new();
}

public class BalanceGeneralViewModel
{
    public DateTime FechaCorte { get; set; }
    public decimal TotalActivos { get; set; }
    public decimal TotalPasivos { get; set; }
    public decimal TotalPatrimonio { get; set; }
    public decimal UtilidadPeriodo { get; set; }
    public bool EstaCuadrado => Math.Abs(TotalActivos - (TotalPasivos + TotalPatrimonio + UtilidadPeriodo)) < 0.01m;
    public List<(string Codigo, string Nombre, decimal Saldo)> Activos { get; set; } = new();
    public List<(string Codigo, string Nombre, decimal Saldo)> Pasivos { get; set; } = new();
    public List<(string Codigo, string Nombre, decimal Saldo)> Patrimonio { get; set; } = new();
}
