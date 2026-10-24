using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class Ingrediente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(30)]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del ingrediente es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    [StringLength(20)]
    [Display(Name = "Unidad de Medida")]
    public string UnidadMedida { get; set; } = "g"; // g, kg, ml, L, unidad, lata, etc.

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Costo Unitario")]
    [Range(0, 999999.99, ErrorMessage = "El costo debe ser positivo.")]
    public decimal CostoUnitario { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    [Display(Name = "Stock Actual")]
    public decimal StockActual { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    [Display(Name = "Stock Mínimo")]
    [Range(0, 999999.99, ErrorMessage = "El stock mínimo no puede ser negativo.")]
    public decimal StockMinimo { get; set; } = 5;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [Display(Name = "Proveedor Habitual")]
    public int? ProveedorHabitualId { get; set; }
    public Proveedor? ProveedorHabitual { get; set; }

    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
    public ICollection<RecetaDetalle> RecetaDetalles { get; set; } = new List<RecetaDetalle>();
}
