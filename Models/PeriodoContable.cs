using System.ComponentModel.DataAnnotations;

namespace PizzeriaContable.Models;

public class PeriodoContable
{
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    [Display(Name = "Nombre del Período")]
    public string Nombre { get; set; } = string.Empty; // Ej. "Octubre 2026", "IV Trimestre 2026"

    [Display(Name = "Fecha de Inicio")]
    public DateTime FechaInicio { get; set; }

    [Display(Name = "Fecha de Fin")]
    public DateTime FechaFin { get; set; }

    [Display(Name = "¿Período Cerrado?")]
    public bool Cerrado { get; set; } = false;

    [Display(Name = "Fecha de Cierre")]
    public DateTime? FechaCierre { get; set; }
}
