using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;
using PizzeriaContable.ViewModels;

namespace PizzeriaContable.Controllers;

public class VentasController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IVentaService _ventaService;
    private readonly ICajaService _cajaService;

    public VentasController(
        ApplicationDbContext context,
        IVentaService ventaService,
        ICajaService cajaService)
    {
        _context = context;
        _ventaService = ventaService;
        _cajaService = cajaService;
    }

    // GET: Ventas
    public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin, string? estado)
    {
        var query = _context.Ventas
            .Include(v => v.Detalles)
            .Include(v => v.Pagos)
            .Include(v => v.CajaSesion)
            .AsQueryable();

        if (fechaInicio.HasValue)
        {
            query = query.Where(v => v.Fecha.Date >= fechaInicio.Value.Date);
        }
        else
        {
            // Por defecto últimos 30 días
            query = query.Where(v => v.Fecha.Date >= DateTime.Today.AddDays(-30));
            fechaInicio = DateTime.Today.AddDays(-30);
        }

        if (fechaFin.HasValue)
        {
            query = query.Where(v => v.Fecha.Date <= fechaFin.Value.Date);
        }
        else
        {
            fechaFin = DateTime.Today;
        }

        if (!string.IsNullOrEmpty(estado))
        {
            query = query.Where(v => v.Estado == estado);
        }

        var ventas = await query
            .OrderByDescending(v => v.Fecha)
            .ToListAsync();

        ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
        ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
        ViewBag.Estado = estado;

        ViewBag.TotalVentas = ventas.Where(v => v.Estado == "Completada").Sum(v => v.Total);
        ViewBag.TotalImpuestos = ventas.Where(v => v.Estado == "Completada").Sum(v => v.Impuesto);
        ViewBag.CantidadCompletadas = ventas.Count(v => v.Estado == "Completada");

        return View(ventas);
    }

    // GET: Ventas/Pos
    public async Task<IActionResult> Pos()
    {
        string? usuario = User.Identity?.Name ?? "Admin";
        var sesionCaja = await _cajaService.ObtenerSesionActivaAsync(usuario);
        ViewBag.SesionCaja = sesionCaja;

        var categorias = await _context.Categorias
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        var productos = await _context.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Receta)
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var config = await _context.Configuraciones.FirstOrDefaultAsync();
        ViewBag.TasaIva = config?.TasaIvaPorcentaje ?? 15.00m;
        ViewBag.NombreEmpresa = config?.NombreComercial ?? "Pizzería Contable";

        ViewBag.Categorias = categorias;
        return View(productos);
    }

    // POST: Ventas/Checkout (AJAX desde el POS)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto dto)
    {
        try
        {
            if (dto == null || dto.Items == null || !dto.Items.Any())
            {
                return Json(new { success = false, message = "La orden no contiene productos." });
            }

            if (dto.Pagos == null || !dto.Pagos.Any())
            {
                return Json(new { success = false, message = "Debe registrarse el pago de la orden." });
            }

            string? usuario = User.Identity?.Name ?? "Admin";

            var venta = new Venta
            {
                ClienteNombre = string.IsNullOrWhiteSpace(dto.ClienteNombre) ? "Cliente General" : dto.ClienteNombre,
                Observaciones = dto.Observaciones,
                Descuento = dto.DescuentoGeneral,
                MetodoPago = dto.Pagos.Count > 1 ? "Mixto" : dto.Pagos.First().MetodoPago
            };

            var detalles = dto.Items.Select(i => new VentaDetalle
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario,
                Descuento = i.Descuento
            }).ToList();

            var pagos = dto.Pagos.Select(p => new VentaPago
            {
                MetodoPago = p.MetodoPago,
                Monto = p.Monto,
                Referencia = p.Referencia
            }).ToList();

            var ventaCreada = await _ventaService.CrearVentaTransaccionalAsync(venta, detalles, pagos, usuario);

            return Json(new
            {
                success = true,
                ventaId = ventaCreada.Id,
                numeroComprobante = ventaCreada.NumeroComprobante,
                total = ventaCreada.Total,
                message = $"Venta {ventaCreada.NumeroComprobante} procesada con éxito."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // GET: Ventas/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var venta = await _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(v => v.Pagos)
            .Include(v => v.CajaSesion)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta == null)
        {
            return NotFound();
        }

        // Asiento contable asociado
        var asiento = await _context.AsientosContables
            .Include(a => a.Detalles)
                .ThenInclude(d => d.CuentaContable)
            .FirstOrDefaultAsync(a => a.TipoOrigen == "Venta" && a.DocumentoOrigenId == id);

        ViewBag.Asiento = asiento;

        // Auditoría
        var auditorias = await _context.Auditorias
            .Where(a => a.Entidad == "Venta" && a.EntidadId == id.ToString())
            .OrderByDescending(a => a.FechaHora)
            .ToListAsync();
        ViewBag.Auditorias = auditorias;

        return View(venta);
    }

    // GET: Ventas/Ticket/5
    public async Task<IActionResult> Ticket(int id)
    {
        var venta = await _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(v => v.Pagos)
            .Include(v => v.CajaSesion)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta == null)
        {
            return NotFound();
        }

        var config = await _context.Configuraciones.FirstOrDefaultAsync();
        ViewBag.Configuracion = config;

        return View(venta);
    }

    // POST: Ventas/Anular/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(int id, string motivo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["ErrorMessage"] = "Debe especificar un motivo justificado para anular la venta.";
                return RedirectToAction(nameof(Details), new { id });
            }

            string? usuario = User.Identity?.Name ?? "Admin";
            await _ventaService.AnularVentaAsync(id, motivo, usuario);

            TempData["SuccessMessage"] = "La venta ha sido anulada con éxito. Se ha revertido el inventario y generado el contra-asiento contable.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al anular venta: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
