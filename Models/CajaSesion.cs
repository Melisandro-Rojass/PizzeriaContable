using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class CajaSesion
{
    public int Id { get; set; }

    [Display(Name = "Fecha y Hora de Apertura")]
    public DateTime FechaApertura { get; set; } = DateTime.Now;

    [Display(Name = "Fecha y Hora de Cierre")]
    public DateTime? FechaCierre { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Monto Inicial (Fondo de Caja)")]
    [Range(0, 999999.99)]
    public decimal MontoInicial { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Ventas en Efectivo")]
    public decimal TotalVentasEfectivo { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Ventas con Tarjeta")]
    public decimal TotalVentasTarjeta { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Ventas por Transferencia")]
    public decimal TotalVentasTransferencia { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Ingresos Adicionales")]
    public decimal TotalIngresos { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Egresos / Retiros")]
    public decimal TotalEgresos { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Efectivo Físico Esperado")]
    public decimal EfectivoEsperado { get; set; } // MontoInicial + TotalVentasEfectivo + TotalIngresos - TotalEgresos

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Efectivo Contado (Arqueo)")]
    public decimal EfectivoContado { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Diferencia")]
    public decimal Diferencia { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Abierta"; // Abierta, Cerrada

    [StringLength(50)]
    [Display(Name = "Usuario")]
    public string? UsuarioId { get; set; }

    [StringLength(250)]
    [Display(Name = "Observaciones de Cierre")]
    public string? Observaciones { get; set; }

    public ICollection<CajaMovimiento> Movimientos { get; set; } = new List<CajaMovimiento>();
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
