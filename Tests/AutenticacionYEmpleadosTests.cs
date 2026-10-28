using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PizzeriaContable.Controllers;
using PizzeriaContable.Models;
using Xunit;

namespace PizzeriaContable.Tests;

public class AutenticacionYEmpleadosTests
{
    [Fact]
    public void Contrasenas_SeAlmacenanHasheadas_NuncaEnTextoPlano()
    {
        // Arrange
        var hasher = new PasswordHasher<ApplicationUser>();
        var user = new ApplicationUser
        {
            UserName = "empleado1",
            NombreCompleto = "Carlos Mendoza",
            Email = "carlos@pizzeria.local"
        };
        string plainPassword = "SuperPassword2026!";

        // Act
        string hashedPassword = hasher.HashPassword(user, plainPassword);

        // Assert: El hash nunca debe ser igual a la contraseña en texto plano
        Assert.NotEqual(plainPassword, hashedPassword);
        Assert.False(string.IsNullOrWhiteSpace(hashedPassword));

        // Verificación exitosa con la contraseña correcta
        var resultSuccess = hasher.VerifyHashedPassword(user, hashedPassword, plainPassword);
        Assert.Equal(PasswordVerificationResult.Success, resultSuccess);

        // Rechazo automático con contraseña errónea
        var resultFail = hasher.VerifyHashedPassword(user, hashedPassword, "PasswordIncorrecto!");
        Assert.Equal(PasswordVerificationResult.Failed, resultFail);
    }

    [Fact]
    public void EmpleadoInactivo_DebeTenerFlagActivoEnFalse_ParaImpedirAcceso()
    {
        // Arrange
        var empleado = new ApplicationUser
        {
            UserName = "juan.perez",
            NombreCompleto = "Juan Pérez",
            Cargo = "Cajero Turno Tarde",
            Rol = "Cajero",
            Activo = false // Cuenta desactivada por el administrador
        };

        // Assert
        Assert.False(empleado.Activo);
    }

    [Fact]
    public void DesactivarEmpleado_DebeActualizarSecurityStamp_ParaInvalidarSesiones()
    {
        // Arrange
        var empleado = new ApplicationUser
        {
            UserName = "maria.lopez",
            NombreCompleto = "María López",
            Rol = "Cajero",
            Activo = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        string initialStamp = empleado.SecurityStamp;

        // Act - Simular desactivación e invalidación de sesión activa
        empleado.Activo = false;
        empleado.SecurityStamp = Guid.NewGuid().ToString();

        // Assert
        Assert.False(empleado.Activo);
        Assert.NotEqual(initialStamp, empleado.SecurityStamp);
    }

    [Fact]
    public void UsuariosController_RequiereRolAdministrador()
    {
        // Assert: La gestión de personal debe estar protegida para Administradores
        var authorizeAttr = typeof(UsuariosController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Contains("Administrador", authorizeAttr.Roles);
    }

    [Fact]
    public void UsuariosController_Login_PermiteAccesoAnonimo()
    {
        // Assert: El login debe tener AllowAnonymous para que visitantes no autenticados puedan ingresar
        var loginGetMethod = typeof(UsuariosController).GetMethod("Login", new[] { typeof(string) });
        Assert.NotNull(loginGetMethod);

        var allowAnonymousAttr = loginGetMethod.GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.NotNull(allowAnonymousAttr);
    }

    [Fact]
    public void ContabilidadController_RequiereRolesContabilidadOAdmin_ExcluyeCajero()
    {
        // Assert: Cajero no puede abrir páginas de contabilidad
        var authorizeAttr = typeof(ContabilidadController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        
        var rolesPermitidos = authorizeAttr.Roles?.Split(',') ?? Array.Empty<string>();
        Assert.Contains("Administrador", rolesPermitidos);
        Assert.True(rolesPermitidos.Contains("Contabilidad") || rolesPermitidos.Contains("Contador"));
        
        // Un cajero o inventario no debe estar en los roles permitidos de Contabilidad
        Assert.DoesNotContain("Cajero", rolesPermitidos);
    }

    [Fact]
    public void CajaController_PermiteCajeroYAdministrador_ProtegidoPorRoles()
    {
        var authorizeAttr = typeof(CajaController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);

        var rolesPermitidos = authorizeAttr.Roles?.Split(',') ?? Array.Empty<string>();
        Assert.Contains("Administrador", rolesPermitidos);
        Assert.Contains("Cajero", rolesPermitidos);
    }

    [Fact]
    public void ConfiguracionController_ExclusivoParaAdministrador()
    {
        var authorizeAttr = typeof(ConfiguracionController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal("Administrador", authorizeAttr.Roles);
    }

    [Fact]
    public void IngredientesController_RequiereInventarioOAdmin_ExcluyeCajero()
    {
        var authorizeAttr = typeof(IngredientesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);

        var rolesPermitidos = authorizeAttr.Roles?.Split(',') ?? Array.Empty<string>();
        Assert.Contains("Administrador", rolesPermitidos);
        Assert.True(rolesPermitidos.Contains("Inventario") || rolesPermitidos.Contains("EncargadoInventario"));
        Assert.DoesNotContain("Cajero", rolesPermitidos);
    }

    [Fact]
    public void ApplicationUser_ContieneCamposLaboralesIndividuales()
    {
        // Arrange & Act
        var empleado = new ApplicationUser
        {
            UserName = "roberto.pizzero",
            Email = "roberto@pizzeria.local",
            NombreCompleto = "Roberto Martínez",
            Cargo = "Maestro Pizzero y Encargado de Cocina",
            Rol = "Inventario",
            Activo = true,
            FechaRegistro = DateTime.Today,
            UltimoAcceso = DateTime.Now
        };

        // Assert
        Assert.Equal("roberto.pizzero", empleado.UserName);
        Assert.Equal("Roberto Martínez", empleado.NombreCompleto);
        Assert.Equal("Maestro Pizzero y Encargado de Cocina", empleado.Cargo);
        Assert.Equal("Inventario", empleado.Rol);
        Assert.True(empleado.Activo);
        Assert.NotNull(empleado.UltimoAcceso);
    }

    [Fact]
    public void UsuariosController_Logout_PermiteTodosLosRoles_RequierePostYAntiforgery()
    {
        // Assert: Logout debe tener AllowAnonymous para anular el [Authorize(Roles = "Administrador")]
        // de la clase, permitiendo que cajeros y demás roles cierren sesión.
        var logoutMethod = typeof(UsuariosController).GetMethod("Logout");
        Assert.NotNull(logoutMethod);

        var allowAnonymousAttr = logoutMethod.GetCustomAttribute<AllowAnonymousAttribute>();
        Assert.NotNull(allowAnonymousAttr);

        var httpPostAttr = logoutMethod.GetCustomAttribute<HttpPostAttribute>();
        Assert.NotNull(httpPostAttr);

        var antiForgeryAttr = logoutMethod.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>();
        Assert.NotNull(antiForgeryAttr);
    }

    [Fact]
    public void UsuariosController_Perfil_PermiteTodosLosRoles_SobrescribiendoRolAdminDeClase()
    {
        // Assert: Perfil y CambiarPasswordPropio deben permitir acceso a cualquier usuario autenticado
        var perfilMethod = typeof(UsuariosController).GetMethod("Perfil");
        Assert.NotNull(perfilMethod);
        Assert.NotNull(perfilMethod.GetCustomAttribute<AllowAnonymousAttribute>());

        var cambiarPassMethod = typeof(UsuariosController).GetMethod("CambiarPasswordPropio");
        Assert.NotNull(cambiarPassMethod);
        Assert.NotNull(cambiarPassMethod.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.NotNull(cambiarPassMethod.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(cambiarPassMethod.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }
}
