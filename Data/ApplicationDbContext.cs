using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PizzeriaContable.Models;

namespace PizzeriaContable.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConfiguracionEmpresa> Configuraciones { get; set; } = null!;
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Producto> Productos { get; set; } = null!;
    public DbSet<Ingrediente> Ingredientes { get; set; } = null!;
    public DbSet<MovimientoInventario> MovimientosInventario { get; set; } = null!;
    public DbSet<Receta> Recetas { get; set; } = null!;
    public DbSet<RecetaDetalle> RecetaDetalles { get; set; } = null!;
    public DbSet<Proveedor> Proveedores { get; set; } = null!;
    public DbSet<Compra> Compras { get; set; } = null!;
    public DbSet<CompraDetalle> CompraDetalles { get; set; } = null!;
    public DbSet<PagoCompra> PagosCompras { get; set; } = null!;
    public DbSet<CajaSesion> CajasSesiones { get; set; } = null!;
    public DbSet<CajaMovimiento> CajasMovimientos { get; set; } = null!;
    public DbSet<Venta> Ventas { get; set; } = null!;
    public DbSet<VentaDetalle> VentaDetalles { get; set; } = null!;
    public DbSet<VentaPago> VentaPagos { get; set; } = null!;
    public DbSet<Gasto> Gastos { get; set; } = null!;
    public DbSet<CuentaContable> CuentasContables { get; set; } = null!;
    public DbSet<AsientoContable> AsientosContables { get; set; } = null!;
    public DbSet<AsientoDetalle> AsientoDetalles { get; set; } = null!;
    public DbSet<PeriodoContable> PeriodosContables { get; set; } = null!;
    public DbSet<AuditoriaRegistro> Auditorias { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Precisión numérica y reglas para SQL Server
        modelBuilder.Entity<Producto>(e =>
        {
            e.Property(p => p.Precio).HasPrecision(18, 2);
            e.HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ingrediente>(e =>
        {
            e.Property(i => i.CostoUnitario).HasPrecision(18, 2);
            e.Property(i => i.StockActual).HasPrecision(18, 3);
            e.Property(i => i.StockMinimo).HasPrecision(18, 3);
            e.HasOne(i => i.ProveedorHabitual)
                .WithMany(pr => pr.IngredientesSuministrados)
                .HasForeignKey(i => i.ProveedorHabitualId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MovimientoInventario>(e =>
        {
            e.Property(m => m.Cantidad).HasPrecision(18, 3);
            e.Property(m => m.CostoUnitario).HasPrecision(18, 2);
            e.Property(m => m.Total).HasPrecision(18, 2);
            e.HasOne(m => m.Ingrediente)
                .WithMany(i => i.Movimientos)
                .HasForeignKey(m => m.IngredienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Receta>(e =>
        {
            e.Property(r => r.MermaPrevistaPorcentaje).HasPrecision(5, 2);
            e.HasOne(r => r.Producto)
                .WithOne(p => p.Receta)
                .HasForeignKey<Receta>(r => r.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecetaDetalle>(e =>
        {
            e.Property(rd => rd.Cantidad).HasPrecision(18, 3);
            e.HasOne(rd => rd.Receta)
                .WithMany(r => r.Detalles)
                .HasForeignKey(rd => rd.RecetaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(rd => rd.Ingrediente)
                .WithMany(i => i.RecetaDetalles)
                .HasForeignKey(rd => rd.IngredienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Compra>(e =>
        {
            e.Property(c => c.Subtotal).HasPrecision(18, 2);
            e.Property(c => c.Impuesto).HasPrecision(18, 2);
            e.Property(c => c.Total).HasPrecision(18, 2);
            e.Property(c => c.SaldoPendiente).HasPrecision(18, 2);
            e.HasOne(c => c.Proveedor)
                .WithMany(pr => pr.Compras)
                .HasForeignKey(c => c.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CompraDetalle>(e =>
        {
            e.Property(cd => cd.Cantidad).HasPrecision(18, 3);
            e.Property(cd => cd.CostoUnitario).HasPrecision(18, 2);
            e.Property(cd => cd.Subtotal).HasPrecision(18, 2);
            e.HasOne(cd => cd.Compra)
                .WithMany(c => c.Detalles)
                .HasForeignKey(cd => cd.CompraId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cd => cd.Ingrediente)
                .WithMany()
                .HasForeignKey(cd => cd.IngredienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PagoCompra>(e =>
        {
            e.Property(p => p.Monto).HasPrecision(18, 2);
            e.HasOne(p => p.Compra)
                .WithMany(c => c.Pagos)
                .HasForeignKey(p => p.CompraId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CajaSesion>(e =>
        {
            e.Property(cs => cs.MontoInicial).HasPrecision(18, 2);
            e.Property(cs => cs.TotalVentasEfectivo).HasPrecision(18, 2);
            e.Property(cs => cs.TotalVentasTarjeta).HasPrecision(18, 2);
            e.Property(cs => cs.TotalVentasTransferencia).HasPrecision(18, 2);
            e.Property(cs => cs.TotalIngresos).HasPrecision(18, 2);
            e.Property(cs => cs.TotalEgresos).HasPrecision(18, 2);
            e.Property(cs => cs.EfectivoEsperado).HasPrecision(18, 2);
            e.Property(cs => cs.EfectivoContado).HasPrecision(18, 2);
            e.Property(cs => cs.Diferencia).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CajaMovimiento>(e =>
        {
            e.Property(cm => cm.Monto).HasPrecision(18, 2);
            e.HasOne(cm => cm.CajaSesion)
                .WithMany(cs => cs.Movimientos)
                .HasForeignKey(cm => cm.CajaSesionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Venta>(e =>
        {
            e.Property(v => v.Subtotal).HasPrecision(18, 2);
            e.Property(v => v.Descuento).HasPrecision(18, 2);
            e.Property(v => v.Impuesto).HasPrecision(18, 2);
            e.Property(v => v.Total).HasPrecision(18, 2);
            e.HasOne(v => v.CajaSesion)
                .WithMany(cs => cs.Ventas)
                .HasForeignKey(v => v.CajaSesionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VentaDetalle>(e =>
        {
            e.Property(vd => vd.PrecioUnitario).HasPrecision(18, 2);
            e.Property(vd => vd.Descuento).HasPrecision(18, 2);
            e.Property(vd => vd.Subtotal).HasPrecision(18, 2);
            e.HasOne(vd => vd.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(vd => vd.VentaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(vd => vd.Producto)
                .WithMany()
                .HasForeignKey(vd => vd.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VentaPago>(e =>
        {
            e.Property(vp => vp.Monto).HasPrecision(18, 2);
            e.HasOne(vp => vp.Venta)
                .WithMany(v => v.Pagos)
                .HasForeignKey(vp => vp.VentaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Gasto>(e =>
        {
            e.Property(g => g.Monto).HasPrecision(18, 2);
            e.HasOne(g => g.Proveedor)
                .WithMany()
                .HasForeignKey(g => g.ProveedorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CuentaContable>(e =>
        {
            e.HasIndex(c => c.Codigo).IsUnique();
            e.HasOne(c => c.CuentaPadre)
                .WithMany(c => c.Subcuentas)
                .HasForeignKey(c => c.CuentaPadreId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AsientoDetalle>(e =>
        {
            e.Property(ad => ad.Debe).HasPrecision(18, 2);
            e.Property(ad => ad.Haber).HasPrecision(18, 2);
            e.HasOne(ad => ad.AsientoContable)
                .WithMany(a => a.Detalles)
                .HasForeignKey(ad => ad.AsientoContableId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ad => ad.CuentaContable)
                .WithMany(c => c.AsientoDetalles)
                .HasForeignKey(ad => ad.CuentaContableId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConfiguracionEmpresa>(e =>
        {
            e.Property(ce => ce.TasaIvaPorcentaje).HasPrecision(5, 2);
        });
    }
}
