using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class Receta
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Producto")]
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    [Display(Name = "Rendimiento (Porciones/Unidades)")]
    [Range(1, 1000)]
    public int Rendimiento { get; set; } = 1;

    [Column(TypeName = "decimal(5,2)")]
    [Display(Name = "Merma Prevista (%)")]
    [Range(0, 100)]
    public decimal MermaPrevistaPorcentaje { get; set; } = 0;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [StringLength(250)]
    [Display(Name = "Instrucciones / Observaciones")]
    public string? Instrucciones { get; set; }

    public ICollection<RecetaDetalle> Detalles { get; set; } = new List<RecetaDetalle>();
}
