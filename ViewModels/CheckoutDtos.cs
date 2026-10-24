using System.ComponentModel.DataAnnotations;

namespace PizzeriaContable.ViewModels;

public class CheckoutRequestDto
{
    public string? ClienteNombre { get; set; } = "Cliente General";
    public string? Observaciones { get; set; }
    public decimal DescuentoGeneral { get; set; } = 0;
    public List<CheckoutItemDto> Items { get; set; } = new();
    public List<CheckoutPagoDto> Pagos { get; set; } = new();
}

public class CheckoutItemDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; } = 0;
}

public class CheckoutPagoDto
{
    public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, Tarjeta, Transferencia
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
}
