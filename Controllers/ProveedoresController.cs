using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;

namespace PizzeriaContable.Controllers;

[Authorize(Roles = "Administrador,Inventario,EncargadoInventario,Contabilidad,Contador")]
public class ProveedoresController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProveedoresController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Proveedores
    public async Task<IActionResult> Index()
    {
        var proveedores = await _context.Proveedores
            .Include(p => p.Compras)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
        return View(proveedores);
    }

    // GET: Proveedores/Create
    public IActionResult Create()
    {
        return View(new Proveedor { Activo = true });
    }

    // POST: Proveedores/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Contacto,Telefono,CorreoElectronico,Direccion,IdentificacionTributaria,DiasCredito,Activo")] Proveedor proveedor)
    {
        if (string.IsNullOrWhiteSpace(proveedor.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre del proveedor es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(proveedor);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Proveedor \"{proveedor.Nombre}\" creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        return View(proveedor);
    }

    // GET: Proveedores/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor == null) return NotFound();

        return View(proveedor);
    }

    // POST: Proveedores/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Contacto,Telefono,CorreoElectronico,Direccion,IdentificacionTributaria,DiasCredito,Activo")] Proveedor proveedor)
    {
        if (id != proveedor.Id) return NotFound();

        if (string.IsNullOrWhiteSpace(proveedor.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre del proveedor es obligatorio.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(proveedor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Proveedor \"{proveedor.Nombre}\" actualizado correctamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Proveedores.Any(e => e.Id == proveedor.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        return View(proveedor);
    }

    // GET: Proveedores/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var proveedor = await _context.Proveedores
            .Include(p => p.Compras)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (proveedor == null) return NotFound();

        return View(proveedor);
    }

    // POST: Proveedores/Desactivar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor == null) return NotFound();

        proveedor.Activo = !proveedor.Activo;
        _context.Update(proveedor);
        await _context.SaveChangesAsync();

        string estado = proveedor.Activo ? "activado" : "desactivado";
        TempData["SuccessMessage"] = $"Proveedor \"{proveedor.Nombre}\" {estado}.";
        return RedirectToAction(nameof(Index));
    }
}
