using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Data;
using PizzeriaContable.Models;

namespace PizzeriaContable.Services;

public class SeedDataService : ISeedDataService
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IContabilidadService _contabilidadService;

    public SeedDataService(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IContabilidadService contabilidadService)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _contabilidadService = contabilidadService;
    }

    public async Task InicializarDatosAsync()
    {
        // 1. Configuración de Empresa
        if (!await _context.Configuraciones.AnyAsync())
        {
            _context.Configuraciones.Add(new ConfiguracionEmpresa
            {
                NombreComercial = "Pizzería La Tradicional",
                RazonSocial = "Pizzería La Tradicional S.A.",
                Direccion = "Plaza Central, Managua, Nicaragua",
                Telefono = "+505 2222-3344",
                CorreoElectronico = "administracion@pizzerialatradicional.ni",
                IdentificacionTributaria = "J0310000012345",
                MonedaPrincipal = "NIO",
                SimboloMoneda = "C$",
                ZonaHoraria = "America/Managua",
                FechaInicioContable = DateTime.Today,
                TasaIvaPorcentaje = 15.00m
            });
            await _context.SaveChangesAsync();
        }

        // 2. Roles del Sistema
        string[] roles = { "Administrador", "Cajero", "EncargadoInventario", "Contador" };
        foreach (var rol in roles)
        {
            if (!await _roleManager.RoleExistsAsync(rol))
            {
                await _roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        // 3. Usuario Administrador por Defecto
        var adminEmail = "admin@pizzeria.local";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                NombreCompleto = "Administrador Principal",
                Rol = "Administrador",
                Activo = true,
                EmailConfirmed = true,
                FechaRegistro = DateTime.Now
            };

            var result = await _userManager.CreateAsync(adminUser, "AdminPizzeria2026!");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Administrador");
            }
        }

        // 4. Catálogo de Cuentas Contables
        await _contabilidadService.InicializarCatalogoCuentasAsync();

        // 5. Categorías Básicas de Productos
        if (!await _context.Categorias.AnyAsync())
        {
            _context.Categorias.AddRange(
                new Categoria { Nombre = "Pizzas Tradicionales", Descripcion = "Pizzas clásicas al horno de piedra", Activo = true },
                new Categoria { Nombre = "Pizzas Gourmet", Descripcion = "Especialidades de la casa con ingredientes selectos", Activo = true },
                new Categoria { Nombre = "Bebidas y Refrescos", Descripcion = "Gaseosas, jugos naturales y cervezas", Activo = true },
                new Categoria { Nombre = "Complementos y Entradas", Descripcion = "Pan con ajo, alitas, palitroques y salsas", Activo = true },
                new Categoria { Nombre = "Postres", Descripcion = "Dulces y postres italianos", Activo = true }
            );
            await _context.SaveChangesAsync();
        }

        // 6. Ingredientes de Muestra
        if (!await _context.Ingredientes.AnyAsync())
        {
            _context.Ingredientes.AddRange(
                new Ingrediente { Codigo = "ING-HARINA", Nombre = "Harina de Trigo Especial", UnidadMedida = "kg", CostoUnitario = 32.50m, StockActual = 50.0m, StockMinimo = 15.0m, Activo = true },
                new Ingrediente { Codigo = "ING-QUESO-MOZZ", Nombre = "Queso Mozzarella Rallado", UnidadMedida = "kg", CostoUnitario = 160.00m, StockActual = 25.0m, StockMinimo = 8.0m, Activo = true },
                new Ingrediente { Codigo = "ING-SALSA-TOM", Nombre = "Salsa de Tomate Base Pizza", UnidadMedida = "kg", CostoUnitario = 45.00m, StockActual = 30.0m, StockMinimo = 10.0m, Activo = true },
                new Ingrediente { Codigo = "ING-PEPPERONI", Nombre = "Pepperoni Rebanado", UnidadMedida = "kg", CostoUnitario = 220.00m, StockActual = 12.0m, StockMinimo = 4.0m, Activo = true },
                new Ingrediente { Codigo = "ING-JAMON", Nombre = "Jamón Ahumado", UnidadMedida = "kg", CostoUnitario = 140.00m, StockActual = 15.0m, StockMinimo = 5.0m, Activo = true },
                new Ingrediente { Codigo = "ING-CHAMPINON", Nombre = "Champiñones Frescos", UnidadMedida = "kg", CostoUnitario = 110.00m, StockActual = 8.0m, StockMinimo = 3.0m, Activo = true }
            );
            await _context.SaveChangesAsync();
        }

        // 7. Proveedor por Defecto
        if (!await _context.Proveedores.AnyAsync())
        {
            _context.Proveedores.Add(new Proveedor
            {
                Nombre = "Distribuidora Lácteos y Embutidos del Norte",
                Contacto = "Carlos Morales",
                Telefono = "+505 8899-1122",
                CorreoElectronico = "ventas@lacteosdelnorte.ni",
                Direccion = "Km 5 Carretera Norte, Managua",
                IdentificacionTributaria = "J0123456789123",
                DiasCredito = 15,
                Activo = true
            });
            await _context.SaveChangesAsync();
        }

        // 8. Productos Iniciales si no existen
        if (!await _context.Productos.AnyAsync())
        {
            var catPizzas = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Pizzas Tradicionales");
            var catBebidas = await _context.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Bebidas y Refrescos");

            if (catPizzas != null)
            {
                var p1 = new Producto
                {
                    Codigo = "PIZ-MOZ-MED",
                    Nombre = "Pizza Mozzarella Clásica",
                    Tamano = "Mediana",
                    Precio = 240.00m,
                    Descripcion = "Salsa pomodoro tradicional, queso mozzarella fundido y orégano",
                    CategoriaId = catPizzas.Id,
                    Activo = true
                };

                var p2 = new Producto
                {
                    Codigo = "PIZ-PEP-FAM",
                    Nombre = "Pizza Pepperoni Suprema",
                    Tamano = "Familiar",
                    Precio = 390.00m,
                    Descripcion = "Doble queso mozzarella y abundante pepperoni crujiente",
                    CategoriaId = catPizzas.Id,
                    Activo = true
                };

                _context.Productos.AddRange(p1, p2);
                await _context.SaveChangesAsync();

                // Asociar receta básica a p1 (Pizza Mozzarella Mediana)
                var ingHarina = await _context.Ingredientes.FirstOrDefaultAsync(i => i.Codigo == "ING-HARINA");
                var ingQueso = await _context.Ingredientes.FirstOrDefaultAsync(i => i.Codigo == "ING-QUESO-MOZZ");
                var ingSalsa = await _context.Ingredientes.FirstOrDefaultAsync(i => i.Codigo == "ING-SALSA-TOM");

                if (ingHarina != null && ingQueso != null && ingSalsa != null)
                {
                    var receta = new Receta
                    {
                        ProductoId = p1.Id,
                        Rendimiento = 1,
                        MermaPrevistaPorcentaje = 2.0m,
                        Activo = true,
                        Instrucciones = "Estirar masa de 280g, agregar 100g de salsa y 200g de queso. Hornear a 320°C por 7 minutos.",
                        Detalles = new List<RecetaDetalle>
                        {
                            new RecetaDetalle { IngredienteId = ingHarina.Id, Cantidad = 0.280m, UnidadMedida = "kg" },
                            new RecetaDetalle { IngredienteId = ingSalsa.Id, Cantidad = 0.100m, UnidadMedida = "kg" },
                            new RecetaDetalle { IngredienteId = ingQueso.Id, Cantidad = 0.200m, UnidadMedida = "kg" }
                        }
                    };
                    _context.Recetas.Add(receta);
                    await _context.SaveChangesAsync();
                }
            }

            if (catBebidas != null)
            {
                _context.Productos.Add(new Producto
                {
                    Codigo = "BEB-COCA-LATA",
                    Nombre = "Coca-Cola 355ml Lata",
                    Tamano = "Individual",
                    Precio = 40.00m,
                    Descripcion = "Refresco frío en lata",
                    CategoriaId = catBebidas.Id,
                    Activo = true
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}
