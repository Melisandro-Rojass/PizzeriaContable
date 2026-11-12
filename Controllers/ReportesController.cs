using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Services;

namespace PizzeriaContable.Controllers;

[Authorize(Roles = "Administrador,Contador,Contabilidad")]
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
        sb.AppendLine(CsvRow("ESTADO DE RESULTADOS INTEGRAL"));
        sb.AppendLine(CsvRow($"Período: {desde.Value:dd/MM/yyyy} al {hasta.Value:dd/MM/yyyy}"));
        sb.AppendLine(CsvRow("Rubro", "Concepto", "Monto (C$)"));
        sb.AppendLine(CsvRow("INGRESOS", "Total Ventas", r.TotalIngresos));
        foreach (var det in r.DetalleIngresos)
        {
            sb.AppendLine(CsvRow("Ingreso Detalle", det.Concepto, det.Monto));
        }
        sb.AppendLine(CsvRow("COSTOS", "Costo de Ventas (Materia Prima)", r.TotalCostosVentas));
        sb.AppendLine(CsvRow("MARGEN BRUTO", "Utilidad Bruta", r.MargenBruto));
        sb.AppendLine(CsvRow("GASTOS", "Total Gastos Operativos", r.TotalGastosOperativos));
        foreach (var det in r.DetalleGastos)
        {
            sb.AppendLine(CsvRow("Gasto Detalle", det.Concepto, det.Monto));
        }
        sb.AppendLine(CsvRow("RESULTADO NETO", "Utilidad Neta del Ejercicio", r.UtilidadOperativa));

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
        sb.AppendLine(CsvRow("BALANCE GENERAL"));
        sb.AppendLine(CsvRow($"Fecha de Corte: {fechaCorte.Value:dd/MM/yyyy}"));
        sb.AppendLine(CsvRow("Grupo", "Código", "Nombre de la Cuenta", "Saldo (C$)"));

        foreach (var item in b.Activos)
        {
            sb.AppendLine(CsvRow("ACTIVO", item.Codigo, item.Nombre, item.Saldo));
        }
        sb.AppendLine(CsvRow("TOTAL ACTIVOS", "", "", b.TotalActivos));

        foreach (var item in b.Pasivos)
        {
            sb.AppendLine(CsvRow("PASIVO", item.Codigo, item.Nombre, item.Saldo));
        }
        sb.AppendLine(CsvRow("TOTAL PASIVOS", "", "", b.TotalPasivos));

        foreach (var item in b.Patrimonio)
        {
            sb.AppendLine(CsvRow("PATRIMONIO", item.Codigo, item.Nombre, item.Saldo));
        }
        sb.AppendLine(CsvRow("PATRIMONIO", "", "Utilidad del Ejercicio", b.UtilidadPeriodo));
        sb.AppendLine(CsvRow("TOTAL PATRIMONIO", "", "", b.TotalPatrimonio + b.UtilidadPeriodo));
        sb.AppendLine(CsvRow("TOTAL PASIVO + PATRIMONIO", "", "", b.TotalPasivos + b.TotalPatrimonio + b.UtilidadPeriodo));

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
        sb.AppendLine(CsvRow("BALANCE DE COMPROBACIÓN"));
        sb.AppendLine(CsvRow($"Período: {desde.Value:dd/MM/yyyy} al {hasta.Value:dd/MM/yyyy}"));
        sb.AppendLine(CsvRow("Código", "Cuenta", "Tipo", "Naturaleza", "Total Debe (C$)", "Total Haber (C$)", "Saldo Deudor (C$)", "Saldo Acreedor (C$)"));

        foreach (var item in lista)
        {
            sb.AppendLine(CsvRow(item.Codigo, item.Nombre, item.TipoCuenta, item.Naturaleza,
                item.TotalDebe, item.TotalHaber, item.SaldoDeudor, item.SaldoAcreedor));
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

    private static string CsvRow(params object?[] values)
    {
        return string.Join(",", values.Select(CsvField));
    }

    private static string CsvField(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        bool isNumber = value is decimal or double or float or int or long;
        string text = value switch
        {
            decimal number => number.ToString("0.00", CultureInfo.InvariantCulture),
            double number => number.ToString("0.00", CultureInfo.InvariantCulture),
            float number => number.ToString("0.00", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };

        if (!isNumber && text.Length > 0 && "=+-@".Contains(text[0]))
        {
            text = "'" + text;
        }

        return isNumber ? text : $"\"{text.Replace("\"", "\"\"")}\"";
    }
}
