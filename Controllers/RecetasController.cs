using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;

namespace PizzeriaContable.Controllers;

[Authorize(Roles = "Administrador,Inventario,EncargadoInventario")]
public class RecetasController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IInventarioService _inventarioService;

    public RecetasController(ApplicationDbContext context, IInventarioService inventarioService)
    {
        _context = context;
        _inventarioService = inventarioService;
    }

    // GET: Recetas
    public async Task<IActionResult> Index()
    {
        var recetas = await _context.Recetas
            .Include(r => r.Producto)
                .ThenInclude(p => p!.Categoria)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.Ingrediente)
            .ToListAsync();

        return View(recetas);
    }

    // GET: Recetas/Create
    public async Task<IActionResult> Create()
    {
        // Productos que aún no tienen receta
        var productosSinReceta = await _context.Productos
            .Where(p => p.Receta == null && p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        ViewBag.Productos = new SelectList(productosSinReceta, "Id", "Nombre");
        ViewBag.Ingredientes = await _context.Ingredientes.Where(i => i.Activo).OrderBy(i => i.Nombre).ToListAsync();

        return View(new Receta { Rendimiento = 1, MermaPrevistaPorcentaje = 0 });
    }

    // POST: Recetas/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Receta receta, int[] ingredienteIds, decimal[] cantidades, string[] unidades)
    {
        if (receta.ProductoId <= 0)
        {
            ModelState.AddModelError("ProductoId", "Debe seleccionar un producto.");
        }

        if (ingredienteIds == null || ingredienteIds.Length == 0)
        {
            ModelState.AddModelError("", "La receta debe incluir al menos un ingrediente.");
        }

        if (ingredienteIds != null && (cantidades == null || unidades == null ||
            ingredienteIds.Length != cantidades.Length || ingredienteIds.Length != unidades.Length))
        {
            ModelState.AddModelError("", "El detalle de ingredientes está incompleto o es inválido.");
        }

        if (ModelState.IsValid)
        {
            var ingredientesValidos = await _context.Ingredientes
                .Where(i => i.Activo && ingredienteIds!.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            receta.Detalles = new List<RecetaDetalle>();

            for (int i = 0; i < ingredienteIds!.Length; i++)
            {
                if (ingredienteIds[i] > 0 && ingredientesValidos.ContainsKey(ingredienteIds[i]) &&
                    cantidades[i] > 0 && !string.IsNullOrWhiteSpace(unidades[i]))
                {
                    receta.Detalles.Add(new RecetaDetalle
                    {
                        IngredienteId = ingredienteIds[i],
                        Cantidad = cantidades[i],
                        UnidadMedida = unidades[i]
                    });
                }
            }

            if (receta.Detalles.Count == 0)
            {
                ModelState.AddModelError("", "La receta debe incluir al menos un ingrediente válido.");
            }
            else
            {
                _context.Recetas.Add(receta);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Receta registrada y costeada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        var productosSinReceta = await _context.Productos
            .Where(p => p.Receta == null && p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
        ViewBag.Productos = new SelectList(productosSinReceta, "Id", "Nombre", receta.ProductoId);
        ViewBag.Ingredientes = await _context.Ingredientes.Where(i => i.Activo).OrderBy(i => i.Nombre).ToListAsync();
        return View(receta);
    }

    // GET: Recetas/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var receta = await _context.Recetas
            .Include(r => r.Producto)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.Ingrediente)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receta == null) return NotFound();

        ViewBag.Ingredientes = await _context.Ingredientes.Where(i => i.Activo).OrderBy(i => i.Nombre).ToListAsync();
        return View(receta);
    }

    // POST: Recetas/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Receta receta, int[] ingredienteIds, decimal[] cantidades, string[] unidades)
    {
        if (id != receta.Id) return NotFound();

        if (ingredienteIds == null || cantidades == null || unidades == null ||
            ingredienteIds.Length != cantidades.Length || ingredienteIds.Length != unidades.Length)
        {
            ModelState.AddModelError("", "El detalle de ingredientes está incompleto o es inválido.");
        }

        if (ModelState.IsValid)
        {
            var recetaDb = await _context.Recetas
                .Include(r => r.Detalles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recetaDb == null) return NotFound();

            recetaDb.Rendimiento = receta.Rendimiento;
            recetaDb.MermaPrevistaPorcentaje = receta.MermaPrevistaPorcentaje;
            recetaDb.Instrucciones = receta.Instrucciones;
            recetaDb.Activo = receta.Activo;

            var ingredientesValidos = await _context.Ingredientes
                .Where(i => i.Activo && ingredienteIds!.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            // Actualizar detalles
            _context.RecetaDetalles.RemoveRange(recetaDb.Detalles);
            recetaDb.Detalles = new List<RecetaDetalle>();

            if (ingredienteIds != null)
            {
                for (int i = 0; i < ingredienteIds.Length; i++)
                {
                    if (ingredienteIds[i] > 0 && ingredientesValidos.ContainsKey(ingredienteIds[i]) &&
                        cantidades[i] > 0 && !string.IsNullOrWhiteSpace(unidades[i]))
                    {
                        recetaDb.Detalles.Add(new RecetaDetalle
                        {
                            RecetaId = id,
                            IngredienteId = ingredienteIds[i],
                            Cantidad = cantidades[i],
                            UnidadMedida = unidades[i]
                        });
                    }
                }
            }

            if (recetaDb.Detalles.Count == 0)
            {
                ModelState.AddModelError("", "La receta debe incluir al menos un ingrediente válido.");
                ViewBag.Ingredientes = await _context.Ingredientes.Where(i => i.Activo).OrderBy(i => i.Nombre).ToListAsync();
                return View(receta);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Receta actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Ingredientes = await _context.Ingredientes.Where(i => i.Activo).OrderBy(i => i.Nombre).ToListAsync();
        return View(receta);
    }

    // GET: Recetas/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var receta = await _context.Recetas
            .Include(r => r.Producto)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.Ingrediente)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (receta == null) return NotFound();

        return View(receta);
    }
}
