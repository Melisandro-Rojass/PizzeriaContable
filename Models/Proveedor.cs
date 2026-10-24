using System.ComponentModel.DataAnnotations;

namespace PizzeriaContable.Models;

public class Proveedor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Proveedor")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Persona de Contacto")]
    public string? Contacto { get; set; }

    [StringLength(30)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Correo Electrónico")]
    public string? CorreoElectronico { get; set; }

    [StringLength(200)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [StringLength(30)]
    [Display(Name = "RUC / Identificación")]
    public string? IdentificacionTributaria { get; set; }

    [Display(Name = "Días de Crédito")]
    [Range(0, 365)]
    public int DiasCredito { get; set; } = 0;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
    public ICollection<Ingrediente> IngredientesSuministrados { get; set; } = new List<Ingrediente>();
}
