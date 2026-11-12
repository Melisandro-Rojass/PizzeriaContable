/*
    Q3 2026 seed for the SQL Server schema defined by the EF Core migrations.

    Schema and mapping:
    - CajasSesiones:
      Id (identity PK), FechaApertura, FechaCierre, MontoInicial, TotalVentasEfectivo,
      TotalVentasTarjeta, TotalVentasTransferencia, TotalIngresos, TotalEgresos,
      EfectivoEsperado, EfectivoContado, Diferencia, Estado, UsuarioId,
      Observaciones.
      The CSV is daily aggregate data, so each date is stored as one closed
      cash-session summary.
      TotalVentasEfectivo = Ventas_Totales - Tarjeta_Transf.
      TotalVentasTransferencia stores the combined Tarjeta_Transf amount because
      the CSV does not provide a card/transfer split; TotalVentasTarjeta is zero.
      The observation preserves this caveat and Tipo_Registro/Detalle_Notas.
      EfectivoContado is zero because the CSV has no independent physical count;
      the UI labels imported sessions as "Sin arqueo" instead of showing a
      fabricated count or cash difference.
    - CajasMovimientos:
      Id (identity PK), CajaSesionId, Tipo, Monto, Descripcion, Fecha, UsuarioId. A positive
      Gastos_Caja becomes one cash-out movement linked to the inserted session.
      CajaSesionId has a required FK to CajasSesiones.Id. UsuarioId is nullable
      and has no FK; the cash-session table has no branch, category, payment-type
      or account IDs.
    - Ventas and VentaPagos:
      One clearly labeled daily consolidated Venta per date and up to two
      summary payments (cash and a combined card/transfer method) make imported
      revenue visible in the existing sales screens without inventing products.
      The record is not a POS ticket; the 15% VAT is treated as included in
      Ventas_Totales, with subtotal and tax separated in Ventas. The CSV still
      does not split card from transfer or identify buyers; ClienteNombre is
      "Resumen diario CSV" and no buyer attribution is fabricated. No
      VentaDetalles rows are created.
    - Gastos:
      One Gastos row per date with positive Gastos_Caja makes the cash expense
      detail visible in the existing expenses screens. It is an aggregate, not
      an itemized receipt; its imported marker and notes are retained.
    - AsientosContables and AsientoDetalles:
      The seed posts each imported daily sale and cash expense, plus projected
      fixed monthly costs. The 15% VAT is included in the CSV gross sales:
      Debe cash/bank for the amount collected, Haber sales for the rounded
      taxable base, and Haber VAT payable for the difference. Non-cash sales
      are debited to the bank account because the CSV combines card and transfer
      payments. Projected monthly expenses are left pending and credited to
      other payables; the CSV does not confirm payment.
    - Gastos:
      Four additional projected monthly records are stored per month: base
      supplies (C$13,890.64), electricity (C$2,600), water (C$500), and payroll
      (C$18,650). These are estimates and may overlap with daily expenses.
    - Other existing transaction tables (not used for this aggregate seed):
      Ventas: Id (identity PK), NumeroComprobante, Fecha, Subtotal, Descuento, Impuesto, Total,
      MetodoPago, Estado, ClienteNombre, Observaciones, CajaSesionId, UsuarioId.
      CajaSesionId is nullable and references CajasSesiones.Id; UsuarioId is
      nullable and has no FK.
      VentaDetalles: Id (identity PK), VentaId, ProductoId, Cantidad,
      PrecioUnitario, Descuento, Subtotal. VentaId references Ventas.Id;
      ProductoId references Productos.Id.
      VentaPagos: Id (identity PK), VentaId, MetodoPago, Monto, Referencia.
      VentaId is required and references Ventas.Id.
      Gastos: Id (identity PK), CategoriaGasto, Descripcion, FechaGasto, FechaPago, Monto,
      ProveedorId, NumeroComprobante, MetodoPago, Estado, FechaVencimiento,
      Observaciones. ProveedorId is nullable and references Proveedores.Id.
      There is no dedicated fixed-cost/payroll table; monthly projections use
      generic Gastos with explicit projection markers and unique references.
    - The source does not contain a per-sale breakdown or independently
      distinguish card from transfer; those details are not fabricated here.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Q3 TABLE
(
    Fecha date NOT NULL PRIMARY KEY,
    TipoRegistro nvarchar(40) NOT NULL,
    VentasTotales decimal(18,2) NOT NULL,
    TarjetaTransferencia decimal(18,2) NOT NULL,
    GastosCaja decimal(18,2) NOT NULL,
    EfectivoNeto decimal(18,2) NOT NULL,
    DetalleNotas nvarchar(250) NOT NULL
);

INSERT INTO @Q3
    (Fecha, TipoRegistro, VentasTotales, TarjetaTransferencia, GastosCaja, EfectivoNeto, DetalleNotas)
VALUES
    (CONVERT(date, '2026-07-01'), N'Estimado (Promedio Día)', 2573.00, 89.00, 64.00, 2420.00, N'Aproximado según promedio histórico de Miércoles'),
    (CONVERT(date, '2026-07-02'), N'Estimado (Promedio Día)', 2325.00, 240.00, 130.00, 1955.00, N'Aproximado según promedio histórico de Jueves'),
    (CONVERT(date, '2026-07-03'), N'Estimado (Promedio Día)', 2604.00, 388.00, 178.00, 2038.00, N'Aproximado según promedio histórico de Viernes'),
    (CONVERT(date, '2026-07-04'), N'Estimado (Promedio Día)', 4258.00, 498.00, 240.00, 3520.00, N'Aproximado según promedio histórico de Sábado'),
    (CONVERT(date, '2026-07-05'), N'Estimado (Promedio Día)', 5090.00, 483.00, 253.00, 4354.00, N'Aproximado según promedio histórico de Domingo'),
    (CONVERT(date, '2026-07-06'), N'Estimado (Promedio Día)', 2098.00, 305.00, 80.00, 1713.00, N'Aproximado según promedio histórico de Lunes'),
    (CONVERT(date, '2026-07-07'), N'Estimado (Promedio Día)', 2487.00, 202.00, 138.00, 2147.00, N'Aproximado según promedio histórico de Martes'),
    (CONVERT(date, '2026-07-08'), N'Estimado (Promedio Día)', 2573.00, 89.00, 64.00, 2420.00, N'Aproximado según promedio histórico de Miércoles'),
    (CONVERT(date, '2026-07-09'), N'Estimado (Promedio Día)', 2325.00, 240.00, 130.00, 1955.00, N'Aproximado según promedio histórico de Jueves'),
    (CONVERT(date, '2026-07-10'), N'Estimado (Promedio Día)', 2604.00, 388.00, 178.00, 2038.00, N'Aproximado según promedio histórico de Viernes'),
    (CONVERT(date, '2026-07-11'), N'Estimado (Promedio Día)', 4258.00, 498.00, 240.00, 3520.00, N'Aproximado según promedio histórico de Sábado'),
    (CONVERT(date, '2026-07-12'), N'Estimado (Promedio Día)', 5090.00, 483.00, 253.00, 4354.00, N'Aproximado según promedio histórico de Domingo'),
    (CONVERT(date, '2026-07-13'), N'Estimado (Promedio Día)', 2098.00, 305.00, 80.00, 1713.00, N'Aproximado según promedio histórico de Lunes'),
    (CONVERT(date, '2026-07-14'), N'Estimado (Promedio Día)', 2487.00, 202.00, 138.00, 2147.00, N'Aproximado según promedio histórico de Martes'),
    (CONVERT(date, '2026-07-15'), N'Estimado (Promedio Día)', 2573.00, 89.00, 64.00, 2420.00, N'Aproximado según promedio histórico de Miércoles'),
    (CONVERT(date, '2026-07-16'), N'Estimado (Promedio Día)', 2325.00, 240.00, 130.00, 1955.00, N'Aproximado según promedio histórico de Jueves'),
    (CONVERT(date, '2026-07-17'), N'Estimado (Promedio Día)', 2604.00, 388.00, 178.00, 2038.00, N'Aproximado según promedio histórico de Viernes'),
    (CONVERT(date, '2026-07-18'), N'Estimado (Promedio Día)', 4258.00, 498.00, 240.00, 3520.00, N'Aproximado según promedio histórico de Sábado'),
    (CONVERT(date, '2026-07-19'), N'Estimado (Promedio Día)', 5090.00, 483.00, 253.00, 4354.00, N'Aproximado según promedio histórico de Domingo'),
    (CONVERT(date, '2026-07-20'), N'Estimado (Promedio Día)', 2098.00, 305.00, 80.00, 1713.00, N'Aproximado según promedio histórico de Lunes'),
    (CONVERT(date, '2026-07-21'), N'Estimado (Promedio Día)', 2487.00, 202.00, 138.00, 2147.00, N'Aproximado según promedio histórico de Martes'),
    (CONVERT(date, '2026-07-22'), N'Estimado (Promedio Día)', 2573.00, 89.00, 64.00, 2420.00, N'Aproximado según promedio histórico de Miércoles'),
    (CONVERT(date, '2026-07-23'), N'Estimado (Promedio Día)', 2325.00, 240.00, 130.00, 1955.00, N'Aproximado según promedio histórico de Jueves'),
    (CONVERT(date, '2026-07-24'), N'Estimado (Promedio Día)', 2604.00, 388.00, 178.00, 2038.00, N'Aproximado según promedio histórico de Viernes'),
    (CONVERT(date, '2026-07-25'), N'Estimado (Promedio Día)', 4258.00, 498.00, 240.00, 3520.00, N'Aproximado según promedio histórico de Sábado'),
    (CONVERT(date, '2026-07-26'), N'Estimado (Promedio Día)', 5090.00, 483.00, 253.00, 4354.00, N'Aproximado según promedio histórico de Domingo'),
    (CONVERT(date, '2026-07-27'), N'Estimado (Promedio Día)', 2098.00, 305.00, 80.00, 1713.00, N'Aproximado según promedio histórico de Lunes'),
    (CONVERT(date, '2026-07-28'), N'Estimado (Promedio Día)', 2487.00, 202.00, 138.00, 2147.00, N'Aproximado según promedio histórico de Martes'),
    (CONVERT(date, '2026-07-29'), N'Estimado (Promedio Día)', 2573.00, 89.00, 64.00, 2420.00, N'Aproximado según promedio histórico de Miércoles'),
    (CONVERT(date, '2026-07-30'), N'Estimado (Promedio Día)', 2325.00, 240.00, 130.00, 1955.00, N'Aproximado según promedio histórico de Jueves'),
    (CONVERT(date, '2026-07-31'), N'Estimado (Promedio Día)', 2604.00, 388.00, 178.00, 2038.00, N'Aproximado según promedio histórico de Viernes'),
    (CONVERT(date, '2026-08-01'), N'Estimado (Promedio Día)', 4258.00, 498.00, 240.00, 3520.00, N'Aproximado según promedio histórico de Sábado'),
    (CONVERT(date, '2026-08-02'), N'Estimado (Promedio Día)', 5090.00, 483.00, 253.00, 4354.00, N'Aproximado según promedio histórico de Domingo'),
    (CONVERT(date, '2026-08-03'), N'Estimado (Promedio Día)', 2098.00, 305.00, 80.00, 1713.00, N'Aproximado según promedio histórico de Lunes'),
    (CONVERT(date, '2026-08-04'), N'Estimado (Promedio Día)', 2487.00, 202.00, 138.00, 2147.00, N'Aproximado según promedio histórico de Martes'),
    (CONVERT(date, '2026-08-05'), N'Estimado (Promedio Día)', 2573.00, 89.00, 64.00, 2420.00, N'Aproximado según promedio histórico de Miércoles'),
    (CONVERT(date, '2026-08-06'), N'Estimado (Promedio Día)', 2325.00, 240.00, 130.00, 1955.00, N'Aproximado según promedio histórico de Jueves'),
    (CONVERT(date, '2026-08-07'), N'Estimado (Promedio Día)', 2604.00, 388.00, 178.00, 2038.00, N'Aproximado según promedio histórico de Viernes'),
    (CONVERT(date, '2026-08-08'), N'Estimado (Promedio Día)', 4258.00, 498.00, 240.00, 3520.00, N'Aproximado según promedio histórico de Sábado'),
    (CONVERT(date, '2026-08-09'), N'Estimado (Promedio Día)', 5090.00, 483.00, 253.00, 4354.00, N'Aproximado según promedio histórico de Domingo'),
    (CONVERT(date, '2026-08-10'), N'Estimado (Promedio Día)', 2098.00, 305.00, 80.00, 1713.00, N'Aproximado según promedio histórico de Lunes'),
    (CONVERT(date, '2026-08-11'), N'Estimado (Promedio Día)', 2487.00, 202.00, 138.00, 2147.00, N'Aproximado según promedio histórico de Martes'),
    (CONVERT(date, '2026-08-12'), N'Estimado (Promedio Día)', 2573.00, 89.00, 64.00, 2420.00, N'Aproximado según promedio histórico de Miércoles'),
    (CONVERT(date, '2026-08-13'), N'Estimado (Promedio Día)', 2325.00, 240.00, 130.00, 1955.00, N'Aproximado según promedio histórico de Jueves'),
    (CONVERT(date, '2026-08-14'), N'Estimado (Promedio Día)', 2604.00, 388.00, 178.00, 2038.00, N'Aproximado según promedio histórico de Viernes'),
    (CONVERT(date, '2026-08-15'), N'Estimado (Promedio Día)', 4258.00, 498.00, 240.00, 3520.00, N'Aproximado según promedio histórico de Sábado'),
    (CONVERT(date, '2026-08-16'), N'Estimado (Promedio Día)', 5090.00, 483.00, 253.00, 4354.00, N'Aproximado según promedio histórico de Domingo'),
    (CONVERT(date, '2026-08-17'), N'Estimado (Promedio Día)', 2098.00, 305.00, 80.00, 1713.00, N'Aproximado según promedio histórico de Lunes'),
    (CONVERT(date, '2026-08-18'), N'Estimado (Promedio Día)', 2487.00, 202.00, 138.00, 2147.00, N'Aproximado según promedio histórico de Martes'),
    (CONVERT(date, '2026-08-19'), N'Real (Cuaderno)', 3320.00, 0.00, 500.00, 2820.00, N'Gas C$500'),
    (CONVERT(date, '2026-08-20'), N'Real (Cuaderno)', 2190.00, 400.00, 50.00, 1740.00, N'Delivery C$50'),
    (CONVERT(date, '2026-08-21'), N'Real (Cuaderno)', 1285.00, 220.00, 0.00, 1065.00, N'Registro diario cuaderno'),
    (CONVERT(date, '2026-08-22'), N'Real (Cuaderno)', 3610.00, 780.00, 250.00, 2580.00, N'15 gaseosas C$225, servilletas C$25'),
    (CONVERT(date, '2026-08-23'), N'Real (Cuaderno)', 5415.00, 550.00, 645.00, 4220.00, N'Jamón C$420, gaseosa C$225'),
    (CONVERT(date, '2026-08-24'), N'Real (Cuaderno)', 1275.00, 160.00, 159.00, 956.00, N'Coca vaso C$119, vigilancia C$40'),
    (CONVERT(date, '2026-08-25'), N'Real (Cuaderno)', 1815.00, 220.00, 430.00, 1165.00, N'Platos C$30, delivery C$400'),
    (CONVERT(date, '2026-08-26'), N'Real (Cuaderno)', 925.00, 0.00, 0.00, 925.00, N'Registro diario cuaderno'),
    (CONVERT(date, '2026-08-27'), N'Real (Cuaderno)', 1200.00, 220.00, 150.00, 830.00, N'Gaseosa C$150'),
    (CONVERT(date, '2026-08-28'), N'Real (Cuaderno)', 1955.00, 785.00, 86.00, 1084.00, N'2 gaseosas C$30, 2 jugos C$56'),
    (CONVERT(date, '2026-08-29'), N'Real (Cuaderno)', 4635.00, 360.00, 260.00, 4015.00, N'Delivery C$190, jabón/cloro C$20, taxi C$50'),
    (CONVERT(date, '2026-08-30'), N'Real (Cuaderno)', 5165.00, 1060.00, 207.00, 3898.00, N'Gaseosa C$150, pajilla C$17, vigilancia C$40'),
    (CONVERT(date, '2026-08-31'), N'Real (Cuaderno)', 1960.00, 0.00, 135.00, 1825.00, N'Platos C$60, servilletas C$25, taxi C$50'),
    (CONVERT(date, '2026-09-01'), N'Real (Cuaderno)', 2145.00, 310.00, 650.00, 1185.00, N'Gas C$500, gaseosa C$150'),
    (CONVERT(date, '2026-09-02'), N'Real (Cuaderno)', 1460.00, 0.00, 0.00, 1460.00, N'Registro diario cuaderno'),
    (CONVERT(date, '2026-09-03'), N'Real (Cuaderno)', 1150.00, 0.00, 55.00, 1095.00, N'Comida perro C$30, servilleta C$25'),
    (CONVERT(date, '2026-09-04'), N'Real (Cuaderno)', 1800.00, 0.00, 250.00, 1550.00, N'Joel C$250 (pago personal/otro)'),
    (CONVERT(date, '2026-09-05'), N'Real (Cuaderno)', 4055.00, 160.00, 225.00, 3670.00, N'Gaseosa C$225'),
    (CONVERT(date, '2026-09-06'), N'Real (Cuaderno)', 4675.00, 420.00, 300.00, 3955.00, N'Gaseosa C$150, platos C$30, jabón C$10, cloro C$10, delivery C$100'),
    (CONVERT(date, '2026-09-07'), N'Real (Cuaderno)', 1890.00, 270.00, 26.00, 1594.00, N'Cebolla/chiltoma C$26'),
    (CONVERT(date, '2026-09-08'), N'Real (Cuaderno)', 2550.00, 170.00, 0.00, 2380.00, N'Registro diario cuaderno'),
    (CONVERT(date, '2026-09-09'), N'Real (Cuaderno)', 2815.00, 0.00, 0.00, 2815.00, N'Registro diario cuaderno'),
    (CONVERT(date, '2026-09-10'), N'Real (Cuaderno)', 1090.00, 440.00, 50.00, 600.00, N'Sal C$10, jabón C$10, otros C$30'),
    (CONVERT(date, '2026-09-11'), N'Real (Cuaderno)', 2210.00, 150.00, 460.00, 1600.00, N'Delivery C$280, cloro C$10, gaseosa C$140, platos C$30'),
    (CONVERT(date, '2026-09-12'), N'Real (Cuaderno)', 4490.00, 325.00, 225.00, 3940.00, N'Gaseosa C$225'),
    (CONVERT(date, '2026-09-13'), N'Real (Cuaderno)', 4790.00, 220.00, 70.00, 4500.00, N'Vigilancia C$40, pollo C$30'),
    (CONVERT(date, '2026-09-14'), N'Real (Cuaderno)', 3065.00, 815.00, 184.00, 2066.00, N'Gaseosa C$150, bolsa C$34'),
    (CONVERT(date, '2026-09-15'), N'Real (Cuaderno)', 3770.00, 300.00, 10.00, 3460.00, N'Jabón C$10'),
    (CONVERT(date, '2026-09-16'), N'Real (Cuaderno)', 700.00, 0.00, 0.00, 700.00, N'Registro diario cuaderno'),
    (CONVERT(date, '2026-09-17'), N'Real (Cuaderno)', 3505.00, 0.00, 200.00, 3305.00, N'Delivery C$200'),
    (CONVERT(date, '2026-09-18'), N'Real (Cuaderno)', 1410.00, 0.00, 302.00, 1108.00, N'Gaseosa C$225, pajilla C$17, platos C$60'),
    (CONVERT(date, '2026-09-19'), N'Real (Cuaderno)', 2900.00, 860.00, 0.00, 2040.00, N'Registro diario cuaderno'),
    (CONVERT(date, '2026-09-20'), N'Real (Cuaderno)', 6620.00, 1060.00, 628.00, 4932.00, N'Platos/servilletas C$58, gas C$570'),
    (CONVERT(date, '2026-09-21'), N'Real (Cuaderno)', 2700.00, 490.00, 40.00, 2170.00, N'Vigilancia C$40'),
    (CONVERT(date, '2026-09-22'), N'Real (Cuaderno)', 2620.00, 0.00, 160.00, 2460.00, N'Gaseosa C$150, jabón C$10'),
    (CONVERT(date, '2026-09-23'), N'Real (Cuaderno)', 2200.00, 0.00, 55.00, 2145.00, N'Gastos menores C$55'),
    (CONVERT(date, '2026-09-24'), N'Real (Cuaderno)', 2675.00, 340.00, 50.00, 2285.00, N'Platos C$30, jabón C$10, cloro C$10'),
    (CONVERT(date, '2026-09-25'), N'Real (Cuaderno)', 2990.00, 220.00, 165.00, 2605.00, N'Gaseosa C$165'),
    (CONVERT(date, '2026-09-26'), N'Real (Cuaderno)', 3350.00, 765.00, 250.00, 2335.00, N'Delivery C$60, gaseosa C$180, jabón C$10'),
    (CONVERT(date, '2026-09-27'), N'Real (Cuaderno)', 5215.00, 150.00, 0.00, 5065.00, N'Registro diario cuaderno'),
    (CONVERT(date, '2026-09-28'), N'Real (Cuaderno)', 1300.00, 0.00, 30.00, 1270.00, N'Platos C$30'),
    (CONVERT(date, '2026-09-29'), N'Real (Cuaderno)', 500.00, 0.00, 0.00, 500.00, N'Registro diario cuaderno'),
    (CONVERT(date, '2026-09-30'), N'Real (Cuaderno)', 2430.00, 0.00, 150.00, 2280.00, N'Eli C$150');

IF (SELECT COUNT(*) FROM @Q3) <> 92
    THROW 51000, 'El seed debe contener exactamente 92 fechas.', 1;

IF (SELECT COUNT(*) FROM @Q3 WHERE TipoRegistro = N'Real (Cuaderno)') <> 43
   OR (SELECT COUNT(*) FROM @Q3 WHERE TipoRegistro = N'Estimado (Promedio Día)') <> 49
    THROW 51001, 'Los conteos de registros reales y estimados no coinciden con el CSV.', 1;

IF (SELECT SUM(VentasTotales) FROM @Q3) <> 267865.00
   OR (SELECT SUM(TarjetaTransferencia) FROM @Q3) <> 27655.00
   OR (SELECT SUM(GastosCaja) FROM @Q3) <> 14988.00
   OR (SELECT SUM(EfectivoNeto) FROM @Q3) <> 225222.00
    THROW 51002, 'Los totales Q3 no coinciden con los totales esperados del CSV.', 1;

IF EXISTS
(
    SELECT 1
    FROM @Q3
    WHERE Fecha < '2026-07-01'
       OR Fecha >= '2026-10-01'
       OR EfectivoNeto <> VentasTotales - TarjetaTransferencia - GastosCaja
       OR LEN(CONCAT(N'Importación CSV Q3 2026 | Registro diario consolidado | Tipo: ', TipoRegistro,
                     N' | Pago no efectivo (tarjeta/transferencia agrupadas): C$ ',
                     TarjetaTransferencia, N' | ', DetalleNotas, N' | Sin arqueo físico independiente')) > 250
       OR LEN(CONCAT(N'Importación CSV Q3 2026 | Registro diario consolidado | Tipo: ', TipoRegistro,
                     N' | Pago no efectivo (tarjeta/transferencia agrupadas): C$ ',
                     TarjetaTransferencia, N' | ', DetalleNotas, N' | IVA incluido al 15%')) > 250
       OR LEN(CONCAT(N'Importación CSV Q3 2026 | Registro diario consolidado | ', DetalleNotas)) > 250
       OR (GastosCaja > 0 AND LEN(CONCAT(N'Caja chica: ', DetalleNotas)) > 200)
)
    THROW 51003, 'Hay una fecha, cálculo o texto fuera de los límites del esquema.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'Seed_Contabilidad_Q3_2026',
        @LockMode = N'Exclusive',
        @LockOwner = N'Transaction',
        @LockTimeout = 10000;

    IF @LockResult < 0
        THROW 51004, 'No se pudo obtener el bloqueo de exclusión para el seed Q3.', 1;

    DECLARE @CostosFijos TABLE
    (
        Fecha date NOT NULL,
        Categoria nvarchar(50) NOT NULL,
        Descripcion nvarchar(200) NOT NULL,
        Monto decimal(18,2) NOT NULL,
        CuentaGasto nvarchar(20) NOT NULL,
        NumeroComprobante nvarchar(50) NOT NULL
    );

    INSERT INTO @CostosFijos
        (Fecha, Categoria, Descripcion, Monto, CuentaGasto, NumeroComprobante)
    VALUES
        ('2026-07-01', N'Otros', N'Insumos base semanales proyectados - julio 2026', 13890.64, N'6.1.99', N'CSV-Q3-FIX-202607-INS'),
        ('2026-07-01', N'Electricidad', N'Electricidad proyectada - julio 2026', 2600.00, N'6.1.02', N'CSV-Q3-FIX-202607-ELE'),
        ('2026-07-01', N'Agua', N'Agua proyectada - julio 2026', 500.00, N'6.1.03', N'CSV-Q3-FIX-202607-AGU'),
        ('2026-07-01', N'Salarios', N'Planilla proyectada - julio 2026', 18650.00, N'6.1.05', N'CSV-Q3-FIX-202607-PLA'),
        ('2026-08-01', N'Otros', N'Insumos base semanales proyectados - agosto 2026', 13890.64, N'6.1.99', N'CSV-Q3-FIX-202608-INS'),
        ('2026-08-01', N'Electricidad', N'Electricidad proyectada - agosto 2026', 2600.00, N'6.1.02', N'CSV-Q3-FIX-202608-ELE'),
        ('2026-08-01', N'Agua', N'Agua proyectada - agosto 2026', 500.00, N'6.1.03', N'CSV-Q3-FIX-202608-AGU'),
        ('2026-08-01', N'Salarios', N'Planilla proyectada - agosto 2026', 18650.00, N'6.1.05', N'CSV-Q3-FIX-202608-PLA'),
        ('2026-09-01', N'Otros', N'Insumos base semanales proyectados - septiembre 2026', 13890.64, N'6.1.99', N'CSV-Q3-FIX-202609-INS'),
        ('2026-09-01', N'Electricidad', N'Electricidad proyectada - septiembre 2026', 2600.00, N'6.1.02', N'CSV-Q3-FIX-202609-ELE'),
        ('2026-09-01', N'Agua', N'Agua proyectada - septiembre 2026', 500.00, N'6.1.03', N'CSV-Q3-FIX-202609-AGU'),
        ('2026-09-01', N'Salarios', N'Planilla proyectada - septiembre 2026', 18650.00, N'6.1.05', N'CSV-Q3-FIX-202609-PLA');

    IF EXISTS
    (
        SELECT 1
        FROM
        (
            SELECT Fecha, SUM(Monto) AS TotalMes
            FROM @CostosFijos
            GROUP BY Fecha
        ) AS costos
        WHERE costos.TotalMes <> 35640.64
    )
        THROW 51011, 'Los costos fijos proyectados no suman C$35,640.64 por mes.', 1;

    IF EXISTS
    (
        SELECT requeridas.Codigo
        FROM
        (
            SELECT N'1.1.01.01' AS Codigo
            UNION ALL SELECT N'1.1.02.01'
            UNION ALL SELECT N'2.1.02'
            UNION ALL SELECT N'2.1.03.01'
            UNION ALL SELECT N'4.1.01'
            UNION ALL SELECT N'6.1.02'
            UNION ALL SELECT N'6.1.03'
            UNION ALL SELECT N'6.1.05'
            UNION ALL SELECT N'6.1.99'
            UNION ALL SELECT DISTINCT CuentaGasto FROM @CostosFijos
        ) AS requeridas
        LEFT JOIN dbo.CuentasContables AS cuenta ON cuenta.Codigo = requeridas.Codigo
        WHERE cuenta.Id IS NULL OR cuenta.Activa = 0 OR cuenta.PermiteMovimiento = 0
    )
        THROW 51012, 'Faltan cuentas contables activas y de movimiento requeridas por el seed.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @CostosFijos AS costo
        INNER JOIN dbo.Gastos AS existente ON existente.NumeroComprobante = costo.NumeroComprobante
        WHERE existente.Monto <> costo.Monto
           OR CONVERT(date, existente.FechaGasto) <> costo.Fecha
           OR existente.Observaciones IS NULL
           OR existente.Observaciones NOT LIKE N'Proyección costos fijos Q3 2026 | Registro mensual consolidado%'
    )
        THROW 51013, 'Hay comprobantes de costos fijos proyectados con datos distintos; revise antes de importar.', 1;

    IF EXISTS
    (
        SELECT CONVERT(date, FechaApertura)
        FROM dbo.CajasSesiones WITH (UPDLOCK, HOLDLOCK)
        WHERE FechaApertura >= '2026-07-01'
          AND FechaApertura < '2026-10-01'
        GROUP BY CONVERT(date, FechaApertura)
        HAVING COUNT_BIG(*) > 1
    )
        THROW 51005, 'Ya existen fechas duplicadas en CajasSesiones durante Q3 2026; no se insertó nada.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Q3 AS q
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.CajasSesiones AS s
            WHERE s.FechaApertura >= CONVERT(datetime2, q.Fecha)
              AND s.FechaApertura < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
              AND
              (
                  s.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado | Tipo:%'
                  OR s.Observaciones LIKE N'Tipo: % | Pago no efectivo (tarjeta/transferencia agrupadas): C$ % | % | Sin arqueo físico independiente'
              )
        )
        AND EXISTS
        (
            SELECT 1
            FROM dbo.CajasSesiones AS s
            WHERE s.FechaApertura >= CONVERT(datetime2, q.Fecha)
              AND s.FechaApertura < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
        )
    )
        THROW 51006, 'Hay sesiones de caja no importadas en fechas del CSV; revise esos días antes de ejecutar el seed.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Q3 AS q
        WHERE EXISTS
        (
            SELECT 1
            FROM dbo.Ventas AS v
            WHERE v.Fecha >= CONVERT(datetime2, q.Fecha)
              AND v.Fecha < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
              AND (v.Observaciones IS NULL OR v.Observaciones NOT LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%')
        )
        AND NOT EXISTS
        (
            SELECT 1
            FROM dbo.Ventas AS v
            WHERE v.Fecha >= CONVERT(datetime2, q.Fecha)
              AND v.Fecha < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
              AND v.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%'
        )
    )
        THROW 51007, 'Hay ventas POS existentes en fechas del CSV; revise esos días antes de importar resúmenes diarios.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Q3 AS q
        WHERE q.GastosCaja > 0
          AND EXISTS
          (
              SELECT 1
              FROM dbo.Gastos AS g
              WHERE g.FechaGasto >= CONVERT(datetime2, q.Fecha)
                AND g.FechaGasto < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
                AND (g.Observaciones IS NULL OR g.Observaciones NOT LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%')
          )
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.Gastos AS g
              WHERE g.FechaGasto >= CONVERT(datetime2, q.Fecha)
                AND g.FechaGasto < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
                AND g.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%'
          )
    )
        THROW 51008, 'Hay gastos existentes en fechas del CSV; revise esos días antes de importar resúmenes diarios.', 1;

    IF EXISTS
    (
        SELECT CONVERT(date, v.Fecha)
        FROM dbo.Ventas AS v
        WHERE v.Fecha >= '2026-07-01'
          AND v.Fecha < '2026-10-01'
          AND v.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%'
        GROUP BY CONVERT(date, v.Fecha)
        HAVING COUNT_BIG(*) > 1
    )
        THROW 51009, 'Hay resúmenes diarios de ventas duplicados en Q3 2026.', 1;

    IF EXISTS
    (
        SELECT CONVERT(date, g.FechaGasto)
        FROM dbo.Gastos AS g
        WHERE g.FechaGasto >= '2026-07-01'
          AND g.FechaGasto < '2026-10-01'
          AND g.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%'
        GROUP BY CONVERT(date, g.FechaGasto)
        HAVING COUNT_BIG(*) > 1
    )
        THROW 51010, 'Hay resúmenes diarios de gastos duplicados en Q3 2026.', 1;

    DECLARE @Insertadas TABLE
    (
        Fecha date NOT NULL PRIMARY KEY,
        CajaSesionId int NOT NULL
    );

    INSERT INTO dbo.CajasSesiones
    (
        FechaApertura,
        FechaCierre,
        MontoInicial,
        TotalVentasEfectivo,
        TotalVentasTarjeta,
        TotalVentasTransferencia,
        TotalIngresos,
        TotalEgresos,
        EfectivoEsperado,
        EfectivoContado,
        Diferencia,
        Estado,
        UsuarioId,
        Observaciones
    )
    OUTPUT CONVERT(date, inserted.FechaApertura), inserted.Id
        INTO @Insertadas (Fecha, CajaSesionId)
    SELECT
        CONVERT(datetime2, q.Fecha),
        DATEADD(second, 86399, CONVERT(datetime2, q.Fecha)),
        0.00,
        q.VentasTotales - q.TarjetaTransferencia,
        0.00,
        q.TarjetaTransferencia,
        0.00,
        q.GastosCaja,
        q.EfectivoNeto,
        0.00,
        0.00,
        N'Cerrada',
        NULL,
        CONCAT(
            N'Importación CSV Q3 2026 | Registro diario consolidado | Tipo: ', q.TipoRegistro,
            N' | Pago no efectivo (tarjeta/transferencia agrupadas): C$ ', q.TarjetaTransferencia,
            N' | ', q.DetalleNotas,
            N' | Sin arqueo físico independiente'
        )
    FROM @Q3 AS q
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.CajasSesiones AS existente WITH (UPDLOCK, HOLDLOCK)
        WHERE existente.FechaApertura >= CONVERT(datetime2, q.Fecha)
          AND existente.FechaApertura < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
    );

    DECLARE @SesionesQ3 TABLE
    (
        Fecha date NOT NULL PRIMARY KEY,
        CajaSesionId int NOT NULL
    );

    INSERT INTO @SesionesQ3 (Fecha, CajaSesionId)
    SELECT q.Fecha, s.Id
    FROM @Q3 AS q
    INNER JOIN dbo.CajasSesiones AS s
        ON s.FechaApertura >= CONVERT(datetime2, q.Fecha)
       AND s.FechaApertura < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
    WHERE s.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado | Tipo:%'
       OR s.Observaciones LIKE N'Tipo: % | Pago no efectivo (tarjeta/transferencia agrupadas): C$ % | % | Sin arqueo físico independiente';

    INSERT INTO dbo.CajasMovimientos
        (CajaSesionId, Tipo, Monto, Descripcion, Fecha, UsuarioId)
    SELECT
        i.CajaSesionId,
        N'Egreso',
        q.GastosCaja,
        CONCAT(N'Caja chica: ', q.DetalleNotas),
        CONVERT(datetime2, q.Fecha),
        NULL
    FROM @Insertadas AS i
    INNER JOIN @Q3 AS q ON q.Fecha = i.Fecha
    WHERE q.GastosCaja > 0;

    DECLARE @VentasInsertadas TABLE
    (
        Fecha date NOT NULL PRIMARY KEY,
        VentaId int NOT NULL
    );

    INSERT INTO dbo.Ventas
    (
        NumeroComprobante,
        Fecha,
        Subtotal,
        Descuento,
        Impuesto,
        Total,
        MetodoPago,
        Estado,
        ClienteNombre,
        Observaciones,
        CajaSesionId,
        UsuarioId
    )
    OUTPUT CONVERT(date, inserted.Fecha), inserted.Id
        INTO @VentasInsertadas (Fecha, VentaId)
    SELECT
        CONCAT(N'CSV-Q3-2026-', CONVERT(char(8), q.Fecha, 112)),
        CONVERT(datetime2, q.Fecha),
        ROUND(q.VentasTotales / 1.15, 2),
        0.00,
        q.VentasTotales - ROUND(q.VentasTotales / 1.15, 2),
        q.VentasTotales,
        CASE
            WHEN q.TarjetaTransferencia = 0 THEN N'Efectivo'
            WHEN q.TarjetaTransferencia = q.VentasTotales THEN N'Tarjeta/Transf.'
            ELSE N'Mixto'
        END,
        N'Completada',
        N'Resumen diario CSV',
        CONCAT(
            N'Importación CSV Q3 2026 | Registro diario consolidado | Tipo: ', q.TipoRegistro,
            N' | Pago no efectivo (tarjeta/transferencia agrupadas): C$ ', q.TarjetaTransferencia,
            N' | ', q.DetalleNotas,
            N' | IVA incluido al 15%'
        ),
        s.CajaSesionId,
        NULL
    FROM @Q3 AS q
    LEFT JOIN @SesionesQ3 AS s ON s.Fecha = q.Fecha
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Ventas AS existente WITH (UPDLOCK, HOLDLOCK)
        WHERE existente.Fecha >= CONVERT(datetime2, q.Fecha)
          AND existente.Fecha < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
          AND existente.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%'
    );

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Ventas AS venta
        INNER JOIN @Q3 AS q
            ON venta.Fecha >= CONVERT(datetime2, q.Fecha)
           AND venta.Fecha < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
        WHERE venta.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%'
          AND venta.Total <> q.VentasTotales
    )
        THROW 51017, 'Un resumen de ventas importado tiene un total distinto al CSV; no se aplicó el IVA.', 1;

    UPDATE venta
    SET Subtotal = ROUND(q.VentasTotales / 1.15, 2),
        Impuesto = q.VentasTotales - ROUND(q.VentasTotales / 1.15, 2),
        ClienteNombre = N'Resumen diario CSV',
        Observaciones = CASE
            WHEN CHARINDEX(N'IVA incluido al 15%', venta.Observaciones) > 0 THEN venta.Observaciones
            WHEN CHARINDEX(N' | Sin desglose de IVA', venta.Observaciones) > 0
                THEN REPLACE(venta.Observaciones, N' | Sin desglose de IVA', N' | IVA incluido al 15%')
            ELSE CONCAT(venta.Observaciones, N' | IVA incluido al 15%')
        END
    FROM dbo.Ventas AS venta
    INNER JOIN @Q3 AS q
        ON venta.Fecha >= CONVERT(datetime2, q.Fecha)
       AND venta.Fecha < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
    WHERE venta.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%';

    INSERT INTO dbo.VentaPagos (VentaId, MetodoPago, Monto, Referencia)
    SELECT
        vi.VentaId,
        pago.MetodoPago,
        pago.Monto,
        N'Importación diaria CSV Q3 2026; resumen agregado'
    FROM @VentasInsertadas AS vi
    INNER JOIN @Q3 AS q ON q.Fecha = vi.Fecha
    CROSS APPLY
    (
        SELECT N'Efectivo' AS MetodoPago, q.VentasTotales - q.TarjetaTransferencia AS Monto
        WHERE q.VentasTotales - q.TarjetaTransferencia > 0
        UNION ALL
        SELECT N'Tarjeta/Transf.', q.TarjetaTransferencia
        WHERE q.TarjetaTransferencia > 0
    ) AS pago;

    INSERT INTO dbo.Gastos
    (
        CategoriaGasto,
        Descripcion,
        FechaGasto,
        FechaPago,
        Monto,
        ProveedorId,
        NumeroComprobante,
        MetodoPago,
        Estado,
        FechaVencimiento,
        Observaciones
    )
    SELECT
        N'Otros',
        N'Gastos de caja chica - resumen diario',
        CONVERT(datetime2, q.Fecha),
        CONVERT(datetime2, q.Fecha),
        q.GastosCaja,
        NULL,
        CONCAT(N'CSV-Q3-EXP-', CONVERT(char(8), q.Fecha, 112)),
        N'Efectivo',
        N'Pagado',
        NULL,
        CONCAT(N'Importación CSV Q3 2026 | Registro diario consolidado | ', q.DetalleNotas)
    FROM @Q3 AS q
    WHERE q.GastosCaja > 0
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.Gastos AS existente WITH (UPDLOCK, HOLDLOCK)
          WHERE existente.FechaGasto >= CONVERT(datetime2, q.Fecha)
            AND existente.FechaGasto < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
            AND existente.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%'
      );
    DECLARE @GastosInsertados int;
    SET @GastosInsertados = @@ROWCOUNT;

    INSERT INTO dbo.Gastos
    (
        CategoriaGasto,
        Descripcion,
        FechaGasto,
        FechaPago,
        Monto,
        ProveedorId,
        NumeroComprobante,
        MetodoPago,
        Estado,
        FechaVencimiento,
        Observaciones
    )
    SELECT
        costo.Categoria,
        costo.Descripcion,
        CONVERT(datetime2, costo.Fecha),
        NULL,
        costo.Monto,
        NULL,
        costo.NumeroComprobante,
        N'Pendiente',
        N'Pendiente',
        NULL,
        N'Proyección costos fijos Q3 2026 | Registro mensual consolidado | Dato proyectado; validar contra gastos diarios y comprobantes reales.'
    FROM @CostosFijos AS costo
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Gastos AS existente WITH (UPDLOCK, HOLDLOCK)
        WHERE existente.NumeroComprobante = costo.NumeroComprobante
    );
    DECLARE @CostosFijosInsertados int;
    SET @CostosFijosInsertados = @@ROWCOUNT;

    DECLARE @VentasQ3 TABLE
    (
        Fecha date NOT NULL,
        VentaId int NOT NULL,
        VentasTotales decimal(18,2) NOT NULL,
        TarjetaTransferencia decimal(18,2) NOT NULL,
        PRIMARY KEY (Fecha, VentaId)
    );

    INSERT INTO @VentasQ3 (Fecha, VentaId, VentasTotales, TarjetaTransferencia)
    SELECT q.Fecha, v.Id, q.VentasTotales, q.TarjetaTransferencia
    FROM @Q3 AS q
    INNER JOIN dbo.Ventas AS v
        ON v.Fecha >= CONVERT(datetime2, q.Fecha)
       AND v.Fecha < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
       AND v.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%';

    IF EXISTS
    (
        SELECT 1
        FROM @VentasQ3 AS importada
        INNER JOIN dbo.Ventas AS venta ON venta.Id = importada.VentaId
        WHERE venta.Total <> importada.VentasTotales
           OR venta.Subtotal <> ROUND(importada.VentasTotales / 1.15, 2)
           OR venta.Impuesto <> importada.VentasTotales - ROUND(importada.VentasTotales / 1.15, 2)
    )
        THROW 51014, 'Un resumen de ventas importado no coincide con los datos del CSV; no se generaron asientos.', 1;

    DECLARE @GastosQ3 TABLE
    (
        Fecha date NOT NULL,
        GastoId int NOT NULL,
        Categoria nvarchar(50) NOT NULL,
        Monto decimal(18,2) NOT NULL,
        CuentaGasto nvarchar(20) NOT NULL,
        CuentaPago nvarchar(20) NOT NULL,
        PRIMARY KEY (Fecha, GastoId)
    );

    INSERT INTO @GastosQ3 (Fecha, GastoId, Categoria, Monto, CuentaGasto, CuentaPago)
    SELECT q.Fecha, g.Id, N'Otros', q.GastosCaja, N'6.1.99', N'1.1.01.01'
    FROM @Q3 AS q
    INNER JOIN dbo.Gastos AS g
        ON g.FechaGasto >= CONVERT(datetime2, q.Fecha)
       AND g.FechaGasto < DATEADD(day, 1, CONVERT(datetime2, q.Fecha))
       AND g.Observaciones LIKE N'Importación CSV Q3 2026 | Registro diario consolidado%'
    WHERE q.GastosCaja > 0;

    IF EXISTS
    (
        SELECT 1
        FROM @GastosQ3 AS importado
        INNER JOIN dbo.Gastos AS gasto ON gasto.Id = importado.GastoId
        WHERE gasto.Monto <> importado.Monto
    )
        THROW 51015, 'Un resumen de gastos diario importado no coincide con el CSV; no se generaron asientos.', 1;

    INSERT INTO @GastosQ3 (Fecha, GastoId, Categoria, Monto, CuentaGasto, CuentaPago)
    SELECT costo.Fecha, gasto.Id, costo.Categoria, costo.Monto, costo.CuentaGasto, N'2.1.02'
    FROM @CostosFijos AS costo
    INNER JOIN dbo.Gastos AS gasto ON gasto.NumeroComprobante = costo.NumeroComprobante;

    DECLARE @DiarioQ3 TABLE
    (
        Fecha date NOT NULL,
        TipoOrigen nvarchar(40) NOT NULL,
        DocumentoOrigenId int NOT NULL,
        Concepto nvarchar(300) NOT NULL,
        CodigoCuenta nvarchar(20) NOT NULL,
        Debe decimal(18,2) NOT NULL,
        Haber decimal(18,2) NOT NULL,
        Descripcion nvarchar(200) NOT NULL
    );

    INSERT INTO @DiarioQ3
        (Fecha, TipoOrigen, DocumentoOrigenId, Concepto, CodigoCuenta, Debe, Haber, Descripcion)
    SELECT venta.Fecha, N'Venta', venta.VentaId, N'Venta diaria importada Q3 2026',
           N'1.1.01.01', venta.VentasTotales - venta.TarjetaTransferencia, 0,
           N'Cobros en efectivo según CSV Q3 2026'
    FROM @VentasQ3 AS venta
    WHERE venta.VentasTotales - venta.TarjetaTransferencia > 0
    UNION ALL
    SELECT venta.Fecha, N'Venta', venta.VentaId, N'Venta diaria importada Q3 2026',
           N'1.1.02.01', venta.TarjetaTransferencia, 0,
           N'Cobro combinado tarjeta/transferencia según CSV Q3 2026'
    FROM @VentasQ3 AS venta
    WHERE venta.TarjetaTransferencia > 0
    UNION ALL
    SELECT venta.Fecha, N'Venta', venta.VentaId, N'Venta diaria importada Q3 2026',
           N'4.1.01', 0, ROUND(venta.VentasTotales / 1.15, 2),
           N'Ingreso neto; IVA del 15% incluido en el importe bruto'
    FROM @VentasQ3 AS venta;

    INSERT INTO @DiarioQ3
        (Fecha, TipoOrigen, DocumentoOrigenId, Concepto, CodigoCuenta, Debe, Haber, Descripcion)
    SELECT venta.Fecha, N'Venta', venta.VentaId, N'Venta diaria importada Q3 2026',
           N'2.1.03.01', 0, venta.VentasTotales - ROUND(venta.VentasTotales / 1.15, 2),
           N'IVA débito fiscal incluido al 15% en Ventas_Totales'
    FROM @VentasQ3 AS venta;

    INSERT INTO @DiarioQ3
        (Fecha, TipoOrigen, DocumentoOrigenId, Concepto, CodigoCuenta, Debe, Haber, Descripcion)
    SELECT gasto.Fecha, N'Gasto', gasto.GastoId,
           CASE WHEN gasto.CuentaPago = N'2.1.02'
                THEN N'Costo fijo mensual proyectado Q3 2026'
                ELSE N'Gasto diario importado Q3 2026' END,
           gasto.CuentaGasto, gasto.Monto, 0,
           CONCAT(N'Gasto ', gasto.Categoria, N' Q3 2026; monto reportado/proyectado')
    FROM @GastosQ3 AS gasto
    UNION ALL
    SELECT gasto.Fecha, N'Gasto', gasto.GastoId,
           CASE WHEN gasto.CuentaPago = N'2.1.02'
                THEN N'Costo fijo mensual proyectado Q3 2026'
                ELSE N'Gasto diario importado Q3 2026' END,
           gasto.CuentaPago, 0, gasto.Monto,
           CASE WHEN gasto.CuentaPago = N'2.1.02'
                THEN N'Obligación pendiente; pago no informado'
                ELSE N'Egreso de caja reportado en CSV Q3 2026' END
    FROM @GastosQ3 AS gasto;

    IF EXISTS
    (
        SELECT 1
        FROM @DiarioQ3
        GROUP BY TipoOrigen, DocumentoOrigenId
        HAVING SUM(Debe) <> SUM(Haber)
    )
        THROW 51016, 'El diario Q3 no cumple partida doble; se canceló la importación.', 1;

    DECLARE @AsientosCreados TABLE
    (
        AsientoId int NOT NULL PRIMARY KEY,
        TipoOrigen nvarchar(40) NOT NULL,
        DocumentoOrigenId int NOT NULL
    );
    DECLARE @AnioAsiento int = YEAR(GETDATE());
    DECLARE @SecuenciaBase int =
    (
        SELECT ISNULL(MAX(TRY_CONVERT(int, SUBSTRING(NumeroAsiento, 9, 20))), 0)
        FROM dbo.AsientosContables
        WHERE NumeroAsiento LIKE CONCAT(N'AS-', YEAR(GETDATE()), N'-%')
    );

    ;WITH AsientosPendientes AS
    (
        SELECT Fecha, TipoOrigen, DocumentoOrigenId, Concepto
        FROM @DiarioQ3
        GROUP BY Fecha, TipoOrigen, DocumentoOrigenId, Concepto
    ),
    AsientosNuevos AS
    (
        SELECT pendiente.*,
               ROW_NUMBER() OVER (ORDER BY Fecha, TipoOrigen, DocumentoOrigenId) AS Secuencia
        FROM AsientosPendientes AS pendiente
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.AsientosContables AS existente WITH (UPDLOCK, HOLDLOCK)
            WHERE existente.TipoOrigen = pendiente.TipoOrigen
              AND existente.DocumentoOrigenId = pendiente.DocumentoOrigenId
        )
    )
    INSERT INTO dbo.AsientosContables
    (
        NumeroAsiento,
        Fecha,
        Concepto,
        TipoOrigen,
        DocumentoOrigenId,
        Contabilizado,
        Anulado,
        FechaCreacion,
        CreadoPor
    )
    OUTPUT inserted.Id, inserted.TipoOrigen, inserted.DocumentoOrigenId
        INTO @AsientosCreados (AsientoId, TipoOrigen, DocumentoOrigenId)
    SELECT
        CONCAT(N'AS-', @AnioAsiento, N'-',
               RIGHT(REPLICATE(N'0', 5) + CONVERT(nvarchar(20), @SecuenciaBase + Secuencia), 5)),
        Fecha,
        Concepto,
        TipoOrigen,
        DocumentoOrigenId,
        1,
        0,
        SYSDATETIME(),
        N'Seed Q3 2026'
    FROM AsientosNuevos;

    INSERT INTO dbo.AsientoDetalles
        (AsientoContableId, CuentaContableId, Debe, Haber, Descripcion)
    SELECT asiento.AsientoId, cuenta.Id, diario.Debe, diario.Haber, diario.Descripcion
    FROM @DiarioQ3 AS diario
    INNER JOIN @AsientosCreados AS asiento
        ON asiento.TipoOrigen = diario.TipoOrigen
       AND asiento.DocumentoOrigenId = diario.DocumentoOrigenId
    INNER JOIN dbo.CuentasContables AS cuenta
        ON cuenta.Codigo = diario.CodigoCuenta
       AND cuenta.Activa = 1
       AND cuenta.PermiteMovimiento = 1;

    DECLARE @AsientosInsertados int = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT
        (SELECT COUNT(*) FROM @Insertadas) AS DiasInsertados,
        (SELECT COUNT(*) FROM @VentasInsertadas) AS VentasDiariasInsertadas,
        @GastosInsertados AS GastosDiariosInsertados,
        (SELECT COUNT(*) FROM @Insertadas AS i INNER JOIN @Q3 AS q ON q.Fecha = i.Fecha WHERE q.GastosCaja > 0) AS MovimientosCajaChicaInsertados,
        @CostosFijosInsertados AS CostosFijosProyectadosInsertados,
        @AsientosInsertados AS ApuntesContablesInsertados;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
