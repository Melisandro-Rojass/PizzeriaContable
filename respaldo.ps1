# ==============================================================================
# SCRIPT DE RESPALDO Y COPIA DE SEGURIDAD - BASE DE DATOS PIZZERÍA CONTABLE
# ==============================================================================

param(
    [string]$RutaDestino = "c:\Proyectos\PizzeriaContable\backups"
)

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "   RESPALDO DE BASE DE DATOS: PizzeriaContableDB" -ForegroundColor Yellow
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Asegurar directorio de respaldos
if (!(Test-Path $RutaDestino)) {
    New-Item -ItemType Directory -Force -Path $RutaDestino | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$nombreArchivo = "PizzeriaContableDB_$timestamp.bak"
$archivoCompleto = Join-Path $RutaDestino $nombreArchivo

Write-Host "[1/2] Generando archivo de respaldo en: $archivoCompleto..." -ForegroundColor White

$connectionString = "Server=(localdb)\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True"
$sqlBackup = @"
BACKUP DATABASE [PizzeriaContableDB]
TO DISK = N'$archivoCompleto'
WITH NOFORMAT, NOINIT,
NAME = N'PizzeriaContableDB-Full Database Backup',
SKIP, NOREWIND, NOUNLOAD, STATS = 10;
"@

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlBackup
    $cmd.CommandTimeout = 120
    $cmd.ExecuteNonQuery() | Out-Null
    $conn.Close()

    Write-Host "[2/2] Respaldo generado con EXITO." -ForegroundColor Green
    Write-Host "      Archivo: $archivoCompleto" -ForegroundColor Cyan
    Write-Host "      Tamano: $((Get-Item $archivoCompleto).Length / 1KB) KB" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Para restaurar este respaldo en caso de contingencia ejecute en SQL:" -ForegroundColor Yellow
    Write-Host "RESTORE DATABASE [PizzeriaContableDB] FROM DISK = N'$archivoCompleto' WITH REPLACE;" -ForegroundColor Gray
}
catch {
    Write-Host "ERROR al generar respaldo: $($_.Exception.Message)" -ForegroundColor Red
}
