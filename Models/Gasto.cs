using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class Gasto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "La categoría del gasto es obligatoria.")]
    [StringLength(50)]
    [Display(Name = "Categoría")]
    public string CategoriaGasto { get; set; } = "Servicios Básicos"; // Alquiler, Electricidad, Agua, Internet, Salarios, Mantenimiento, Transporte, Otros

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(200)]
    [Display(Name = "Descripción del Gasto")]
    public string Descripcion { get; set; } = string.Empty;

    [Display(Name = "Fecha de Emisión / Devengo")]
    public DateTime FechaGasto { get; set; } = DateTime.Today;

    [Display(Name = "Fecha de Pago")]
    public DateTime? FechaPago { get; set; }

    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Monto")]
    [Range(0.01, 999999.99, ErrorMessage = "El monto debe ser positivo.")]
    public decimal Monto { get; set; }

    [Display(Name = "Proveedor / Acreedor")]
    public int? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    [StringLength(50)]
    [Display(Name = "N° Factura / Recibo")]
    public string? NumeroComprobante { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Forma de Pago")]
    public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, Transferencia, Cheque, Pendiente

    [Required]
    [StringLength(20)]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Pagado"; // Pagado, Pendiente, Anulado

    [Display(Name = "Fecha de Vencimiento")]
    public DateTime? FechaVencimiento { get; set; }

    [StringLength(250)]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }
}
