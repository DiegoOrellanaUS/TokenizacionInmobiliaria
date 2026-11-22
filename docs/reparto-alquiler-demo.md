# Cierre mensual y reparto de alquiler en la demo

El recorrido usa Comercio para guardar el cierre y las asignaciones en PostgreSQL y Tokenización para firmar y confirmar los pagos. Las operaciones están disponibles en el entorno local `Development`. Las rentas se contabilizan en bolivianos y los pagos usan XLM de **Stellar Testnet**. La conversión en Bs por XLM se registra para cada cierre como dato simulado.

## Recorrido

1. Inicia Administración, Inmuebles, Comercio, Tokenización, Gateway y el frontend con la configuración local. Comercio y Tokenización deben compartir `Comercio__ClaveLiquidacionDemo`. Tokenización necesita Stellar CLI y acceso a Testnet. [Configuración local](flujo-inmuebles.md)
2. Con una cuenta titular, abre **Cierres de alquiler** y selecciona un inmueble con oferta publicada y emisión confirmada. Elige un mes terminado. Introduce el alquiler cobrado, las bases tributarias, los gastos con sus referencias, la reserva y las boletas que quieras registrar para la demo.
3. Con una cuenta revisora, abre **Repartos de alquiler**. El detalle muestra el cálculo y los respaldos. Selecciona **Aprobar reparto** y luego **Enviar pagos Testnet**. Si una confirmación se demora, vuelve a usar el mismo botón. Comercio reutiliza el sobre firmado que guardó hasta obtener un resultado definitivo.
4. Con la cuenta compradora, abre **Mi portafolio → Mis rentas**. Allí aparecen los importes aprobados, la referencia del reparto y el enlace a Stellar cuando se confirma el pago.

## Contabilidad de la demo

El ejemplo usa un vehículo que tributa como empresa en el régimen general. La cuenta titular introduce las bases declarables y sus respaldos. Los impuestos pagados se muestran con su boleta y fecha como información de conciliación. El efectivo neto descuenta cada obligación o provisión una vez.

| Concepto | Tratamiento |
| --- | --- |
| IVA | Calcula el débito al 13 % sobre la base informada y descuenta el crédito fiscal admisible de compras respaldadas. Muestra el saldo a pagar del período. [Guía del SIN sobre alquiler](https://www.impuestos.gob.bo/wp-content/uploads/2025/10/TEXTO-ALQUILER.pdf) |
| IT | Calcula el 3 % sobre la base informada. Permite aplicar IUE pagado como crédito hasta el IT determinado. [Formulario 400 del SIN](https://siatanexo.impuestos.gob.bo/index.php/declaraciones-juradas-en-formato-electronico/impuesto-a-las-transacciones/400-impuesto-a-las-transacciones) |
| IUE | Calcula una provisión de caja del 25 % de la utilidad fiscal estimada informada para el período. La liquidación de IUE corresponde a la gestión fiscal. [Ley 843 y compendio del SIN](https://www.impuestos.gob.bo/index.php/compendio-tributario-actualizado/) |
| IPBI | Prorratea en doce meses el importe anual informado para el inmueble. El monto base debe salir del gobierno municipal que corresponda. [Calculadora tributaria de RUAT](https://portalconsolida.ruat.gob.bo/inmuebles/pizarratributaria/InicioDetalleCalculo.jsf) |

El cierre registra gastos y pagos tributarios en la demo. La presentación de declaraciones, los pagos al SIN o al municipio y las retenciones individuales requieren el proceso fiscal que corresponda al contribuyente. [Obligaciones y formularios del SIN](https://www.impuestos.gob.bo/index.php/formularios/)

**Corte.** El reparto usa las entregas confirmadas que Umbral registra al guardar el cierre. Guarda ese momento y las cantidades de cada comprador. Para titulares que transfieran activos fuera de Umbral hace falta una instantánea verificable del ledger. El importe se calcula sobre todas las unidades emitidas, con redondeo hacia abajo a centavos. La parte correspondiente a unidades sin entregar y el redondeo quedan en «Sin asignar y redondeo».

**Ejemplo.** Con 100 unidades emitidas y Bs 2.000 cobrados, se descuentan Bs 200 de mantenimiento, Bs 230 de IVA, Bs 50 de IT, Bs 100 de provisión de IUE, Bs 100 de provisión de IPBI y Bs 120 de reserva. Quedan Bs 1.200. Un comprador con 10 unidades recibe Bs 120 en la liquidación, equivalentes a 1,2 XLM de prueba si se registran Bs 100 por XLM.
