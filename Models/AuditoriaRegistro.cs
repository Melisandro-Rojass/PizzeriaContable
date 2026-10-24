using System.ComponentModel.DataAnnotations;

namespace PizzeriaContable.Models;

public class AuditoriaRegistro
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Módulo")]
    public string Modulo { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Acción")]
    public string Accion { get; set; } = string.Empty; // Crear, Modificar, Anular, Desactivar, CierreCaja, etc.

    [StringLength(50)]
    [Display(Name = "Entidad Afectada")]
    public string? Entidad { get; set; }

    [StringLength(50)]
    [Display(Name = "ID Entidad")]
    public string? EntidadId { get; set; }

    [StringLength(500)]
    [Display(Name = "Detalle")]
    public string? Detalle { get; set; }

    [Display(Name = "Fecha y Hora")]
    public DateTime FechaHora { get; set; } = DateTime.Now;

    [StringLength(50)]
    [Display(Name = "ID Usuario")]
    public string? UsuarioId { get; set; }

    [StringLength(100)]
    [Display(Name = "Nombre de Usuario")]
    public string? UsuarioNombre { get; set; }
}
