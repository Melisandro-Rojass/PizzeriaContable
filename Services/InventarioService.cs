using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;

namespace PizzeriaContable.Services;

public class InventarioService : IInventarioService
{
    private readonly ApplicationDbContext _context;

    public InventarioService(ApplicationDbContext context)
    {
        _context = context;
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
        var ingrediente = await _context.Ingredientes.FindAsync(ingredienteId)
            ?? throw new ArgumentException($"Ingrediente ID {ingredienteId} no encontrado.");

        // Actualizar stock de acuerdo al movimiento
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

        return movimiento;
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
                decimal cantA_Descontar = ConvertirUnidades(cantTotalReceta, recetaDetalle.UnidadMedida, ingrediente.UnidadMedida);

                // Descontar inventario (cantidad negativa)
                ingrediente.StockActual -= cantA_Descontar;

                var mov = new MovimientoInventario
                {
                    IngredienteId = ingrediente.Id,
                    TipoMovimiento = "SalidaVenta",
                    Cantidad = -cantA_Descontar,
                    CostoUnitario = ingrediente.CostoUnitario,
                    Total = Math.Round(cantA_Descontar * ingrediente.CostoUnitario, 2),
                    Fecha = DateTime.Now,
                    Referencia = $"VENTA-{ventaId}",
                    Observaciones = $"Consumo por venta de {detalle.Cantidad}x Producto ID {detalle.ProductoId}",
                    UsuarioId = usuario ?? "Sistema"
                };

                _context.MovimientosInventario.Add(mov);
                _context.Ingredientes.Update(ingrediente);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task RevertirInventarioPorAnulacionVentaAsync(int ventaId, string? usuario)
    {
        var movimientosPrevios = await _context.MovimientosInventario
            .Where(m => m.Referencia == $"VENTA-{ventaId}" && m.TipoMovimiento == "SalidaVenta")
            .ToListAsync();

        foreach (var mov in movimientosPrevios)
        {
            var ingrediente = await _context.Ingredientes.FindAsync(mov.IngredienteId);
            if (ingrediente != null)
            {
                decimal cantRevertir = Math.Abs(mov.Cantidad);
                ingrediente.StockActual += cantRevertir;

                var revMov = new MovimientoInventario
                {
                    IngredienteId = ingrediente.Id,
                    TipoMovimiento = "ReversionAnulacion",
                    Cantidad = cantRevertir,
                    CostoUnitario = mov.CostoUnitario,
                    Total = mov.Total,
                    Fecha = DateTime.Now,
                    Referencia = $"ANULACION-VENTA-{ventaId}",
                    Observaciones = $"Reversión de salida por anulación de Venta #{ventaId}",
                    UsuarioId = usuario ?? "Sistema"
                };

                _context.MovimientosInventario.Add(revMov);
                _context.Ingredientes.Update(ingrediente);
            }
        }

        await _context.SaveChangesAsync();
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

                var mov = new MovimientoInventario
                {
                    IngredienteId = ingrediente.Id,
                    TipoMovimiento = "EntradaCompra",
                    Cantidad = detalle.Cantidad,
                    CostoUnitario = detalle.CostoUnitario,
                    Total = detalle.Subtotal,
                    Fecha = DateTime.Now,
                    Referencia = $"COMPRA-{compraId}",
                    Observaciones = $"Entrada por Compra Factura #{compra.NumeroFactura}",
                    UsuarioId = usuario ?? "Sistema"
                };

                _context.MovimientosInventario.Add(mov);
                _context.Ingredientes.Update(ingrediente);
            }
        }

        await _context.SaveChangesAsync();
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
