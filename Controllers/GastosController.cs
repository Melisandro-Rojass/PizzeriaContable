using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;

namespace PizzeriaContable.Controllers;

[Authorize(Roles = "Administrador,Contabilidad,Contador")]
public class GastosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IContabilidadService _contabilidadService;

    public GastosController(ApplicationDbContext context, IContabilidadService contabilidadService)
    {
        _context = context;
        _contabilidadService = contabilidadService;
    }

    // GET: Gastos
    public async Task<IActionResult> Index(string? categoria, DateTime? desde, DateTime? hasta)
    {
        IQueryable<Gasto> query = _context.Gastos.Include(g => g.Proveedor);

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            query = query.Where(g => g.CategoriaGasto == categoria);
        }

        if (desde.HasValue)
        {
            query = query.Where(g => g.FechaGasto >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            query = query.Where(g => g.FechaGasto <= hasta.Value.Date);
        }

        ViewData["Categoria"] = categoria;
        ViewData["Desde"] = desde?.ToString("yyyy-MM-dd");
        ViewData["Hasta"] = hasta?.ToString("yyyy-MM-dd");
        ViewBag.TotalGastos = await query.Where(g => g.Estado != "Anulado").SumAsync(g => g.Monto);

        var gastos = await query.OrderByDescending(g => g.FechaGasto).ToListAsync();
        return View(gastos);
    }

    // GET: Gastos/Create
    public async Task<IActionResult> Create()
    {
        await LoadProveedoresAsync();
        return View(new Gasto 
        { 
            FechaGasto = DateTime.Today,
            Estado = "Pagado",
            MetodoPago = "Efectivo"
        });
    }

    // POST: Gastos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Gasto gasto)
    {
        if (string.IsNullOrWhiteSpace(gasto.Descripcion))
        {
            ModelState.AddModelError("Descripcion", "La descripción del gasto es obligatoria.");
        }

        if (gasto.Monto <= 0)
        {
            ModelState.AddModelError("Monto", "El monto del gasto debe ser un valor positivo.");
        }

        if (ModelState.IsValid)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (gasto.Estado == "Pagado" && gasto.FechaPago == null)
                {
                    gasto.FechaPago = gasto.FechaGasto;
                }

                _context.Gastos.Add(gasto);
                await _context.SaveChangesAsync();

                // Resolver cuenta de gasto
                string cuentaGastoCodigo = MapearCuentaGasto(gasto.CategoriaGasto);
                string cuentaPagoCodigo = gasto.Estado == "Pagado" 
                    ? (gasto.MetodoPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) ? "1.1.01.01" : "1.1.02.01")
                    : "2.1.02"; // Gastos por pagar

                var apuntes = new List<(string codigoCuenta, decimal debe, decimal haber, string? detalle)>
                {
                    (cuentaGastoCodigo, gasto.Monto, 0, $"Gasto: {gasto.Descripcion}"),
                    (cuentaPagoCodigo, 0, gasto.Monto, $"Forma de pago: {gasto.MetodoPago} ({gasto.Estado})")
                };

                await _contabilidadService.CrearAsientoAutomaticoAsync(
                    $"Registro de Gasto: {gasto.CategoriaGasto} - {gasto.Descripcion}",
                    "Gasto",
                    gasto.Id,
                    apuntes,
                    User.Identity?.Name);

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Gasto de {gasto.Monto:C2} registrado y contabilizado.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error al registrar el gasto: " + ex.Message);
            }
        }

        await LoadProveedoresAsync(gasto.ProveedorId);
        return View(gasto);
    }

    // POST: Gastos/Anular/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anular(int id)
    {
        var gasto = await _context.Gastos.FindAsync(id);
        if (gasto == null) return NotFound();

        if (gasto.Estado == "Anulado")
        {
            TempData["ErrorMessage"] = "El gasto ya se encuentra anulado.";
            return RedirectToAction(nameof(Index));
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            gasto.Estado = "Anulado";
            _context.Gastos.Update(gasto);

            // Anular asiento previo
            var asiento = await _context.AsientosContables
                .Include(a => a.Detalles)
                .FirstOrDefaultAsync(a => a.TipoOrigen == "Gasto" && a.DocumentoOrigenId == id && !a.Anulado);

            if (asiento != null)
            {
                asiento.Anulado = true;
                _context.AsientosContables.Update(asiento);

                // Crear contra-asiento
                int totalAsientos = await _context.AsientosContables.CountAsync() + 1;
                var contraAsiento = new AsientoContable
                {
                    NumeroAsiento = $"AS-{DateTime.Now.Year}-{totalAsientos:D5}",
                    Fecha = DateTime.Today,
                    Concepto = $"Reversión de Gasto #{gasto.Id} - {gasto.Descripcion}",
                    TipoOrigen = "ReversionGasto",
                    DocumentoOrigenId = gasto.Id,
                    Contabilizado = true,
                    Anulado = false,
                    FechaCreacion = DateTime.Now,
                    CreadoPor = User.Identity?.Name ?? "Sistema",
                    Detalles = asiento.Detalles.Select(d => new AsientoDetalle
                    {
                        CuentaContableId = d.CuentaContableId,
                        Debe = d.Haber,
                        Haber = d.Debe,
                        Descripcion = $"Reversión de Gasto #{gasto.Id}"
                    }).ToList()
                };
                _context.AsientosContables.Add(contraAsiento);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Gasto #{gasto.Id} anulado y revertido en contabilidad.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Error al anular el gasto: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private string MapearCuentaGasto(string categoria)
    {
        return categoria switch
        {
            "Alquiler" => "6.1.01",
            "Electricidad" => "6.1.02",
            "Agua" => "6.1.03",
            "Internet" => "6.1.04",
            "Salarios" => "6.1.05",
            "Mantenimiento" => "6.1.06",
            "Transporte" => "6.1.07",
            "Publicidad" => "6.1.08",
            _ => "6.1.99"
        };
    }

    private async Task LoadProveedoresAsync(int? selectedId = null)
    {
        var proveedores = await _context.Proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        ViewBag.Proveedores = new SelectList(proveedores, "Id", "Nombre", selectedId);
    }
}
