using Microsoft.AspNetCore.Identity;

namespace PizzeriaContable.Models;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;

    public string Rol { get; set; } = "Cajero"; // Administrador, Cajero, EncargadoInventario, Contador

    public bool Activo { get; set; } = true;

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}
