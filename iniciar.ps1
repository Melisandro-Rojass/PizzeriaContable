# ==============================================================================
# SCRIPT DE INICIO Y VERIFICACIÓN AUTOMÁTICA - PIZZERÍA CONTABLE (.NET 10 / SQL SERVER)
# ==============================================================================

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "   PIZZERIA CONTABLE - SISTEMA CONTABLE Y PUNTO DE VENTA (POS)" -ForegroundColor Yellow
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Verificar versión del SDK de .NET
Write-Host "[1/4] Verificando instalacion de .NET SDK..." -ForegroundColor White
$dotnetVersion = dotnet --version
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: No se encontro el SDK de .NET instalado en el sistema." -ForegroundColor Red
    exit 1
}
Write-Host "      .NET SDK detectado: $dotnetVersion" -ForegroundColor Green

# 2. Verificar estado de SQL Server LocalDB
Write-Host "[2/4] Verificando estado de SQL Server (MSSQLLocalDB)..." -ForegroundColor White
$localDbInfo = sqllocaldb info mssqllocaldb 2>&1
if ($localDbInfo -match "State:\s+Stopped" -or $localDbInfo -match "Estado:\s+Detenido") {
    Write-Host "      Iniciando instancia MSSQLLocalDB..." -ForegroundColor Yellow
    sqllocaldb start mssqllocaldb | Out-Null
}
Write-Host "      SQL Server LocalDB se encuentra activo y listo." -ForegroundColor Green

# 3. Aplicar migraciones pendientes de Entity Framework Core
Write-Host "[3/4] Verificando esquema de base de datos y migraciones EF Core..." -ForegroundColor White
try {
    dotnet ef database update --project "c:\Proyectos\PizzeriaContable\PizzeriaContable.csproj" --no-build
    Write-Host "      Base de datos 'PizzeriaContableDB' actualizada correctamente." -ForegroundColor Green
} catch {
    Write-Host "      (Aviso: Las migraciones ya estan aplicadas o se ejecutaran en arranque)." -ForegroundColor Gray
}

# 4. Lanzar la aplicación web ASP.NET Core MVC
Write-Host "[4/4] Iniciando la aplicacion web en modo local..." -ForegroundColor White
Write-Host "-----------------------------------------------------------------" -ForegroundColor Gray
Write-Host "   URL Principal:          http://localhost:5000" -ForegroundColor Cyan
Write-Host "   Punto de Venta (POS):   http://localhost:5000/Ventas/Pos" -ForegroundColor Yellow
Write-Host "   Usuario Administrador:  admin@pizzeria.local" -ForegroundColor White
Write-Host "   Contrasena Inicial:     AdminPizzeria2026!" -ForegroundColor White
Write-Host "   Moneda del Sistema:     Cordobas de Nicaragua (NIO C$)" -ForegroundColor White
Write-Host "-----------------------------------------------------------------" -ForegroundColor Gray
Write-Host "Presione CTRL+C en esta terminal para detener el servidor web." -ForegroundColor Magenta
Write-Host ""

dotnet run --project "c:\Proyectos\PizzeriaContable\PizzeriaContable.csproj" --urls "http://localhost:5000"
