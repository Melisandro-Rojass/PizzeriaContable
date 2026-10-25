using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;

namespace PizzeriaContable.Controllers;

[Authorize(Roles = "Administrador,Inventario,EncargadoInventario,Contabilidad,Contador,Cajero,Consulta")]
public class ProductosController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Productos
    public async Task<IActionResult> Index(string? searchString, int? categoriaId, bool mostrarInactivos = false)
    {
        IQueryable<Producto> query = _context.Productos.Include(p => p.Categoria);

        if (!mostrarInactivos)
        {
            query = query.Where(p => p.Activo);
        }

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(p => p.Nombre.Contains(searchString) || p.Codigo.Contains(searchString));
        }

        if (categoriaId.HasValue && categoriaId.Value > 0)
        {
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        }

        ViewData["SearchString"] = searchString;
        ViewData["CategoriaId"] = categoriaId;
        ViewData["MostrarInactivos"] = mostrarInactivos;
        ViewBag.Categorias = new SelectList(await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", categoriaId);

        var productos = await query.OrderBy(p => p.Categoria!.Nombre).ThenBy(p => p.Nombre).ToListAsync();
        return View(productos);
    }

    // GET: Productos/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var producto = await _context.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Receta)
                .ThenInclude(r => r!.Detalles)
                .ThenInclude(d => d.Ingrediente)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (producto == null)
        {
            return NotFound();
        }

        return View(producto);
    }

    // GET: Productos/Create
    public async Task<IActionResult> Create()
    {
        await LoadCategoriasAsync();
        return View(new Producto 
        { 
            Activo = true,
            Codigo = $"PRD-{DateTime.Now:yyyyMMddHHmmss}"
        });
    }

    // POST: Productos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Codigo,Nombre,Descripcion,Tamano,Precio,Activo,CategoriaId")] Producto producto)
    {
        if (string.IsNullOrWhiteSpace(producto.Codigo))
        {
            producto.Codigo = $"PRD-{DateTime.Now:yyyyMMddHHmmss}";
        }

        if (string.IsNullOrWhiteSpace(producto.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre del producto no puede estar vacío.");
        }

        if (producto.Precio <= 0)
        {
            ModelState.AddModelError("Precio", "El precio debe ser un valor positivo.");
        }

        if (producto.CategoriaId <= 0)
        {
            ModelState.AddModelError("CategoriaId", "Debe seleccionar una categoría.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(producto);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Producto \"{producto.Nombre}\" creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        await LoadCategoriasAsync(producto.CategoriaId);
        return View(producto);
    }

    // GET: Productos/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var producto = await _context.Productos.FindAsync(id);
        if (producto == null)
        {
            return NotFound();
        }

        await LoadCategoriasAsync(producto.CategoriaId);
        return View(producto);
    }

    // POST: Productos/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Codigo,Nombre,Descripcion,Tamano,Precio,Activo,CategoriaId")] Producto producto)
    {
        if (id != producto.Id)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(producto.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre del producto no puede estar vacío.");
        }

        if (producto.Precio <= 0)
        {
            ModelState.AddModelError("Precio", "El precio debe ser un valor positivo.");
        }

        if (producto.CategoriaId <= 0)
        {
            ModelState.AddModelError("CategoriaId", "Debe seleccionar una categoría.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(producto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Producto \"{producto.Nombre}\" actualizado correctamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductoExists(producto.Id))
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

        await LoadCategoriasAsync(producto.CategoriaId);
        return View(producto);
    }

    // POST: Productos/Desactivar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto == null)
        {
            return NotFound();
        }

        producto.Activo = !producto.Activo;
        _context.Update(producto);
        await _context.SaveChangesAsync();

        string estadoTexto = producto.Activo ? "activado" : "desactivado";
        TempData["SuccessMessage"] = $"Producto \"{producto.Nombre}\" {estadoTexto} correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // GET: Productos/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var producto = await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (producto == null)
        {
            return NotFound();
        }

        return View(producto);
    }

    // POST: Productos/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto != null)
        {
            // Validar que no tenga ventas históricas relacionadas antes de eliminación física
            bool tieneVentas = await _context.VentaDetalles.AnyAsync(vd => vd.ProductoId == id);
            if (tieneVentas)
            {
                producto.Activo = false;
                _context.Update(producto);
                await _context.SaveChangesAsync();
                TempData["WarningMessage"] = $"El producto \"{producto.Nombre}\" tiene ventas históricas asociadas y no puede eliminarse físicamente; ha sido desactivado.";
            }
            else
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Producto \"{producto.Nombre}\" eliminado permanentemente.";
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private bool ProductoExists(int id)
    {
        return _context.Productos.Any(e => e.Id == id);
    }

    private async Task LoadCategoriasAsync(int? selectedId = null)
    {
        var categorias = await _context.Categorias
            .Where(c => c.Activo || c.Id == (selectedId ?? 0))
            .OrderBy(c => c.Nombre)
            .ToListAsync();
        ViewBag.Categorias = new SelectList(categorias, "Id", "Nombre", selectedId);
    }
}
