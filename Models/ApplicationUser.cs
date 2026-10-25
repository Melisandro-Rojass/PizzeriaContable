using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PizzeriaContable.Models;

public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre Completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Rol del Sistema")]
    public string Rol { get; set; } = "Cajero"; // Administrador, Contabilidad, Contador, Cajero, Inventario, EncargadoInventario, Consulta

    [StringLength(100)]
    [Display(Name = "Cargo / Puesto de Trabajo")]
    public string? Cargo { get; set; } // Puesto de trabajo (e.g. "Cajero de Turno", "Contador Principal", "Encargado de Inventario")

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [Display(Name = "Fecha de Alta")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [Display(Name = "Último Acceso")]
    public DateTime? UltimoAcceso { get; set; }
}
