using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Models;
using PizzeriaContable.ViewModels;

namespace PizzeriaContable.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsuariosController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    // GET: Usuarios/Login
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirigirSegunRol();
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    // POST: Usuarios/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 1. Buscar usuario por Nombre de Usuario o por Correo Electrónico
        string busqueda = model.UserNameOrEmail.Trim();
        var user = await _userManager.FindByNameAsync(busqueda)
            ?? await _userManager.FindByEmailAsync(busqueda);

        // 2. Mensaje genérico para evitar enumeración de usuarios
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Credenciales inválidas. Por favor verifique sus datos.");
            return View(model);
        }

        // 3. Validar estado de cuenta del empleado
        if (!user.Activo)
        {
            ModelState.AddModelError(string.Empty, "La cuenta de este colaborador se encuentra inactiva. Comuníquese con la administración.");
            return View(model);
        }

        // 4. Autenticación con Identity, lockout activado contra ataques de fuerza bruta
        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Registrar fecha de último acceso
            user.UltimoAcceso = DateTime.Now;
            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return await RedirigirSegunRolUsuarioAsync(user);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "La cuenta ha sido bloqueada temporalmente por exceso de intentos fallidos. Intente más tarde.");
            return View(model);
        }

        // Mensaje genérico de error
        ModelState.AddModelError(string.Empty, "Credenciales inválidas. Por favor verifique sus datos.");
        return View(model);
    }

    // POST: Usuarios/Logout
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        // Evitar que el historial del navegador almacene en caché páginas protegidas
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        return RedirectToAction(nameof(Login));
    }

    // GET: Usuarios (Gestión de Personal y Cuentas de Empleados)
    public async Task<IActionResult> Index(string? search, string? rol, bool? activo)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.NombreCompleto.Contains(search) 
                                  || u.UserName!.Contains(search) 
                                  || (u.Email != null && u.Email.Contains(search))
                                  || (u.Cargo != null && u.Cargo.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(rol))
        {
            query = query.Where(u => u.Rol == rol);
        }

        if (activo.HasValue)
        {
            query = query.Where(u => u.Activo == activo.Value);
        }

        var usuarios = await query
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Rol = rol;
        ViewBag.Activo = activo;
        ViewBag.RolesDisponibles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

        return View(usuarios);
    }

    // GET: Usuarios/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.RolesDisponibles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return View(new UsuarioCreateViewModel());
    }

    // POST: Usuarios/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Validar nombre de usuario único
            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
            {
                ModelState.AddModelError("UserName", "El nombre de usuario ya está en uso.");
                ViewBag.RolesDisponibles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            // Validar correo si se especificó
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var existingEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("Email", "El correo electrónico ya está registrado por otro empleado.");
                    ViewBag.RolesDisponibles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                    return View(model);
                }
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                NombreCompleto = model.NombreCompleto.Trim(),
                Cargo = model.Cargo?.Trim(),
                Rol = model.Rol,
                Activo = true,
                EmailConfirmed = true,
                FechaRegistro = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(model.Rol))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Rol));
                }
                await _userManager.AddToRoleAsync(user, model.Rol);

                TempData["SuccessMessage"] = $"Cuenta del empleado '{user.NombreCompleto}' ({user.UserName}) creada con éxito con rol '{model.Rol}'.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewBag.RolesDisponibles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return View(model);
    }

    // GET: Usuarios/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        ViewBag.RolesDisponibles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

        var model = new UsuarioEditViewModel
        {
            Id = user.Id,
            NombreCompleto = user.NombreCompleto,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            Cargo = user.Cargo,
            Rol = user.Rol,
            Activo = user.Activo
        };

        return View(model);
    }

    // POST: Usuarios/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UsuarioEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Evitar que el administrador actual se desactive a sí mismo
            if (!model.Activo && user.UserName == User.Identity?.Name)
            {
                ModelState.AddModelError("Activo", "No puedes desactivar tu propia cuenta mientras estás en sesión.");
                ViewBag.RolesDisponibles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            // Validar unicidad de UserName si cambió
            if (user.UserName != model.UserName)
            {
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("UserName", "El nombre de usuario ya está en uso por otro empleado.");
                    ViewBag.RolesDisponibles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                    return View(model);
                }
            }

            bool estabaActivo = user.Activo;

            user.NombreCompleto = model.NombreCompleto.Trim();
            user.UserName = model.UserName.Trim();
            user.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            user.Cargo = model.Cargo?.Trim();
            user.Activo = model.Activo;

            // Actualizar rol
            if (user.Rol != model.Rol)
            {
                var rolesActuales = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, rolesActuales);

                if (!await _roleManager.RoleExistsAsync(model.Rol))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Rol));
                }
                await _userManager.AddToRoleAsync(user, model.Rol);
                user.Rol = model.Rol;
            }

            // Si se desactiva la cuenta, invalidar el sello de seguridad para cerrar sesiones activas
            if (estabaActivo && !model.Activo)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Actualizar contraseña si se ingresó
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                    if (!passResult.Succeeded)
                    {
                        foreach (var err in passResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, err.Description);
                        }
                        ViewBag.RolesDisponibles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                        return View(model);
                    }
                }

                TempData["SuccessMessage"] = $"Datos laborales del colaborador '{user.NombreCompleto}' actualizados con éxito.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewBag.RolesDisponibles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return View(model);
    }

    // POST: Usuarios/ToggleActivo/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActivo(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.UserName == User.Identity?.Name && user.Activo)
        {
            TempData["ErrorMessage"] = "No puedes desactivar tu propia cuenta activa de administrador.";
            return RedirectToAction(nameof(Index));
        }

        user.Activo = !user.Activo;

        // Si se desactiva, invalidar cookies activas
        if (!user.Activo)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        await _userManager.UpdateAsync(user);

        TempData["SuccessMessage"] = user.Activo 
            ? $"Cuenta de '{user.NombreCompleto}' reactivada exitosamente." 
            : $"Cuenta de '{user.NombreCompleto}' desactivada. Las sesiones activas han sido invalidadas.";

        return RedirectToAction(nameof(Index));
    }

    // POST: Usuarios/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(UsuarioResetPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                // Forzar re-autenticación con el nuevo password
                await _userManager.UpdateSecurityStampAsync(user);
                TempData["SuccessMessage"] = $"Contraseña restablecida exitosamente para '{user.NombreCompleto}'.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
            }
        }
        else
        {
            TempData["ErrorMessage"] = "Los datos ingresados para restablecer la contraseña no son válidos.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Usuarios/Perfil
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Perfil()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(user);
    }

    // POST: Usuarios/CambiarPasswordPropio
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPasswordPropio(string currentPassword, string newPassword, string confirmPassword)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Login));
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["ErrorMessage"] = "La nueva contraseña debe tener al menos 6 caracteres.";
            return RedirectToAction(nameof(Perfil));
        }

        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] = "Las nuevas contraseñas no coinciden.";
            return RedirectToAction(nameof(Perfil));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Tu contraseña ha sido actualizada correctamente.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Perfil));
    }

    // GET: Usuarios/AccessDenied
    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirigirSegunRol()
    {
        if (User.IsInRole("Cajero"))
            return RedirectToAction("Pos", "Ventas");
        if (User.IsInRole("Contador") || User.IsInRole("Contabilidad"))
            return RedirectToAction("CuentasT", "Contabilidad");
        if (User.IsInRole("Inventario") || User.IsInRole("EncargadoInventario"))
            return RedirectToAction("Index", "Ingredientes");

        return RedirectToAction("Index", "Home");
    }

    private async Task<IActionResult> RedirigirSegunRolUsuarioAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Cajero"))
            return RedirectToAction("Pos", "Ventas");
        if (roles.Contains("Contador") || roles.Contains("Contabilidad"))
            return RedirectToAction("CuentasT", "Contabilidad");
        if (roles.Contains("Inventario") || roles.Contains("EncargadoInventario"))
            return RedirectToAction("Index", "Ingredientes");

        return RedirectToAction("Index", "Home");
    }
}
