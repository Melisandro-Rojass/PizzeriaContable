using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;

namespace PizzeriaContable.Controllers;

public class CategoriasController : Controller
{
    private readonly ApplicationDbContext _context;

    public CategoriasController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Categorias
    public async Task<IActionResult> Index()
    {
        var categorias = await _context.Categorias
            .Include(c => c.Productos)
            .OrderBy(c => c.Nombre)
            .ToListAsync();
        return View(categorias);
    }

    // GET: Categorias/Create
    public IActionResult Create()
    {
        return View(new Categoria { Activo = true });
    }

    // POST: Categorias/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion,Activo")] Categoria categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre de la categoría es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(categoria);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Categoría \"{categoria.Nombre}\" registrada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        return View(categoria);
    }

    // GET: Categorias/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null)
        {
            return NotFound();
        }

        return View(categoria);
    }

    // POST: Categorias/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,Activo")] Categoria categoria)
    {
        if (id != categoria.Id)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(categoria.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre de la categoría es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(categoria);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Categoría \"{categoria.Nombre}\" actualizada correctamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoriaExists(categoria.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        return View(categoria);
    }

    // POST: Categorias/Desactivar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null)
        {
            return NotFound();
        }

        categoria.Activo = !categoria.Activo;
        _context.Update(categoria);
        await _context.SaveChangesAsync();

        string estado = categoria.Activo ? "activada" : "desactivada";
        TempData["SuccessMessage"] = $"Categoría \"{categoria.Nombre}\" {estado}.";
        return RedirectToAction(nameof(Index));
    }

    private bool CategoriaExists(int id)
    {
        return _context.Categorias.Any(e => e.Id == id);
    }
}
