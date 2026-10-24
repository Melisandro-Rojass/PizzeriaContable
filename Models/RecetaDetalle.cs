using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class RecetaDetalle
{
    public int Id { get; set; }

    [Required]
    public int RecetaId { get; set; }
    public Receta? Receta { get; set; }

    [Required]
    [Display(Name = "Ingrediente")]
    public int IngredienteId { get; set; }
    public Ingrediente? Ingrediente { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,3)")]
    [Display(Name = "Cantidad Necesaria")]
    [Range(0.001, 999999.99)]
    public decimal Cantidad { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Unidad")]
    public string UnidadMedida { get; set; } = "g";
}
