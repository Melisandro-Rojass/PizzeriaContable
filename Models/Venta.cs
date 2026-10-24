using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class Venta
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "N° Ticket / Comprobante")]
    public string NumeroComprobante { get; set; } = string.Empty;

    [Display(Name = "Fecha y Hora")]
    public DateTime Fecha { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Subtotal")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Descuento")]
    public decimal Descuento { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Impuesto (IVA)")]
    public decimal Impuesto { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Total")]
    public decimal Total { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Método de Pago Principal")]
    public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, Tarjeta, Transferencia, Mixto

    [Required]
    [StringLength(20)]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Completada"; // Completada, Anulada

    [StringLength(100)]
    [Display(Name = "Nombre del Cliente")]
    public string? ClienteNombre { get; set; } = "Cliente General";

    [StringLength(250)]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    [Display(Name = "Sesión de Caja")]
    public int? CajaSesionId { get; set; }
    public CajaSesion? CajaSesion { get; set; }

    [StringLength(50)]
    [Display(Name = "Cajero / Usuario")]
    public string? UsuarioId { get; set; }

    public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
    public ICollection<VentaPago> Pagos { get; set; } = new List<VentaPago>();
}
