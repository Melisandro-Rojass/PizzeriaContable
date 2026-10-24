using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;
using PizzeriaContable.ViewModels;

namespace PizzeriaContable.Controllers;

public class ContabilidadController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IContabilidadService _contabilidadService;

    public ContabilidadController(
        ApplicationDbContext context,
        IContabilidadService contabilidadService)
    {
        _context = context;
        _contabilidadService = contabilidadService;
    }

    // GET: Contabilidad/Cuentas
    public async Task<IActionResult> Cuentas(string? search, string? tipo)
    {
        var query = _context.CuentasContables
            .Include(c => c.CuentaPadre)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Codigo.Contains(search) || c.Nombre.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            query = query.Where(c => c.TipoCuenta == tipo);
        }

        var cuentas = await query
            .OrderBy(c => c.Codigo)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Tipo = tipo;
        ViewBag.CuentasPadre = await _context.CuentasContables
            .Where(c => !c.PermiteMovimiento)
            .OrderBy(c => c.Codigo)
            .ToListAsync();

        return View(cuentas);
    }

    // POST: Contabilidad/CrearCuenta
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCuenta(CuentaContable cuenta)
    {
        if (await _context.CuentasContables.AnyAsync(c => c.Codigo == cuenta.Codigo))
        {
            TempData["ErrorMessage"] = $"El código contable '{cuenta.Codigo}' ya existe en el catálogo.";
            return RedirectToAction(nameof(Cuentas));
        }

        if (ModelState.IsValid)
        {
            _context.CuentasContables.Add(cuenta);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Cuenta {cuenta.Codigo} - {cuenta.Nombre} creada con éxito.";
            return RedirectToAction(nameof(Cuentas));
        }

        TempData["ErrorMessage"] = "Datos inválidos para crear la cuenta contable.";
        return RedirectToAction(nameof(Cuentas));
    }

    // GET: Contabilidad/Asientos (Libro Diario)
    public async Task<IActionResult> Asientos(DateTime? desde, DateTime? hasta, string? tipoOrigen)
    {
        desde ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        hasta ??= DateTime.Today;

        var query = _context.AsientosContables
            .Include(a => a.Detalles)
                .ThenInclude(d => d.CuentaContable)
            .Where(a => a.Fecha.Date >= desde.Value.Date && a.Fecha.Date <= hasta.Value.Date)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tipoOrigen))
        {
            query = query.Where(a => a.TipoOrigen == tipoOrigen);
        }

        var asientos = await query
            .OrderByDescending(a => a.Fecha)
            .ThenByDescending(a => a.Id)
            .ToListAsync();

        ViewBag.Desde = desde.Value.ToString("yyyy-MM-dd");
        ViewBag.Hasta = hasta.Value.ToString("yyyy-MM-dd");
        ViewBag.TipoOrigen = tipoOrigen;

        ViewBag.TotalDebe = asientos.Where(a => !a.Anulado).Sum(a => a.Detalles.Sum(d => d.Debe));
        ViewBag.TotalHaber = asientos.Where(a => !a.Anulado).Sum(a => a.Detalles.Sum(d => d.Haber));

        return View(asientos);
    }

    // GET: Contabilidad/DetalleAsiento/5
    public async Task<IActionResult> DetalleAsiento(int id)
    {
        var asiento = await _context.AsientosContables
            .Include(a => a.Detalles)
                .ThenInclude(d => d.CuentaContable)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (asiento == null)
        {
            return NotFound();
        }

        return View(asiento);
    }

    // GET: Contabilidad/CrearAsiento
    public async Task<IActionResult> CrearAsiento()
    {
        ViewBag.Cuentas = await _context.CuentasContables
            .Where(c => c.Activa && c.PermiteMovimiento)
            .OrderBy(c => c.Codigo)
            .ToListAsync();

        return View(new AsientoManualViewModel());
    }

    // POST: Contabilidad/CrearAsiento
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearAsiento(AsientoManualViewModel model)
    {
        if (model.Lineas == null || model.Lineas.Count < 2)
        {
            ModelState.AddModelError("", "Un asiento contable de partida doble debe contener al menos 2 líneas de movimiento.");
        }
        else
        {
            decimal totalDebe = model.Lineas.Sum(l => l.Debe);
            decimal totalHaber = model.Lineas.Sum(l => l.Haber);

            if (totalDebe <= 0 || totalHaber <= 0)
            {
                ModelState.AddModelError("", "El total del Debe y del Haber deben ser mayores a cero.");
            }
            else if (Math.Abs(totalDebe - totalHaber) > 0.01m)
            {
                ModelState.AddModelError("", $"El asiento está desbalanceado. Suma Debe: {totalDebe:C2} | Suma Haber: {totalHaber:C2}. Diferencia: {Math.Abs(totalDebe - totalHaber):C2}.");
            }
        }

        if (ModelState.IsValid)
        {
            int totalAsientos = await _context.AsientosContables.CountAsync() + 1;
            string numAsiento = $"AS-{DateTime.Today.Year}-{totalAsientos:D5}";

            var asiento = new AsientoContable
            {
                NumeroAsiento = numAsiento,
                Fecha = model.Fecha,
                Concepto = model.Concepto,
                TipoOrigen = "Manual",
                Contabilizado = true,
                Anulado = false,
                FechaCreacion = DateTime.Now,
                CreadoPor = User.Identity?.Name ?? "Admin",
                Detalles = model.Lineas.Select(l => new AsientoDetalle
                {
                    CuentaContableId = l.CuentaContableId,
                    Debe = l.Debe,
                    Haber = l.Haber,
                    Descripcion = l.Descripcion
                }).ToList()
            };

            _context.AsientosContables.Add(asiento);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Asiento {asiento.NumeroAsiento} registrado correctamente con partida doble equilibrada.";
            return RedirectToAction(nameof(Asientos));
        }

        ViewBag.Cuentas = await _context.CuentasContables
            .Where(c => c.Activa && c.PermiteMovimiento)
            .OrderBy(c => c.Codigo)
            .ToListAsync();

        return View(model);
    }

    // GET: Contabilidad/Apertura
    public async Task<IActionResult> Apertura()
    {
        var asientoAperturaExistente = await _context.AsientosContables
            .Include(a => a.Detalles)
                .ThenInclude(d => d.CuentaContable)
            .FirstOrDefaultAsync(a => a.TipoOrigen == "Apertura" && !a.Anulado);

        ViewBag.AsientoExistente = asientoAperturaExistente;

        return View(new AperturaContableViewModel());
    }

    // POST: Contabilidad/Apertura
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apertura(AperturaContableViewModel model)
    {
        var asientoExistente = await _context.AsientosContables
            .FirstOrDefaultAsync(a => a.TipoOrigen == "Apertura" && !a.Anulado);

        if (asientoExistente != null)
        {
            TempData["ErrorMessage"] = $"Ya existe un asiento de apertura registrado ({asientoExistente.NumeroAsiento}). Si necesita corregirlo, primero anule el existente.";
            return RedirectToAction(nameof(Apertura));
        }

        if (!model.EstaBalanceado)
        {
            ModelState.AddModelError("", $"El balance de apertura no cuadra. Activos: {model.TotalActivos:C2} != Pasivo + Capital: {(model.TotalPasivos + model.TotalPatrimonio):C2}. Diferencia: {model.Diferencia:C2}.");
        }

        if (ModelState.IsValid)
        {
            var apuntes = new List<(string codigo, decimal debe, decimal haber, string? desc)>();

            if (model.EfectivoCaja > 0)
                apuntes.Add(("1.1.01.01", model.EfectivoCaja, 0, "Apertura - Efectivo en Caja General"));

            if (model.SaldoBancos > 0)
                apuntes.Add(("1.1.02.01", model.SaldoBancos, 0, "Apertura - Saldo en Bancos"));

            if (model.InventarioInicial > 0)
                apuntes.Add(("1.1.04.01", model.InventarioInicial, 0, "Apertura - Inventario Inicial de Materia Prima"));

            if (model.MobiliarioEquipo > 0)
                apuntes.Add(("1.2.01.01", model.MobiliarioEquipo, 0, "Apertura - Mobiliario y Equipo de Local"));

            if (model.HornosYEquipos > 0)
                apuntes.Add(("1.2.02.01", model.HornosYEquipos, 0, "Apertura - Hornos y Maquinaria de Pizzería"));

            if (model.CuentasPorPagarProveedores > 0)
                apuntes.Add(("2.1.01.01", 0, model.CuentasPorPagarProveedores, "Apertura - Deudas Iniciales con Proveedores"));

            if (model.OtrasObligaciones > 0)
                apuntes.Add(("2.1.02.01", 0, model.OtrasObligaciones, "Apertura - Otras Obligaciones por Pagar"));

            if (model.CapitalSocial > 0)
                apuntes.Add(("3.1.01.01", 0, model.CapitalSocial, "Apertura - Capital Social / Patrimonio Inicial"));

            string? usuario = User.Identity?.Name ?? "Admin";

            var asientoCreado = await _contabilidadService.CrearAsientoAutomaticoAsync(
                $"Asiento de Apertura Contable - {model.Observaciones}",
                "Apertura",
                null,
                apuntes,
                usuario);

            TempData["SuccessMessage"] = $"Asiento de Apertura {asientoCreado.NumeroAsiento} registrado exitosamente con equilibrio patrimonial.";
            return RedirectToAction(nameof(Apertura));
        }

        return View(model);
    }

    // GET: Contabilidad/LibroMayor/5
    public async Task<IActionResult> LibroMayor(int id, DateTime? desde, DateTime? hasta)
    {
        var cuenta = await _context.CuentasContables.FindAsync(id);
        if (cuenta == null) return NotFound();

        var (cta, movimientos, saldoDeudor, saldoAcreedor) = await _contabilidadService.ObtenerLibroMayorAsync(id, desde, hasta);

        ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
        ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
        ViewBag.SaldoDeudor = saldoDeudor;
        ViewBag.SaldoAcreedor = saldoAcreedor;

        return View((cta, movimientos));
    }
}
