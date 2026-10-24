using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class PagoCompra
{
    public int Id { get; set; }

    [Required]
    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    [Display(Name = "Fecha de Pago")]
    public DateTime Fecha { get; set; } = DateTime.Now;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Monto Pagado")]
    [Range(0.01, 999999.99)]
    public decimal Monto { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Método de Pago")]
    public string MetodoPago { get; set; } = "Efectivo";

    [StringLength(100)]
    [Display(Name = "Referencia / Comprobante")]
    public string? Referencia { get; set; }

    [StringLength(250)]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }
}
