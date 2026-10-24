using System.ComponentModel.DataAnnotations;

namespace PizzeriaContable.Models;

public class AsientoContable
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "N° de Asiento")]
    public string NumeroAsiento { get; set; } = string.Empty;

    [Display(Name = "Fecha Contable")]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "El concepto o glosa del asiento es obligatorio.")]
    [StringLength(300)]
    [Display(Name = "Concepto / Glosa")]
    public string Concepto { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    [Display(Name = "Origen")]
    public string TipoOrigen { get; set; } = "Manual"; // "Apertura", "Venta", "Compra", "Gasto", "Pago", "Cierre", "Manual"

    [Display(Name = "ID Documento Origen")]
    public int? DocumentoOrigenId { get; set; }

    [Display(Name = "¿Contabilizado?")]
    public bool Contabilizado { get; set; } = true;

    [Display(Name = "¿Anulado?")]
    public bool Anulado { get; set; } = false;

    [Display(Name = "Fecha de Registro")]
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    [StringLength(50)]
    [Display(Name = "Registrado Por")]
    public string? CreadoPor { get; set; }

    public ICollection<AsientoDetalle> Detalles { get; set; } = new List<AsientoDetalle>();
}
