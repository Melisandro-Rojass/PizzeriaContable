using System.ComponentModel.DataAnnotations;

namespace PizzeriaContable.Models;

public class CuentaContable
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código contable es obligatorio.")]
    [StringLength(30)]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty; // Ej. 1.1.01.01

    [Required(ErrorMessage = "El nombre de la cuenta es obligatorio.")]
    [StringLength(120)]
    [Display(Name = "Nombre de la Cuenta")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [Display(Name = "Tipo de Cuenta")]
    public string TipoCuenta { get; set; } = "Activo"; // Activo, Pasivo, Capital, Ingreso, Costo, Gasto

    [Required]
    [StringLength(20)]
    [Display(Name = "Naturaleza")]
    public string Naturaleza { get; set; } = "Deudora"; // Deudora, Acreedora

    [Display(Name = "Cuenta Superior / Padre")]
    public int? CuentaPadreId { get; set; }
    public CuentaContable? CuentaPadre { get; set; }

    [Display(Name = "Nivel Jerárquico")]
    public int Nivel { get; set; } = 1;

    [Display(Name = "¿Acepta Movimientos Directos?")]
    public bool PermiteMovimiento { get; set; } = true;

    [Display(Name = "Activa")]
    public bool Activa { get; set; } = true;

    public ICollection<CuentaContable> Subcuentas { get; set; } = new List<CuentaContable>();
    public ICollection<AsientoDetalle> AsientoDetalles { get; set; } = new List<AsientoDetalle>();
}
