using System.ComponentModel.DataAnnotations;

namespace PizzeriaContable.ViewModels;

public class AperturaContableViewModel
{
    [Required]
    [Display(Name = "Fecha de Apertura")]
    public DateTime FechaApertura { get; set; } = DateTime.Today;

    [Display(Name = "Efectivo en Caja Inicial (C$)")]
    [Range(0, 99999999.99)]
    public decimal EfectivoCaja { get; set; } = 0;

    [Display(Name = "Saldo en Cuentas Bancarias (C$)")]
    [Range(0, 99999999.99)]
    public decimal SaldoBancos { get; set; } = 0;

    [Display(Name = "Valoración de Inventario Inicial (C$)")]
    [Range(0, 99999999.99)]
    public decimal InventarioInicial { get; set; } = 0;

    [Display(Name = "Mobiliario y Enseres (Mesas, Sillas) (C$)")]
    [Range(0, 99999999.99)]
    public decimal MobiliarioEquipo { get; set; } = 0;

    [Display(Name = "Maquinaria, Hornos y Equipo de Cocina (C$)")]
    [Range(0, 99999999.99)]
    public decimal HornosYEquipos { get; set; } = 0;

    [Display(Name = "Deudas Pendientes con Proveedores (C$)")]
    [Range(0, 99999999.99)]
    public decimal CuentasPorPagarProveedores { get; set; } = 0;

    [Display(Name = "Otras Obligaciones / Préstamos por Pagar (C$)")]
    [Range(0, 99999999.99)]
    public decimal OtrasObligaciones { get; set; } = 0;

    [Display(Name = "Capital Social / Aportación de Dueños (C$)")]
    [Range(0, 99999999.99)]
    public decimal CapitalSocial { get; set; } = 0;

    [StringLength(300)]
    [Display(Name = "Notas / Justificación de Apertura")]
    public string? Observaciones { get; set; } = "Asiento de apertura histórica de operaciones";

    public decimal TotalActivos => EfectivoCaja + SaldoBancos + InventarioInicial + MobiliarioEquipo + HornosYEquipos;
    public decimal TotalPasivos => CuentasPorPagarProveedores + OtrasObligaciones;
    public decimal TotalPatrimonio => CapitalSocial;
    public decimal Diferencia => TotalActivos - (TotalPasivos + TotalPatrimonio);
    public bool EstaBalanceado => Math.Abs(Diferencia) < 0.01m && TotalActivos > 0;
}

public class AsientoManualViewModel
{
    [Required(ErrorMessage = "La fecha es requerida.")]
    [Display(Name = "Fecha Contable")]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "El concepto o glosa del asiento es obligatorio.")]
    [StringLength(300)]
    [Display(Name = "Concepto / Glosa")]
    public string Concepto { get; set; } = string.Empty;

    public List<AsientoManualLineaDto> Lineas { get; set; } = new();
}

public class AsientoManualLineaDto
{
    public int CuentaContableId { get; set; }
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public string? Descripcion { get; set; }
}
