using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;

namespace PizzeriaContable.Controllers;

[Authorize(Roles = "Administrador,Inventario,EncargadoInventario")]
public class IngredientesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IInventarioService _inventarioService;

    public IngredientesController(ApplicationDbContext context, IInventarioService inventarioService)
    {
        _context = context;
        _inventarioService = inventarioService;
    }

    // GET: Ingredientes
    public async Task<IActionResult> Index(string? searchString, bool soloBajoStock = false)
    {
        IQueryable<Ingrediente> query = _context.Ingredientes.Include(i => i.ProveedorHabitual);

        if (soloBajoStock)
        {
            query = query.Where(i => i.Activo && i.StockActual <= i.StockMinimo);
        }

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(i => i.Nombre.Contains(searchString) || i.Codigo.Contains(searchString));
        }

        ViewData["SearchString"] = searchString;
        ViewData["SoloBajoStock"] = soloBajoStock;
        ViewBag.ValorTotalInventario = await _inventarioService.ObtenerValorizacionInventarioAsync();

        var ingredientes = await query.OrderBy(i => i.Nombre).ToListAsync();
        return View(ingredientes);
    }

    // GET: Ingredientes/Create
    public async Task<IActionResult> Create()
    {
        await LoadProveedoresAsync();
        return View(new Ingrediente 
        { 
            Activo = true,
            Codigo = $"ING-{DateTime.Now:yyyyMMddHHmmss}"
        });
    }

    // POST: Ingredientes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Codigo,Nombre,UnidadMedida,CostoUnitario,StockActual,StockMinimo,Activo,ProveedorHabitualId")] Ingrediente ingrediente)
    {
        if (string.IsNullOrWhiteSpace(ingrediente.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre del ingrediente es obligatorio.");
        }

        if (ingrediente.CostoUnitario < 0)
        {
            ModelState.AddModelError("CostoUnitario", "El costo unitario no puede ser negativo.");
        }

        if (ingrediente.StockActual < 0)
        {
            ModelState.AddModelError("StockActual", "La existencia inicial no puede ser negativa.");
        }

        if (ModelState.IsValid)
        {
            decimal stockInicial = ingrediente.StockActual;
            ingrediente.StockActual = 0;
            _context.Add(ingrediente);
            await _context.SaveChangesAsync();

            // Si se ingresó existencia inicial mayor a 0, registrar movimiento de inventario inicial
            if (stockInicial > 0)
            {
                await _inventarioService.RegistrarMovimientoAsync(
                    ingrediente.Id,
                    "InventarioInicial",
                    stockInicial,
                    ingrediente.CostoUnitario,
                    "INVENTARIO-INICIAL",
                    "Registro de existencia inicial",
                    User.Identity?.Name);
            }

            TempData["SuccessMessage"] = $"Ingrediente \"{ingrediente.Nombre}\" creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        await LoadProveedoresAsync(ingrediente.ProveedorHabitualId);
        return View(ingrediente);
    }

    // GET: Ingredientes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var ingrediente = await _context.Ingredientes.FindAsync(id);
        if (ingrediente == null) return NotFound();

        await LoadProveedoresAsync(ingrediente.ProveedorHabitualId);
        return View(ingrediente);
    }

    // POST: Ingredientes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Codigo,Nombre,UnidadMedida,CostoUnitario,StockMinimo,Activo,ProveedorHabitualId")] Ingrediente ingrediente)
    {
        if (id != ingrediente.Id) return NotFound();

        if (string.IsNullOrWhiteSpace(ingrediente.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre del ingrediente es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var ingredienteDb = await _context.Ingredientes.FindAsync(id);
                if (ingredienteDb == null) return NotFound();

                ingredienteDb.Codigo = ingrediente.Codigo;
                ingredienteDb.Nombre = ingrediente.Nombre;
                ingredienteDb.UnidadMedida = ingrediente.UnidadMedida;
                ingredienteDb.CostoUnitario = ingrediente.CostoUnitario;
                ingredienteDb.StockMinimo = ingrediente.StockMinimo;
                ingredienteDb.Activo = ingrediente.Activo;
                ingredienteDb.ProveedorHabitualId = ingrediente.ProveedorHabitualId;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Ingrediente \"{ingrediente.Nombre}\" actualizado correctamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Ingredientes.Any(e => e.Id == ingrediente.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        await LoadProveedoresAsync(ingrediente.ProveedorHabitualId);
        return View(ingrediente);
    }

    // GET: Ingredientes/Ajuste/5
    public async Task<IActionResult> Ajuste(int? id)
    {
        if (id == null) return NotFound();

        var ingrediente = await _context.Ingredientes.FindAsync(id);
        if (ingrediente == null) return NotFound();

        ViewBag.Ingrediente = ingrediente;
        return View();
    }

    // POST: Ingredientes/Ajuste/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ajuste(int id, string tipoAjuste, decimal cantidad, string? observaciones)
    {
        var ingrediente = await _context.Ingredientes.FindAsync(id);
        if (ingrediente == null) return NotFound();

        var tiposAjuste = new HashSet<string>(StringComparer.Ordinal)
        {
            "SalidaMerma", "AjusteNegativo", "AjustePositivo", "Devolucion"
        };

        if (!tiposAjuste.Contains(tipoAjuste))
        {
            ModelState.AddModelError("tipoAjuste", "El tipo de ajuste no es válido.");
        }

        if (cantidad <= 0)
        {
            ModelState.AddModelError("", "La cantidad a ajustar debe ser mayor a 0.");
        }

        if (string.IsNullOrWhiteSpace(observaciones))
        {
            ModelState.AddModelError("observaciones", "Debe indicar el motivo del ajuste.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Ingrediente = ingrediente;
            return View();
        }

        decimal cantMov = tipoAjuste is "SalidaMerma" or "AjusteNegativo" ? -cantidad : cantidad;

        await _inventarioService.RegistrarMovimientoAsync(
            id,
            tipoAjuste,
            cantMov,
            ingrediente.CostoUnitario,
            "AJUSTE-MANUAL",
            observaciones ?? $"Ajuste manual de {tipoAjuste}",
            User.Identity?.Name);

        TempData["SuccessMessage"] = $"Ajuste registrado para {ingrediente.Nombre}. Nuevo stock: {ingrediente.StockActual:0.##} {ingrediente.UnidadMedida}.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Ingredientes/Movimientos/5
    public async Task<IActionResult> Movimientos(int? id)
    {
        if (id == null) return NotFound();

        var ingrediente = await _context.Ingredientes.FindAsync(id);
        if (ingrediente == null) return NotFound();

        var movimientos = await _context.MovimientosInventario
            .Where(m => m.IngredienteId == id)
            .OrderByDescending(m => m.Fecha)
            .ToListAsync();

        ViewBag.Ingrediente = ingrediente;
        return View(movimientos);
    }

    // POST: Ingredientes/Desactivar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var ingrediente = await _context.Ingredientes.FindAsync(id);
        if (ingrediente == null) return NotFound();

        ingrediente.Activo = !ingrediente.Activo;
        _context.Update(ingrediente);
        await _context.SaveChangesAsync();

        string estado = ingrediente.Activo ? "activado" : "desactivado";
        TempData["SuccessMessage"] = $"Ingrediente \"{ingrediente.Nombre}\" {estado}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadProveedoresAsync(int? selectedId = null)
    {
        var proveedores = await _context.Proveedores
            .Where(p => p.Activo || p.Id == (selectedId ?? 0))
            .OrderBy(p => p.Nombre)
            .ToListAsync();
        ViewBag.Proveedores = new SelectList(proveedores, "Id", "Nombre", selectedId);
    }
}
