using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaContable.Models;

public class ConfiguracionEmpresa
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre comercial es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre Comercial")]
    public string NombreComercial { get; set; } = "Pizzería La Tradicional";

    [StringLength(150)]
    [Display(Name = "Razón Social")]
    public string? RazonSocial { get; set; }

    [StringLength(200)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [StringLength(30)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Correo Electrónico")]
    public string? CorreoElectronico { get; set; }

    [StringLength(30)]
    [Display(Name = "RUC / Identificación Tributaria")]
    public string? IdentificacionTributaria { get; set; }

    [Required]
    [StringLength(10)]
    [Display(Name = "Moneda Principal")]
    public string MonedaPrincipal { get; set; } = "NIO";

    [Required]
    [StringLength(10)]
    [Display(Name = "Símbolo Moneda")]
    public string SimboloMoneda { get; set; } = "C$";

    [Required]
    [StringLength(50)]
    [Display(Name = "Zona Horaria")]
    public string ZonaHoraria { get; set; } = "America/Managua";

    [Display(Name = "Fecha Inicio Control Contable")]
    public DateTime FechaInicioContable { get; set; } = DateTime.Today;

    [Column(TypeName = "decimal(5,2)")]
    [Display(Name = "Tasa IVA Estándar (%)")]
    public decimal TasaIvaPorcentaje { get; set; } = 15.00m;
}
