namespace PizzeriaContable.ViewModels;

/// <summary>
/// Datos recibidos desde el POS para registrar una venta completa con sus detalles y pagos.
/// </summary>
public class PedidoCobroDto
{
    public string? ClienteNombre { get; set; } = "Cliente General";
    public string? Observaciones { get; set; }
    public decimal DescuentoGeneral { get; set; } = 0;

    /// <summary>Líneas de producto incluidas en la orden.</summary>
    public List<DetallePedidoDto> Items { get; set; } = new();

    /// <summary>Métodos de pago aplicados a la orden (puede ser pago mixto).</summary>
    public List<PagoPedidoDto> Pagos { get; set; } = new();
}

/// <summary>
/// Línea de producto dentro de una orden del POS.
/// </summary>
public class DetallePedidoDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; } = 0;
}

/// <summary>
/// Pago parcial o total registrado en la orden del POS.
/// </summary>
public class PagoPedidoDto
{
    /// <summary>Método de pago: Efectivo, Tarjeta o Transferencia.</summary>
    public string MetodoPago { get; set; } = "Efectivo";
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
}
