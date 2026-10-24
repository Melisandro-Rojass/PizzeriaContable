using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class AsientoDetalle
{
    public int Id { get; set; }

    [Required]
    public int AsientoContableId { get; set; }
    public AsientoContable? AsientoContable { get; set; }

    [Required(ErrorMessage = "La cuenta contable es obligatoria.")]
    [Display(Name = "Cuenta Contable")]
    public int CuentaContableId { get; set; }
    public CuentaContable? CuentaContable { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Debe")]
    [Range(0, 99999999.99)]
    public decimal Debe { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Haber")]
    [Range(0, 99999999.99)]
    public decimal Haber { get; set; } = 0;

    [StringLength(200)]
    [Display(Name = "Descripción del Apunte")]
    public string? Descripcion { get; set; }
}
