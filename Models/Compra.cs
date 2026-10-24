using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class Compra
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    [Display(Name = "Proveedor")]
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    [Required(ErrorMessage = "El número de factura del proveedor es obligatorio.")]
    [StringLength(50)]
    [Display(Name = "N° Factura / Comprobante")]
    public string NumeroFactura { get; set; } = string.Empty;

    [Display(Name = "Fecha de Compra")]
    public DateTime FechaCompra { get; set; } = DateTime.Today;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Subtotal")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Impuesto (IVA)")]
    public decimal Impuesto { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Total")]
    public decimal Total { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Método de Pago")]
    public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, Transferencia, Cheque, Credito

    [Display(Name = "¿Es a Crédito?")]
    public bool EsCredito { get; set; } = false;

    [Display(Name = "Fecha de Vencimiento")]
    public DateTime? FechaVencimiento { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Saldo Pendiente")]
    public decimal SaldoPendiente { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Completada"; // Completada, Pendiente, Anulada

    [StringLength(250)]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
    public ICollection<PagoCompra> Pagos { get; set; } = new List<PagoCompra>();
}
