using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PizzeriaContable.Data;
using PizzeriaContable.Models;

namespace PizzeriaContable.Services;

public class VentaService : IVentaService
{
    private readonly ApplicationDbContext _context;
    private readonly IInventarioService _inventarioService;
    private readonly IContabilidadService _contabilidadService;
    private readonly ICajaService _cajaService;
    private readonly ILogger<VentaService>? _logger;

    public VentaService(
        ApplicationDbContext context,
        IInventarioService inventarioService,
        IContabilidadService contabilidadService,
        ICajaService cajaService,
        ILogger<VentaService>? logger = null)
    {
        _context = context;
        _inventarioService = inventarioService;
        _contabilidadService = contabilidadService;
        _cajaService = cajaService;
        _logger = logger;
    }

    public async Task<string> GenerarNumeroComprobanteAsync()
    {
        int countHoy = await _context.Ventas
            .Where(v => v.Fecha.Date == DateTime.Today)
            .CountAsync() + 1;

        return $"TCK-{DateTime.Today:yyyyMMdd}-{countHoy:D4}";
    }

    public async Task<Venta> CrearVentaTransaccionalAsync(
        Venta venta, 
        List<VentaDetalle> detalles, 
        List<VentaPago> pagos, 
        string? usuario)
    {
        string etapa = "Validación inicial";

        if (detalles == null || !detalles.Any())
        {
            throw new ArgumentException("La venta debe contener al menos un producto.");
        }

        if (pagos == null || !pagos.Any())
        {
            throw new ArgumentException("Debe registrarse al menos un método de pago.");
        }

        // 1. Validar productos, existencia, estado activo y precios oficiales desde la base de datos
        etapa = "Validación de catálogo y precios";
        foreach (var det in detalles)
        {
            if (det.Cantidad <= 0)
            {
                throw new ArgumentException("La cantidad vendida de cada producto debe ser mayor a cero.");
            }

            var producto = await _context.Productos.FindAsync(det.ProductoId)
                ?? throw new ArgumentException($"El producto con ID {det.ProductoId} no existe en el catálogo.");

            if (!producto.Activo)
            {
                throw new InvalidOperationException($"El producto '{producto.Nombre}' está inactivo y no puede venderse.");
            }

            // Asegurar precio oficial del servidor
            det.PrecioUnitario = producto.Precio;

            if (det.Descuento < 0 || det.Descuento > (det.Cantidad * det.PrecioUnitario))
            {
                throw new ArgumentException($"El descuento para el producto '{producto.Nombre}' no es válido.");
            }

            det.Subtotal = Math.Round((det.Cantidad * det.PrecioUnitario) - det.Descuento, 2);
        }

        // 2. Validar métodos de pago
        etapa = "Validación de pagos";
        var metodosPermitidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Efectivo", "Tarjeta", "Transferencia" };
        foreach (var pago in pagos)
        {
            if (string.IsNullOrWhiteSpace(pago.MetodoPago) || !metodosPermitidos.Contains(pago.MetodoPago.Trim()))
            {
                throw new ArgumentException($"El método de pago '{pago.MetodoPago}' no es válido.");
            }

            if (pago.Monto <= 0)
            {
                throw new ArgumentException("El monto a pagar de cada método debe ser mayor a cero.");
            }
        }

        // 3. Iniciar transacción atómica en la base de datos (siempre que sea relacional y no exista una ya abierta)
        etapa = "Inicio de transacción";
        bool hasRootTransaction = _context.Database.IsRelational() && _context.Database.CurrentTransaction == null;
        await using var transaction = hasRootTransaction ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            _logger?.LogInformation("Iniciando transacción de venta para usuario '{Usuario}' (RootTransaction: {IsRoot}).", usuario, hasRootTransaction);

            // 4. Asignar datos de cabecera y calcular montos server-side
            etapa = "Cálculo de montos y cabecera";
            venta.NumeroComprobante = await GenerarNumeroComprobanteAsync();
            venta.Fecha = DateTime.Now;
            venta.Estado = "Completada";
            venta.UsuarioId = usuario;

            decimal subtotal = detalles.Sum(d => d.Subtotal);
            venta.Subtotal = subtotal;

            var config = await _context.Configuraciones.FirstOrDefaultAsync();
            decimal tasaIva = config?.TasaIvaPorcentaje ?? 15.00m;
            decimal impuesto = Math.Round(subtotal * (tasaIva / 100m), 2);
            venta.Impuesto = impuesto;

            decimal descuentoGeneral = venta.Descuento > 0 ? venta.Descuento : 0;
            venta.Descuento = descuentoGeneral;
            venta.Total = Math.Round(subtotal + impuesto - descuentoGeneral, 2);

            decimal totalPagado = Math.Round(pagos.Sum(p => p.Monto), 2);
            if (Math.Abs(totalPagado - venta.Total) > 0.05m)
            {
                throw new InvalidOperationException($"El total pagado ({totalPagado:C2}) no coincide con el total de la venta ({venta.Total:C2}).");
            }

            // 5. Vincular turno de caja activo
            etapa = "Asociación de caja";
            if (!venta.CajaSesionId.HasValue)
            {
                var sesionCaja = await _cajaService.ObtenerSesionActivaAsync(usuario);
                if (sesionCaja != null)
                {
                    venta.CajaSesionId = sesionCaja.Id;
                }
            }

            // 6. Persistir venta
            etapa = "Persistencia de venta";
            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();

            // 7. Persistir detalles
            etapa = "Persistencia de detalles";
            foreach (var det in detalles)
            {
                det.VentaId = venta.Id;
                _context.VentaDetalles.Add(det);
            }

            // 8. Persistir pagos y actualizar acumulados de caja
            etapa = "Persistencia de pagos y actualización de caja";
            foreach (var pago in pagos)
            {
                pago.VentaId = venta.Id;
                _context.VentaPagos.Add(pago);

                if (venta.CajaSesionId.HasValue)
                {
                    await _cajaService.ActualizarTotalesPorVentaAsync(venta.CajaSesionId.Value, pago.MetodoPago, pago.Monto);
                }
            }

            await _context.SaveChangesAsync();

            // 9. Descontar inventario según recetas
            etapa = "Descuento de inventario";
            await _inventarioService.DescontarInventarioPorVentaAsync(venta.Id, usuario);

            // 10. Contabilización automática de doble partida (participa en la transacción activa)
            etapa = "Contabilización de partida doble";
            var apuntes = new List<(string codigoCuenta, decimal debe, decimal haber, string? detalle)>();

            foreach (var p in pagos)
            {
                string ctaCobro = p.MetodoPago.Equals("Efectivo", StringComparison.OrdinalIgnoreCase) 
                    ? "1.1.01.01" 
                    : "1.1.02.01";
                apuntes.Add((ctaCobro, p.Monto, 0, $"Cobro venta {venta.NumeroComprobante} ({p.MetodoPago})"));
            }

            apuntes.Add(("4.1.01", 0, venta.Subtotal, $"Ingreso por ventas Ticket {venta.NumeroComprobante}"));
            if (venta.Impuesto > 0)
            {
                apuntes.Add(("2.1.03.01", 0, venta.Impuesto, $"IVA Débito Fiscal Ticket {venta.NumeroComprobante}"));
            }

            await _contabilidadService.CrearAsientoAutomaticoAsync(
                $"Venta Ticket #{venta.NumeroComprobante} - Cliente: {venta.ClienteNombre}",
                "Venta",
                venta.Id,
                apuntes,
                usuario);

            // 11. Registro de auditoría
            etapa = "Registro de auditoría";
            _context.Auditorias.Add(new AuditoriaRegistro
            {
                Modulo = "Ventas",
                Accion = "Crear",
                Entidad = "Venta",
                EntidadId = venta.Id.ToString(),
                Detalle = $"Venta {venta.NumeroComprobante} por valor de {venta.Total:C2}",
                FechaHora = DateTime.Now,
                UsuarioId = usuario,
                UsuarioNombre = usuario
            });
            await _context.SaveChangesAsync();

            // 12. Confirmar transacción
            etapa = "Commit de transacción";
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            _logger?.LogInformation("Venta {Comprobante} (ID: {VentaId}) confirmada exitosamente con total {Total:C2}.",
                venta.NumeroComprobante, venta.Id, venta.Total);

            return venta;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error en VentaService.CrearVentaTransaccionalAsync durante etapa '{Etapa}'. Ejecutando rollback.", etapa);
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }

    public async Task AnularVentaAsync(int ventaId, string motivo, string? usuario)
    {
        var venta = await _context.Ventas
            .Include(v => v.Detalles)
            .Include(v => v.Pagos)
            .FirstOrDefaultAsync(v => v.Id == ventaId)
            ?? throw new ArgumentException($"Venta ID {ventaId} no encontrada.");

        if (venta.Estado == "Anulada")
        {
            throw new InvalidOperationException("La venta ya ha sido anulada anteriormente.");
        }

        bool hasRootTransaction = _context.Database.IsRelational() && _context.Database.CurrentTransaction == null;
        await using var transaction = hasRootTransaction ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            _logger?.LogInformation("Iniciando anulación de venta {VentaId} por usuario '{Usuario}'.", ventaId, usuario);

            venta.Estado = "Anulada";
            venta.Observaciones = $"[ANULADA el {DateTime.Now:dd/MM/yyyy HH:mm} por {usuario}]. Motivo: {motivo}. " + venta.Observaciones;
            _context.Ventas.Update(venta);

            // 1. Revertir inventario
            await _inventarioService.RevertirInventarioPorAnulacionVentaAsync(venta.Id, usuario);

            // 2. Anular asiento contable previo o crear asiento de reversión
            var asientoOriginal = await _context.AsientosContables
                .Include(a => a.Detalles)
                .FirstOrDefaultAsync(a => a.TipoOrigen == "Venta" && a.DocumentoOrigenId == ventaId && !a.Anulado);

            if (asientoOriginal != null)
            {
                asientoOriginal.Anulado = true;
                _context.AsientosContables.Update(asientoOriginal);

                // Crear contra-asiento para trazabilidad
                var contraApuntes = asientoOriginal.Detalles.Select(d => (
                    d.CuentaContableId,
                    d.Haber, // Invertir Debe y Haber
                    d.Debe,
                    $"Contra-asiento por anulación de Venta #{venta.NumeroComprobante}"
                )).ToList();

                int totalAsientos = await _context.AsientosContables.CountAsync() + 1;
                var contraAsiento = new AsientoContable
                {
                    NumeroAsiento = $"AS-{DateTime.Now.Year}-{totalAsientos:D5}",
                    Fecha = DateTime.Today,
                    Concepto = $"Reversión de Venta #{venta.NumeroComprobante} - Motivo: {motivo}",
                    TipoOrigen = "ReversionVenta",
                    DocumentoOrigenId = venta.Id,
                    Contabilizado = true,
                    Anulado = false,
                    FechaCreacion = DateTime.Now,
                    CreadoPor = usuario ?? "Sistema",
                    Detalles = contraApuntes.Select(ca => new AsientoDetalle
                    {
                        CuentaContableId = ca.CuentaContableId,
                        Debe = ca.Item2,
                        Haber = ca.Item3,
                        Descripcion = ca.Item4
                    }).ToList()
                };
                _context.AsientosContables.Add(contraAsiento);
            }

            // 3. Auditoría
            _context.Auditorias.Add(new AuditoriaRegistro
            {
                Modulo = "Ventas",
                Accion = "Anular",
                Entidad = "Venta",
                EntidadId = venta.Id.ToString(),
                Detalle = $"Anulación de venta {venta.NumeroComprobante}. Motivo: {motivo}",
                FechaHora = DateTime.Now,
                UsuarioId = usuario,
                UsuarioNombre = usuario
            });

            await _context.SaveChangesAsync();
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            _logger?.LogInformation("Venta {VentaId} anulada y revertida exitosamente.", ventaId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error al anular venta {VentaId}. Ejecutando rollback.", ventaId);
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }
}
