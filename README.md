# PIZZERÍA CONTABLE - SISTEMA CONTABLE WEB Y PUNTO DE VENTA (POS)
## ASP.NET Core MVC (.NET 10 LTS) + SQL Server + Entity Framework Core 10

---

## 1. Resumen del Sistema y Arquitectura

**Pizzería Contable** es una solución integral de gestión contable, inventarios, compras, gastos, turnos de caja y punto de venta (POS) diseñada para pizzerías y negocios gastronómicos con operaciones en marcha.

### Principios de Arquitectura:
- **Clean Architecture & Monolito Modular**: Desarrollado 100% en ASP.NET Core MVC con .NET 10 LTS.
- **Partida Doble Estricta**: Cada transacción comercial (Venta en Efectivo/Tarjeta, Compra de Harinas/Quesos, Pago de Servicios/Alquiler o Abonos a Proveedores) genera automáticamente un asiento contable equilibrado (`SUM(Debe) == SUM(Haber)`) con validación de idempotencia por documento origen.
- **Deducción de Inventario por Receta**: La venta de pizzas deduce automáticamente gramos o mililitros de sus ingredientes asociados, manejando conversiones exactas (`g <-> kg`, `ml <-> L`, `oz <-> g`).
- **Arqueo de Caja Físico Riguroso**: Las ventas con tarjeta o transferencia se registran en los ingresos del turno pero se segregan del efectivo físico en gaveta para evitar descuadres en el conteo de billetes.
- **Localización Nicaragüense**: Configurado con cultura `es-NI`, moneda oficial Córdoba (`C$ NIO`), zona horaria `America/Managua` y tasa de IVA del 15%.

---

## 2. Requisitos

Este proyecto está preparado para **Windows** y necesita:

1. **.NET 10 SDK**. Comprueba la instalación con `dotnet --list-sdks`.
2. **SQL Server Express LocalDB** (instancia `MSSQLLocalDB`), o una instancia de SQL Server accesible.
   LocalDB se instala por separado; puedes incluirlo desde el instalador de SQL Server Express o desde Visual Studio Installer.
3. **PowerShell 5.1 o superior** para los comandos de esta guía.
4. **`dotnet-ef` 10.0.13**, necesario para crear/actualizar el esquema de la base de datos.

## 3. Instalación y primer inicio

Abre PowerShell en la carpeta raíz del repositorio (donde está `PizzeriaContable.csproj`) y ejecuta estos pasos.

### 3.1. Comprobar e iniciar LocalDB

```powershell
sqllocaldb info MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

La cadena de conexión predeterminada está en `appsettings.json` y `appsettings.Development.json`:
`(localdb)\MSSQLLocalDB`, base de datos `PizzeriaContableDB`, autenticación de Windows.
Si usas otra instancia de SQL Server, actualiza `ConnectionStrings:ConexionPizzeria` en ambos archivos y asegúrate de que la cuenta tenga permisos para crear/actualizar la base de datos.

### 3.2. Instalar la herramienta de Entity Framework

Instala la versión que corresponde a las dependencias del proyecto:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.13
```

Si ya tienes `dotnet-ef` instalado, actualízalo en vez de instalarlo otra vez:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.13
```

Si PowerShell no reconoce `dotnet-ef` después de la instalación, cierra y vuelve a abrir la terminal.

### 3.3. Restaurar dependencias y preparar la base de datos

```powershell
dotnet restore .\PizzeriaContable.csproj
dotnet ef database update --project .\PizzeriaContable.csproj
```

El segundo comando crea `PizzeriaContableDB` si aún no existe y aplica las migraciones. Es necesario ejecutarlo antes del primer inicio: la aplicación inicializa datos de ejemplo al arrancar, pero no aplica las migraciones automáticamente.

### 3.4. Iniciar la aplicación

```powershell
dotnet run --project .\PizzeriaContable.csproj --urls http://localhost:7298
```

Abre **[http://localhost:7298](http://localhost:7298)**. Para detener el servidor, presiona `Ctrl+C` en la terminal.

También existe `.\iniciar.ps1`, que intenta preparar la base de datos e iniciar el sitio, pero está configurado con la ruta fija `C:\Proyectos\PizzeriaContable`. Si clonaste el repositorio en otra ubicación, usa los comandos anteriores.

### Acceso inicial

En la primera ejecución se crean los roles, los datos de muestra y el usuario administrador:

- **Usuario:** `admin` (también puedes iniciar sesión con `admin@pizzeria.local`)
- **Contraseña predeterminada:** `AdminPizzeria2026!`
- **Rol:** `Administrador`

Antes del primer inicio puedes definir otra contraseña inicial en PowerShell:

```powershell
$env:PIZZERIA_ADMIN_PASSWORD = "EscribeUnaClaveSegura"
```

Después ejecuta el comando de inicio en esa misma terminal. Esta configuración solo se utiliza cuando se crea el primer administrador; no cambia la contraseña de una cuenta ya existente. Cambia la contraseña predeterminada después del primer acceso.

---

## 4. Módulos del Sistema

### 4.1. Panel de Control (Dashboard Financiero)
- **Ruta:** `/` o `/Home/Index`
- Indicadores en tiempo real: Ventas del día/mes, Margen bruto acumulado, Utilidad neta estimada, Dinero físico en caja, y Alertas automáticas de stock crítico de ingredientes.

### 4.2. Punto de Venta (POS) Táctil
- **Ruta:** `/Ventas/Pos`
- Catálogo visual por categorías, búsqueda instantánea, carrito dinámico con adición y modificación de cantidades, cálculo en vivo de subtotal e IVA (15%), cobro en Efectivo con cálculo de cambio, cobro con Tarjeta o Transferencia bancaria, y emisión de comprobantes consecutivos (`TCK-YYYYMMDD-XXXX`).

### 4.3. Historial de Ventas, Anulación y Tickets Térmicos
- **Ruta:** `/Ventas/Index`
- Listado de ventas con filtros de fecha y estado.
- **Detalle de Venta (`/Ventas/Details/id`)**: Muestra los productos facturados, el desglose de pagos, el asiento contable generado y la trazabilidad de auditoría.
- **Anulación con Reversión (`/Ventas/Anular`)**: Reincorpora los ingredientes de la receta al stock y registra un contra-asiento contable de reversión.
- **Ticket Imprimible (`/Ventas/Ticket/id`)**: Formato limpio para impresoras térmicas de 58mm/80mm o navegadores.

### 4.4. Control de Turnos de Caja (Arqueo y Cierre)
- **Ruta:** `/Caja/Index`
- Apertura de turno con fondo inicial en gaveta, registro de entradas y salidas de efectivo (gastos menores o retiros parciales), y cierre de turno con conteo físico y cálculo de diferencia (Sobrante / Faltante).

### 4.5. Catálogo de Cuentas y Libro Diario
- **Ruta:** `/Contabilidad/Cuentas` y `/Contabilidad/Asientos`
- Plan de cuentas contables de 4 niveles adaptado a la industria de restaurantes (Activo, Pasivo, Capital, Ingreso, Costo, Gasto).
- Libro Diario con visualización de asientos, balance de sumas iguales y opción para registrar asientos manuales (`/Contabilidad/CrearAsiento`) con validación en vivo.
- **Libro Mayor (`/Contabilidad/LibroMayor/id`)**: Detalle cronológico de débitos, créditos y saldos por cuenta contable.

### 4.6. Asistente de Apertura Contable (Pizzería en Marcha)
- **Ruta:** `/Contabilidad/Apertura`
- Permite a pizzerías con operaciones previas ingresar sus saldos históricos de arranque:
  - *Activos:* Efectivo en caja, saldos bancarios, valoración de existencias iniciales de quesos/harinas, mobiliario y hornos de pizza.
  - *Pasivos:* Deudas vigentes con proveedores y préstamos.
  - *Capital:* Patrimonio neto de los propietarios.
- Verifica en tiempo real la ecuación contable: `Activo = Pasivo + Capital` antes de asentar.

### 4.7. Reportes Financieros con Exportación CSV
- **Estado de Resultados Integral (`/Reportes/EstadoResultados`)**: Ventas Totales, Costo de Ventas (Materia Prima), Margen Bruto, Gastos Operativos y Utilidad Neta. Exportación a CSV.
- **Balance General Clasificado (`/Reportes/BalanceGeneral`)**: Activos, Pasivos y Patrimonio con confirmación de ecuación cuadrada. Exportación a CSV.
- **Balance de Comprobación (`/Reportes/BalanceComprobacion`)**: 8 columnas con sumas de movimientos y saldos deudores/acreedores verificados. Exportación a CSV.
- **Kardex General de Inventario (`/Reportes/KardexGeneral`)**: Auditoría completa de entradas, consumos por receta, ajustes y mermas.

### 4.8. Compras y Cuentas por Pagar a Proveedores
- **Rutas:** `/Compras` y `/Proveedores`
- Registro de compras de materia prima a crédito o contado, actualización automática del costo promedio ponderado de los ingredientes, registro de abonos parciales y contabilización instantánea contra proveedores (`2.1.01.01`).

### 4.9. Gastos Operativos
- **Ruta:** `/Gastos`
- Registro de alquiler, energía eléctrica, gas propano para hornos, nómina y publicidad. Contabilización automática en cuentas de gasto (`6.x`) y egreso de caja o banco.

### 4.10. Recetas y Costeo de Pizzas
- **Ruta:** `/Recetas`
- Definición de recetas estándar por pizza (harina, queso mozzarella, salsa, pepperoni, etc.), rendimiento, cálculo automático de costo teórico por porción y porcentaje de margen bruto.

### 4.11. Usuarios, Roles y Seguridad
- **Ruta:** `/Usuarios`
- Autenticación con ASP.NET Core Identity. Gestión de roles: `Administrador`, `Cajero`, `EncargadoInventario` y `Contador`.

### 4.12. Configuración Fiscal de la Empresa
- **Ruta:** `/Configuracion`
- Parámetros comerciales: RUC/Cédula, Razón Social, Dirección, Teléfono, Tasa de IVA (15%), Moneda (C$) y Zona Horaria.

---

## 5. Respaldos de Base de Datos y Recuperación

Para generar una copia de seguridad física `.bak` de SQL Server:

```powershell
.\respaldo.ps1
```

El script creará un archivo con fecha y hora dentro de la carpeta `backups/` (ej. `backups/PizzeriaContableDB_20261024_163939.bak`).

Para restaurar una copia de seguridad en caso de contingencia, ejecute en SQL Server:
```sql
RESTORE DATABASE [PizzeriaContableDB]
FROM DISK = N'c:\Proyectos\PizzeriaContable\backups\PizzeriaContableDB_YYYYMMDD_HHMMSS.bak'
WITH REPLACE;
```

---

## 6. Pruebas Unitarias Automatizadas (xUnit)

La solución cuenta con un proyecto de pruebas en `Tests/PizzeriaContable.Tests.csproj`.

Para ejecutar la batería de pruebas:

```bash
dotnet test
```

### Casos de Prueba Verificados:
1. **Recetas y Costeo:** Cálculo de costo teórico según componentes y costo unitario de ingredientes; verificación de margen bruto porcentual.
2. **Conversión de Unidades de Inventario:** Conversiones exactas `g <-> kg`, `ml <-> L`, `oz <-> g`, y detección/rechazo de unidades incompatibles.
3. **Partida Doble Contable:** Validación de asientos balanceados (`Debe == Haber`), rechazo de asientos desbalanceados, y prevención de doble contabilización para el mismo documento.
4. **Arqueo y Segregación de Medios de Pago:** Comprobación de que pagos con Tarjeta o Transferencia no distorsionan el efectivo físico en caja y cálculo de diferencias en el cierre.
