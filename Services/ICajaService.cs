using PizzeriaContable.Models;

namespace PizzeriaContable.Services;

public interface ICajaService
{
    Task<CajaSesion?> ObtenerSesionActivaAsync(string? usuarioId);

    Task<CajaSesion> AbrirCajaAsync(decimal montoInicial, string? usuarioId, string? observaciones = null);

    Task<CajaMovimiento> RegistrarMovimientoAsync(int cajaSesionId, string tipo, decimal monto, string descripcion, string? usuarioId);

    Task<CajaSesion> CerrarCajaAsync(int cajaSesionId, decimal efectivoContado, string? observaciones, string? usuarioId);

    Task ActualizarTotalesPorVentaAsync(int cajaSesionId, string metodoPago, decimal monto);
}
