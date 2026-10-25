namespace PizzeriaContable.ViewModels;

public class CuentaTViewModel
{
    public int CuentaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string TipoCuenta { get; set; } = string.Empty; // Activo, Pasivo, Capital, Ingreso, Costo, Gasto
    public string Naturaleza { get; set; } = string.Empty; // Deudora o Acreedora
    public int Nivel { get; set; }
    public bool PermiteMovimiento { get; set; }

    public List<MovimientoTLineaDto> MovimientosDebe { get; set; } = new();
    public List<MovimientoTLineaDto> MovimientosHaber { get; set; } = new();

    public decimal TotalDebe => MovimientosDebe.Sum(m => m.Importe);
    public decimal TotalHaber => MovimientosHaber.Sum(m => m.Importe);

    /// <summary>
    /// Saldo según la naturaleza contable:
    /// Naturaleza Deudora: Debe - Haber
    /// Naturaleza Acreedora: Haber - Debe
    /// </summary>
    public decimal SaldoFinal => Naturaleza == "Deudora" 
        ? (TotalDebe - TotalHaber) 
        : (TotalHaber - TotalDebe);

    public string TipoSaldoVisual
    {
        get
        {
            if (SaldoFinal > 0)
                return $"Saldo {Naturaleza}";
            if (SaldoFinal < 0)
                return Naturaleza == "Deudora" ? "Saldo Acreedor (Invertido)" : "Saldo Deudor (Invertido)";
            return "Saldada (Cero)";
        }
    }

    public bool TieneMovimientos => MovimientosDebe.Count > 0 || MovimientosHaber.Count > 0;
}

public class MovimientoTLineaDto
{
    public int AsientoId { get; set; }
    public string NumeroAsiento { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public decimal Importe { get; set; }
    public decimal SaldoAcumulado { get; set; }
}

public class ResumenCuentasTViewModel
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? TipoSeleccionado { get; set; }
    public string? Busqueda { get; set; }
    public List<CuentaTViewModel> Cuentas { get; set; } = new();

    public decimal TotalDebeGeneral => Cuentas.Sum(c => c.TotalDebe);
    public decimal TotalHaberGeneral => Cuentas.Sum(c => c.TotalHaber);
    public bool PartidaDobleEquilibrada => Math.Abs(TotalDebeGeneral - TotalHaberGeneral) < 0.01m;
}
