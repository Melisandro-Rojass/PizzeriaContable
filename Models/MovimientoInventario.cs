using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class MovimientoInventario
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Ingrediente")]
    public int IngredienteId { get; set; }
    public Ingrediente? Ingrediente { get; set; }

    [Required]
    [StringLength(40)]
    [Display(Name = "Tipo de Movimiento")]
    public string TipoMovimiento { get; set; } = string.Empty; 
    // "InventarioInicial", "EntradaCompra", "SalidaVenta", "Merma", "Devolucion", "AjustePositivo", "AjusteNegativo", "ReversionAnulacion"

    [Column(TypeName = "decimal(18,3)")]
    [Display(Name = "Cantidad")]
    public decimal Cantidad { get; set; } // Positivo para entradas/aumentos, negativo para salidas

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Costo Unitario")]
    public decimal CostoUnitario { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Total")]
    public decimal Total { get; set; }

    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; } = DateTime.Now;

    [StringLength(100)]
    [Display(Name = "Referencia / Documento")]
    public string? Referencia { get; set; } // Número de compra, venta, o acta de ajuste

    [StringLength(250)]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    [StringLength(50)]
    [Display(Name = "Usuario")]
    public string? UsuarioId { get; set; }
}
