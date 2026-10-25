using System.ComponentModel.DataAnnotations;

namespace PizzeriaContable.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "El usuario o correo electrónico es obligatorio.")]
    [Display(Name = "Usuario o Correo")]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordarme en este equipo")]
    public bool RememberMe { get; set; } = false;
}

public class UsuarioCreateViewModel
{
    [Required(ErrorMessage = "El nombre completo del empleado es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
    [Display(Name = "Nombre Completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "El usuario solo puede contener letras, números, puntos, guiones y guiones bajos.")]
    [Display(Name = "Nombre de Usuario (Login)")]
    public string UserName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [Display(Name = "Correo Electrónico (Opcional)")]
    public string? Email { get; set; }

    [StringLength(100)]
    [Display(Name = "Puesto / Cargo Laboral")]
    public string? Cargo { get; set; } // Ej. Cajero Turno Matutino, Maestro Pizzero, Contador General

    [Required(ErrorMessage = "El rol en el sistema es obligatorio.")]
    [Display(Name = "Rol del Sistema")]
    public string Rol { get; set; } = "Cajero"; // Administrador, Contabilidad, Cajero, Inventario, Consulta

    [Required(ErrorMessage = "La contraseña inicial es obligatoria.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña Inicial")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class UsuarioEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre Completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(50, MinimumLength = 3)]
    [Display(Name = "Nombre de Usuario")]
    public string UserName { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "Correo Electrónico")]
    public string? Email { get; set; }

    [StringLength(100)]
    [Display(Name = "Puesto / Cargo")]
    public string? Cargo { get; set; }

    [Required]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = "Cajero";

    [Display(Name = "Cuenta Activa")]
    public bool Activo { get; set; } = true;

    [DataType(DataType.Password)]
    [Display(Name = "Nueva Contraseña (dejar en blanco para conservar)")]
    public string? NewPassword { get; set; }
}

public class UsuarioResetPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;

    [Display(Name = "Nombre del Empleado")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Display(Name = "Usuario")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva Contraseña")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Nueva Contraseña")]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
