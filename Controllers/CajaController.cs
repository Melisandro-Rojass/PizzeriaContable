using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.Services;

namespace PizzeriaContable.Controllers;

[Authorize(Roles = "Administrador,Cajero")]
public class CajaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICajaService _cajaService;

    public CajaController(ApplicationDbContext context, ICajaService cajaService)
    {
        _context = context;
        _cajaService = cajaService;
    }

    // GET: Caja
    public async Task<IActionResult> Index()
    {
        var sesionActiva = await _cajaService.ObtenerSesionActivaAsync(User.Identity?.Name);
        var historial = await _context.CajasSesiones
            .OrderByDescending(cs => cs.FechaApertura)
            .Take(15)
            .ToListAsync();

        ViewBag.Historial = historial;
        return View(sesionActiva);
    }

    // POST: Caja/Abrir
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Abrir(decimal montoInicial, string? observaciones)
    {
        if (montoInicial < 0)
        {
            TempData["ErrorMessage"] = "El fondo inicial no puede ser negativo.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _cajaService.AbrirCajaAsync(montoInicial, User.Identity?.Name, observaciones);
            TempData["SuccessMessage"] = $"Caja abierta exitosamente con un fondo inicial de {montoInicial:C2}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Caja/Movimiento
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Movimiento(int cajaSesionId, string tipo, decimal monto, string descripcion)
    {
        if (monto <= 0)
        {
            TempData["ErrorMessage"] = "El monto debe ser mayor a 0.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            TempData["ErrorMessage"] = "Debe indicar el concepto o justificación del movimiento.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _cajaService.RegistrarMovimientoAsync(cajaSesionId, tipo, monto, descripcion, User.Identity?.Name);
            TempData["SuccessMessage"] = $"Movimiento de {tipo} por {monto:C2} registrado correctamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Caja/Cerrar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int cajaSesionId, decimal efectivoContado, string? observaciones)
    {
        try
        {
            var sesion = await _cajaService.CerrarCajaAsync(cajaSesionId, efectivoContado, observaciones, User.Identity?.Name);
            string difTexto = sesion.Diferencia == 0 
                ? "Caja cuadrada exactamente." 
                : (sesion.Diferencia > 0 ? $"Sobrante de {sesion.Diferencia:C2}" : $"Faltante de {Math.Abs(sesion.Diferencia):C2}");

            TempData["SuccessMessage"] = $"Caja cerrada exitosamente. Efectivo esperado: {sesion.EfectivoEsperado:C2}, Contado: {sesion.EfectivoContado:C2}. {difTexto}";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Caja/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var sesion = await _context.CajasSesiones
            .Include(cs => cs.Movimientos)
            .Include(cs => cs.Ventas)
            .FirstOrDefaultAsync(cs => cs.Id == id);

        if (sesion == null) return NotFound();

        return View(sesion);
    }
}
