using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;

namespace PizzeriaContable.Services;

public class CajaService : ICajaService
{
    private readonly ApplicationDbContext _context;

    public CajaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CajaSesion?> ObtenerSesionActivaAsync(string? usuarioId)
    {
        return await _context.CajasSesiones
            .Include(cs => cs.Movimientos)
            .Include(cs => cs.Ventas)
            .FirstOrDefaultAsync(cs => cs.Estado == "Abierta");
    }

    public async Task<CajaSesion> AbrirCajaAsync(decimal montoInicial, string? usuarioId, string? observaciones = null)
    {
        var sesionAbierta = await ObtenerSesionActivaAsync(usuarioId);
        if (sesionAbierta != null)
        {
            throw new InvalidOperationException($"Ya existe un turno de caja abierto iniciado el {sesionAbierta.FechaApertura:dd/MM/yyyy HH:mm}. Debe cerrarlo antes de iniciar uno nuevo.");
        }

        var nuevaSesion = new CajaSesion
        {
            FechaApertura = DateTime.Now,
            MontoInicial = montoInicial,
            TotalVentasEfectivo = 0,
            TotalVentasTarjeta = 0,
            TotalVentasTransferencia = 0,
            TotalIngresos = 0,
            TotalEgresos = 0,
            EfectivoEsperado = montoInicial,
            EfectivoContado = 0,
            Diferencia = 0,
            Estado = "Abierta",
            UsuarioId = usuarioId,
            Observaciones = observaciones
        };

        _context.CajasSesiones.Add(nuevaSesion);
        await _context.SaveChangesAsync();
        return nuevaSesion;
    }

    public async Task<CajaMovimiento> RegistrarMovimientoAsync(
        int cajaSesionId, 
        string tipo, 
        decimal monto, 
        string descripcion, 
        string? usuarioId)
    {
        var sesion = await _context.CajasSesiones.FindAsync(cajaSesionId)
            ?? throw new ArgumentException($"Sesión de caja ID {cajaSesionId} no encontrada.");

        if (sesion.Estado != "Abierta")
        {
            throw new InvalidOperationException("No se pueden registrar movimientos en una caja cerrada.");
        }

        var movimiento = new CajaMovimiento
        {
            CajaSesionId = cajaSesionId,
            Tipo = tipo,
            Monto = monto,
            Descripcion = descripcion,
            Fecha = DateTime.Now,
            UsuarioId = usuarioId
        };

        if (tipo == "Ingreso" || tipo == "Deposito")
        {
            sesion.TotalIngresos += monto;
        }
        else if (tipo == "Egreso" || tipo == "Retiro")
        {
            sesion.TotalEgresos += monto;
        }

        // Recalcular efectivo físico esperado (excluyendo ventas por tarjeta o transferencia)
        sesion.EfectivoEsperado = sesion.MontoInicial + sesion.TotalVentasEfectivo + sesion.TotalIngresos - sesion.TotalEgresos;

        _context.CajasMovimientos.Add(movimiento);
        _context.CajasSesiones.Update(sesion);
        await _context.SaveChangesAsync();

        return movimiento;
    }

    public async Task ActualizarTotalesPorVentaAsync(int cajaSesionId, string metodoPago, decimal monto)
    {
        var sesion = await _context.CajasSesiones.FindAsync(cajaSesionId);
        if (sesion == null || sesion.Estado != "Abierta")
        {
            return;
        }

        switch (metodoPago.ToLowerInvariant())
        {
            case "efectivo":
                sesion.TotalVentasEfectivo += monto;
                break;
            case "tarjeta":
                sesion.TotalVentasTarjeta += monto;
                break;
            case "transferencia":
                sesion.TotalVentasTransferencia += monto;
                break;
        }

        // Solo el efectivo físico suma al efectivo esperado en gaveta
        sesion.EfectivoEsperado = sesion.MontoInicial + sesion.TotalVentasEfectivo + sesion.TotalIngresos - sesion.TotalEgresos;
        _context.CajasSesiones.Update(sesion);
        await _context.SaveChangesAsync();
    }

    public async Task<CajaSesion> CerrarCajaAsync(int cajaSesionId, decimal efectivoContado, string? observaciones, string? usuarioId)
    {
        var sesion = await _context.CajasSesiones.FindAsync(cajaSesionId)
            ?? throw new ArgumentException($"Sesión de caja ID {cajaSesionId} no encontrada.");

        if (sesion.Estado != "Abierta")
        {
            throw new InvalidOperationException("La sesión de caja ya se encuentra cerrada.");
        }

        // Efectivo físico esperado = Fondo Inicial + Ventas Efectivo + Ingresos - Egresos
        sesion.EfectivoEsperado = sesion.MontoInicial + sesion.TotalVentasEfectivo + sesion.TotalIngresos - sesion.TotalEgresos;
        sesion.EfectivoContado = efectivoContado;
        sesion.Diferencia = efectivoContado - sesion.EfectivoEsperado;
        sesion.FechaCierre = DateTime.Now;
        sesion.Estado = "Cerrada";
        sesion.Observaciones = observaciones;

        _context.CajasSesiones.Update(sesion);
        await _context.SaveChangesAsync();
        return sesion;
    }
}
