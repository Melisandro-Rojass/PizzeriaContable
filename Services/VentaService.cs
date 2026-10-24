using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;

namespace PizzeriaContable.Services;

public class VentaService : IVentaService
{
    private readonly ApplicationDbContext _context;
    private readonly IInventarioService _inventarioService;
    private readonly IContabilidadService _contabilidadService;
    private readonly ICajaService _cajaService;

    public VentaService(
        ApplicationDbContext context,
        IInventarioService inventarioService,
        IContabilidadService contabilidadService,
        ICajaService cajaService)
    {
        _context = context;
        _inventarioService = inventarioService;
        _contabilidadService = contabilidadService;
        _cajaService = cajaService;
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
        if (detalles == null || !detalles.Any())
        {
            throw new ArgumentException("La venta debe contener al menos un producto.");
        }

        if (pagos == null || !pagos.Any())
        {
            throw new ArgumentException("Debe registrarse al menos un método de pago.");
        }

        // Ejecutar en transacción atómica de base de datos
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Número de comprobante consecutivo
            venta.NumeroComprobante = await GenerarNumeroComprobanteAsync();
            venta.Fecha = DateTime.Now;
            venta.Estado = "Completada";
            venta.UsuarioId = usuario;

            // 2. Calcular montos
            decimal subtotal = detalles.Sum(d => d.Cantidad * d.PrecioUnitario - d.Descuento);
            venta.Subtotal = subtotal;

            var config = await _context.Configuraciones.FirstOrDefaultAsync();
            decimal tasaIva = config?.TasaIvaPorcentaje ?? 15.00m;
            decimal impuesto = Math.Round(subtotal * (tasaIva / 100m), 2);
            venta.Impuesto = impuesto;
            venta.Total = subtotal + impuesto - venta.Descuento;

            decimal totalPagado = pagos.Sum(p => p.Monto);
            if (Math.Abs(totalPagado - venta.Total) > 0.05m)
            {
                throw new InvalidOperationException($"El total pagado ({totalPagado:C2}) no coincide con el total de la venta ({venta.Total:C2}).");
            }

            // Asociar sesión de caja activa si no viene indicada
            if (!venta.CajaSesionId.HasValue)
            {
                var sesionCaja = await _cajaService.ObtenerSesionActivaAsync(usuario);
                if (sesionCaja != null)
                {
                    venta.CajaSesionId = sesionCaja.Id;
                }
            }

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();

            // 3. Guardar detalles
            foreach (var det in detalles)
            {
                det.VentaId = venta.Id;
                det.Subtotal = (det.Cantidad * det.PrecioUnitario) - det.Descuento;
                _context.VentaDetalles.Add(det);
            }

            // 4. Guardar pagos
            foreach (var pago in pagos)
            {
                pago.VentaId = venta.Id;
                _context.VentaPagos.Add(pago);

                // Si hay caja abierta, actualizar totales de caja
                if (venta.CajaSesionId.HasValue)
                {
                    await _cajaService.ActualizarTotalesPorVentaAsync(venta.CajaSesionId.Value, pago.MetodoPago, pago.Monto);
                }
            }

            await _context.SaveChangesAsync();

            // 5. Descuento de ingredientes del inventario según receta
            await _inventarioService.DescontarInventarioPorVentaAsync(venta.Id, usuario);

            // 6. Contabilización automática de doble partida
            // DEBE: Caja (1.1.01.01) o Bancos (1.1.02.01)
            // HABER: Ingresos (4.1.01)
            // HABER: IVA por pagar (2.1.03.01)
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

            // 7. Registro de auditoría
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

            await transaction.CommitAsync();
            return venta;
        }
        catch
        {
            await transaction.RollbackAsync();
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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
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
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
