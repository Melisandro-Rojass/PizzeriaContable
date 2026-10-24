using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class VentaPago
{
    public int Id { get; set; }

    [Required]
    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Método de Pago")]
    public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, Tarjeta, Transferencia

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Monto")]
    [Range(0.01, 999999.99)]
    public decimal Monto { get; set; }

    [StringLength(100)]
    [Display(Name = "N° Referencia / Aprobación")]
    public string? Referencia { get; set; }
}
