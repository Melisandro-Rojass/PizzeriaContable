using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;

namespace PizzeriaContable.Services;

public class InventarioService : IInventarioService
{
    private readonly ApplicationDbContext _context;
    private readonly IContabilidadService _contabilidadService;

    public InventarioService(ApplicationDbContext context, IContabilidadService contabilidadService)
    {
        _context = context;
        _contabilidadService = contabilidadService;
    }

    public decimal ConvertirUnidades(decimal cantidad, string unidadOrigen, string unidadDestino)
    {
        var origen = unidadOrigen.Trim().ToLowerInvariant();
        var destino = unidadDestino.Trim().ToLowerInvariant();

        if (origen == destino)
        {
            return cantidad;
        }

        // Peso: g <-> kg
        if ((origen == "g" || origen == "gramo" || origen == "gramos") && (destino == "kg" || destino == "kilogramo" || destino == "kilos"))
        {
            return cantidad / 1000m;
        }
        if ((origen == "kg" || origen == "kilogramo" || origen == "kilos") && (destino == "g" || destino == "gramo" || destino == "gramos"))
        {
            return cantidad * 1000m;
        }

        // Volumen: ml <-> l
        if ((origen == "ml" || origen == "mililitro" || origen == "mililitros") && (destino == "l" || destino == "litro" || destino == "litros"))
        {
            return cantidad / 1000m;
        }
        if ((origen == "l" || origen == "litro" || origen == "litros") && (destino == "ml" || destino == "mililitro" || destino == "mililitros"))
        {
            return cantidad * 1000m;
        }

        // Peso: oz <-> g (1 oz = 28.3495 g)
        if ((origen == "oz" || origen == "onza" || origen == "onzas") && (destino == "g" || destino == "gramo"))
        {
            return cantidad * 28.3495m;
        }
        if ((origen == "g" || origen == "gramo") && (destino == "oz" || destino == "onza" || destino == "onzas"))
        {
            return cantidad / 28.3495m;
        }

        // Unidades incompatibles
        throw new InvalidOperationException($"No se puede realizar una conversión automática entre las unidades incompatibles '{unidadOrigen}' y '{unidadDestino}'.");
    }

    public async Task<MovimientoInventario> RegistrarMovimientoAsync(
        int ingredienteId,
        string tipoMovimiento,
        decimal cantidad,
        decimal costoUnitario,
        string? referencia,
        string? observaciones,
        string? usuario)
    {
        bool transaccionPrincipal = _context.Database.IsRelational() && _context.Database.CurrentTransaction == null;
        await using var transaction = transaccionPrincipal ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            var ingrediente = await _context.Ingredientes.FindAsync(ingredienteId)
                ?? throw new ArgumentException($"Ingrediente ID {ingredienteId} no encontrado.");

            var tiposMovimiento = new HashSet<string>(StringComparer.Ordinal)
            {
                "InventarioInicial", "EntradaCompra", "SalidaVenta", "Merma", "SalidaMerma", "Devolucion",
                "AjustePositivo", "AjusteNegativo", "ReversionAnulacion"
            };
            if (!tiposMovimiento.Contains(tipoMovimiento))
            {
                throw new ArgumentException($"El tipo de movimiento '{tipoMovimiento}' no es válido.");
            }

            if (cantidad == 0)
            {
                throw new ArgumentException("La cantidad del movimiento no puede ser cero.");
            }
            if (costoUnitario < 0)
            {
                throw new ArgumentException("El costo unitario no puede ser negativo.");
            }

            ingrediente.StockActual += cantidad;

            var movimiento = new MovimientoInventario
            {
                IngredienteId = ingredienteId,
                TipoMovimiento = tipoMovimiento,
                Cantidad = cantidad,
                CostoUnitario = costoUnitario,
                Total = Math.Abs(cantidad * costoUnitario),
                Fecha = DateTime.Now,
                Referencia = referencia,
                Observaciones = observaciones,
                UsuarioId = usuario ?? "Sistema"
            };

            _context.MovimientosInventario.Add(movimiento);
            _context.Ingredientes.Update(ingrediente);
            await _context.SaveChangesAsync();
            await RegistrarAsientoMovimientoAsync(movimiento, usuario);

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return movimiento;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }

    public async Task DescontarInventarioPorVentaAsync(int ventaId, string? usuario)
    {
        // 1. Evitar descontar inventario más de una vez para la misma venta
        bool yaDescontado = await _context.MovimientosInventario
            .AnyAsync(m => m.Referencia == $"VENTA-{ventaId}" && m.TipoMovimiento == "SalidaVenta");

        if (yaDescontado)
        {
            return;
        }

        bool transaccionPrincipal = _context.Database.IsRelational() && _context.Database.CurrentTransaction == null;
        await using var transaction = transaccionPrincipal ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == ventaId)
                ?? throw new ArgumentException($"Venta ID {ventaId} no encontrada.");

            foreach (var detalle in venta.Detalles)
            {
                var receta = await _context.Recetas
                    .Include(r => r.Detalles)
                        .ThenInclude(d => d.Ingrediente)
                    .FirstOrDefaultAsync(r => r.ProductoId == detalle.ProductoId && r.Activo);

                if (receta == null)
                {
                    // Producto sin receta (ej. bebidas embotelladas directamente o ítems sin receta desglosada)
                    continue;
                }

                int rendimiento = receta.Rendimiento > 0 ? receta.Rendimiento : 1;
                decimal factorMerma = 1 + (receta.MermaPrevistaPorcentaje / 100m);

                foreach (var recetaDetalle in receta.Detalles)
                {
                    var ingrediente = recetaDetalle.Ingrediente;
                    if (ingrediente == null) continue;

                    // Cantidad unitaria según rendimiento y merma
                    decimal cantPorUnidad = (recetaDetalle.Cantidad / rendimiento) * factorMerma;
                    decimal cantTotalReceta = cantPorUnidad * detalle.Cantidad;

                    // Conversión de unidades si la unidad de la receta difiere de la del inventario
                    decimal cantidadADescontar = ConvertirUnidades(cantTotalReceta, recetaDetalle.UnidadMedida, ingrediente.UnidadMedida);

                    // Descontar inventario (cantidad negativa)
                    ingrediente.StockActual -= cantidadADescontar;

                    var movimiento = new MovimientoInventario
                    {
                        IngredienteId = ingrediente.Id,
                        TipoMovimiento = "SalidaVenta",
                        Cantidad = -cantidadADescontar,
                        CostoUnitario = ingrediente.CostoUnitario,
                        Total = Math.Round(cantidadADescontar * ingrediente.CostoUnitario, 2),
                        Fecha = venta.Fecha != default ? venta.Fecha : DateTime.Now,
                        Referencia = $"VENTA-{ventaId}",
                        Observaciones = $"Consumo por venta de {detalle.Cantidad}x Producto ID {detalle.ProductoId}",
                        UsuarioId = usuario ?? "Sistema"
                    };

                    _context.MovimientosInventario.Add(movimiento);
                    _context.Ingredientes.Update(ingrediente);
                }
            }

            await _context.SaveChangesAsync();
            decimal costoTotal = _context.MovimientosInventario.Local
                .Where(m => m.Referencia == $"VENTA-{ventaId}" && m.TipoMovimiento == "SalidaVenta")
                .Sum(m => m.Total);
            if (costoTotal > 0)
            {
                await _contabilidadService.CrearAsientoAutomaticoAsync(
                    $"Costo de ventas e inventario - Venta #{ventaId}",
                    "CostoVenta",
                    ventaId,
                    new List<(string codigoCuenta, decimal debe, decimal haber, string? detalle)>
                    {
                    ("5.1.01", costoTotal, 0, $"Consumo de inventario por Venta #{ventaId}"),
                    ("1.1.04.01", 0, costoTotal, $"Salida de inventario por Venta #{ventaId}")
                    },
                    usuario,
                    venta.Fecha);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }

    public async Task RevertirInventarioPorAnulacionVentaAsync(int ventaId, string? usuario)
    {
        bool yaRevertido = await _context.MovimientosInventario
            .AnyAsync(m => m.Referencia == $"ANULACION-VENTA-{ventaId}" && m.TipoMovimiento == "ReversionAnulacion");
        if (yaRevertido)
        {
            return;
        }

        bool transaccionPrincipal = _context.Database.IsRelational() && _context.Database.CurrentTransaction == null;
        await using var transaction = transaccionPrincipal ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            var movimientosPrevios = await _context.MovimientosInventario
                .Where(m => m.Referencia == $"VENTA-{ventaId}" && m.TipoMovimiento == "SalidaVenta")
                .ToListAsync();

            foreach (var movimientoPrevio in movimientosPrevios)
            {
                var ingrediente = await _context.Ingredientes.FindAsync(movimientoPrevio.IngredienteId);
                if (ingrediente != null)
                {
                    decimal cantidadARevertir = Math.Abs(movimientoPrevio.Cantidad);
                    ingrediente.StockActual += cantidadARevertir;

                    // Registra la entrada de reversión para mantener la trazabilidad del inventario
                    var movimientoReversion = new MovimientoInventario
                    {
                        IngredienteId = ingrediente.Id,
                        TipoMovimiento = "ReversionAnulacion",
                        Cantidad = cantidadARevertir,
                        CostoUnitario = movimientoPrevio.CostoUnitario,
                        Total = movimientoPrevio.Total,
                        Fecha = DateTime.Now,
                        Referencia = $"ANULACION-VENTA-{ventaId}",
                        Observaciones = $"Reversión de salida por anulación de Venta #{ventaId}",
                        UsuarioId = usuario ?? "Sistema"
                    };

                    _context.MovimientosInventario.Add(movimientoReversion);
                    _context.Ingredientes.Update(ingrediente);
                }
            }

            await _context.SaveChangesAsync();
            decimal costoTotal = movimientosPrevios.Sum(m => m.Total);
            if (costoTotal > 0)
            {
                await _contabilidadService.CrearAsientoAutomaticoAsync(
                    $"Reversión de costo de ventas - Venta #{ventaId}",
                    "ReversionCostoVenta",
                    ventaId,
                    new List<(string codigoCuenta, decimal debe, decimal haber, string? detalle)>
                    {
                    ("1.1.04.01", costoTotal, 0, $"Reingreso de inventario por anulación de Venta #{ventaId}"),
                    ("5.1.01", 0, costoTotal, $"Reversión de costo por anulación de Venta #{ventaId}")
                    },
                    usuario);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }

    public async Task AumentarInventarioPorCompraAsync(int compraId, string? usuario)
    {
        bool yaIngresado = await _context.MovimientosInventario
            .AnyAsync(m => m.Referencia == $"COMPRA-{compraId}" && m.TipoMovimiento == "EntradaCompra");

        if (yaIngresado)
        {
            return;
        }

        var compra = await _context.Compras
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.Id == compraId)
            ?? throw new ArgumentException($"Compra ID {compraId} no encontrada.");

        foreach (var detalle in compra.Detalles)
        {
            var ingrediente = await _context.Ingredientes.FindAsync(detalle.IngredienteId);
            if (ingrediente != null)
            {
                // Promedio ponderado para el costo unitario
                decimal stockAnterior = Math.Max(0, ingrediente.StockActual);
                decimal valorAnterior = stockAnterior * ingrediente.CostoUnitario;
                decimal valorNuevo = detalle.Cantidad * detalle.CostoUnitario;
                decimal nuevoStock = stockAnterior + detalle.Cantidad;

                if (nuevoStock > 0)
                {
                    ingrediente.CostoUnitario = Math.Round((valorAnterior + valorNuevo) / nuevoStock, 2);
                }

                ingrediente.StockActual += detalle.Cantidad;

                var movimiento = new MovimientoInventario
                {
                    IngredienteId = ingrediente.Id,
                    TipoMovimiento = "EntradaCompra",
                    Cantidad = detalle.Cantidad,
                    CostoUnitario = detalle.CostoUnitario,
                    Total = detalle.Subtotal,
                    Fecha = compra.FechaCompra != default ? compra.FechaCompra : DateTime.Now,
                    Referencia = $"COMPRA-{compraId}",
                    Observaciones = $"Entrada por Compra Factura #{compra.NumeroFactura}",
                    UsuarioId = usuario ?? "Sistema"
                };

                _context.MovimientosInventario.Add(movimiento);
                _context.Ingredientes.Update(ingrediente);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task RegistrarAsientoMovimientoAsync(MovimientoInventario movimiento, string? usuario)
    {
        string? cuentaDebe;
        string? cuentaHaber;

        switch (movimiento.TipoMovimiento)
        {
            case "InventarioInicial":
                cuentaDebe = "1.1.04.01";
                cuentaHaber = "3.1.01";
                break;
            case "AjustePositivo":
                cuentaDebe = "1.1.04.01";
                cuentaHaber = "4.1.03";
                break;
            case "Merma":
            case "SalidaMerma":
            case "AjusteNegativo":
                cuentaDebe = "5.1.03";
                cuentaHaber = "1.1.04.01";
                break;
            case "Devolucion":
            case "ReversionAnulacion":
                cuentaDebe = "1.1.04.01";
                cuentaHaber = "5.1.01";
                break;
            default:
                return;
        }

        if (movimiento.Total <= 0)
        {
            return;
        }

        await _contabilidadService.CrearAsientoAutomaticoAsync(
            $"Movimiento de inventario {movimiento.TipoMovimiento} #{movimiento.Id}",
            "MovimientoInventario",
            movimiento.Id,
            new List<(string codigoCuenta, decimal debe, decimal haber, string? detalle)>
            {
                (cuentaDebe, movimiento.Total, 0, movimiento.Observaciones),
                (cuentaHaber, 0, movimiento.Total, movimiento.Observaciones)
            },
            usuario,
            movimiento.Fecha);
    }

    public async Task<List<Ingrediente>> ObtenerAlertasStockBajoAsync()
    {
        return await _context.Ingredientes
            .Where(i => i.Activo && i.StockActual <= i.StockMinimo)
            .OrderBy(i => i.StockActual)
            .ToListAsync();
    }

    public async Task<decimal> ObtenerValorizacionInventarioAsync()
    {
        return await _context.Ingredientes
            .Where(i => i.Activo)
            .SumAsync(i => i.StockActual * i.CostoUnitario);
    }
}
