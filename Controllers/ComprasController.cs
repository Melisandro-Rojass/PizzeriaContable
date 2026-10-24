using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;

namespace PizzeriaContable.Controllers;

public class ComprasController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IInventarioService _inventarioService;
    private readonly IContabilidadService _contabilidadService;

    public ComprasController(
        ApplicationDbContext context,
        IInventarioService inventarioService,
        IContabilidadService contabilidadService)
    {
        _context = context;
        _inventarioService = inventarioService;
        _contabilidadService = contabilidadService;
    }

    // GET: Compras
    public async Task<IActionResult> Index(string? searchString, bool soloCredito = false)
    {
        IQueryable<Compra> query = _context.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles);

        if (soloCredito)
        {
            query = query.Where(c => c.EsCredito && c.SaldoPendiente > 0);
        }

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(c => c.NumeroFactura.Contains(searchString) || c.Proveedor!.Nombre.Contains(searchString));
        }

        ViewData["SearchString"] = searchString;
        ViewData["SoloCredito"] = soloCredito;

        var compras = await query.OrderByDescending(c => c.FechaCompra).ToListAsync();
        return View(compras);
    }

    // GET: Compras/Create
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new Compra 
        { 
            FechaCompra = DateTime.Today,
            MetodoPago = "Efectivo"
        });
    }

    // POST: Compras/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Compra compra, 
        int[] ingredienteIds, 
        decimal[] cantidades, 
        decimal[] costosUnitarios)
    {
        if (compra.ProveedorId <= 0)
        {
            ModelState.AddModelError("ProveedorId", "Debe seleccionar un proveedor.");
        }

        if (string.IsNullOrWhiteSpace(compra.NumeroFactura))
        {
            ModelState.AddModelError("NumeroFactura", "El número de factura del proveedor es obligatorio.");
        }

        if (ingredienteIds == null || ingredienteIds.Length == 0)
        {
            ModelState.AddModelError("", "La compra debe incluir al menos un ingrediente.");
        }

        if (ModelState.IsValid)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                compra.Detalles = new List<CompraDetalle>();
                decimal subtotal = 0;

                for (int i = 0; i < ingredienteIds!.Length; i++)
                {
                    if (ingredienteIds[i] > 0 && cantidades[i] > 0)
                    {
                        decimal itemSub = Math.Round(cantidades[i] * costosUnitarios[i], 2);
                        subtotal += itemSub;
                        compra.Detalles.Add(new CompraDetalle
                        {
                            IngredienteId = ingredienteIds[i],
                            Cantidad = cantidades[i],
                            CostoUnitario = costosUnitarios[i],
                            Subtotal = itemSub
                        });
                    }
                }

                compra.Subtotal = subtotal;
                compra.Total = subtotal + compra.Impuesto;
                compra.Estado = "Completada";

                if (compra.EsCredito)
                {
                    compra.SaldoPendiente = compra.Total;
                    if (compra.FechaVencimiento == null)
                    {
                        var prov = await _context.Proveedores.FindAsync(compra.ProveedorId);
                        int dias = prov?.DiasCredito ?? 15;
                        compra.FechaVencimiento = compra.FechaCompra.AddDays(dias);
                    }
                }
                else
                {
                    compra.SaldoPendiente = 0;
                    compra.Pagos.Add(new PagoCompra
                    {
                        Fecha = DateTime.Now,
                        Monto = compra.Total,
                        MetodoPago = compra.MetodoPago,
                        Referencia = $"Pago de Contado Factura #{compra.NumeroFactura}"
                    });
                }

                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();

                // 1. Aumentar inventario y actualizar costo ponderado
                await _inventarioService.AumentarInventarioPorCompraAsync(compra.Id, User.Identity?.Name);

                // 2. Contabilidad de partida doble
                // DEBE: Inventario de Materias Primas e Ingredientes (1.1.04.01)
                // HABER: Caja (1.1.01.01) / Banco (1.1.02.01) si contado, o Cuentas por Pagar (2.1.01.01) si crédito
                var apuntes = new List<(string codigoCuenta, decimal debe, decimal haber, string? detalle)>();

                apuntes.Add(("1.1.04.01", compra.Total, 0, $"Entrada a Inventario por Compra Factura #{compra.NumeroFactura}"));

                if (compra.EsCredito)
                {
                    apuntes.Add(("2.1.01.01", 0, compra.Total, $"Cuenta por pagar proveedor #{compra.ProveedorId}"));
                }
                else
                {
                    string cuentaPago = compra.MetodoPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) 
                        ? "1.1.01.01" 
                        : "1.1.02.01";
                    apuntes.Add((cuentaPago, 0, compra.Total, $"Salida de {compra.MetodoPago} por Compra Factura #{compra.NumeroFactura}"));
                }

                await _contabilidadService.CrearAsientoAutomaticoAsync(
                    $"Compra Factura #{compra.NumeroFactura} - Proveedor ID: {compra.ProveedorId}",
                    "Compra",
                    compra.Id,
                    apuntes,
                    User.Identity?.Name);

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Compra #{compra.NumeroFactura} registrada e ingresada al inventario.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error al procesar la compra: " + ex.Message);
            }
        }

        await LoadDropdownsAsync(compra.ProveedorId);
        return View(compra);
    }

    // GET: Compras/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var compra = await _context.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Ingrediente)
            .Include(c => c.Pagos)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (compra == null) return NotFound();

        return View(compra);
    }

    // POST: Compras/RegistrarPago
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarPago(int compraId, decimal monto, string metodoPago, string? referencia)
    {
        var compra = await _context.Compras.FindAsync(compraId);
        if (compra == null) return NotFound();

        if (monto <= 0 || monto > compra.SaldoPendiente)
        {
            TempData["ErrorMessage"] = $"El monto a abonar debe ser mayor a 0 y no exceder el saldo pendiente ({compra.SaldoPendiente:C2}).";
            return RedirectToAction(nameof(Details), new { id = compraId });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var pago = new PagoCompra
            {
                CompraId = compraId,
                Fecha = DateTime.Now,
                Monto = monto,
                MetodoPago = metodoPago,
                Referencia = referencia,
                Observaciones = $"Abono a Factura #{compra.NumeroFactura}"
            };

            compra.SaldoPendiente -= monto;

            _context.PagosCompras.Add(pago);
            _context.Compras.Update(compra);
            await _context.SaveChangesAsync();

            // Contabilización del abono a proveedor:
            // DEBE: Cuentas por Pagar Proveedores (2.1.01.01)
            // HABER: Caja (1.1.01.01) o Bancos (1.1.02.01)
            string cuentaSalida = metodoPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase)
                ? "1.1.01.01"
                : "1.1.02.01";

            var apuntes = new List<(string codigoCuenta, decimal debe, decimal haber, string? detalle)>
            {
                ("2.1.01.01", monto, 0, $"Abono a Proveedor Factura #{compra.NumeroFactura}"),
                (cuentaSalida, 0, monto, $"Salida por pago a proveedor ({metodoPago})")
            };

            await _contabilidadService.CrearAsientoAutomaticoAsync(
                $"Pago a Proveedor - Factura #{compra.NumeroFactura}",
                "PagoCompra",
                pago.Id,
                apuntes,
                User.Identity?.Name);

            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Abono de {monto:C2} aplicado exitosamente a la factura #{compra.NumeroFactura}.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Error al procesar el pago: " + ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = compraId });
    }

    private async Task LoadDropdownsAsync(int? selectedProveedorId = null)
    {
        var proveedores = await _context.Proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        ViewBag.Proveedores = new SelectList(proveedores, "Id", "Nombre", selectedProveedorId);
        ViewBag.Ingredientes = await _context.Ingredientes.Where(i => i.Activo).OrderBy(i => i.Nombre).ToListAsync();
    }
}
