using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class CajaMovimiento
{
    public int Id { get; set; }

    [Required]
    public int CajaSesionId { get; set; }
    public CajaSesion? CajaSesion { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Tipo")]
    public string Tipo { get; set; } = "Ingreso"; // Ingreso, Egreso, Retiro, Deposito

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Monto")]
    [Range(0.01, 999999.99)]
    public decimal Monto { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Concepto / Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Display(Name = "Fecha y Hora")]
    public DateTime Fecha { get; set; } = DateTime.Now;

    [StringLength(50)]
    [Display(Name = "Usuario")]
    public string? UsuarioId { get; set; }
}
