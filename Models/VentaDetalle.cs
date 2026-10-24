using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class VentaDetalle
{
    public int Id { get; set; }

    [Required]
    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    [Required]
    [Display(Name = "Producto")]
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    [Required]
    [Display(Name = "Cantidad")]
    [Range(1, 1000)]
    public int Cantidad { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Precio Unitario")]
    public decimal PrecioUnitario { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Descuento")]
    public decimal Descuento { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Subtotal")]
    public decimal Subtotal { get; set; }
}
