using PizzeriaContable.Models;

namespace PizzeriaContable.Services;

public interface IVentaService
{
    Task<Venta> CrearVentaTransaccionalAsync(
        Venta venta, 
        List<VentaDetalle> detalles, 
        List<VentaPago> pagos, 
        string? usuario);

    Task AnularVentaAsync(int ventaId, string motivo, string? usuario);

    Task<string> GenerarNumeroComprobanteAsync();
}
