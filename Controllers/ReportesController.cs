using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Services;

namespace PizzeriaContable.Controllers;

public class ReportesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IContabilidadService _contabilidadService;

    public ReportesController(
        ApplicationDbContext context,
        IContabilidadService contabilidadService)
    {
        _context = context;
        _contabilidadService = contabilidadService;
    }

    // GET: Reportes/EstadoResultados
    public async Task<IActionResult> EstadoResultados(DateTime? desde, DateTime? hasta)
    {
        desde ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        hasta ??= DateTime.Today;

        var reporte = await _contabilidadService.ObtenerEstadoResultadosAsync(desde.Value, hasta.Value);

        ViewBag.Desde = desde.Value.ToString("yyyy-MM-dd");
        ViewBag.Hasta = hasta.Value.ToString("yyyy-MM-dd");

        return View(reporte);
    }

    // GET: Reportes/ExportarEstadoResultadosCsv
    public async Task<IActionResult> ExportarEstadoResultadosCsv(DateTime? desde, DateTime? hasta)
    {
        desde ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        hasta ??= DateTime.Today;

        var r = await _contabilidadService.ObtenerEstadoResultadosAsync(desde.Value, hasta.Value);

        var sb = new StringBuilder();
        sb.AppendLine("ESTADO DE RESULTADOS INTEGRAL");
        sb.AppendLine($"Período: {desde.Value:dd/MM/yyyy} al {hasta.Value:dd/MM/yyyy}");
        sb.AppendLine("Rubro,Concepto,Monto (C$)");
        sb.AppendLine($"INGRESOS,Total Ventas,{r.TotalIngresos:F2}");
        foreach (var det in r.DetalleIngresos)
        {
            sb.AppendLine($"Ingreso Detalle,{det.Concepto},{det.Monto:F2}");
        }
        sb.AppendLine($"COSTOS,Costo de Ventas (Materia Prima),{r.TotalCostosVentas:F2}");
        sb.AppendLine($"MARGEN BRUTO,Utilidad Bruta,{r.MargenBruto:F2}");
        sb.AppendLine($"GASTOS,Total Gastos Operativos,{r.TotalGastosOperativos:F2}");
        foreach (var det in r.DetalleGastos)
        {
            sb.AppendLine($"Gasto Detalle,{det.Concepto},{det.Monto:F2}");
        }
        sb.AppendLine($"RESULTADO NETO,Utilidad Neta del Ejercicio,{r.UtilidadOperativa:F2}");

        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"EstadoResultados_{desde.Value:yyyyMMdd}_{hasta.Value:yyyyMMdd}.csv");
    }

    // GET: Reportes/BalanceGeneral
    public async Task<IActionResult> BalanceGeneral(DateTime? fechaCorte)
    {
        fechaCorte ??= DateTime.Today;

        var balance = await _contabilidadService.ObtenerBalanceGeneralAsync(fechaCorte.Value);
        ViewBag.FechaCorte = fechaCorte.Value.ToString("yyyy-MM-dd");

        return View(balance);
    }

    // GET: Reportes/ExportarBalanceGeneralCsv
    public async Task<IActionResult> ExportarBalanceGeneralCsv(DateTime? fechaCorte)
    {
        fechaCorte ??= DateTime.Today;

        var b = await _contabilidadService.ObtenerBalanceGeneralAsync(fechaCorte.Value);

        var sb = new StringBuilder();
        sb.AppendLine("BALANCE GENERAL");
        sb.AppendLine($"Fecha de Corte: {fechaCorte.Value:dd/MM/yyyy}");
        sb.AppendLine("Grupo,Código,Nombre de la Cuenta,Saldo (C$)");

        foreach (var item in b.Activos)
        {
            sb.AppendLine($"ACTIVO,{item.Codigo},{item.Nombre},{item.Saldo:F2}");
        }
        sb.AppendLine($"TOTAL ACTIVOS,,,{b.TotalActivos:F2}");

        foreach (var item in b.Pasivos)
        {
            sb.AppendLine($"PASIVO,{item.Codigo},{item.Nombre},{item.Saldo:F2}");
        }
        sb.AppendLine($"TOTAL PASIVOS,,,{b.TotalPasivos:F2}");

        foreach (var item in b.Patrimonio)
        {
            sb.AppendLine($"PATRIMONIO,{item.Codigo},{item.Nombre},{item.Saldo:F2}");
        }
        sb.AppendLine($"PATRIMONIO,,Utilidad del Ejercicio,{b.UtilidadPeriodo:F2}");
        sb.AppendLine($"TOTAL PATRIMONIO,,,{b.TotalPatrimonio + b.UtilidadPeriodo:F2}");
        sb.AppendLine($"TOTAL PASIVO + PATRIMONIO,,,{b.TotalPasivos + b.TotalPatrimonio + b.UtilidadPeriodo:F2}");

        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"BalanceGeneral_{fechaCorte.Value:yyyyMMdd}.csv");
    }

    // GET: Reportes/BalanceComprobacion
    public async Task<IActionResult> BalanceComprobacion(DateTime? desde, DateTime? hasta)
    {
        desde ??= new DateTime(DateTime.Today.Year, 1, 1);
        hasta ??= DateTime.Today;

        var lista = await _contabilidadService.ObtenerBalanceComprobacionAsync(desde.Value, hasta.Value);

        ViewBag.Desde = desde.Value.ToString("yyyy-MM-dd");
        ViewBag.Hasta = hasta.Value.ToString("yyyy-MM-dd");

        ViewBag.TotalDebe = lista.Sum(x => x.TotalDebe);
        ViewBag.TotalHaber = lista.Sum(x => x.TotalHaber);
        ViewBag.TotalSaldoDeudor = lista.Sum(x => x.SaldoDeudor);
        ViewBag.TotalSaldoAcreedor = lista.Sum(x => x.SaldoAcreedor);

        return View(lista);
    }

    // GET: Reportes/ExportarBalanceComprobacionCsv
    public async Task<IActionResult> ExportarBalanceComprobacionCsv(DateTime? desde, DateTime? hasta)
    {
        desde ??= new DateTime(DateTime.Today.Year, 1, 1);
        hasta ??= DateTime.Today;

        var lista = await _contabilidadService.ObtenerBalanceComprobacionAsync(desde.Value, hasta.Value);

        var sb = new StringBuilder();
        sb.AppendLine("BALANCE DE COMPROBACIÓN");
        sb.AppendLine($"Período: {desde.Value:dd/MM/yyyy} al {hasta.Value:dd/MM/yyyy}");
        sb.AppendLine("Código,Cuenta,Tipo,Naturaleza,Total Debe (C$),Total Haber (C$),Saldo Deudor (C$),Saldo Acreedor (C$)");

        foreach (var item in lista)
        {
            sb.AppendLine($"{item.Codigo},{item.Nombre},{item.TipoCuenta},{item.Naturaleza},{item.TotalDebe:F2},{item.TotalHaber:F2},{item.SaldoDeudor:F2},{item.SaldoAcreedor:F2}");
        }

        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"BalanceComprobacion_{desde.Value:yyyyMMdd}_{hasta.Value:yyyyMMdd}.csv");
    }

    // GET: Reportes/KardexGeneral
    public async Task<IActionResult> KardexGeneral(DateTime? desde, DateTime? hasta)
    {
        desde ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        hasta ??= DateTime.Today;

        var movimientos = await _context.MovimientosInventario
            .Include(m => m.Ingrediente)
            .Where(m => m.Fecha.Date >= desde.Value.Date && m.Fecha.Date <= hasta.Value.Date)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();

        ViewBag.Desde = desde.Value.ToString("yyyy-MM-dd");
        ViewBag.Hasta = hasta.Value.ToString("yyyy-MM-dd");

        return View(movimientos);
    }
}
