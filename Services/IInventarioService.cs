using PizzeriaContable.Models;

namespace PizzeriaContable.Services;

public interface IInventarioService
{
    Task<MovimientoInventario> RegistrarMovimientoAsync(
        int ingredienteId,
        string tipoMovimiento,
        decimal cantidad,
        decimal costoUnitario,
        string? referencia,
        string? observaciones,
        string? usuario);

    Task DescontarInventarioPorVentaAsync(int ventaId, string? usuario);

    Task RevertirInventarioPorAnulacionVentaAsync(int ventaId, string? usuario);

    Task AumentarInventarioPorCompraAsync(int compraId, string? usuario);

    Task<List<Ingrediente>> ObtenerAlertasStockBajoAsync();

    Task<decimal> ObtenerValorizacionInventarioAsync();

    decimal ConvertirUnidades(decimal cantidad, string unidadOrigen, string unidadDestino);
}
