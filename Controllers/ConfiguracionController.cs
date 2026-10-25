using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;

namespace PizzeriaContable.Controllers;

[Authorize(Roles = "Administrador")]
public class ConfiguracionController : Controller
{
    private readonly ApplicationDbContext _context;

    public ConfiguracionController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Configuracion
    public async Task<IActionResult> Index()
    {
        var config = await _context.Configuraciones.FirstOrDefaultAsync();
        if (config == null)
        {
            config = new ConfiguracionEmpresa
            {
                NombreComercial = "Pizzería La Tradicional",
                RazonSocial = "Pizzería La Tradicional S.A.",
                Direccion = "Managua, Nicaragua",
                Telefono = "+505 2222-3344",
                CorreoElectronico = "administracion@pizzerialatradicional.ni",
                IdentificacionTributaria = "J0310000012345",
                MonedaPrincipal = "NIO",
                SimboloMoneda = "C$",
                ZonaHoraria = "America/Managua",
                FechaInicioContable = DateTime.Today,
                TasaIvaPorcentaje = 15.00m
            };
            _context.Configuraciones.Add(config);
            await _context.SaveChangesAsync();
        }

        return View(config);
    }

    // POST: Configuracion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ConfiguracionEmpresa model)
    {
        if (ModelState.IsValid)
        {
            var config = await _context.Configuraciones.FirstOrDefaultAsync(c => c.Id == model.Id);
            if (config == null)
            {
                _context.Configuraciones.Add(model);
            }
            else
            {
                config.NombreComercial = model.NombreComercial;
                config.RazonSocial = model.RazonSocial;
                config.Direccion = model.Direccion;
                config.Telefono = model.Telefono;
                config.CorreoElectronico = model.CorreoElectronico;
                config.IdentificacionTributaria = model.IdentificacionTributaria;
                config.MonedaPrincipal = model.MonedaPrincipal;
                config.SimboloMoneda = model.SimboloMoneda;
                config.ZonaHoraria = model.ZonaHoraria;
                config.FechaInicioContable = model.FechaInicioContable;
                config.TasaIvaPorcentaje = model.TasaIvaPorcentaje;
                _context.Configuraciones.Update(config);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Configuración de la empresa guardada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = "Por favor revise los campos con error.";
        return View(model);
    }
}
