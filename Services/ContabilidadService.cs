using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;
using PizzeriaContable.ViewModels;

namespace PizzeriaContable.Services;

public class ContabilidadService : IContabilidadService
{
    private readonly ApplicationDbContext _context;

    public ContabilidadService(ApplicationDbContext context)
    {
        _context = context;
    }

    public bool ValidarAsientoEquilibrado(IEnumerable<AsientoDetalle> detalles)
    {
        if (detalles == null) return false;
        var lista = detalles.ToList();
        if (lista.Count < 2) return false;

        var totalDebe = lista.Sum(d => d.Debe);
        var totalHaber = lista.Sum(d => d.Haber);

        if (totalDebe <= 0 || totalHaber <= 0) return false;

        return Math.Abs(totalDebe - totalHaber) < 0.01m;
    }

    public async Task<AsientoContable> CrearAsientoAutomaticoAsync(
        string concepto,
        string tipoOrigen,
        int? documentoId,
        List<(string codigoCuenta, decimal debe, decimal haber, string? detalle)> apuntes,
        string? usuario)
    {
        if (apuntes == null || apuntes.Count < 2)
        {
            throw new InvalidOperationException("Un asiento contable debe contener al menos dos apuntes (partida doble).");
        }

        // 1. Prevención de doble contabilización para el mismo origen
        if (documentoId.HasValue && tipoOrigen != "Manual" && tipoOrigen != "Demostracion")
        {
            var existe = await _context.AsientosContables
                .AnyAsync(a => a.TipoOrigen == tipoOrigen && a.DocumentoOrigenId == documentoId.Value && !a.Anulado);

            if (existe)
            {
                throw new InvalidOperationException($"El documento {tipoOrigen} con ID {documentoId} ya se encuentra contabilizado.");
            }
        }

        // 2. Resolver cuentas contables por código y validar importes
        var detalles = new List<AsientoDetalle>();
        foreach (var apunte in apuntes)
        {
            if (apunte.debe == 0 && apunte.haber == 0)
            {
                continue;
            }

            if (apunte.debe < 0 || apunte.haber < 0)
            {
                throw new InvalidOperationException("No se permiten importes contables negativos.");
            }

            if (apunte.debe > 0 && apunte.haber > 0)
            {
                throw new InvalidOperationException("Un apunte no puede registrar importes simultáneos al Debe y al Haber.");
            }

            var cuenta = await _context.CuentasContables
                .FirstOrDefaultAsync(c => c.Codigo == apunte.codigoCuenta && c.Activa);

            if (cuenta == null)
            {
                throw new InvalidOperationException($"La cuenta contable con código '{apunte.codigoCuenta}' no existe o no está activa.");
            }

            if (!cuenta.PermiteMovimiento)
            {
                throw new InvalidOperationException($"La cuenta '{cuenta.Codigo} - {cuenta.Nombre}' es de agrupación (rubro superior) y no permite movimientos directos.");
            }

            detalles.Add(new AsientoDetalle
            {
                CuentaContableId = cuenta.Id,
                Debe = Math.Round(apunte.debe, 2),
                Haber = Math.Round(apunte.haber, 2),
                Descripcion = apunte.detalle ?? concepto
            });
        }

        // 3. Regla obligatoria de partida doble: SUMA(DEBE) == SUMA(HABER)
        if (!ValidarAsientoEquilibrado(detalles))
        {
            var totalD = detalles.Sum(d => d.Debe);
            var totalH = detalles.Sum(d => d.Haber);
            throw new InvalidOperationException($"El asiento no cumple el principio de partida doble. Total Debe: {totalD:C2}, Total Haber: {totalH:C2}. Diferencia: {Math.Abs(totalD - totalH):C2}");
        }

        var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            // 4. Generar consecutivo de asiento
            int totalAsientos = await _context.AsientosContables.CountAsync() + 1;
            string numeroAsiento = $"AS-{DateTime.Now.Year}-{totalAsientos:D5}";

            var asiento = new AsientoContable
            {
                NumeroAsiento = numeroAsiento,
                Fecha = DateTime.Today,
                Concepto = concepto,
                TipoOrigen = tipoOrigen,
                DocumentoOrigenId = documentoId,
                Contabilizado = true,
                Anulado = false,
                FechaCreacion = DateTime.Now,
                CreadoPor = usuario ?? "Sistema",
                Detalles = detalles
            };

            _context.AsientosContables.Add(asiento);
            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();

            return asiento;
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (transaction != null) await transaction.DisposeAsync();
        }
    }

    public async Task<AsientoContable> CrearAsientoManualAsync(AsientoManualViewModel model, string? usuario)
    {
        if (model.Lineas == null || model.Lineas.Count < 2)
        {
            throw new InvalidOperationException("Un asiento contable de partida doble debe contener al menos 2 líneas de movimiento.");
        }

        var detalles = new List<AsientoDetalle>();

        foreach (var linea in model.Lineas)
        {
            if (linea.Debe == 0 && linea.Haber == 0) continue;

            if (linea.Debe < 0 || linea.Haber < 0)
            {
                throw new InvalidOperationException("No se permiten importes contables negativos.");
            }

            if (linea.Debe > 0 && linea.Haber > 0)
            {
                throw new InvalidOperationException("Una línea contable no puede tener montos simultáneos en Debe y Haber.");
            }

            var cuenta = await _context.CuentasContables.FindAsync(linea.CuentaContableId);
            if (cuenta == null || !cuenta.Activa)
            {
                throw new InvalidOperationException($"La cuenta seleccionada (ID: {linea.CuentaContableId}) no existe o está inactiva.");
            }

            if (!cuenta.PermiteMovimiento)
            {
                throw new InvalidOperationException($"La cuenta '{cuenta.Codigo} - {cuenta.Nombre}' es de nivel superior y no acepta imputaciones directas.");
            }

            detalles.Add(new AsientoDetalle
            {
                CuentaContableId = cuenta.Id,
                Debe = Math.Round(linea.Debe, 2),
                Haber = Math.Round(linea.Haber, 2),
                Descripcion = string.IsNullOrWhiteSpace(linea.Descripcion) ? model.Concepto : linea.Descripcion
            });
        }

        if (!ValidarAsientoEquilibrado(detalles))
        {
            decimal totalD = detalles.Sum(d => d.Debe);
            decimal totalH = detalles.Sum(d => d.Haber);
            throw new InvalidOperationException($"El asiento no cumple el equilibrio de partida doble. Suma Debe: {totalD:C2} | Suma Haber: {totalH:C2}.");
        }

        var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            int totalAsientos = await _context.AsientosContables.CountAsync() + 1;
            string numAsiento = $"AS-{model.Fecha.Year}-{totalAsientos:D5}";

            var asiento = new AsientoContable
            {
                NumeroAsiento = numAsiento,
                Fecha = model.Fecha,
                Concepto = model.Concepto,
                TipoOrigen = "Manual",
                Contabilizado = true,
                Anulado = false,
                FechaCreacion = DateTime.Now,
                CreadoPor = usuario ?? "Admin",
                Detalles = detalles
            };

            _context.AsientosContables.Add(asiento);
            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();

            return asiento;
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (transaction != null) await transaction.DisposeAsync();
        }
    }

    public async Task<bool> AnularAsientoAsync(int asientoId, string motivo, string? usuario)
    {
        var asiento = await _context.AsientosContables
            .Include(a => a.Detalles)
            .FirstOrDefaultAsync(a => a.Id == asientoId);

        if (asiento == null) return false;
        if (asiento.Anulado)
        {
            throw new InvalidOperationException($"El asiento {asiento.NumeroAsiento} ya se encuentra anulado.");
        }

        var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            asiento.Anulado = true;
            asiento.FechaAnulacion = DateTime.Now;
            asiento.MotivoAnulacion = string.IsNullOrWhiteSpace(motivo) ? "Anulado por el usuario" : motivo;

            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();
            return true;
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (transaction != null) await transaction.DisposeAsync();
        }
    }

    public async Task<ResumenCuentasTViewModel> ObtenerResumenCuentasTAsync(
        DateTime? desde, DateTime? hasta, string? tipo, string? search)
    {
        var queryCuentas = _context.CuentasContables
            .Where(c => c.Activa && c.PermiteMovimiento)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            queryCuentas = queryCuentas.Where(c => c.TipoCuenta == tipo);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryCuentas = queryCuentas.Where(c => c.Codigo.Contains(search) || c.Nombre.Contains(search));
        }

        var cuentas = await queryCuentas.OrderBy(c => c.Codigo).ToListAsync();

        var queryMovimientos = _context.AsientoDetalles
            .Include(ad => ad.AsientoContable)
            .Where(ad => !ad.AsientoContable!.Anulado)
            .AsQueryable();

        if (desde.HasValue)
        {
            queryMovimientos = queryMovimientos.Where(ad => ad.AsientoContable!.Fecha >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            queryMovimientos = queryMovimientos.Where(ad => ad.AsientoContable!.Fecha <= hasta.Value.Date);
        }

        var movimientos = await queryMovimientos
            .OrderBy(ad => ad.AsientoContable!.Fecha)
            .ThenBy(ad => ad.AsientoContableId)
            .ToListAsync();

        var cuentasTVm = new List<CuentaTViewModel>();

        foreach (var c in cuentas)
        {
            var movsCuenta = movimientos.Where(m => m.CuentaContableId == c.Id).ToList();

            var vm = new CuentaTViewModel
            {
                CuentaId = c.Id,
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                TipoCuenta = c.TipoCuenta,
                Naturaleza = c.Naturaleza,
                Nivel = c.Nivel,
                PermiteMovimiento = c.PermiteMovimiento,
                MovimientosDebe = movsCuenta
                    .Where(m => m.Debe > 0)
                    .Select(m => new MovimientoTLineaDto
                    {
                        AsientoId = m.AsientoContableId,
                        NumeroAsiento = m.AsientoContable?.NumeroAsiento ?? string.Empty,
                        Fecha = m.AsientoContable?.Fecha ?? DateTime.Today,
                        Concepto = m.AsientoContable?.Concepto ?? string.Empty,
                        Detalle = m.Descripcion,
                        Importe = m.Debe
                    }).ToList(),
                MovimientosHaber = movsCuenta
                    .Where(m => m.Haber > 0)
                    .Select(m => new MovimientoTLineaDto
                    {
                        AsientoId = m.AsientoContableId,
                        NumeroAsiento = m.AsientoContable?.NumeroAsiento ?? string.Empty,
                        Fecha = m.AsientoContable?.Fecha ?? DateTime.Today,
                        Concepto = m.AsientoContable?.Concepto ?? string.Empty,
                        Detalle = m.Descripcion,
                        Importe = m.Haber
                    }).ToList()
            };

            cuentasTVm.Add(vm);
        }

        return new ResumenCuentasTViewModel
        {
            Desde = desde,
            Hasta = hasta,
            TipoSeleccionado = tipo,
            Busqueda = search,
            Cuentas = cuentasTVm
        };
    }

    public async Task<CuentaTViewModel?> ObtenerCuentaTDetalladaAsync(int cuentaId, DateTime? desde, DateTime? hasta)
    {
        var cuenta = await _context.CuentasContables.FindAsync(cuentaId);
        if (cuenta == null) return null;

        var query = _context.AsientoDetalles
            .Include(ad => ad.AsientoContable)
            .Where(ad => ad.CuentaContableId == cuentaId && !ad.AsientoContable!.Anulado)
            .AsQueryable();

        if (desde.HasValue)
        {
            query = query.Where(ad => ad.AsientoContable!.Fecha >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            query = query.Where(ad => ad.AsientoContable!.Fecha <= hasta.Value.Date);
        }

        var movimientos = await query
            .OrderBy(ad => ad.AsientoContable!.Fecha)
            .ThenBy(ad => ad.AsientoContableId)
            .ToListAsync();

        var movsDebe = new List<MovimientoTLineaDto>();
        var movsHaber = new List<MovimientoTLineaDto>();

        decimal saldoAcumulado = 0m;

        foreach (var m in movimientos)
        {
            if (cuenta.Naturaleza == "Deudora")
            {
                saldoAcumulado += (m.Debe - m.Haber);
            }
            else
            {
                saldoAcumulado += (m.Haber - m.Debe);
            }

            var dto = new MovimientoTLineaDto
            {
                AsientoId = m.AsientoContableId,
                NumeroAsiento = m.AsientoContable?.NumeroAsiento ?? string.Empty,
                Fecha = m.AsientoContable?.Fecha ?? DateTime.Today,
                Concepto = m.AsientoContable?.Concepto ?? string.Empty,
                Detalle = m.Descripcion,
                Importe = m.Debe > 0 ? m.Debe : m.Haber,
                SaldoAcumulado = saldoAcumulado
            };

            if (m.Debe > 0)
            {
                movsDebe.Add(dto);
            }
            else if (m.Haber > 0)
            {
                movsHaber.Add(dto);
            }
        }

        return new CuentaTViewModel
        {
            CuentaId = cuenta.Id,
            Codigo = cuenta.Codigo,
            Nombre = cuenta.Nombre,
            TipoCuenta = cuenta.TipoCuenta,
            Naturaleza = cuenta.Naturaleza,
            Nivel = cuenta.Nivel,
            PermiteMovimiento = cuenta.PermiteMovimiento,
            MovimientosDebe = movsDebe,
            MovimientosHaber = movsHaber
        };
    }

    public async Task<AsientoContable> RegistrarVentaContadoEjemploAsync(string? usuario)
    {
        await InicializarCatalogoCuentasAsync();

        var apuntes = new List<(string codigoCuenta, decimal debe, decimal haber, string? detalle)>
        {
            ("1.1.01.01", 1000.00m, 0.00m, "Venta contado: Ingreso de efectivo a Caja Principal"),
            ("4.1.01", 0.00m, 1000.00m, "Venta contado: Ingreso devengado por ventas de pizzas")
        };

        return await CrearAsientoAutomaticoAsync(
            "Demostración Didáctica: Venta de pizzas de contado por C$ 1,000.00 (Sin impuestos)",
            "Demostracion",
            null,
            apuntes,
            usuario ?? "Demostración Cuentas T"
        );
    }

    public async Task<List<AsientoContable>> ObtenerLibroDiarioAsync(
        DateTime? desde, DateTime? hasta, string? tipoOrigen = null, int? cuentaId = null, string? search = null)
    {
        IQueryable<AsientoContable> query = _context.AsientosContables
            .Include(a => a.Detalles)
                .ThenInclude(d => d.CuentaContable)
            .AsQueryable();

        if (desde.HasValue)
        {
            query = query.Where(a => a.Fecha >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            query = query.Where(a => a.Fecha <= hasta.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(tipoOrigen))
        {
            query = query.Where(a => a.TipoOrigen == tipoOrigen);
        }

        if (cuentaId.HasValue)
        {
            query = query.Where(a => a.Detalles.Any(d => d.CuentaContableId == cuentaId.Value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.NumeroAsiento.Contains(search) || a.Concepto.Contains(search));
        }

        return await query.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id).ToListAsync();
    }

    public async Task<(CuentaContable cuenta, List<AsientoDetalle> movimientos, decimal saldoDeudor, decimal saldoAcreedor)> 
        ObtenerLibroMayorAsync(int cuentaId, DateTime? desde, DateTime? hasta)
    {
        var cuenta = await _context.CuentasContables.FindAsync(cuentaId)
            ?? throw new ArgumentException($"Cuenta con ID {cuentaId} no encontrada.");

        IQueryable<AsientoDetalle> query = _context.AsientoDetalles
            .Include(ad => ad.AsientoContable)
            .Where(ad => ad.CuentaContableId == cuentaId && !ad.AsientoContable!.Anulado);

        if (desde.HasValue)
        {
            query = query.Where(ad => ad.AsientoContable!.Fecha >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            query = query.Where(ad => ad.AsientoContable!.Fecha <= hasta.Value.Date);
        }

        var movimientos = await query.OrderBy(ad => ad.AsientoContable!.Fecha).ToListAsync();
        var totalDebe = movimientos.Sum(m => m.Debe);
        var totalHaber = movimientos.Sum(m => m.Haber);

        decimal saldoDeudor = 0;
        decimal saldoAcreedor = 0;

        if (cuenta.Naturaleza == "Deudora")
        {
            saldoDeudor = totalDebe - totalHaber;
        }
        else
        {
            saldoAcreedor = totalHaber - totalDebe;
        }

        return (cuenta, movimientos, saldoDeudor, saldoAcreedor);
    }

    public async Task<List<BalanceComprobacionItem>> ObtenerBalanceComprobacionAsync(DateTime? desde, DateTime? hasta)
    {
        var cuentas = await _context.CuentasContables
            .Where(c => c.Activa && c.PermiteMovimiento)
            .OrderBy(c => c.Codigo)
            .ToListAsync();

        IQueryable<AsientoDetalle> query = _context.AsientoDetalles
            .Include(ad => ad.AsientoContable)
            .Where(ad => !ad.AsientoContable!.Anulado);

        if (desde.HasValue)
        {
            query = query.Where(ad => ad.AsientoContable!.Fecha >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            query = query.Where(ad => ad.AsientoContable!.Fecha <= hasta.Value.Date);
        }

        var movimientos = await query.ToListAsync();

        var resultado = new List<BalanceComprobacionItem>();

        foreach (var c in cuentas)
        {
            var movsCuenta = movimientos.Where(m => m.CuentaContableId == c.Id).ToList();
            var debe = movsCuenta.Sum(m => m.Debe);
            var haber = movsCuenta.Sum(m => m.Haber);

            decimal sDeudor = 0;
            decimal sAcreedor = 0;

            if (c.Naturaleza == "Deudora")
            {
                var dif = debe - haber;
                if (dif >= 0) sDeudor = dif;
                else sAcreedor = Math.Abs(dif);
            }
            else
            {
                var dif = haber - debe;
                if (dif >= 0) sAcreedor = dif;
                else sDeudor = Math.Abs(dif);
            }

            if (debe > 0 || haber > 0)
            {
                resultado.Add(new BalanceComprobacionItem
                {
                    Codigo = c.Codigo,
                    Nombre = c.Nombre,
                    TipoCuenta = c.TipoCuenta,
                    Naturaleza = c.Naturaleza,
                    TotalDebe = debe,
                    TotalHaber = haber,
                    SaldoDeudor = sDeudor,
                    SaldoAcreedor = sAcreedor
                });
            }
        }

        return resultado;
    }

    public async Task<EstadoResultadosViewModel> ObtenerEstadoResultadosAsync(DateTime desde, DateTime hasta)
    {
        var balance = await ObtenerBalanceComprobacionAsync(desde, hasta);

        var ingresos = balance.Where(b => b.TipoCuenta == "Ingreso").ToList();
        var costos = balance.Where(b => b.TipoCuenta == "Costo").ToList();
        var gastos = balance.Where(b => b.TipoCuenta == "Gasto").ToList();

        decimal totalIngresos = ingresos.Sum(i => i.SaldoAcreedor - i.SaldoDeudor);
        decimal totalCostos = costos.Sum(c => c.SaldoDeudor - c.SaldoAcreedor);
        decimal totalGastos = gastos.Sum(g => g.SaldoDeudor - g.SaldoAcreedor);

        return new EstadoResultadosViewModel
        {
            FechaDesde = desde,
            FechaHasta = hasta,
            TotalIngresos = Math.Max(0, totalIngresos),
            TotalCostosVentas = Math.Max(0, totalCostos),
            TotalGastosOperativos = Math.Max(0, totalGastos),
            DetalleIngresos = ingresos.Select(i => (i.Nombre, i.SaldoAcreedor - i.SaldoDeudor)).ToList(),
            DetalleGastos = gastos.Select(g => (g.Nombre, g.SaldoDeudor - g.SaldoAcreedor)).ToList()
        };
    }

    public async Task<BalanceGeneralViewModel> ObtenerBalanceGeneralAsync(DateTime fechaCorte)
    {
        var balance = await ObtenerBalanceComprobacionAsync(null, fechaCorte);

        var activos = balance.Where(b => b.TipoCuenta == "Activo")
            .Select(b => (b.Codigo, b.Nombre, b.SaldoDeudor - b.SaldoAcreedor))
            .Where(x => x.Item3 != 0)
            .ToList();

        var pasivos = balance.Where(b => b.TipoCuenta == "Pasivo")
            .Select(b => (b.Codigo, b.Nombre, b.SaldoAcreedor - b.SaldoDeudor))
            .Where(x => x.Item3 != 0)
            .ToList();

        var patrimonio = balance.Where(b => b.TipoCuenta == "Capital")
            .Select(b => (b.Codigo, b.Nombre, b.SaldoAcreedor - b.SaldoDeudor))
            .Where(x => x.Item3 != 0)
            .ToList();

        // Utilidad del período = Ingresos - Costos - Gastos
        decimal totalIng = balance.Where(b => b.TipoCuenta == "Ingreso").Sum(i => i.SaldoAcreedor - i.SaldoDeudor);
        decimal totalCos = balance.Where(b => b.TipoCuenta == "Costo").Sum(c => c.SaldoDeudor - c.SaldoAcreedor);
        decimal totalGas = balance.Where(b => b.TipoCuenta == "Gasto").Sum(g => g.SaldoDeudor - g.SaldoAcreedor);
        decimal utilidad = totalIng - totalCos - totalGas;

        return new BalanceGeneralViewModel
        {
            FechaCorte = fechaCorte,
            TotalActivos = activos.Sum(a => a.Item3),
            TotalPasivos = pasivos.Sum(p => p.Item3),
            TotalPatrimonio = patrimonio.Sum(p => p.Item3),
            UtilidadPeriodo = utilidad,
            Activos = activos,
            Pasivos = pasivos,
            Patrimonio = patrimonio
        };
    }

    public async Task InicializarCatalogoCuentasAsync()
    {
        if (await _context.CuentasContables.AnyAsync())
        {
            return;
        }

        // Catálogo contable estándar para negocio gastronómico / pizzería (Nicaragua)
        var cuentas = new List<CuentaContable>
        {
            // 1. ACTIVOS
            new CuentaContable { Codigo = "1", Nombre = "ACTIVO", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 1, PermiteMovimiento = false },
            new CuentaContable { Codigo = "1.1", Nombre = "ACTIVO CORRIENTE", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 2, PermiteMovimiento = false },
            new CuentaContable { Codigo = "1.1.01", Nombre = "Caja General", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = false },
            new CuentaContable { Codigo = "1.1.01.01", Nombre = "Efectivo en Caja Principal", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 4, PermiteMovimiento = true },
            new CuentaContable { Codigo = "1.1.02", Nombre = "Bancos y Cuentas de Ahorro", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = false },
            new CuentaContable { Codigo = "1.1.02.01", Nombre = "Banco Cuenta Corriente NIO", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 4, PermiteMovimiento = true },
            new CuentaContable { Codigo = "1.1.03", Nombre = "Cuentas por Cobrar Comerciales", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = false },
            new CuentaContable { Codigo = "1.1.03.01", Nombre = "Clientes Locales", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 4, PermiteMovimiento = true },
            new CuentaContable { Codigo = "1.1.04", Nombre = "Inventarios", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = false },
            new CuentaContable { Codigo = "1.1.04.01", Nombre = "Inventario de Materias Primas e Ingredientes", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 4, PermiteMovimiento = true },
            new CuentaContable { Codigo = "1.1.04.02", Nombre = "Inventario de Bebidas y Mercancías", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 4, PermiteMovimiento = true },

            new CuentaContable { Codigo = "1.2", Nombre = "ACTIVO NO CORRIENTE", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 2, PermiteMovimiento = false },
            new CuentaContable { Codigo = "1.2.01", Nombre = "Mobiliario y Equipo de Cocina", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "1.2.02", Nombre = "Equipo de Cómputo y POS", TipoCuenta = "Activo", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },

            // 2. PASIVOS
            new CuentaContable { Codigo = "2", Nombre = "PASIVO", TipoCuenta = "Pasivo", Naturaleza = "Acreedora", Nivel = 1, PermiteMovimiento = false },
            new CuentaContable { Codigo = "2.1", Nombre = "PASIVO CORRIENTE", TipoCuenta = "Pasivo", Naturaleza = "Acreedora", Nivel = 2, PermiteMovimiento = false },
            new CuentaContable { Codigo = "2.1.01", Nombre = "Cuentas por Pagar Proveedores", TipoCuenta = "Pasivo", Naturaleza = "Acreedora", Nivel = 3, PermiteMovimiento = false },
            new CuentaContable { Codigo = "2.1.01.01", Nombre = "Proveedores de Ingredientes", TipoCuenta = "Pasivo", Naturaleza = "Acreedora", Nivel = 4, PermiteMovimiento = true },
            new CuentaContable { Codigo = "2.1.02", Nombre = "Gastos por Pagar y Acreedores Varios", TipoCuenta = "Pasivo", Naturaleza = "Acreedora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "2.1.03", Nombre = "Impuestos por Pagar", TipoCuenta = "Pasivo", Naturaleza = "Acreedora", Nivel = 3, PermiteMovimiento = false },
            new CuentaContable { Codigo = "2.1.03.01", Nombre = "IVA por Pagar (Débito Fiscal)", TipoCuenta = "Pasivo", Naturaleza = "Acreedora", Nivel = 4, PermiteMovimiento = true },
            new CuentaContable { Codigo = "2.1.03.02", Nombre = "Retenciones de Impuesto en la Fuente", TipoCuenta = "Pasivo", Naturaleza = "Acreedora", Nivel = 4, PermiteMovimiento = true },

            // 3. CAPITAL / PATRIMONIO
            new CuentaContable { Codigo = "3", Nombre = "PATRIMONIO", TipoCuenta = "Capital", Naturaleza = "Acreedora", Nivel = 1, PermiteMovimiento = false },
            new CuentaContable { Codigo = "3.1.01", Nombre = "Capital Social / Aportes Iniciales", TipoCuenta = "Capital", Naturaleza = "Acreedora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "3.1.02", Nombre = "Resultados Acumulados de Años Anteriores", TipoCuenta = "Capital", Naturaleza = "Acreedora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "3.1.03", Nombre = "Resultado del Ejercicio Actual", TipoCuenta = "Capital", Naturaleza = "Acreedora", Nivel = 3, PermiteMovimiento = true },

            // 4. INGRESOS
            new CuentaContable { Codigo = "4", Nombre = "INGRESOS", TipoCuenta = "Ingreso", Naturaleza = "Acreedora", Nivel = 1, PermiteMovimiento = false },
            new CuentaContable { Codigo = "4.1.01", Nombre = "Ingresos por Ventas de Pizzas y Alimentos", TipoCuenta = "Ingreso", Naturaleza = "Acreedora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "4.1.02", Nombre = "Ingresos por Ventas de Bebidas", TipoCuenta = "Ingreso", Naturaleza = "Acreedora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "4.1.03", Nombre = "Otros Ingresos Operativos", TipoCuenta = "Ingreso", Naturaleza = "Acreedora", Nivel = 3, PermiteMovimiento = true },

            // 5. COSTOS
            new CuentaContable { Codigo = "5", Nombre = "COSTOS", TipoCuenta = "Costo", Naturaleza = "Deudora", Nivel = 1, PermiteMovimiento = false },
            new CuentaContable { Codigo = "5.1.01", Nombre = "Costo de Ventas - Alimentos y Pizzas", TipoCuenta = "Costo", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "5.1.02", Nombre = "Costo de Ventas - Bebidas", TipoCuenta = "Costo", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "5.1.03", Nombre = "Mermas y Desperdicios de Cocina", TipoCuenta = "Costo", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },

            // 6. GASTOS
            new CuentaContable { Codigo = "6", Nombre = "GASTOS OPERATIVOS", TipoCuenta = "Gasto", Naturaleza = "Deudora", Nivel = 1, PermiteMovimiento = false },
            new CuentaContable { Codigo = "6.1.01", Nombre = "Gasto de Alquiler de Local", TipoCuenta = "Gasto", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "6.1.02", Nombre = "Gasto de Electricidad / Luz", TipoCuenta = "Gasto", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "6.1.03", Nombre = "Gasto de Agua Potable", TipoCuenta = "Gasto", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "6.1.04", Nombre = "Gasto de Internet y Telefonía", TipoCuenta = "Gasto", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "6.1.05", Nombre = "Sueldos y Salarios del Personal", TipoCuenta = "Gasto", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "6.1.06", Nombre = "Mantenimiento de Hornos y Equipos", TipoCuenta = "Gasto", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "6.1.07", Nombre = "Gastos de Transporte y Combustible", TipoCuenta = "Gasto", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "6.1.08", Nombre = "Gastos de Publicidad y Marketing", TipoCuenta = "Gasto", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true },
            new CuentaContable { Codigo = "6.1.99", Nombre = "Otros Gastos Generales y Administrativos", TipoCuenta = "Gasto", Naturaleza = "Deudora", Nivel = 3, PermiteMovimiento = true }
        };

        _context.CuentasContables.AddRange(cuentas);
        await _context.SaveChangesAsync();
    }
}
