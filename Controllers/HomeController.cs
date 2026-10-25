using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;
using PizzeriaContable.ViewModels;

namespace PizzeriaContable.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICajaService _cajaService;
    private readonly IInventarioService _inventarioService;

    public HomeController(
        ApplicationDbContext context,
        ICajaService cajaService,
        IInventarioService inventarioService)
    {
        _context = context;
        _cajaService = cajaService;
        _inventarioService = inventarioService;
    }

    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
    {
        DateTime fechaInicio = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTime fechaFin = hasta ?? DateTime.Today;

        // Ventas hoy
        var ventasHoyQuery = _context.Ventas
            .Where(v => v.Fecha.Date == DateTime.Today && v.Estado == "Completada");
        decimal ventasHoy = await ventasHoyQuery.SumAsync(v => v.Total);
        int cantVentasHoy = await ventasHoyQuery.CountAsync();

        // Ventas período
        decimal ventasMes = await _context.Ventas
            .Where(v => v.Fecha.Date >= fechaInicio.Date && v.Fecha.Date <= fechaFin.Date && v.Estado == "Completada")
            .SumAsync(v => v.Total);

        // Compras período
        decimal comprasPeriodo = await _context.Compras
            .Where(c => c.FechaCompra.Date >= fechaInicio.Date && c.FechaCompra.Date <= fechaFin.Date && c.Estado != "Anulada")
            .SumAsync(c => c.Total);

        // Gastos período
        decimal gastosPeriodo = await _context.Gastos
            .Where(g => g.FechaGasto.Date >= fechaInicio.Date && g.FechaGasto.Date <= fechaFin.Date && g.Estado != "Anulado")
            .SumAsync(g => g.Monto);

        // Costo de ventas (salidas de inventario por venta en el período)
        decimal costoVentas = await _context.MovimientosInventario
            .Where(m => m.Fecha.Date >= fechaInicio.Date && m.Fecha.Date <= fechaFin.Date && m.TipoMovimiento == "SalidaVenta")
            .SumAsync(m => m.Total);

        // Caja activa
        var cajaActiva = await _cajaService.ObtenerSesionActivaAsync(User.Identity?.Name);

        // Cuentas por pagar pendientes
        decimal cxp = await _context.Compras
            .Where(c => c.EsCredito && c.SaldoPendiente > 0 && c.Estado != "Anulada")
            .SumAsync(c => c.SaldoPendiente);

        // Ingredientes con stock bajo
        var stockBajo = await _inventarioService.ObtenerAlertasStockBajoAsync();

        // Últimas ventas
        var ultimasVentas = await _context.Ventas
            .Include(v => v.Detalles)
            .OrderByDescending(v => v.Fecha)
            .Take(8)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            VentasHoy = ventasHoy,
            CantidadVentasHoy = cantVentasHoy,
            VentasMes = ventasMes,
            ComprasPeriodo = comprasPeriodo,
            GastosPeriodo = gastosPeriodo,
            CostoVentasPeriodo = costoVentas,
            EfectivoEnCajaActual = cajaActiva?.EfectivoEsperado ?? 0,
            HayCajaAbierta = cajaActiva != null,
            CuentasPorPagarPendientes = cxp,
            CuentasPorCobrarPendientes = 0,
            IngredientesStockBajo = stockBajo,
            UltimasVentas = ultimasVentas
        };

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
