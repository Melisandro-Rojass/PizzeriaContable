using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class CompraDetalle
{
    public int Id { get; set; }

    [Required]
    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    [Required(ErrorMessage = "El ingrediente es obligatorio.")]
    [Display(Name = "Ingrediente")]
    public int IngredienteId { get; set; }
    public Ingrediente? Ingrediente { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,3)")]
    [Display(Name = "Cantidad")]
    [Range(0.001, 999999.99)]
    public decimal Cantidad { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Costo Unitario")]
    [Range(0.01, 999999.99)]
    public decimal CostoUnitario { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Subtotal")]
    public decimal Subtotal { get; set; }
}
