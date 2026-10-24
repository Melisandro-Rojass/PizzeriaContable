using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class Producto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código o SKU es obligatorio.")]
    [StringLength(30)]
    [Display(Name = "Código / SKU")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [StringLength(50)]
    [Display(Name = "Tamaño / Presentación")]
    public string? Tamano { get; set; } // Personal, Mediana, Familiar, Individual, etc.

    [Required(ErrorMessage = "El precio de venta es obligatorio.")]
    [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Precio de Venta")]
    public decimal Precio { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [Display(Name = "Categoría")]
    public int CategoriaId { get; set; }

    public Categoria? Categoria { get; set; }

    public Receta? Receta { get; set; }
}
