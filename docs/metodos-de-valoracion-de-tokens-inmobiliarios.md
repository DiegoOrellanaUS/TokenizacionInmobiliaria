# Tokenización inmobiliaria en Stellar: valoración, distribuciones y flujo del inversionista

Edición corregida para Bolivia (Santa Cruz y Cochabamba)

| | |
|---|---|
| Estado | Borrador para revisión del equipo |
| Versión | 2.2, 2026-11-14 |
| Reemplaza a | "Real Estate Token Valuation Methods", la exportación de un chat de Gemini de 18 páginas |
| Stack supuesto | Stellar (activos clásicos, Soroban opcional), SDK de Pollar 0.12.1, USDC |
| Aviso legal | Es una referencia de diseño técnico, no asesoría legal ni tributaria. Los puntos marcados **Asesoría legal** deben confirmarse con abogados bolivianos de mercado de valores y tributarios antes del lanzamiento. |
| Idioma | Español. Versión en inglés: [real-estate-token-valuation-methods.md](real-estate-token-valuation-methods.md) |

## Cómo leer este documento

- Las secciones 1 a 8 son el diseño. El Anexo A reúne las fórmulas, el Anexo B contiene un contrato de referencia probado, el Anexo C enumera cada corrección al original y el Anexo D lista las fuentes.
- Todos los ejemplos usan un mismo edificio y un mismo conjunto de supuestos (sección 0.3), así que las cifras coinciden entre secciones. Son ilustraciones, no datos de mercado.
- Las afirmaciones legales y tributarias citan el texto que se leyó. Cuando solo hubo una fuente secundaria, el texto lo dice.
- Los números usan punto decimal y coma para los miles, como la tabla de cotizaciones del BCB y el código.
- El diseño no depende del stack. El backend puede vivir en los servicios .NET del repositorio. Solo el contrato opcional del Anexo B necesita Rust.
- El código del Anexo B se reproduce exactamente como se probó, por eso sus comentarios siguen en inglés.

## 0. Resumen

La plataforma vende fracciones de inmuebles como tokens: edificios terminados, proyectos en construcción y terrenos todavía sin construir, uno por uno o como un portafolio de varios. Valora cada token a partir de un NAV con avalúo independiente, cobra los alquileres en bolivianos cuando el inmueble los genera, paga a los titulares el flujo neto distribuible en USDC y usa un asistente de inversión con IA que convierte el presupuesto de un comprador en opciones de compra. El ejemplo de referencia es un edificio. La sección 1.6 trata los terrenos y la sección 1.7, los portafolios de varios inmuebles.

El texto original acertó al fijar el precio del token a partir del NAV, al usar aritmética entera y al mantener los cálculos fuera del modelo de lenguaje. También tenía problemas de fondo. Su contrato podía crear tokens. Su diseño en Stellar mezclaba modelos de token que no pueden funcionar juntos. Faltaban las normas bolivianas de valores y de fintech. Sus ejemplos usaban un tipo de cambio fijo y rendimientos que su propia cascada de distribución no podía producir. Esta edición corrige esos puntos y registra cada cambio en el Anexo C.

### 0.1 Arquitectura en una vista

```text
COMPRA
  Billetera del inversionista (Pollar)
    1. KYC y aprobación. La trustline de la billetera existe, pero queda sin autorizar
    2. BOB por QR -> orden de rampa -> USDC en la billetera (varios tramos, minutos)
    3. Una transacción de Stellar armada por la plataforma: autorizar la trustline,
       USDC inversionista -> ESCROW, tokens DISTRIBUIDORA -> inversionista, quitar la autorización

ALQUILERES
  Inquilinos --BOB--> cuenta bancaria del vehículo
    cierre mensual: impuestos, costos, reservas -> monto neto en BOB
    conversión a un tipo de cambio registrado -> USDC -> cuenta de DISTRIBUCIÓN
    instantánea de saldos en la fecha de corte -> pagos de USDC en lotes -> titulares

CUENTAS (ninguna cumple dos funciones)
  Emisora (multifirma, llave de servicio para los flags de trustline) | Distribuidora (tokens no vendidos)
  Escrow (fondos de ventas, tokens reservados)
  Distribución (USDC de alquileres) | Patrocinadora (comisiones de red)
```

### 0.2 Decisiones

| # | Decisión | Valor por defecto en esta edición | Estado |
|---|---|---|---|
| D1 | Estructura legal y vía de oferta pública | Sin elegir. El diseño no depende de la estructura (sección 5) | **Asesoría legal**, decidir primero |
| D2 | Modelo de token | Activo clásico de Stellar con flags del emisor, "Modelo A" (sección 2.2) | Recomendado para v1 |
| D3 | Control de transferencias | `AUTH_REQUIRED`, `AUTH_REVOCABLE`, clawback. Los titulares quedan sin autorizar entre transacciones de la plataforma, así que los tokens solo se mueven a través de ella (sección 2.3) | Recomendado |
| D4 | Distribución de alquileres | Instantánea en la fecha de corte y pagos de USDC en lotes (sección 6.1) | Recomendado para v1 |
| D5 | Liquidez secundaria | Sin mercado abierto. Ventanas de rescate con tope, si la ley lo permite (sección 5.4) | **Asesoría legal** |
| D6 | Valoración | Avalúo independiente, NAV publicado con el hash del informe, cambio acotado (sección 1.3) | Recomendado |
| D7 | Moneda | USDC on-chain. BOB convertido a un tipo de cambio registrado en cada periodo (sección 4.5) | Por confirmar |
| D8 | Pollar | Billeteras, trustlines patrocinadas, flujo de KYC, rampas. La ruta de BOB está por confirmar (sección 3) | Por confirmar con Pollar |
| D9 | Asistente con IA | Modelo de lenguaje para la conversación, herramientas deterministas para cada número (sección 7) | Recomendado |
| D10 | Unidad de oferta | Un token por inmueble. Un token de portafolio solo mediante un fondo regulado o un vehículo creado para eso (sección 1.7) | Recomendado para v1 |
| D11 | Terrenos sin construcción | Reserva de costos de tenencia financiada en el lanzamiento, sin distribuciones hasta una venta o un alquiler, y primero lotes urbanos con título limpio (sección 1.6) | **Asesoría legal** para terrenos rurales |

### 0.3 Ejemplo de referencia y supuestos

Un solo edificio sostiene todos los ejemplos. Si cambia un supuesto, cambia cada cifra que depende de él.

| ID | Supuesto |
|---|---|
| A1 | **SC-1**: diez departamentos en Santa Cruz, con alquileres corporativos de largo plazo, comprado a su avalúo de $1,000,000. |
| A2 | Capital de $1,050,000 = precio de $1,000,000 + $40,000 de costos de cierre y estructuración (gastados) + $10,000 de reserva inicial de efectivo, financiado con la venta de 105,000 tokens a $10.00. Hasta que se vendan todos, quien financió la compra tiene el resto. No incluye el impuesto a la transferencia (sección 1.2). |
| A3 | Alquiler bruto facturado: 70,000 BOB al mes, con impuestos incluidos. El IVA de 13% y el IT de 3% se calculan sobre ese monto. Provisión de IPBI: 1,800 BOB. Expensas de unidades vacías y reparaciones: 3,500 BOB. Comisión de administración: 10% del alquiler bruto. Reserva de CapEx y vacancia: 5% del alquiler bruto. Todos los costos se facturan con IVA. |
| A4 | Provisión de IUE: 8,000 BOB al mes. Verificación: con el terreno en 30% del precio y la edificación depreciada a 2.5% anual, la utilidad imponible estimada es 370,155 BOB al año, así que el IUE es 92,539 BOB al año, o 7,712 BOB al mes (sección 4.2). |
| A5 | Tipo de cambio de 11.67 BOB por USD, el oficial del BCB del 14 de noviembre de 2026, constante en las proyecciones. |
| A6 | **CB-1**: un edificio terminado en Cochabamba a $25.00 por token, con rendimiento bruto de 4.5% sobre el precio del token y la misma relación neto/bruto que SC-1. **SC-2**: una preventa a $8.00 por token, con un valor proyectado a la entrega de +18% y sin alquileres durante la construcción. |
| A7 | **LT-1**: un lote urbano de 2,000 m² sin construcción, comprado a su avalúo de $300,000 ($150 por m²). Costos de cierre y estructuración de $12,000 (4%, como en SC-1). Costos de tenencia de $6,600 al año: IPBI $600, cerramiento, vigilancia y limpieza $1,800, comisión de administración de 1% del avalúo $3,000, contabilidad y auditoría $1,200. Se financia en el lanzamiento una reserva para 3 años ($19,800), y el plan es vender a los 3 años. Los costos de venta son el 6% del precio (3% de impuesto a la transferencia, 3% de corretaje). Cifras ilustrativas, no datos de mercado. |

### 0.4 Términos

| Término | Significado |
|---|---|
| BCB, ASFI, UIF, SIN | Banco Central de Bolivia, Autoridad de Supervisión del Sistema Financiero, Unidad de Investigaciones Financieras, Servicio de Impuestos Nacionales |
| IVA 13%, IT 3% | Impuesto al Valor Agregado e Impuesto a las Transacciones, ambos sobre el alquiler facturado |
| IUE 25%, IUE-BE 12.5% | Impuesto sobre las Utilidades de las Empresas, y el mismo impuesto retenido en pagos a beneficiarios del exterior |
| RC-IVA 13% | Régimen Complementario al IVA, para personas naturales |
| IPBI, IMT | Impuesto anual a la Propiedad de Bienes Inmuebles, e Impuesto Municipal a las Transferencias (3%) |
| NIT, factura, SIAT | Número de Identificación Tributaria, factura fiscal, y el sistema de facturación y declaraciones del SIN |
| RMV, EDV, agencia de bolsa | Registro del Mercado de Valores, Entidad de Depósito de Valores, intermediario de valores autorizado |
| ETF, PSAV | Empresa de Tecnología Financiera, y Proveedor de Servicios de Activos Virtuales (ambos definidos en el DS 5384) |
| SAFI, FIC | Sociedad Administradora de Fondos de Inversión, y Fondo de Inversión Cerrado |
| SEGIP, RUAT | Servicio General de Identificación Personal, y la plataforma que usan muchos municipios para cobrar sus impuestos |
| Derechos Reales, folio real | El registro de la propiedad, y la inscripción de un inmueble en él |
| INRA, saneamiento, título ejecutorial | Instituto Nacional de Reforma Agraria, su procedimiento para regularizar la propiedad rural, y el título final que emite |
| FES | Función económico social: el uso productivo que la propiedad rural mediana y empresarial debe mostrar para conservar su título |
| Expensas, anticrético | Cuotas de gastos comunes del edificio, y un contrato en el que el inquilino entrega una suma global en lugar de pagar alquiler |
| Patrimonio autónomo | Un conjunto de bienes separado jurídicamente, que usan los fideicomisos, las titularizaciones y los fondos de inversión |
| UFV | Unidad de Fomento de Vivienda, unidad de cuenta indexada a la inflación que publica el BCB |
| NAV, NOI | Valor neto de los activos (net asset value), ingreso operativo neto (net operating income) |
| SPV, "el vehículo" | La entidad o el patrimonio dueño del edificio |
| Trustline | Línea de confianza: el registro de Stellar que permite a una cuenta tener un activo emitido |
| SAC, SEP | Stellar Asset Contract, Stellar Ecosystem Proposal |

## 1. Precio del token y NAV

### 1.1 Qué representa un token

Un token es una participación fija en los derechos económicos del vehículo dueño de un inmueble (un edificio, un proyecto en construcción o un terreno) o de un portafolio de inmuebles (sección 1.7). La participación de un titular es igual a sus tokens divididos entre los tokens en circulación (sección 1.3). La oferta se fija al emitir, así que nunca hace falta guardar porcentajes.

| Tokens enteros emitidos | Unidades base (10^7 por token) | Un token equivale a |
|---|---|---|
| 10,000 | 10^11 | 0.01% |
| 100,000 | 10^12 | 0.001% |
| 105,000 (ejemplo de referencia) | 1,050,000,000,000 | 0.000952% |

Elija una oferta que dé un precio unitario cómodo ($10 a $100). Los clientes igual pueden tener fracciones de hasta siete decimales.

Evite atar los tokens a metros cuadrados. Una regla de 100 tokens por metro cuadrado supone que cada metro cuadrado vale lo mismo en todos los pisos, vistas y usos. Lo que importa es el porcentaje del patrimonio, así que use la superficie solo como descripción.

Lo que el token es legalmente depende de la estructura legal (sección 5). No lo describa como propiedad del inmueble salvo que la estructura otorgue ese derecho.

### 1.2 Precio primario

```math
P_0 = \frac{\text{precio de compra} + \text{costos de cierre y estructuración} + \text{reserva inicial de efectivo} - \text{deuda}}{N}
```

Ejemplo de referencia (A1, A2): P0 = (1,000,000 + 40,000 + 10,000 - 0) / 105,000 = **$10.00**.

Los inversionistas pagan $10.00, pero el NAV del día 1 es (1,000,000 + 10,000 - 0) / 105,000 = **$9.6190**. La diferencia de $0.3810 (3.81% del precio) es un costo de entrada. Muéstrela antes de la compra y en el primer informe de NAV.

**Impuesto a la transferencia.** El Impuesto Municipal a las Transferencias (IMT, 3%) lo debe la persona a cuyo nombre está registrado el inmueble, es decir, el vendedor ([DS 24054](https://www.lexivox.org/norms/BO-DS-24054.xhtml), art. 5). La primera venta de una construcción nueva paga en cambio el IT nacional (art. 2). Quién asume el costo en la práctica es una cláusula del contrato de compra. Súmelo a los costos de arriba solo si el contrato lo traslada al vehículo. La base es el mayor entre el precio pagado y el valor usado para el IPBI (art. 6).

### 1.3 NAV por token

```math
NAV_t = \frac{1}{\text{tokens en circulación}}
\left(
\begin{aligned}
& \text{valor de avalúo} \\
& + \text{efectivo y reservas} \\
& + \text{cuentas por cobrar, netas de previsión por incobrables} \\
& - \text{deuda e intereses devengados} \\
& - \text{depósitos en garantía de inquilinos} \\
& - \text{capital recibido en anticrético} \\
& - \text{impuestos por pagar y devengados (IVA, IT, IUE, IPBI)} \\
& - \text{distribuciones declaradas y aún no pagadas} \\
& - \text{comisiones devengadas}
\end{aligned}
\right)
```

- **Denominador.** "Tokens en circulación" son todos los tokens que son un derecho sobre el vehículo: cada token emitido, salvo los que tenga el propio vehículo. Los tokens de las cuentas distribuidora y escrow cuentan, porque quien financió la compra pagó esa parte del edificio. El original dividía solo entre los tokens "circulantes", lo que los deja fuera y sobrestima el NAV por token. Si el propio vehículo guarda tokens sin vender, trátelos como acciones en tesorería: exclúyalos y registre como pasivo el dinero que financió esa parte del edificio (por ejemplo, un préstamo puente).
- **Avalúo.** Use un tasador independiente. Para vivienda en Bolivia, el método de ventas comparables debe ser el principal y el enfoque de ingresos debe servir de control. Haga un nuevo avalúo al menos una vez al año y después de un hecho importante (CapEx grande, vacancia larga, daños).
- **Gobierno.** El tasador debe ser independiente del administrador del activo. Publique el SHA-256 del informe firmado y regístrelo con cada actualización del NAV. Limite el cambio por actualización (20% por defecto). Un cambio mayor requiere una decisión aparte y registrada. Use una multifirma para la llave del NAV.
- **Presentación.** Muestre la fecha del NAV y la del avalúo junto a cada precio.
- **Vista de salida (opcional).** Para planificar rescates, tome el NAV_t y reste los costos de venta, el impuesto a la transferencia que debe el vendedor y el IUE sobre la ganancia (25% del valor de venta menos la base tributaria). El vendedor es el vehículo, así que asume esos costos.

### 1.4 El enfoque de ingresos como control

```math
\text{valor implícito} = \frac{\text{NOI}}{\text{tasa de capitalización}}
```

El NOI es el alquiler neto de IVA, menos el IT, la vacancia y los incobrables, el IPBI, los seguros, las reparaciones, la administración, y las expensas y los servicios que paga el propietario. Se mide antes del IUE, del servicio de la deuda, de la depreciación y de la reserva de CapEx.

Hay pocos datos públicos confiables de tasas de capitalización para vivienda en Bolivia. Los rendimientos brutos de Numbeo, aportados por usuarios (actualizados al 10 de noviembre de 2026), sirven como guía aproximada: [Santa Cruz](https://www.numbeo.com/property-investment/in/Santa-Cruz-Bolivia) 6.34% en el centro y 10.20% fuera de él, [Cochabamba](https://www.numbeo.com/property-investment/in/Cochabamba) 4.24% y 3.71%. Las muestras son pequeñas (44 y 38 registros). El rendimiento bruto de SC-1, 7.20% sobre su avalúo, cae dentro del rango de Santa Cruz.

### 1.5 Precio después del lanzamiento

Las ventas primarias se hacen a P0, o al último NAV más una comisión informada. Cualquier mercado de negociación fijaría precios por encima o por debajo del NAV según la demanda. No prometa rescates al NAV salvo que exista un mecanismo de rescate con fondos (sección 5.4).

### 1.6 Terrenos sin construcción

Un terreno no genera alquiler, así que su token solo rinde por el cambio de valor y por lo que pase al final: una venta, un alquiler o un proyecto de construcción.

**Valoración**

- Use ventas comparables de terrenos con precio por m², ajustadas por ubicación, superficie, frente, uso de suelo permitido y servicios (agua, alcantarillado, electricidad). El valor que usa el municipio para el IPBI no es un valor de mercado.
- Para un terreno donde se podría construir, contraste con el método residual:

```math
\begin{aligned}
\text{valor del terreno} = {} & \text{valor del proyecto terminado} \\
& - \text{costos de construcción} - \text{honorarios, permisos e impuestos} \\
& - \text{costos financieros} - \text{utilidad del desarrollador}
\end{aligned}
```

- El enfoque de ingresos (sección 1.4) solo se aplica si el terreno se alquila, por ejemplo como estacionamiento o para uso agrícola.
- El terreno no se deprecia (sección 4.3), y sin ingresos los costos de tenencia generan pérdidas tributarias. Una pérdida solo puede compensarse con utilidades de los tres años siguientes (Ley 843, art. 48, modificado por la Ley 169, según una [resolución del SIN](https://boliviaimpuestos.com/resultados-acumulados-negativos-contabilidad/) reproducida por boliviaimpuestos.com). Si la venta se atrasa más allá del año 4, vencen las pérdidas del primer año. Algunos emprendimientos productivos nuevos tienen cinco años (**Asesoría legal**).

**Costos de tenencia y la reserva**

Sin alquiler, el IPBI, el cerramiento, la vigilancia, la limpieza, el seguro y la comisión de administración deben pagarse con efectivo. Financie en el lanzamiento una reserva para el periodo de tenencia previsto, más un margen, e inclúyala en el precio. No cuente con pedir más dinero a los titulares después: cobrar un aporte adicional a miles de titulares de tokens es difícil y puede contar como una nueva oferta (**Asesoría legal**).

**Ejemplo resuelto (LT-1, supuesto A7)**

| Concepto | Valor |
|---|---|
| Capital | $331,800 = precio de $300,000 + $12,000 de costos de cierre + $19,800 de reserva, es decir, 33,180 tokens a $10.00 |
| NAV del día 1 | ($300,000 + $19,800) / 33,180 = $9.6383, un costo de entrada de $0.3617 (3.62%) |
| NAV si el valor del terreno no cambia | $9.4394 después del año 1, $9.2405 después del año 2 y $9.0416 después del año 3, a medida que se gasta la reserva |
| Precio de venta de equilibrio | $352,978.72, un 17.66% más que el precio de compra, o 5.57% anual durante 3 años |

| Precio de venta a los 3 años | Por token para los titulares | Resultado total | Por año |
|---|---:|---:|---:|
| -10% ($270,000) | $7.6492 | -23.51% | -8.55% |
| Sin cambio ($300,000) | $8.4991 | -15.01% | -5.28% |
| +10% ($330,000) | $9.3490 | -6.51% | -2.22% |
| Equilibrio ($352,978.72) | $10.0000 | 0.00% | 0.00% |
| +30% ($390,000) | $10.7866 | +7.87% | +2.56% |

Los montos por token se calculan después del 6% de costos de venta y del 25% de IUE sobre la ganancia, suponiendo que los tres años de costos de tenencia se compensan con la ganancia. Solo el caso de +30% paga IUE ($8,700). Con estos supuestos el terreno debe ganar cerca de 18% en tres años antes de que los titulares recuperen su dinero, así que el asistente muestra esta tabla en lugar de un rendimiento (sección 7.4).

**Verificaciones de título y legales**

- **Lotes urbanos.** Un título inscrito en Derechos Reales (folio real) a nombre del vendedor, el registro catastral municipal, ninguna sobreposición con los predios vecinos y un plano de urbanización aprobado si el lote es parte de una urbanización. Si la construcción es parte del plan, revise el uso de suelo permitido y las normas de edificación. La Ley 477 de 2013 tipifica como delitos el avasallamiento y el tráfico de tierras ([Lexivox](https://www.lexivox.org/norms/BO-L-N477.xhtml)), pero un terreno vacío igual necesita cerramiento y vigilancia, que están en los costos de tenencia.
- **Terrenos rurales ("campos").** Trátelos como un producto aparte que requiere asesoría legal antes de cualquier oferta. Los números de artículo de abajo siguen la Constitución de 2009 tal como se promulgó ([copia de la OEA](http://www.oas.org/juridico/PDFs/mesicic4_blv_const.pdf)).
  - El título debe salir del saneamiento del INRA, el procedimiento que regulariza la propiedad agraria ([INRA](https://www.inra.gob.bo/saneamiento-de-la-propiedad-agraria/)). La oficina del INRA en Cochabamba recomendó a los compradores pedir el título ejecutorial del vendedor, un plano y un certificado catastral, y el folio real en Derechos Reales ([Los Tiempos](https://www.lostiempos.com/actualidad/cochabamba/20220306/instan-verificar-6-requisitos-evitar-estafas-avasalladores)).
  - La propiedad agraria mediana y la empresarial deben cumplir la FES, y no cumplirla es causal de reversión al Estado (arts. 397 y 401).
  - La pequeña propiedad es indivisible (art. 394), y se prohíbe dividir propiedades en superficies menores a la máxima de la pequeña propiedad (art. 400).
  - Se prohíbe obtener renta de la tierra por su uso especulativo (art. 395).
  - Las personas jurídicas pueden ser dueñas de tierra siempre que la usen para su objeto, para generar empleo y para producir bienes o servicios (art. 315). Un vehículo que mantiene un terreno ocioso solo para venderlo después queda expuesto bajo los arts. 315 y 395. La asesoría legal también debe confirmar si el art. 315 alcanza a los lotes urbanos.
  - Dentro de los 50 km de una frontera, un extranjero no puede adquirir propiedad, solo ni en sociedad (art. 262).
  - Estas reglas hacen que el terreno rural encaje mal en un token pasivo. Empiece con lotes urbanos.

**Salida.** Al final el vehículo vende, alquila o construye. Una venta reparte el producto neto y cierra el token (sección 6.3). Un alquiler convierte el terreno en un inmueble con renta según la sección 4. Un proyecto de construcción necesita dinero nuevo, así que la oferta debe decir antes del lanzamiento quién decide, cómo se financiaría la obra (tokens nuevos al NAV, deuda o un socio desarrollador) y qué pasa con los titulares que no participen.

### 1.7 Varios inmuebles

Hay dos formas de ofrecer más de un inmueble.

| | Un token por inmueble (por defecto) | Un token de portafolio |
|---|---|---|
| Qué tiene el titular | Una participación en un vehículo dueño de un inmueble | Una participación en un vehículo dueño de varios inmuebles |
| NAV | Sección 1.3, por inmueble | Todos los avalúos, el efectivo y las reservas, menos todos los pasivos, divididos entre los tokens en circulación del portafolio |
| Distribuciones | El monto neto propio de cada inmueble (sección 4) | Un solo monto: el alquiler neto de los inmuebles con renta menos los costos de tenencia de los terrenos |
| Diversificación | La arma el inversionista comprando varios tokens (opción B de la sección 7.4) | Viene incluida |
| Separación de riesgos | Un problema en un inmueble queda ahí | Un problema en un inmueble alcanza a todos los titulares |
| Encaje legal | Cada vehículo y cada oferta son independientes | Un fondo de inversión cerrado (FIC) es la estructura natural (sección 5.2) |
| En Stellar | Un activo por inmueble, por ejemplo `SC1`, `CB1` y `LT1` | Un activo para el portafolio |

Un código de activo tiene de 1 a 12 caracteres y, junto con la cuenta emisora, identifica al activo ([recetario de la CLI de Stellar](https://developers.stellar.org/docs/tools/cli/cookbook/asset-management)). Use una cuenta emisora por vehículo legal, para que los flags y el registro sigan al dueño legal.

**Agregar un inmueble a un portafolio.** Emita los tokens nuevos al NAV vigente más los costos de la nueva adquisición, para no diluir a los titulares actuales:

```math
\begin{aligned}
& \text{tokens nuevos} && N_{\text{nuevo}} = \frac{V_{\text{nuevo}}}{NAV_t} \\[0.5em]
& \text{precio de emisión} && P_{\text{nuevo}} = NAV_t \times \frac{V_{\text{nuevo}} + C_{\text{nuevo}}}{V_{\text{nuevo}}}
\end{aligned}
```

Ejemplo: el NAV_t es $9.80 y un inmueble nuevo cuesta $500,000 más $20,000 de costos de adquisición. Se emiten 51,020.4082 tokens a $10.1920 para captar $520,000, y el NAV por token sigue en $9.80. Emitir 53,061.2245 tokens al NAV de $9.80, sin contar los costos, bajaría el NAV de todos los titulares a $9.6735, una dilución de 1.29%. Haga un avalúo de todo el portafolio antes de cada emisión, porque el NAV_t debe estar al día.

Mezclar terrenos e inmuebles con renta en un mismo portafolio baja el pago de todo el token. Informe la composición y la parte del NAV de cada inmueble.

## 2. El token en Stellar

### 2.1 Precisión y oferta

Los activos de Stellar usan siete decimales. Una unidad base (un "stroop") es 10^-7 de un token, así que una oferta de N tokens enteros son N x 10^7 unidades base. Guarde y calcule solo con enteros. Soroban no tiene tipos de punto flotante, así que cualquier cálculo en un contrato usa punto fijo en `i128`. Redondee los pagos hacia abajo y arrastre el residuo al siguiente periodo (sección 6.1).

Fije un monto mínimo de compra (por ejemplo, un token) y permita fracciones de hasta siete decimales por encima de él.

### 2.2 Elegir un solo modelo de token

El original mezclaba tres modelos: un activo clásico con su Stellar Asset Contract (página 2), negociación en el DEX de Stellar (página 4) y un contrato que guardaba los saldos en su propio almacenamiento (páginas 9 a 15). No pueden funcionar juntos. Un saldo guardado solo en el almacenamiento privado de un contrato no es un token SEP-41, así que las billeteras no lo muestran y nada más puede negociarlo. Un activo clásico negociado en el DEX nunca llama al contrato, así que el punto de control de alquileres del contrato nunca se ejecuta y el alquiler acumulado le llega al titular equivocado.

La [comparación de activos](https://developers.stellar.org/docs/tokens/anatomy-of-an-asset) de Stellar describe las tres opciones que usa esta edición:

| | **A. Activo clásico** (con su SAC) | **B. Token de contrato SEP-57** (T-REX, ERC-3643) | **C. Token SEP-41 propio** con índice de alquileres (Anexo B) |
|---|---|---|---|
| Dónde están los saldos | Trustlines | Almacenamiento del contrato | Almacenamiento del contrato |
| Billeteras y DEX de Stellar | Soporte completo en billeteras. El DEX funciona con operaciones clásicas, que este diseño bloquea (sección 2.3) | Algunas billeteras muestran saldos. Las transferencias y las funciones de control todavía no tienen soporte en billeteras. Sin DEX | Las billeteras pueden mostrar el saldo mediante SEP-41. Sin DEX |
| Controles de cumplimiento | Flags del emisor y el patrón de transacción de SEP-8 | Identidad y reglas de transferencia on-chain, por ejemplo la [suite RWA de OpenZeppelin](https://docs.openzeppelin.com/stellar-contracts/tokens/rwa/rwa) | Una lista de autorizados que usted escribe |
| Distribución de alquileres | Instantánea off-chain más pagos (sección 6.1) | Un módulo que usted agrega. Los indexadores deben ser propios | Incorporada (índice on-chain) |
| Carga de auditoría | Sin contrato propio | Biblioteca auditada, pero sus reglas y módulos no lo están | Todo |
| Encaje con Pollar | Las trustlines patrocinadas tienen soporte | Llamadas a contratos mediante el firmante SEP-43 | Igual que B |

**Recomendación.** Empiece con el Modelo A. En cualquier estructura que cumpla la ley, el registro legal está fuera de la cadena, así que las distribuciones pueden seguir la práctica habitual de los valores: una fecha de corte y luego el pago. Pase a B o C solo cuando necesite reglas de transferencia on-chain o acumulación totalmente on-chain, y presupueste una auditoría antes de hacerlo.

### 2.3 Controles de cumplimiento del Modelo A

| Flag o función | Qué permite |
|---|---|
| `AUTH_REQUIRED` | Las trustlines nuevas empiezan sin autorizar. Nadie puede recibir el token hasta que el emisor autoriza la trustline |
| `AUTH_REVOCABLE` | El emisor puede quitar una autorización en cualquier momento. Lo necesitan el patrón de abajo y el congelamiento de un titular |
| `AUTH_CLAWBACK_ENABLED` | La plataforma puede recuperar o quemar tokens por órdenes judiciales, herencias o llaves perdidas. Requiere `AUTH_REVOCABLE`. El flag es visible en el ledger público, así que debe informarse ([guía de clawback de Stellar](https://developers.stellar.org/docs/build/guides/transactions/clawbacks)) |

Active los flags antes de que exista la primera trustline y no active `AUTH_IMMUTABLE`.

**Mantener las transferencias dentro de la plataforma.** Un titular cuya trustline sigue autorizada puede enviar tokens a cualquier otro titular autorizado, o publicar una oferta en el DEX de Stellar, sin pasar por la plataforma. Eso abriría el mercado que la sección 5.4 descarta para v1. Por eso cada transacción que arma la plataforma sigue el patrón de [SEP-8, activos regulados](https://github.com/stellar/stellar-protocol/blob/master/ecosystem/sep-0008.md): la primera operación autoriza la trustline del titular, luego vienen los pagos y la última operación quita la autorización. La transacción es atómica, así que el titular nunca queda autorizado fuera de ella.

- Los tokens siguen en la cuenta del titular y se ven en la billetera.
- Los pagos en USDC siguen llegando, porque usan otra trustline.
- El clawback sigue funcionando. La guía de clawback primero quita la autorización y después recupera los tokens.
- Un servidor de aprobación SEP-8 público solo hace falta si los titulares pueden iniciar sus propias transferencias, algo que v1 no permite. El propio SEP-8 desaconseja los activos regulados salvo que sean necesarios, por la carga operativa.

Verifique que el flujo de billetera de Pollar puede firmar una transacción armada por la plataforma que incluya estas operaciones del emisor (sección 3.5).

### 2.4 Cuentas y llaves

| Cuenta | Qué tiene | Firma |
|---|---|---|
| Emisora | Nada. Es dueña del activo y de sus flags | Multifirma fría para las operaciones de umbral alto. Un firmante operativo aparte, limitado por umbrales, para autorizar trustlines en el día a día |
| Distribuidora | Tokens no vendidos | Llave de servicio de la plataforma, con límites de gasto |
| Escrow | Fondos de ventas en USDC y tokens reservados para planes de cuotas | Llave de servicio de la plataforma, conciliada a diario |
| Distribución | USDC del pago de alquileres en curso | Llave de servicio de la plataforma, con fondos solo al momento de pagar |
| Patrocinadora | XLM para comisiones de red y reservas | Llave de servicio de la plataforma |

La [lista de operaciones](https://developers.stellar.org/docs/learn/fundamentals/transactions/list-of-operations) de Stellar fija los umbrales que definen el diseño de firmantes. Autorizar o revocar una trustline (`SetTrustLineFlags`, `AllowTrust`) es una operación de umbral bajo. El clawback y el cambio de flags de la cuenta son de umbral medio. Cambiar firmantes, umbrales o el peso de la llave maestra es de umbral alto. Ejemplo para la cuenta emisora: tres llaves frías y una llave de servicio de la plataforma, cada una con peso 1, umbrales bajo 1, medio 2 y alto 3, y la llave maestra con peso 0. Así la llave de servicio puede autorizar y desautorizar trustlines sola, algo que necesita cada transacción de la plataforma. El clawback necesita la llave de servicio más una llave fría, o dos llaves frías. Cambiar firmantes necesita tres llaves.

La trustline de cada titular inmoviliza una reserva de 0.5 XLM, que Pollar puede patrocinar.

## 3. Alta y compra del inversionista (Pollar)

### 3.1 Quién hace qué

Esta tabla refleja el [repositorio del SDK de Pollar](https://github.com/pollar-xyz/pollar) (`@pollar/core` 0.12.1, Apache-2.0).

| Capacidad | Lo da Pollar | Lo mantiene la plataforma |
|---|---|---|
| Inicio de sesión y billeteras | Inicio de sesión con email, Google, GitHub y billeteras externas. Billeteras administradas por Pollar | La decisión de exigir o no billeteras administradas |
| Comisiones y trustlines | Comisiones patrocinadas por la app. Trustlines patrocinadas | Las operaciones del emisor dentro de cada transacción de la plataforma (sección 2.3). El presupuesto de patrocinio |
| KYC | Flujos de KYC alojados y pasos propios de cada rampa, incluida una consulta al registro del SEGIP para Bolivia | El expediente propio de KYC y AML, la calificación de riesgo, la decisión de aprobación y los reportes a la UIF |
| Rampas | `getRampRoutes`, `quoteRamp`, `createRampOrder`, `pollRampOrder`. Las órdenes tienen varios tramos y las cotizaciones públicas duran 15 minutos | Vincular órdenes con compras, idempotencia, devoluciones |
| Firma | Un firmante SEP-43 para entradas de autorización de Soroban | Armar la transacción de compra y firmarla también |

El KYC de Pollar no reemplaza el expediente propio de KYC y AML del emisor.

### 3.2 Lo que el original dijo mal sobre Pollar

- **No hay función de tipo de cambio.** `get_pollar_exchange_rate()` no existe en el SDK. Use como referencia el tipo oficial que publica el BCB, y `quoteRamp()` para el tipo ejecutado.
- **No se encontró un token atado al BOB** en el SDK ni en sus documentos de diseño.
- **Hay que confirmar la ruta del BOB.** El README menciona la rampa de BOB de Bolivia (proveedor Stereum, con el puente CCTP de Circle) en su sección de Solana. El ADR de rampas de Pollar (`docs/adr/0001-ramp-api-and-lifecycle.md`) dice que un depósito por Stereum debe terminar como USDC entregado en Stellar, pero aclara que sus ejemplos son ilustrativos y que las rutas reales requieren soporte verificado. Confirme por escrito, y revise `getRampRoutes()` para su app y su red, que está activa una ruta de BOB que liquida en USDC en Stellar.
- **Activos sustitutos.** Un tramo de Stereum puede pagar en USDT, y un tramo posterior lo convierte. Considere la orden completa solo cuando haya llegado el USDC.

### 3.3 Flujo de compra

1. **Registro.** El inversionista inicia sesión con Pollar y recibe una billetera.
2. **KYC.** El inversionista completa el flujo de Pollar. La plataforma completa su propio expediente.
3. **Aprobación.** La billetera del inversionista crea una trustline hacia el activo del inmueble (Pollar la patrocina). La trustline queda sin autorizar (sección 2.3). La plataforma registra al inversionista como aprobado.
4. **Cotización y reserva.** El asistente cotiza tokens al precio vigente y reserva ese inventario mientras dure la cotización (hasta los 15 minutos de la cotización pública de Pollar).
5. **Pago.** El inversionista paga en BOB por QR. La orden de rampa lo convierte en USDC en su billetera. Esto toma varios tramos y algunos minutos. También puede pagar directamente en USDC.
6. **Liquidación.** Una transacción de Stellar armada por la plataforma: autorizar la trustline del inversionista, pagar USDC del inversionista al **escrow**, pagar tokens de la **distribuidora** al inversionista y quitar la autorización. La firman el inversionista, la distribuidora y la llave de servicio del emisor. Solo este último paso puede ser atómico. La rampa anterior no lo es.
7. **Conciliación.** Guarde el id de la orden, los tipos de cambio, el id de la orden de rampa y los hashes de las transacciones. Transfiera los fondos del escrow a su dueño (el vehículo, o quien financió la compra) según un calendario acordado con la asesoría legal.
8. **Confirmación.** Envíe el comprobante y muestre el costo de entrada (sección 1.2).

| Evento | Manejo |
|---|---|
| La cotización vence antes del pago | Liberar la reserva y pedir una nueva cotización |
| Un tramo de la rampa falla o se devuelve | No se liberan tokens. Conciliar con el estado de la orden en Pollar. El inversionista puede reintentar |
| La rampa paga USDT en lugar de USDC | Esperar el tramo de conversión. Verificar el activo final antes de armar la transacción de liquidación |
| El pago del inversionista no coincide con la cotización | La transacción de liquidación fija ambos montos, así que falla. Devolver o completar según los términos de la orden |
| El inversionista no está aprobado, o se retiró su aprobación | La plataforma no arma la transacción de liquidación. Devolver el USDC |
| La trustline no existe | El pago de tokens falla y falla toda la transacción. No queda ningún estado parcial |
| La reserva venció y los tokens se vendieron a otra persona | Devolver el USDC |

### 3.4 Separar los fondos de ventas de los alquileres

El original enviaba el USDC de las compras a la "Property Distribution Vault". Eso mezcla el capital de los inversionistas con los alquileres, y una distribución podría devolver capital como si fuera renta. Use cuentas separadas: los fondos de compras van al **escrow**, y a la cuenta de **distribución** solo va el alquiler neto.

### 3.5 Preguntas para resolver con Pollar

1. ¿Está habilitada para nuestra app una ruta de depósito en BOB que liquide como USDC en Stellar? ¿Cuáles son los límites, las comisiones y los tiempos de liquidación?
2. ¿Puede la app presentar una transacción armada por ella, con operaciones del inversionista, de la distribuidora y del emisor, para que la billetera del usuario la firme?
3. ¿Cuál es el modelo de custodia de las billeteras de Stellar administradas por Pollar, y qué entidad legal actúa como custodio?
4. ¿La billetera maneja un activo cuya trustline queda sin autorizar en reposo (`AUTH_REQUIRED`, `AUTH_REVOCABLE`), y podría trabajar más adelante con un servidor de aprobación SEP-8?
5. ¿Qué garantías de estado de las órdenes, webhooks e idempotencia ofrece la API de rampas?
6. ¿Cuál es la situación regulatoria de Pollar y de Stereum en Bolivia (ASFI, UIF)?
7. ¿Qué evidencia de KYC debe conservar la plataforma y cómo se comparte?

## 4. Ingresos por alquiler y cascada de distribución (Bolivia)

### 4.1 La fórmula de distribución

Esta sección se aplica a los inmuebles que generan alquiler. Los terrenos y los proyectos en construcción no tienen alquiler que repartir: sus costos salen de una reserva (sección 1.6) y los titulares cobran cuando se vende el inmueble (sección 6.3).

Distribuya el flujo neto distribuible, nunca el alquiler bruto. Cada mes la contabilidad se procesa fuera de la cadena, en bolivianos, y produce el monto neto. Luego la plataforma lo convierte a USDC y paga a los titulares (sección 6).

```math
\begin{aligned}
D_p = {} & \text{alquiler bruto cobrado} \\
& - \text{IVA por pagar} && \text{(13\% del alquiler facturado, menos el crédito fiscal de los costos facturados)} \\
& - \text{IT} && \text{(3\% del alquiler facturado, hasta que lo cubra el crédito del IUE)} \\
& - \text{provisión de IPBI} && \text{(impuesto anual / 12)} \\
& - \text{costos operativos} && \text{(expensas de unidades vacías, reparaciones, servicios, seguros)} \\
& - \text{reserva de CapEx y vacancia} \\
& - \text{comisión de administración del activo} \\
& - \text{provisión de IUE} && \text{(a nivel del vehículo)} \\[1em]
\text{pago}_i = {} & \left\lfloor \frac{\text{saldo}_i \times D_p}{\text{tokens en circulación}} \right\rfloor - \text{retención}_i
\end{aligned}
```

- La retención es por titular y se aplica después del reparto. El original restaba una "reserva de retención" del monto común, con lo que los titulares residentes pagan parte del impuesto de los titulares del exterior. El IUE-BE (12.5%) se aplica solo a pagos a beneficiarios del exterior.
- Los depósitos en garantía de los inquilinos y el capital del anticrético son pasivos. Nunca entran en D_p.
- Las reservas quedan en el vehículo como efectivo. Aparecen en el NAV hasta que se gastan y no son renta de los titulares.
- El efectivo distribuible no es lo mismo que la utilidad distribuible (última fila de la sección 4.3).

### 4.2 Ejemplo resuelto (SC-1)

Supuestos A1 a A5. Montos en BOB por mes, con impuestos incluidos. "Año 1" significa antes de haber pagado algún IUE. "Régimen estable" significa que el IUE pagado por el año anterior alcanza para cubrir todo el IT.

| Línea | Año 1 | Régimen estable |
|---|---:|---:|
| Alquiler bruto facturado | 70,000 | 70,000 |
| IVA sobre el alquiler (13%) | -9,100 | -9,100 |
| Crédito fiscal de costos facturados (13% de 7,000 + 3,500) | +1,365 | +1,365 |
| IT (3%) | -2,100 | 0 (pagado con el crédito del IUE) |
| Provisión de IPBI (1/12) | -1,800 | -1,800 |
| Expensas de unidades vacías y reparaciones | -3,500 | -3,500 |
| Reserva de CapEx y vacancia (5%) | -3,500 | -3,500 |
| Comisión de administración (10%) | -7,000 | -7,000 |
| Provisión de IUE, a nivel del vehículo | -8,000 | -8,000 |
| **Monto neto distribuible** | **36,365** | **38,465** |
| En USDC a 11.67 | 3,116.11 | 3,296.06 |
| Proporción del alquiler facturado | 51.95% | 54.95% |
| Por token por mes (105,000 tokens) | 0.3463 BOB (0.0297 USD) | 0.3663 BOB (0.0314 USD) |
| Rendimiento neto en efectivo sobre el precio de $10.00 | 3.56% | 3.77% |

Un titular de 2.5% (2,625 tokens) recibe 909.13 BOB al mes en el año 1 y 961.63 BOB en régimen estable, antes de cualquier impuesto a nivel del titular. Si se le retiene 12.5% por ser beneficiario del exterior, los montos son 795.48 y 841.42 BOB.

**Verificación del supuesto de IUE (A4).** El ingreso neto de IVA es 730,800 BOB al año. Los costos deducibles suman 156,420 BOB (IT 25,200, IPBI 21,600, reparaciones 36,540 y comisión de administración 73,080, estas dos netas de IVA). La depreciación es 204,225 BOB: el precio es 11,670,000 BOB a 11.67, la edificación es el 70% (8,169,000 BOB) y se deprecia a 2.5% anual. La utilidad imponible es 370,155 BOB, así que el IUE al 25% es 92,539 BOB al año, o 7,712 BOB al mes. Los 8,000 BOB usados están cerca. El IUE pagado en un año (92,539) es mayor que un año de IT (25,200), y por eso el IT baja a cero en régimen estable.

**Cómo leer el resultado.** Los titulares reciben cerca de la mitad del alquiler facturado. Los impuestos y la reserva explican casi todo el resto. El original citaba rendimientos netos de 9.2% y 6.8% sin ningún cálculo detrás. Su propia cascada repartía el 50% del alquiler bruto, así que esos rendimientos habrían exigido rendimientos brutos de 18.4% y 13.6%. Cite rendimientos solo a partir de este cálculo, nunca a partir de una meta.

### 4.3 Reglas de impuestos y costos

| Tema | Regla | Base y confianza |
|---|---|---|
| Impuestos sobre el alquiler | IVA 13% (formulario 200), IT 3% (formulario 400), IUE 25% sobre la utilidad del vehículo (formulario 500). Un propietario persona natural suele declarar RC-IVA 13% (formulario 610) | [Guía de YoFacturo](https://yo-facturo.com/blog/impuestos-alquiler-inmuebles-bolivia/). Fuente secundaria |
| Crédito fiscal IVA | Cada costo necesita una factura emitida al NIT del vehículo. El 13% de esas facturas compensa el 13% de IVA del alquiler. El original lo recomendaba en las páginas 6 y 8 a 9, pero su ejemplo de la página 5 suponía crédito cero | Normas del SIN. Confirmar con el contador |
| Proveedores sin factura | Si una persona natural no inscrita en el SIN vende bienes o presta servicios en forma eventual, el vehículo debe retener y pagar. En servicios, 13% de RC-IVA (formulario 604) más 3% de IT (formulario 410), 16% en total. En bienes, 5% de IUE (formulario 570) más 3% de IT (formulario 410), 8% en total. Los formularios se presentan cada mes por el SIAT, según el último dígito del NIT, entre el 13 y el 22 | [Preguntas frecuentes del SIN sobre retenciones](https://www.impuestos.gob.bo/wp-content/uploads/2025/10/PREGUNTAS-RETENCIONES-25.pdf). Oficial |
| Depreciación | Edificaciones: 2.5% anual (40 años) sobre el valor de la construcción, sin el terreno. Reduce la utilidad imponible, no el efectivo | [Anexo del DS 24051](https://bolivia.infoleyes.com/articulo/7033). Reproducción secundaria |
| Crédito del IUE contra el IT | Ley 843, art. 77: el IUE pagado por la gestión se considera pago a cuenta del IT de cada periodo mensual, desde el mes siguiente a su pago. Con cierre en diciembre, el IUE anual vence unos 120 días después del cierre, así que el crédito empieza en el segundo año. El DS 5327 (febrero de 2025) exigía pagar el IUE hasta su vencimiento para usar el crédito. El DS 5563 (marzo de 2026) eliminó esa condición. El IUE pagado no compensa el IT de las transferencias de bienes (DS 21532, art. 7). Más depreciación implica menos IUE y un crédito menor | [boliviaimpuestos.com](https://boliviaimpuestos.com/decreto-supremo-nuevo-marco-normativo-para-la-compensacion-iue-it/). Fuente secundaria para el DS 5563. **Asesoría legal** debe confirmar la regla vigente |
| IPBI | Impuesto municipal anual a la propiedad, que se paga al gobierno municipal donde está registrado el inmueble, a menudo por la plataforma del RUAT. Los inmuebles del Urubó pagan a Porongo, no a Santa Cruz de la Sierra (sección 4.4). Provisión de 1/12 por mes. Los descuentos por pronto pago varían según el municipio y el año: Cochabamba (Cercado) dio 15% hasta el 30 de junio de 2026 y 10% hasta el 28 de diciembre de 2026 por la gestión 2025. Un aviso municipal de Santa Cruz (sin fecha) anunciaba 15% hasta el 1 de abril | [Los Tiempos](https://www.lostiempos.com/actualidad/cochabamba/20260701/amplian-perdonazo-impuestos-municipales-cercado). Revisar cada municipio todos los años |
| Impuesto a la transferencia | Ver la sección 1.2. 3% del mayor entre el precio y el valor del IPBI. Lo debe el vendedor. Se paga dentro de los 10 días hábiles siguientes. Los notarios y el registro no tramitan la minuta sin el comprobante de pago. El IUE no es deducible contra él | [DS 24054](https://www.lexivox.org/norms/BO-DS-24054.xhtml), arts. 2, 5, 6, 8 y 12. Texto primario |
| IUE-BE (beneficiarios del exterior) | Retención de 12.5% en pagos al exterior, formulario 530. Las preguntas frecuentes del SIN confirman la tasa para servicios prestados desde el exterior a una empresa boliviana. Cómo se aplica a los pagos a titulares extranjeros depende de la forma legal del token | Supuesto de planificación de 12.5%. **Asesoría legal** |
| Reserva legal | S.A. y S.R.L.: al menos 5% de las utilidades efectivas y líquidas, hasta que la reserva alcance la mitad del capital pagado | [Código de Comercio, art. 169](https://bolivia.infoleyes.com/articulo/26270). Reproducción secundaria |
| Efectivo frente a utilidad | La depreciación reduce la utilidad, no el efectivo. Si el token es un derecho patrimonial (acciones o cuotas), los pagos suelen limitarse a la utilidad efectiva y líquida después de la reserva legal. El original trataba el efectivo protegido por la depreciación como libremente distribuible. Los pagos mensuales en efectivo encajan con un derecho contractual, o con anticipos que se concilian al cierre de la gestión | **Asesoría legal** |

### 4.4 Factores de cada ciudad

**Santa Cruz (Equipetrol, Sirari, Urubó).** El calor, la humedad y las lluvias fuertes encarecen el aire acondicionado, la impermeabilización, la prevención de moho, los ascensores y las piscinas. La demanda de corto plazo y corporativa es fuerte, con más rotación y más gasto de limpieza. CRE (electricidad) y SAGUAPAC (agua) son cooperativas que atienden a la ciudad ([CRE](https://correodelsur.com/sociedad/20250806/cre-un-orgullo-para-los-bolivianos.html), [SAGUAPAC](https://www.bnamericas.com/es/perfil-empresa/cooperativa-de-servicios-publicos-santa-cruz-ltda)). Confirme los proveedores de cada edificio. El original mencionaba "AXS" para la energía de las áreas comunes, y ninguna fuente ubica a AXS en Santa Cruz ni en Cochabamba. El Urubó está al otro lado del río Piraí, en el municipio de Porongo ([eju.tv](https://eju.tv/2024/10/la-hora-de-porongo-y-la-planificacion-urbana/)), así que sus impuestos a la propiedad y a la transferencia van a Porongo, no a Santa Cruz de la Sierra.

**Cochabamba (Zona Norte, Cala Cala, Recoleta, Tupuraya).** Un clima más templado reduce el desgaste de la climatización. Los costos de agua en época seca incluyen cisternas, mantenimiento de bombas y filtración. Predominan los alquileres residenciales y profesionales de largo plazo, con menos rotación. El original menciona a ELFEC (electricidad) y SEMAPA (agua) como proveedores.

**En ambas ciudades.** Las unidades vacías siguen generando expensas, y las paga el monto común. Mantenga los contratos de anticrético fuera de un edificio que reparte renta: no generan efectivo mensual y crean un pasivo grande cuando el inquilino se va. Registre esa suma global, y cada depósito de inquilino, como pasivo en el NAV.

**Control del alquiler.** Numbeo reporta un alquiler mensual promedio de Bs 10,700 para un departamento de tres dormitorios en el centro de Santa Cruz y de Bs 3,675 en Cochabamba. Los 7,000 BOB por departamento de SC-1 son razonables para Santa Cruz. Serían altos para Cochabamba.

### 4.5 Moneda y tipo de cambio

- **El régimen cambió.** El boliviano flota desde el 29 de junio de 2026. Ese día el BCB pasó de un tipo fijo de Bs 6.96 a un tipo oficial de Bs 9.73 ([Infobae](https://www.infobae.com/america/america-latina/2026/06/29/bolivia-flexibiliza-el-dolar-y-cierra-una-era-de-15-anos-de-tipo-de-cambio-fijo/)). Después fijó Bs 12.26 para el 24 de septiembre, anunciado la noche anterior ([Red Uno](https://www.reduno.com.bo/economia/nueva-escalada-oficial-el-bcb-eleva-el-tipo-de-cambio-del-dolar-para-este-jueves-2026923202122)), y [Bs 11.67 para el 14 de noviembre](https://www.bcb.gob.bo/librerias/indicadores/otras/ultimo.php). El mercado paralelo cotizaba entre Bs 11.73 y 11.93 el 7 de noviembre ([Red Uno](https://www.reduno.com.bo/economia/a-cuanto-esta-el-dolar-en-bolivia-este-sabado-7-de-noviembre-asi-cotizan-el-cambio-oficial-y-paralelo-202611773627)). Un sitio de seguimiento de cotizaciones calculó la brecha entre ambos en 1.1% el 10 de noviembre ([dolarbolivia.net](https://dolarbolivia.net/)).
- **Qué significa.** La preocupación del original por la brecha entre el tipo oficial y el paralelo ya no es el riesgo principal. Lo es la volatilidad. Un alquiler fijo en bolivianos compraba 17% menos dólares el 14 de noviembre que el 29 de junio, y 21% menos el 24 de septiembre.
- **Política.**
  1. Registre el tipo oficial del BCB y el tipo de conversión ejecutado en cada distribución.
  2. Convierta el monto neto en la fecha de distribución e indíquelo en los términos.
  3. Pague en USDC on-chain. Ofrezca BOB por transferencia bancaria o QR a los titulares que lo prefieran, al tipo registrado.
  4. Considere contratos de alquiler en USD o indexados a la UFV donde la ley lo permita (**Asesoría legal**).
  5. Las compras pagadas en cuotas mensuales en BOB se convierten al tipo de cada fecha de pago (sección 7.4).

### 4.6 Cierre mensual e informe de transparencia

1. Concilie los cobros de alquiler con las facturas emitidas por el SIAT.
2. Cierre el mes: crédito fiscal, impuestos, retenciones declaradas, provisión de IPBI, reservas.
3. Calcule D_p en BOB. El contador firma el informe.
4. Publique el informe (PDF y JSON) y calcule su SHA-256.
5. Convierta a USDC y registre ambos tipos de cambio.
6. Distribuya (sección 6) con el hash del informe en cada transacción de pago.

JSON de ejemplo (valores ilustrativos; los nombres de los campos quedan en inglés, como en el código):

```json
{
  "building_id": "SC-1",
  "period": "2026-11",
  "fx": { "bcb_official_bob_per_usd": "11.67", "executed_bob_per_usd": "11.67" },
  "gross_rent_bob": "70000.00",
  "lines_bob": {
    "iva_payable": "-7735.00",
    "it": "-2100.00",
    "ipbi_accrual": "-1800.00",
    "operating_costs": "-3500.00",
    "capex_vacancy_reserve": "-3500.00",
    "management_fee": "-7000.00",
    "iue_accrual": "-8000.00"
  },
  "net_pool_bob": "36365.00",
  "net_pool_usdc": "3116.11",
  "tokens_outstanding": "105000.0000000",
  "per_token_usdc": "0.0296772",
  "record_ledger": "<secuencia del ledger de la instantánea>",
  "report_sha256": "<sha-256 del PDF firmado del informe>"
}
```

## 5. Estructura legal y perímetro regulatorio

Esta sección es el vacío más grande del original. Las afirmaciones de abajo vienen de los textos legales que se leyeron ([Ley 1834](https://www.lexivox.org/norms/BO-L-1834.xhtml), [DS 5384](https://www.lexivox.org/norms/BO-DS-N5384.xhtml), [DS 24054](https://www.lexivox.org/norms/BO-DS-24054.xhtml)) y de las fuentes secundarias indicadas en cada fila. Lexivox reproduce los textos por comodidad y no es la Gaceta Oficial. Su página de la Ley 1834 muestra el texto de 1998, que menciona a la Superintendencia de Valores. Hoy esa función es de la ASFI (Ley 393, art. 16, citada en el DS 5384), y puede que no aparezcan modificaciones posteriores. Confirme todo con la asesoría legal.

### 5.1 El perímetro

| Norma | Qué dice | Qué significa aquí |
|---|---|---|
| **Constitución** (CPE), art. 331 | La intermediación financiera, los servicios financieros y cualquier otra actividad relacionada con el manejo, el uso y la inversión del ahorro son de interés público y requieren autorización previa del Estado (citado en los considerandos del DS 5384) | Aunque el token no fuera un valor, captar dinero del público para invertirlo en un edificio requiere autorización. **Asesoría legal** |
| **Constitución**, arts. 262, 315, 394, 395, 397, 400 y 401 (tierra) | Reglas sobre la propiedad agraria, la tierra de las personas jurídicas y la zona de seguridad fronteriza (sección 1.6) | El terreno rural requiere asesoría legal antes de cualquier oferta. Primero van los lotes urbanos. **Asesoría legal** |
| **Ley 1834**, Ley del Mercado de Valores, arts. 2, 3, 6 y 10 | Oferta pública es toda invitación o propuesta dirigida al público en general o a sectores específicos, por cualquier medio, para realizar un negocio jurídico con valores (art. 6). El regulador puede declarar sujeta a la ley una oferta dudosa (art. 6). Son valores, entre otros, los instrumentos creados según un reglamento específico que identifican al beneficiario de los fondos, tienen oferta pública autorizada y representan una obligación efectiva del emisor. Los valores son libremente transferibles, y es nula toda limitación a su circulación (art. 2). Los únicos intermediarios autorizados son las agencias de bolsa (art. 3). Una sociedad por acciones con 25 o más accionistas debe inscribir sus acciones en el RMV (art. 10) | Un token que paga a compradores minoristas, por una app, una parte del alquiler y del producto de una venta es muy probablemente una oferta de valores, o se trata como tal. Requiere autorización y registro, salvo que aplique una excepción. Un token con lista de autorizados choca con la regla contra los límites a la circulación. **Asesoría legal** |
| **DS 5384** (7 de mayo de 2025) | Define el "activo tokenizado" como la representación digital de un activo real o financiero en una blockchain mediante tokens. Un proveedor de servicios de activos virtuales (PSAV) incluye el intercambio, la transferencia, la custodia, y la participación o prestación de servicios financieros relacionados con la oferta de un emisor o la venta de un activo virtual (art. 3). Las empresas de tecnología financiera (ETF) deben constituirse como entidades financieras, con permiso de constitución y licencia de funcionamiento de la ASFI (art. 4). Sus categorías incluyen activos tokenizados y PSAV, y la ASFI puede exigir el ingreso a un entorno controlado de pruebas (art. 5). Las empresas existentes deben adecuarse (disposición transitoria segunda) | Vender y mantener activos tokenizados para clientes parece entrar en el régimen de las ETF. Planifique una licencia de la ASFI y pregunte si el entorno controlado de pruebas está disponible |
| Reglamentos de la ASFI | No se revisaron. Una nota de Dentons del 22 de mayo de 2026 reporta modificaciones al reglamento de fintech que aclaran los servicios con activos virtuales (solo el resumen; la página no se pudo leer) | La asesoría legal debe leer la Recopilación de Normas para Servicios Financieros (RNSF) vigente y la modificación de 2026 |
| **Ley 393**, Ley de Servicios Financieros, art. 486 | Prohíbe realizar intermediación financiera y servicios financieros complementarios sin autorización ([ASFI](https://asfi.gob.bo/pb/y-donde-denunciar-actividad-financiera-ilegal)) | El dinero que se cobra al público en cuotas o reservas, antes de entregar los tokens, es la parte expuesta. **Asesoría legal** |
| UIF | Un proveedor de tokenización reporta que la UIF exige registro a quienes ofrecen intercambio, transferencia o custodia de activos virtuales ([Metlabs](https://metlabs.io/en/tokenization/bolivia/)). Fuente secundaria | Planifique un programa de prevención de lavado de dinero (AML) y el registro ante la UIF. **Asesoría legal** |
| Código de Comercio | Una S.R.L. admite como máximo 25 socios y sus cuotas de capital no pueden representarse en títulos negociables (Metlabs). La reserva legal está en la sección 4.3 | La vía de la S.R.L. no puede llevar directamente un token para minoristas |

### 5.2 Opciones de estructura legal

| Opción | Qué es el token | Regulación | Encaje |
|---|---|---|---|
| Titularización | Un reflejo de un valor registrado que emite un patrimonio autónomo administrado por una sociedad de titularización autorizada | La Ley 1834 regula las sociedades de titularización y la titularización (art. 1). Metlabs cita los arts. 76 a 79 | La mayor separación patrimonial, pero la más lenta y la más cara |
| Sociedad anónima (S.A.) | Un reflejo del libro de registro de acciones | Con 25 o más accionistas debe inscribirse en el RMV (art. 10). Una oferta pública requiere autorización | Una forma societaria conocida |
| Fondo de inversión cerrado (FIC) administrado por una SAFI autorizada | Un reflejo de las cuotas de participación del fondo | Un fondo es un patrimonio autónomo separado de su administradora, constituido según la Ley 1834 ([BNB SAFI](https://www.bnb.com.bo/PortalSAFI/Principal/PreguntasFrecuentes)), en un mercado que supervisa la ASFI ([Fortaleza SAFI](https://www.fortalezasafi.com/productos/)). Un fondo cerrado no rescata cuotas salvo según su reglamento ([Banco Unión SAFI](https://bancounion.com.bo/GrupoUnion/SAFIProductos)) | Una estructura regulada que ya existe, con custodia y reportes. Si un fondo inmobiliario admite cuotas tokenizadas es una pregunta para la asesoría legal. El original no la consideró |
| S.R.L. con contratos de cuentas en participación | Un derecho contractual privado a una renta | Los límites de la S.R.L. ya indicados. Un derecho contractual ofrecido al público sigue siendo oferta pública | Solo para un grupo privado pequeño, no para minoristas |
| Fideicomiso con un banco como fiduciario | Derechos de beneficiario sobre el patrimonio | Requiere una entidad financiera regulada como fiduciario (dato del original, sin verificar) | Separación patrimonial, pero costoso. La misma pregunta de oferta pública |

**Recomendación.** No fije la estructura legal en el diseño técnico. Construya el sistema para que cada token apunte a una entrada de registro y cada distribución tenga líneas de pago por titular. Así la estructura puede cambiar sin rediseñar.

### 5.3 Qué tiene un titular

En cada estructura, el titular tiene un derecho contractual o un derecho sobre valores frente al emisor o al patrimonio. El vehículo es dueño del inmueble, no el inversionista. El marketing, la app y el asistente deben describir lo que recibe el titular (una parte del alquiler, una parte del producto de una venta, votos, derechos de información) exactamente como lo define la estructura. No escriba "usted es dueño del 2% del edificio".

### 5.4 Negociación secundaria y liquidez

- **Se eliminó del original** la idea de que la tesorería ponga "muros de liquidez" alrededor del NAV en el DEX de Stellar. Un emisor que sostiene su propio precio se arriesga a que se lo trate como manipulación de mercado o como un mercado sin licencia (**Asesoría legal**).
- Si los tokens son valores, la intermediación corresponde a las agencias de bolsa (Ley 1834, art. 3). Como el art. 2 anula los límites a la circulación, pregunte a la asesoría legal cómo restringir quién puede tener tokens. Restringir en la capa del intermediario y de la custodia puede ser legal donde una restricción en el token no lo es.
- **Política de v1:** sin mercado abierto. Los titulares quedan sin autorizar entre transacciones de la plataforma (sección 2.3), así que los tokens solo se mueven por los flujos de la plataforma: compras, ventanas de rescate si se permiten y transferencias forzosas. Si se permiten rescates, haga ventanas periódicas al último NAV, con fondos de una reserva y con un tope por ventana. Fondos inmobiliarios de otros países que prometían salidas fáciles suspendieron los rescates en momentos de estrés, así que incluya el tope desde el primer día.

### 5.5 Preguntas para la asesoría legal

1. ¿Es el token un valor según la Ley 1834, y qué vía conviene para un solo edificio: titularización, S.A. inscrita en el RMV o un fondo de inversión cerrado?
2. ¿Necesita la plataforma ser una ETF según el DS 5384, en qué categoría, y puede empezar en el entorno controlado de pruebas de la ASFI?
3. ¿Cuáles son las obligaciones como PSAV, ante la UIF y de prevención de lavado de dinero, y desde cuándo rigen?
4. ¿Cómo puede convivir una restricción a las transferencias con el art. 2 de la Ley 1834?
5. ¿Pueden los titulares recibir USDC, o los pagos deben hacerse por bancos en bolivianos? ¿Qué dicen las normas del BCB y de la ASFI sobre activos virtuales en este flujo? Una consultora de licencias, fuente secundaria, reporta que la Resolución 082/2024 del BCB permite operaciones con activos virtuales por canales autorizados ([PriFinance](https://prifinance.com/es/licencia-de-intercambio-de-criptomonedas/bolivia/)).
6. ¿Cuál es el tratamiento tributario de los pagos para cada tipo de titular (persona natural residente, empresa residente, extranjero) y qué se retiene?
7. ¿Cómo se tratan el IVA, el IT y el IUE del vehículo en la estructura elegida, y tiene NIT propio un patrimonio autónomo o un fondo?
8. ¿Quién puede intermediar las transferencias secundarias, y puede el emisor hacer ventanas de rescate?
9. ¿Es legal cobrar cuotas antes de entregar los tokens? ¿Qué escrow se exige? ¿Es válida la pérdida del descuento de preventa según las normas de protección al consumidor?
10. ¿Quién es el custodio legal de los tokens en billeteras administradas por Pollar?
11. ¿Qué cuenta como asesoría de inversión para el asistente con IA, y qué advertencias son obligatorias?
12. ¿Puede un vehículo tener lotes urbanos o terrenos rurales solo para venderlos después, dados los arts. 315 y 395 de la Constitución? ¿Qué estructura conviene para terrenos, y cómo se aplican la FES, la reversión y la zona de seguridad fronteriza?
13. ¿Puede un vehículo o un fondo tener varios inmuebles y emitir tokens nuevos cuando agrega uno, y qué aprobación necesita cada emisión?

## 6. Mecánica de distribución

### 6.1 Modelo A: instantánea en la fecha de corte y pagos en USDC

Es la práctica habitual de fecha de corte, sin contrato propio.

1. **Cierre y apruebe el mes** (sección 4.6) y calcule el hash del informe.
2. **Fije el ledger de corte y tome la instantánea de saldos.** Cada transferencia pasa por una transacción armada por la plataforma (sección 2.3), así que la plataforma ya conoce la dirección de cada titular. Lea los saldos de las trustlines en lotes con `getLedgerEntries` de Stellar RPC (hasta 200 claves por llamada, ver la [guía de migración de Horizon a RPC](https://developers.stellar.org/docs/data/apis/migrate-from-horizon-to-rpc)) o desde su propio indexador, y registre el número de ledger que devuelve la respuesta. Horizon está [cerca del fin de su vida útil](https://developers.stellar.org/docs/data/apis) y RPC devuelve el estado actual, no el historial, así que guarde cada instantánea.
3. **Verifique la instantánea.** Los saldos de los titulares más los de la distribuidora, el escrow y las demás cuentas de la plataforma deben sumar los tokens emitidos menos los quemados. Si no coinciden (una transferencia cayó entre dos lotes), tome la instantánea de nuevo.
4. **Calcule los pagos con enteros:** `floor(saldo x monto / tokens en circulación)` en unidades base. Los tokens de las cuentas distribuidora y escrow ganan alquiler para su dueño, porque son fracciones del edificio no vendidas o no liberadas. La sección 8.2 lo lista como decisión. Reste la retención de cada titular. Arrastre los residuos de redondeo al siguiente periodo.
5. **Fondee y pague.** Fondee la cuenta de distribución con el total exacto en USDC. Pague en transacciones de hasta 100 operaciones, cada una con el hash del informe como `MEMO_HASH`. Para 300 titulares son 3 transacciones, para 1,500 son 15 y para 5,000 son 50.
6. **Maneje las fallas.** Una transacción de Stellar falla completa si falla una sola operación. Verifique la trustline de USDC de cada destino antes de armar los lotes, y divida un lote que falle. Registre como pasivo el monto de un titular no pagado y páguelo con el siguiente periodo o cuando lo pida.
7. **Publique** el informe y un archivo de pagos con el id del titular, el bruto, la retención, el neto y el hash de la transacción.

El titular registrado en el ledger de corte recibe el pago de todo el periodo. Deje esa regla en los términos. Más adelante se puede acumular por tiempo de tenencia, porque cada transferencia queda en el ledger, pero eso agrega complejidad. Los titulares no pagan comisiones de red. Cualquiera puede recalcular los pagos con el informe publicado y los datos públicos del ledger.

Opciones para después, no necesarias en v1: publicar una raíz de Merkle de la lista de pagos para que los titulares cobren con una prueba, o usar saldos reclamables (claimable balances) para titulares sin trustline de USDC.

### 6.2 Acumulación on-chain (Modelo C, Anexo B)

Elíjala cuando las transferencias sean frecuentes y pequeñas, quiera que la acumulación se liquide on-chain en cada transferencia y acepte una auditoría y un token que no se puede negociar en el DEX de Stellar.

```math
\begin{aligned}
& \text{al depositar:} && \text{índice} \mathrel{+}= \frac{\text{monto} \times 10^{14} + \text{arrastre}}{\text{oferta}} \\[0.5em]
& \text{antes de cualquier cambio de saldo de la cuenta } u\text{:} && \text{ganado}_u = \frac{\text{saldo}_u \times (\text{índice} - \text{último\_índice}_u)}{10^{14}} \\
& && \text{no\_cobrado}_u \mathrel{+}= \text{ganado}_u; \quad \text{último\_índice}_u = \text{índice} \\[0.5em]
& \text{al cobrar:} && \text{pagar } \text{no\_cobrado}_u
\end{aligned}
```

Un titular nuevo empieza en el índice vigente, así que no puede cobrar alquileres pasados. Un vendedor conserva lo acumulado hasta la venta. El punto de control debe ejecutarse antes de cada cambio de saldo: transferencias, transferencias forzosas y quemas.

Qué hacía mal el contrato original y qué hace el Anexo B al respecto:

| Original (páginas 9 a 15) | Problema | Anexo B |
|---|---|---|
| `transfer_fraction` leía ambos saldos y luego escribía ambos | Una autotransferencia sobrescribe el débito con el crédito, así que el saldo crece. En el entorno de pruebas de Soroban (SDK 29.0.1), 1,000 tokens se convirtieron en 32,000 tras cinco autotransferencias. Un titular con una parte justa de 35 USDC cobró 1,120 USDC de un depósito de 3,500 USDC | Escribe el saldo del remitente antes de leer el del destinatario, así que una autotransferencia suma cero |
| `initialize` sin autorización | Cualquiera puede inicializarlo primero después del despliegue | `__constructor`, que se ejecuta dentro de la transacción de despliegue |
| Nada escribía el flag de KYC salvo para el admin | Ningún inversionista podía recibir tokens | `set_allowed`, con un evento |
| Saldos en un mapa privado | No es un token SEP-41, así que las billeteras no lo muestran | La interfaz SEP-41 completa |
| `deposit_net_rent` repetible y residuo perdido | Depósitos dobles y residuos perdidos | Un depósito por id de periodo, con el hash del informe guardado. El residuo se arrastra |
| `update_nav(new_nav_per_token)` recibía un número opaco y nadie lo leía | Sin rastro de auditoría ni límite para un valor erróneo | `update_nav(appraisal, reserves, liabilities, report_sha256)` calcula el NAV y limita el cambio por actualización |
| `ipfs_hash: BytesN<32>` | Un identificador de contenido de IPFS tiene más de 32 bytes | Guarda el SHA-256 del informe. El identificador de contenido va en el evento o en el informe |
| Sin eventos ni extensión de TTL | Las entradas se pueden archivar y nada es auditable | Eventos y extensión de TTL en cada entrada que se toca |
| Sin transferencia forzosa | No hay camino para órdenes judiciales o llaves perdidas | `forced_transfer` con un hash del motivo |
| Las compilaciones de release no detectan el desbordamiento salvo que se configure | Desbordamiento silencioso | `overflow-checks = true` en el perfil de release |

### 6.3 Producto de una venta y cierre de un token

Cuando se vende un inmueble, como suele pasar con un terreno al final de su plan, los titulares reciben el producto de la venta una sola vez, como devolución de capital y por separado de los alquileres.

1. **Apruebe la venta** como exijan los documentos de la oferta: una votación de los titulares, o una decisión del administrador dentro de límites fijados.
2. **Liquide el vehículo.** Pague los costos de venta, el impuesto a la transferencia, el IUE sobre la ganancia y los pasivos pendientes. Guarde una reserva para las últimas declaraciones de impuestos y los costos de cierre, y reparta lo que sobre al final.
3. **Pague** con la misma instantánea en la fecha de corte que se usa para los alquileres (sección 6.1). Rotule el pago como producto de una venta, porque su tratamiento tributario puede ser distinto del de la renta (**Asesoría legal**).
4. **Cierre el token.** En el Modelo A, el emisor recupera con clawback los saldos que queden después del pago final, como lo permitan los términos de la oferta, y el clawback los quema ([lista de operaciones](https://developers.stellar.org/docs/learn/fundamentals/transactions/list-of-operations)). En el Modelo C, el admin mueve los saldos a la tesorería con `forced_transfer` y la tesorería los quema.
5. **Publique** el informe de cierre: precio de venta, costos, impuestos, monto neto por token y hashes de las transacciones.

En un portafolio, la venta de un inmueble reparte su producto neto entre todos los titulares del token de portafolio o, si la oferta lo permite, financia otro inmueble. El token de portafolio sigue abierto.

## 7. Asistente de inversión con IA

### 7.1 Propósito y límites

El asistente convierte el efectivo inicial, el presupuesto mensual y el horizonte de tiempo en dos o tres opciones de compra a partir del inventario vigente. Si la oferta está regulada, sus recomendaciones pueden contar como asesoría de inversión o intermediación y deben seguir las reglas de la entidad autorizada (**Asesoría legal**). Presente las opciones como información con riesgos, nunca como promesas.

### 7.2 Arquitectura

El modelo de lenguaje maneja la conversación, la intención y la explicación. Herramientas deterministas calculan cada número. Esta parte del original se mantiene.

| Herramienta | Devuelve |
|---|---|
| `get_inventory()` | Inmuebles con su tipo (edificio terminado, proyecto en construcción, terreno o portafolio), precio, tokens disponibles, fecha del NAV, rendimiento bruto y neto histórico de los informes publicados (o un rendimiento modelado, rotulado como tal, para un inmueble sin historial), para los terrenos los escenarios de venta y el precio de equilibrio (sección 1.6), y notas de riesgo |
| `get_fx()` | El tipo oficial del BCB y su fecha. Al pagar, la cotización ejecutada de la rampa con `quoteRamp()` |
| `build_options(profile, inventory, fx)` | Dos o tres opciones, cada una con un `calcId`, sus entradas, resultados y supuestos |
| `reserve_tokens(optionId, ttl)` | Una reserva que vence junto con la cotización de la rampa |
| `create_order(optionId)` | Una orden que inicia el flujo de compra (sección 3.3) |

- Cada número de una respuesta debe aparecer en el resultado de una herramienta. Un control posterior compara los números del borrador con esos resultados y regenera la respuesta si alguno no coincide.
- Tres o cuatro edificios no necesitan un optimizador. Use plantillas basadas en reglas (renta ahora, equilibrada, preventa). Son fáciles de explicar y de auditar.
- La interfaz muestra el resultado estructurado como tarjetas. El flujo de compra recibe el `calcId`, nunca el texto del modelo.

### 7.3 Datos de entrada e idoneidad

Pida el efectivo inicial (BOB o USDC), el presupuesto mensual, el número de meses, la tolerancia al riesgo y el horizonte, la residencia (afecta la retención) y el estado del KYC. Limite la concentración: por defecto, ninguna opción pone todo el presupuesto en un solo inmueble. Ofrezca terrenos solo si el horizonte del cliente cubre el periodo de tenencia previsto. Registre las entradas, los resultados y las advertencias mostradas.

### 7.4 Ejemplo resuelto

Cliente: "Tengo 14,000 BOB ahora y puedo pagar 2,000 BOB al mes durante 12 meses."

A 11.67 BOB por USD (A5), 14,000 BOB son $1,199.66 y 2,000 BOB son $171.38, así que el capital a 12 meses es **$3,256.21**. El original mostraba $3,800 a 10 BOB por USD. Las cuotas mensuales se convierten al tipo de cada fecha de pago. Si el boliviano se debilita a 13.0 justo después de la primera compra, las 12 cuotas compran $1,846 en lugar de $2,057, un 10.2% menos.

Inventario (A6): SC-1 a $10.00 (rendimiento neto en efectivo de 3.56% en el año 1), CB-1 a $25.00 (2.34%), preventa SC-2 a $8.00. El rendimiento de CB-1 es el 4.5% bruto multiplicado por la relación neto/bruto de 51.95% de SC-1.

| Opción | Composición | Día 1 | A los 12 meses | Notas |
|---|---|---|---|---|
| A. Renta ahora | 100% SC-1 | 119.97 tokens | 333.35 tokens (0.3175% del edificio). Alquiler recibido: $77.33. Ritmo anual de unos $118.72 | Cada mes, la cuota y el alquiler recibido compran más tokens |
| B. Dos ciudades | 60% SC-1, 40% CB-1 | 71.98 tokens de SC-1 y 19.19 de CB-1 | 200.01 tokens de SC-1 y 52.91 de CB-1, que valen $3,322.82. Alquiler recibido: $66.61. Rendimiento neto combinado de 3.07% | Reparte el riesgo entre ciudades y baja la renta |
| C. Preventa | 100% SC-2 a $8.00 | 149.96 tokens, y luego 21.42 por mes | 407.03 tokens por $3,256.21. Sin alquiler durante la construcción | Valor a la entrega: $3,842.33 con +18% (ganancia de $586.12), $3,256.21 si no cambia, $2,930.59 con -10% (pérdida de $325.62) |

En la opción C, el pago inicial es el 36.84% del capital. Si el cliente deja de pagar después de cuatro cuotas, habrá pagado $1,885.18, que son 235.65 tokens a $8.00, o 188.52 tokens si el contrato los recalcula a $10.00 y pierde el descuento. Esa pérdida es una cláusula que debe revisar la asesoría legal (sección 7.5).

Estas cifras salen de `build_options` con los supuestos indicados, reinvirtiendo el alquiler y con el tipo de cambio constante. Son proyecciones, no pronósticos.

Un token de terreno como LT-1 no paga nada hasta la venta. Para un terreno, el asistente muestra los escenarios de venta y el precio de equilibrio de la sección 1.6 en lugar de un rendimiento, y solo lo ofrece si el horizonte del cliente cubre el periodo de tenencia previsto.

### 7.5 Planes de cuotas (liberación por tramos)

- Los tokens reservados quedan en la cuenta escrow. Cuando se acredita cada cuota, la plataforma libera ese tramo con un pago armado por ella que autoriza la trustline del inversionista solo para esa transacción (sección 2.3), así que el Modelo A no necesita otro contrato.
- La cadena no puede ver un pago QR en BOB, así que la plataforma decide cuándo se acreditó un pago. El original decía que el contrato "libera los siguientes 25 tokens en cuanto se acredita el pago". Informe este supuesto de confianza.
- El alquiler sigue al titular legal. Los tokens reservados y no liberados pertenecen al escrow, así que no generan nada para el comprador.
- Si el comprador deja de pagar, se queda con los tokens liberados. Que además pierda el descuento de preventa debe pasar una revisión de protección al consumidor (**Asesoría legal**).
- Una preventa tiene riesgo de obra inconclusa, de permisos y de insolvencia del desarrollador. Mantenga los fondos de la preventa en escrow y libérelos contra hitos de obra (**Asesoría legal**). Muestre un escenario sin cambio y uno negativo junto al caso base.

### 7.6 Simulación para el cliente

Acompañe cada opción con un control deslizante que llame a la misma función determinista con otro rendimiento, otro tipo de cambio u otro horizonte.

```math
\begin{aligned}
\text{tokens}(t+1) &= \text{tokens}(t) + \frac{\text{cuota\_usdc}(t) + \text{alquiler}(t)}{\text{precio}} \\[0.5em]
\text{alquiler}(t) &= \frac{\text{tokens}(t) \times \text{precio} \times \text{rendimiento\_neto}}{12}
\end{aligned}
```

Muestre siempre los supuestos junto al gráfico.

### 7.7 Estructura de datos y salvaguardas

```ts
interface InvestmentOption {
  calcId: string;                  // une cada cifra con una sola ejecución de la herramienta
  strategy: "income" | "balanced" | "presale";
  lines: { buildingId: string; tokens: string; priceUsdc: string; sharePct: string }[];
  upfrontUsdc: string;
  monthlyUsdc: string;
  months: number;
  fx: { bcbOfficial: string; asOf: string };
  projection: {
    tokensAtEnd: string;
    rentReceivedUsdc: string;
    runRateUsdcYear: string;
    assumptions: string[];
  };
  scenarios?: { label: string; valueUsdc: string; resultUsdc: string }[];
  disclosures: string[];           // riesgos, comisiones, costo de entrada, "proyección, no promesa"
  expiresAt: string;               // vencimiento de la reserva de inventario
}
```

- **Bloqueo de inventario.** Reserve antes de cotizar. Nunca ofrezca más de lo disponible.
- **Moneda.** Muestre BOB y USD lado a lado e indique qué tipo de cambio se aplica a las cuotas futuras.
- **Redacción.** Nada de "garantizado" ni "ganancia inmediata". Rotule cada proyección.
- **Trazabilidad.** Cada número de la respuesta lleva un `calcId`.

## 8. Riesgos y decisiones abiertas

### 8.1 Riesgos

| Riesgo | Dónde aparece | Mitigación |
|---|---|---|
| La oferta está regulada y la plataforma no tiene licencia | Secciones 5.1 y 5.5 | Asesoría legal primero. Preguntar por el entorno controlado de pruebas de la ASFI. Mantener el diseño independiente de la estructura legal |
| Compromiso o pérdida de las llaves del emisor | Secciones 2.3 y 2.4 | Multifirma fría, una llave operativa limitada a operaciones de umbral bajo, una alerta ante cualquier autorización de trustline que no haya armado la plataforma, un procedimiento de recuperación, una política de clawback |
| Avalúo desactualizado o sesgado | Sección 1.3 | Tasador independiente, cambio de NAV acotado, hash del informe, fechas junto a cada precio |
| Variaciones del tipo de cambio | Sección 4.5 | Tipos registrados, pagos en USDC, advertencias |
| Corrida de rescates | Sección 5.4 | Topes, ventanas, una reserva con fondos, ninguna promesa de rescate al NAV |
| Falla de la rampa o del proveedor | Sección 3.3 | Órdenes idempotentes, conciliación, escrow antes de liberar tokens, términos escritos con el proveedor |
| Defecto en un contrato propio | Anexo B | El Modelo A no necesita contrato propio. Auditar antes de usar el Anexo B en producción |
| Tratamiento tributario de los pagos | Sección 4.3 | Una opinión tributaria por tipo de titular antes del primer pago |
| Recomendaciones inadecuadas o engañosas | Sección 7 | Datos de idoneidad, redacción regulada, control de números, registros |
| Concentración en un edificio o una ciudad | Sección 7.4 | Topes por defecto y opciones diversificadas |
| Terrenos: defectos de título, sobreposiciones o avasallamientos | Sección 1.6 | Verificación de título y catastro antes de comprar, cerramiento y vigilancia |
| Terrenos: los costos de tenencia superan la reserva, o la venta se atrasa | Sección 1.6 | Una reserva con margen, una ventana de venta, el precio de equilibrio informado |
| Reglas de la tierra rural (FES, reversión, especulación, zona fronteriza) | Secciones 1.6 y 5.1 | Asesoría legal antes de cualquier oferta rural. Empezar con lotes urbanos |
| Portafolio: dilución al agregar un inmueble | Sección 1.7 | Tokens nuevos al NAV más los costos de adquisición, después de un avalúo al día |

### 8.2 Decisiones a resolver antes del diseño

1. La estructura legal, y si la primera oferta es pública (asesoría legal). Todo lo demás depende de esto.
2. El modelo de token: Modelo A (recomendado) o SEP-57.
3. A qué da derecho un token: solo alquiler, alquiler y producto de la venta, votos, derechos de información.
4. Moneda de distribución y política de tipo de cambio: fuente del tipo, momento de la conversión, quién asume sus costos.
5. Cómo se tratan en las distribuciones y en el NAV los tokens de las cuentas distribuidora y escrow. Esta edición supone que son de quien financió la compra, así que cuentan en el NAV y ganan alquiler.
6. Liquidez secundaria: ninguna, ventanas de rescate o un mercado autorizado.
7. Política de avalúos: método, frecuencia, independencia del tasador, cambio máximo del NAV por actualización.
8. El papel y la situación regulatoria de Pollar, y la ruta de BOB a Stellar, confirmados por escrito.
9. Comisiones: la de administración, cualquier comisión de la plataforma, quién las cobra y cómo se informan. Los ejemplos incluyen solo la comisión de administración (10% del alquiler en SC-1, 1% anual del avalúo en LT-1).
10. Con qué tipos de inmueble empezar (edificios terminados, proyectos en construcción, lotes urbanos, terrenos rurales) y en qué orden.
11. Si se ofrecen tokens de portafolio, y la regla para agregar inmuebles.

## Anexo A. Referencia de fórmulas

```math
\begin{aligned}
& \text{Precio primario} && P_0 = \frac{V + C_{\text{cierre}} + R_0 - D}{N} \\[0.4em]
& \text{NAV del día 1} && NAV_0 = \frac{V + R_0 - D - \text{otros pasivos}}{N} \\[0.4em]
& \text{Costo de entrada} && P_0 - NAV_0 \\
& \text{NAV vigente} && \text{sección 1.3, dividido entre los tokens en circulación} \\
& \text{Enfoque de ingresos} && \text{valor} = \frac{\text{NOI}}{\text{tasa de capitalización}} \\[0.4em]
& \text{Monto neto} && D_p = \text{sección 4.1} \\
& \text{Pago} && \text{pago}_i = \left\lfloor \frac{\text{saldo}_i \times D_p}{N_{\text{circ}}} \right\rfloor - \text{retención}_i \\[0.4em]
& \text{Rendimiento neto} && \frac{\text{pago por token} \times 12 \text{ (en USD)}}{\text{precio}} \\[0.4em]
& \text{Proyección} && \text{tokens}(t+1) = \text{tokens}(t) + \frac{\text{cuota\_usdc} + \text{alquiler}(t)}{\text{precio}} \\
& && \text{alquiler}(t) = \frac{\text{tokens}(t) \times \text{precio} \times \text{rendimiento\_neto}}{12} \\[0.4em]
& \text{Tipo de las cuotas} && \text{usdc}_t = \frac{\text{cuota\_bob}}{tc_t} \\[0.4em]
& \text{Índice de alquileres} && \text{índice} \mathrel{+}= \frac{\text{monto} \times 10^{14} + \text{arrastre}}{\text{oferta}} \\
& && \text{ganado}_u = \frac{\text{saldo}_u \times (\text{índice} - \text{último}_u)}{10^{14}} \\[0.4em]
& \text{Terreno, residual} && \text{terreno} = \text{valor terminado} - \text{construcción} - \text{honorarios e impuestos} - \text{financiamiento} - \text{utilidad del desarrollador} \\
& \text{Equilibrio de terreno} && S_{\text{eq}} = \frac{V + C_{\text{cierre}} + R_{\text{tenencia}}}{1 - s} \quad \text{(en el equilibrio no se paga IUE)} \\[0.4em]
& \text{Emisión de portafolio} && N_{\text{nuevo}} = \frac{V_{\text{nuevo}}}{NAV_t}; \quad P_{\text{nuevo}} = NAV_t \times \frac{V_{\text{nuevo}} + C_{\text{nuevo}}}{V_{\text{nuevo}}}
\end{aligned}
```

Símbolos: `V` precio, `C_cierre` costos de cierre y estructuración, `R0` reserva inicial de efectivo, `D` deuda, `N` tokens emitidos, `N_circ` tokens en circulación (sección 1.3), `tc` tipo de cambio, `R_tenencia` reserva de costos de tenencia, `s` tasa de costos de venta, `V_nuevo` y `C_nuevo` precio y costos de adquisición de un inmueble agregado.

## Anexo B. Contrato de referencia para el Modelo C (probado, no auditado)

Un token SEP-41 con permisos para un edificio, con un índice de alquileres on-chain, como se describe en la sección 6.2.

**Estado**

- Compilado y probado el 2026-11-14 con Rust 1.100.0, `soroban-sdk` y `soroban-token-sdk` 29.0.1, y `stellar-cli` 30.0.1.
- Las 12 pruebas pasan en el entorno de pruebas de Soroban. `cargo clippy --all-targets -- -D warnings` no reporta nada. `stellar contract build` produce un WASM de 18,128 bytes.
- No está desplegado en ninguna red, no está auditado y no se midió contra los presupuestos de recursos de Soroban. Úselo como referencia para leer y extender, y audítelo antes de usarlo en producción.
- El contrato original se probó tal cual en el mismo entorno. Cuatro pruebas reproducen sus defectos: una autotransferencia del saldo completo lo duplica, cinco autotransferencias convierten 1,000 tokens en 32,000, el titular inflado cobra 1,120 USDC frente a una parte justa de 35 USDC, y ningún inversionista puede recibir tokens porque nada activa el flag de KYC.
- El código se reproduce exactamente como se probó, así que sus comentarios están en inglés.

**Decisiones de diseño**

- El constructor emite toda la oferta a la tesorería, que es el primer titular. Los tokens no vendidos de la tesorería ganan alquiler, igual que en la sección 6.1.
- `update_nav` divide entre la oferta total. Eso es correcto cuando quien financió la compra tiene los tokens no vendidos. Si los guarda el propio vehículo, divida entre los tokens que están fuera del vehículo (sección 1.3).
- Las dos partes de una transferencia deben estar en la lista de autorizados. Revocar a un titular congela sus transferencias y sus cobros. `forced_transfer` igual mueve sus tokens por órdenes judiciales o llaves perdidas.
- Tal como está escrito, los titulares autorizados pueden transferirse tokens entre sí sin la plataforma. Para mantener las transferencias dentro de la plataforma, como hace la sección 2.3 con el Modelo A, exija también la autorización del admin en `transfer` y `transfer_from`.
- `forced_transfer` mueve tokens, no el alquiler ya acumulado. Ese alquiler sigue siendo cobrable por la dirección anterior, así que queda trabado si se perdió la llave. Agregue un cobro forzoso si sus procedimientos lo necesitan.
- El índice de alquileres arrastra el residuo de la división. Cada periodo se puede depositar una sola vez y guarda el hash de su informe firmado.
- El NAV se calcula a partir de sus entradas, y el cambio por actualización tiene un tope. El tope solo cambia con una llamada aparte del admin.
- El contrato guarda el NAV para los reportes. No vende tokens al NAV. Las ventas primarias ocurren fuera del contrato (sección 3.3).
- Hay una sola dirección de admin. Use una cuenta multifirma. No hay función de actualización, así que un defecto requiere una migración. Agregue una actualización con timelock si la auditoría lo pide.

**Qué muestran las pruebas**

| Prueba | Qué demuestra |
|---|---|
| `exposes_the_sep41_surface` | Nombre, símbolo, decimales, saldo, approve, allowance y `transfer_from` funcionan con el cliente de token estándar |
| `self_transfer_does_not_mint` | Cinco autotransferencias del saldo completo dejan 1,000 tokens en 1,000 y la oferta sin cambios |
| `only_allowed_accounts_can_receive_and_a_revoked_sender_is_frozen` | Un desconocido no puede recibir. Un titular revocado no puede enviar. Al restaurar el flag vuelven las transferencias |
| `pro_rata_share_matches_the_worked_example` | Un titular de 2,500 de 100,000 tokens gana 87.5 USDC de un depósito de 3,500 USDC y no puede cobrar dos veces |
| `rent_stays_with_the_holder_across_a_mid_period_sale` | El vendedor conserva el alquiler ganado antes de la venta. El comprador gana solo desde el siguiente depósito |
| `a_period_can_be_deposited_once` | Un periodo repetido se rechaza y no mueve USDC. El hash del informe queda guardado |
| `division_remainder_is_carried_forward` | Tres depósitos de 1 stroop sobre una oferta de 3 tokens pagan los 3 stroops |
| `nav_is_computed_from_its_inputs_and_bounded` | El NAV del ejemplo de referencia es 9.6190476 USDC. Una actualización de +5% pasa, una de +50% se rechaza, un patrimonio negativo se rechaza, y subir el tope permite el cambio mayor |
| `forced_transfer_settles_rent_on_both_sides` | El titular anterior conserva el alquiler ganado antes de la orden. El nuevo empieza en el índice vigente |
| `burn_shrinks_supply_and_keeps_accrued_rent` | Quemar reduce la oferta y deja cobrable el alquiler acumulado |
| `state_survives_a_long_idle_period` | Los saldos y los cobros siguen funcionando después de 20 días de ledgers |
| `never_owes_more_than_was_deposited` | Una secuencia aleatoria con semilla de 120 transferencias (incluidas autotransferencias), depósitos y cobros nunca debe más de lo depositado. El residuo de redondeo terminó en 35 stroops en unos 150 puntos de control. El contrato guarda exactamente los depósitos menos los cobros, y la oferta de tokens se conserva |

**Reproducir**

Cree un crate con los tres archivos de abajo y ejecute:

```bash
cargo test
cargo clippy --all-targets -- -D warnings
stellar contract build   # soroban-sdk 29 no compila el WASM con cargo build directo y pide stellar-cli
```

Al correr las pruebas se escriben instantáneas JSON en `test_snapshots/`. Haga commit de ellas o agregue la carpeta a `.gitignore`.

<details>
<summary><code>Cargo.toml</code></summary>

```toml
[package]
name = "building-token"
description = "Reference SEP-41 permissioned building token with an on-chain rent index"
version = "0.1.0"
edition = "2021"
rust-version = "1.92.0"
publish = false

[lib]
crate-type = ["cdylib"]

[dependencies]
soroban-sdk = { version = "29.0.1" }
soroban-token-sdk = { version = "29.0.1" }

[dev-dependencies]
soroban-sdk = { version = "29.0.1", features = ["testutils"] }

[profile.release]
opt-level = "z"
overflow-checks = true
debug = 0
strip = "symbols"
debug-assertions = false
panic = "abort"
codegen-units = 1
lto = true
```

</details>

<details>
<summary><code>src/lib.rs</code> (el contrato)</summary>

```rust
#![no_std]
//! Reference implementation, not audited: a SEP-41 permissioned building token with an
//! on-chain rent index. Balances live in this contract, so wallets can read them through the
//! standard token interface, and every balance change settles accrued rent first.

use soroban_sdk::{
    contract, contracterror, contractevent, contractimpl, contracttype, panic_with_error,
    token::{TokenClient, TokenInterface},
    Address, BytesN, Env, MuxedAddress, String,
};
use soroban_token_sdk::{events, metadata::TokenMetadata, TokenUtils};

/// One whole token = 10^7 base units (7 decimals, like USDC on Stellar).
const TOKEN_UNIT: i128 = 10_000_000;
/// Fixed-point scale of the rent index (10^14).
const INDEX_SCALE: i128 = 100_000_000_000_000;

const DAY_IN_LEDGERS: u32 = 17_280;
const INSTANCE_BUMP: u32 = 7 * DAY_IN_LEDGERS;
const INSTANCE_THRESHOLD: u32 = INSTANCE_BUMP - DAY_IN_LEDGERS;
const ENTRY_BUMP: u32 = 30 * DAY_IN_LEDGERS;
const ENTRY_THRESHOLD: u32 = ENTRY_BUMP - DAY_IN_LEDGERS;

#[contracterror]
#[derive(Copy, Clone, Debug, Eq, PartialEq, PartialOrd, Ord)]
#[repr(u32)]
pub enum Error {
    InvalidAmount = 1,
    NotAllowed = 2,
    InsufficientBalance = 3,
    InsufficientAllowance = 4,
    PeriodAlreadyDeposited = 5,
    NothingToClaim = 6,
    InvalidNav = 7,
    NavMoveTooLarge = 8,
    InvalidSupply = 9,
}

#[contracttype]
#[derive(Clone)]
pub struct AllowanceKey {
    pub from: Address,
    pub spender: Address,
}

#[contracttype]
pub struct AllowanceValue {
    pub amount: i128,
    pub live_until_ledger: u32,
}

/// Valuation inputs and the NAV computed from them, all in USDC base units (7 decimals).
#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct NavSnapshot {
    pub appraisal: i128,
    pub reserves: i128,
    pub liabilities: i128,
    pub nav_per_token: i128,
    pub ledger: u32,
    pub report_sha256: BytesN<32>,
}

#[contracttype]
#[derive(Clone)]
pub enum DataKey {
    Admin,
    Usdc,
    TotalSupply,
    MaxNavMoveBps,
    /// Cumulative USDC per token base unit, scaled by INDEX_SCALE.
    RentIndex,
    /// Division remainder carried into the next deposit so no stroop is lost.
    IndexCarry,
    Nav,
    Balance(Address),
    UserIndex(Address),
    Unclaimed(Address),
    Allowed(Address),
    Period(u32),
    Allowance(AllowanceKey),
}

#[contractevent]
pub struct RentDeposited {
    #[topic]
    pub period: u32,
    pub amount: i128,
    pub report_sha256: BytesN<32>,
}

#[contractevent]
pub struct RentClaimed {
    #[topic]
    pub investor: Address,
    pub amount: i128,
}

#[contractevent]
pub struct NavUpdated {
    pub nav_per_token: i128,
    pub report_sha256: BytesN<32>,
}

#[contractevent(data_format = "single-value")]
pub struct NavCapSet {
    pub max_bps: u32,
}

#[contractevent]
pub struct AllowedSet {
    #[topic]
    pub account: Address,
    pub allowed: bool,
}

#[contractevent]
pub struct ForcedTransfer {
    #[topic]
    pub from: Address,
    #[topic]
    pub to: Address,
    pub amount: i128,
    pub reason_sha256: BytesN<32>,
}

#[contractevent(data_format = "single-value")]
pub struct SetAdmin {
    #[topic]
    pub admin: Address,
    pub new_admin: Address,
}

// ---------------------------------------------------------------- storage helpers

fn bump_instance(e: &Env) {
    e.storage().instance().extend_ttl(INSTANCE_THRESHOLD, INSTANCE_BUMP);
}

fn read_i128(e: &Env, key: &DataKey) -> i128 {
    match e.storage().persistent().get::<DataKey, i128>(key) {
        Some(v) => {
            e.storage().persistent().extend_ttl(key, ENTRY_THRESHOLD, ENTRY_BUMP);
            v
        }
        None => 0,
    }
}

fn write_i128(e: &Env, key: &DataKey, value: i128) {
    e.storage().persistent().set(key, &value);
    e.storage().persistent().extend_ttl(key, ENTRY_THRESHOLD, ENTRY_BUMP);
}

fn read_admin(e: &Env) -> Address {
    e.storage().instance().get::<DataKey, Address>(&DataKey::Admin).unwrap()
}

fn read_supply(e: &Env) -> i128 {
    e.storage().instance().get::<DataKey, i128>(&DataKey::TotalSupply).unwrap()
}

fn read_index(e: &Env) -> i128 {
    e.storage().instance().get::<DataKey, i128>(&DataKey::RentIndex).unwrap_or(0)
}

fn is_allowed(e: &Env, who: &Address) -> bool {
    let key = DataKey::Allowed(who.clone());
    match e.storage().persistent().get::<DataKey, bool>(&key) {
        Some(allowed) => {
            e.storage().persistent().extend_ttl(&key, ENTRY_THRESHOLD, ENTRY_BUMP);
            allowed
        }
        None => false,
    }
}

fn require_allowed(e: &Env, who: &Address) {
    if !is_allowed(e, who) {
        panic_with_error!(e, Error::NotAllowed);
    }
}

fn check_amount(e: &Env, amount: i128) {
    if amount < 0 {
        panic_with_error!(e, Error::InvalidAmount);
    }
}

// ---------------------------------------------------------------- balances and rent accrual

fn read_balance(e: &Env, who: &Address) -> i128 {
    read_i128(e, &DataKey::Balance(who.clone()))
}

fn spend_balance(e: &Env, who: &Address, amount: i128) {
    let balance = read_balance(e, who);
    if balance < amount {
        panic_with_error!(e, Error::InsufficientBalance);
    }
    write_i128(e, &DataKey::Balance(who.clone()), balance - amount);
}

fn receive_balance(e: &Env, who: &Address, amount: i128) {
    // Read after the sender was written: a self-transfer nets to zero instead of minting.
    let balance = read_balance(e, who);
    write_i128(e, &DataKey::Balance(who.clone()), balance + amount);
}

/// Moves the rent earned since the user's last checkpoint into `Unclaimed`.
/// Must run before any change to the user's balance.
fn checkpoint(e: &Env, who: &Address) {
    let index = read_index(e);
    let user_key = DataKey::UserIndex(who.clone());
    let last = read_i128(e, &user_key);
    if index > last {
        let balance = read_balance(e, who);
        if balance > 0 {
            let earned = balance * (index - last) / INDEX_SCALE;
            if earned > 0 {
                let key = DataKey::Unclaimed(who.clone());
                write_i128(e, &key, read_i128(e, &key) + earned);
            }
        }
        write_i128(e, &user_key, index);
    }
}

fn move_tokens(e: &Env, from: &Address, to: &Address, amount: i128) {
    require_allowed(e, from);
    require_allowed(e, to);
    checkpoint(e, from);
    checkpoint(e, to);
    spend_balance(e, from, amount);
    receive_balance(e, to, amount);
}

// ---------------------------------------------------------------- allowances (SEP-41)

fn read_allowance(e: &Env, from: &Address, spender: &Address) -> AllowanceValue {
    let key = DataKey::Allowance(AllowanceKey { from: from.clone(), spender: spender.clone() });
    match e.storage().temporary().get::<DataKey, AllowanceValue>(&key) {
        Some(a) if a.live_until_ledger >= e.ledger().sequence() => a,
        Some(a) => AllowanceValue { amount: 0, live_until_ledger: a.live_until_ledger },
        None => AllowanceValue { amount: 0, live_until_ledger: 0 },
    }
}

fn write_allowance(e: &Env, from: &Address, spender: &Address, amount: i128, live_until: u32) {
    if amount > 0 && live_until < e.ledger().sequence() {
        panic_with_error!(e, Error::InvalidAmount);
    }
    let key = DataKey::Allowance(AllowanceKey { from: from.clone(), spender: spender.clone() });
    e.storage()
        .temporary()
        .set(&key, &AllowanceValue { amount, live_until_ledger: live_until });
    if amount > 0 {
        let live_for = live_until - e.ledger().sequence();
        e.storage().temporary().extend_ttl(&key, live_for, live_for);
    }
}

fn spend_allowance(e: &Env, from: &Address, spender: &Address, amount: i128) {
    let allowance = read_allowance(e, from, spender);
    if allowance.amount < amount {
        panic_with_error!(e, Error::InsufficientAllowance);
    }
    if amount > 0 {
        write_allowance(e, from, spender, allowance.amount - amount, allowance.live_until_ledger);
    }
}

// ---------------------------------------------------------------- contract

#[contract]
pub struct BuildingToken;

#[allow(clippy::too_many_arguments)]
#[contractimpl]
impl BuildingToken {
    #[allow(clippy::too_many_arguments)]
    /// Runs inside the deploy transaction, so nobody can initialise the contract first.
    /// The whole fixed supply is minted to `treasury`, which is the only holder at the start.
    pub fn __constructor(
        e: Env,
        admin: Address,
        usdc: Address,
        treasury: Address,
        name: String,
        symbol: String,
        total_supply: i128,
        max_nav_move_bps: u32,
    ) {
        if total_supply <= 0 {
            panic_with_error!(&e, Error::InvalidSupply);
        }
        let inst = e.storage().instance();
        inst.set(&DataKey::Admin, &admin);
        inst.set(&DataKey::Usdc, &usdc);
        inst.set(&DataKey::TotalSupply, &total_supply);
        inst.set(&DataKey::MaxNavMoveBps, &max_nav_move_bps);
        TokenUtils::new(&e)
            .metadata()
            .set_metadata(&TokenMetadata { decimal: 7, name, symbol });
        e.storage().persistent().set(&DataKey::Allowed(treasury.clone()), &true);
        write_i128(&e, &DataKey::Balance(treasury.clone()), total_supply);
        events::MintWithAmountOnly { to: treasury, amount: total_supply }.publish(&e);
    }

    // ---- compliance (admin)

    /// Allowlist maintained from the KYC provider. Revoking freezes transfers and claims.
    pub fn set_allowed(e: Env, account: Address, allowed: bool) {
        read_admin(&e).require_auth();
        bump_instance(&e);
        e.storage().persistent().set(&DataKey::Allowed(account.clone()), &allowed);
        e.storage().persistent().extend_ttl(
            &DataKey::Allowed(account.clone()),
            ENTRY_THRESHOLD,
            ENTRY_BUMP,
        );
        AllowedSet { account, allowed }.publish(&e);
    }

    /// Court order, inheritance or lost key. Settles rent for both sides first.
    pub fn forced_transfer(
        e: Env,
        from: Address,
        to: Address,
        amount: i128,
        reason_sha256: BytesN<32>,
    ) {
        read_admin(&e).require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        require_allowed(&e, &to);
        checkpoint(&e, &from);
        checkpoint(&e, &to);
        spend_balance(&e, &from, amount);
        receive_balance(&e, &to, amount);
        ForcedTransfer { from, to, amount, reason_sha256 }.publish(&e);
    }

    /// Use a multisig account as admin in production.
    pub fn set_admin(e: Env, new_admin: Address) {
        let admin = read_admin(&e);
        admin.require_auth();
        bump_instance(&e);
        e.storage().instance().set(&DataKey::Admin, &new_admin);
        SetAdmin { admin, new_admin }.publish(&e);
    }

    // ---- rent

    /// Deposits one period's net distributable cash. A period can be deposited once, and its
    /// signed report hash is stored with it. Unsold tokens held by the treasury earn rent too.
    pub fn deposit_net_rent(e: Env, period: u32, amount: i128, report_sha256: BytesN<32>) {
        let admin = read_admin(&e);
        admin.require_auth();
        if amount <= 0 {
            panic_with_error!(&e, Error::InvalidAmount);
        }
        bump_instance(&e);
        let period_key = DataKey::Period(period);
        if e.storage().persistent().has(&period_key) {
            panic_with_error!(&e, Error::PeriodAlreadyDeposited);
        }
        let supply = read_supply(&e);
        if supply <= 0 {
            panic_with_error!(&e, Error::InvalidSupply);
        }
        let usdc = e.storage().instance().get::<DataKey, Address>(&DataKey::Usdc).unwrap();
        TokenClient::new(&e, &usdc).transfer(&admin, e.current_contract_address(), &amount);

        let carry = e.storage().instance().get::<DataKey, i128>(&DataKey::IndexCarry).unwrap_or(0);
        let scaled = amount * INDEX_SCALE + carry;
        e.storage().instance().set(&DataKey::RentIndex, &(read_index(&e) + scaled / supply));
        e.storage().instance().set(&DataKey::IndexCarry, &(scaled % supply));

        e.storage().persistent().set(&period_key, &report_sha256);
        e.storage().persistent().extend_ttl(&period_key, ENTRY_THRESHOLD, ENTRY_BUMP);
        RentDeposited { period, amount, report_sha256 }.publish(&e);
    }

    /// Pays all accrued rent to the investor in USDC.
    pub fn claim_rent(e: Env, investor: Address) -> i128 {
        investor.require_auth();
        bump_instance(&e);
        require_allowed(&e, &investor);
        checkpoint(&e, &investor);
        let key = DataKey::Unclaimed(investor.clone());
        let payout = read_i128(&e, &key);
        if payout <= 0 {
            panic_with_error!(&e, Error::NothingToClaim);
        }
        write_i128(&e, &key, 0);
        let usdc = e.storage().instance().get::<DataKey, Address>(&DataKey::Usdc).unwrap();
        TokenClient::new(&e, &usdc).transfer(&e.current_contract_address(), &investor, &payout);
        RentClaimed { investor, amount: payout }.publish(&e);
        payout
    }

    /// Rent the investor could claim right now, including accrual not yet checkpointed.
    pub fn claimable(e: Env, investor: Address) -> i128 {
        let index = read_index(&e);
        let last = read_i128(&e, &DataKey::UserIndex(investor.clone()));
        let balance = read_balance(&e, &investor);
        let pending = read_i128(&e, &DataKey::Unclaimed(investor));
        if balance > 0 && index > last {
            pending + balance * (index - last) / INDEX_SCALE
        } else {
            pending
        }
    }

    // ---- valuation

    /// NAV per whole token = (appraisal + reserves - liabilities) / tokens outstanding.
    /// All inputs are USDC base units. After the first update, a move larger than
    /// `max_nav_move_bps` is rejected, so a single bad appraisal cannot reprice the token.
    pub fn update_nav(
        e: Env,
        appraisal: i128,
        reserves: i128,
        liabilities: i128,
        report_sha256: BytesN<32>,
    ) {
        read_admin(&e).require_auth();
        bump_instance(&e);
        if appraisal <= 0 || reserves < 0 || liabilities < 0 {
            panic_with_error!(&e, Error::InvalidNav);
        }
        let equity = appraisal + reserves - liabilities;
        let supply = read_supply(&e);
        if equity <= 0 || supply <= 0 {
            panic_with_error!(&e, Error::InvalidNav);
        }
        let nav_per_token = equity * TOKEN_UNIT / supply;
        if nav_per_token <= 0 {
            panic_with_error!(&e, Error::InvalidNav);
        }
        if let Some(prev) = e.storage().instance().get::<DataKey, NavSnapshot>(&DataKey::Nav) {
            let max_bps = e
                .storage()
                .instance()
                .get::<DataKey, u32>(&DataKey::MaxNavMoveBps)
                .unwrap() as i128;
            let moved = (nav_per_token - prev.nav_per_token).abs();
            if moved * 10_000 > prev.nav_per_token * max_bps {
                panic_with_error!(&e, Error::NavMoveTooLarge);
            }
        }
        let snapshot = NavSnapshot {
            appraisal,
            reserves,
            liabilities,
            nav_per_token,
            ledger: e.ledger().sequence(),
            report_sha256: report_sha256.clone(),
        };
        e.storage().instance().set(&DataKey::Nav, &snapshot);
        NavUpdated { nav_per_token, report_sha256 }.publish(&e);
    }

    /// Changes the per-update NAV cap. Raise it deliberately before a large, documented re-appraisal.
    pub fn set_max_nav_move_bps(e: Env, max_bps: u32) {
        read_admin(&e).require_auth();
        bump_instance(&e);
        e.storage().instance().set(&DataKey::MaxNavMoveBps, &max_bps);
        NavCapSet { max_bps }.publish(&e);
    }

    // ---- views

    pub fn nav(e: Env) -> Option<NavSnapshot> {
        e.storage().instance().get::<DataKey, NavSnapshot>(&DataKey::Nav)
    }

    pub fn total_supply(e: Env) -> i128 {
        read_supply(&e)
    }

    pub fn rent_index(e: Env) -> i128 {
        read_index(&e)
    }

    pub fn is_allowed(e: Env, account: Address) -> bool {
        is_allowed(&e, &account)
    }

    pub fn period_report(e: Env, period: u32) -> Option<BytesN<32>> {
        e.storage().persistent().get::<DataKey, BytesN<32>>(&DataKey::Period(period))
    }
}

#[contractimpl(contracttrait)]
impl TokenInterface for BuildingToken {
    fn allowance(e: Env, from: Address, spender: Address) -> i128 {
        bump_instance(&e);
        read_allowance(&e, &from, &spender).amount
    }

    fn approve(e: Env, from: Address, spender: Address, amount: i128, live_until_ledger: u32) {
        from.require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        write_allowance(&e, &from, &spender, amount, live_until_ledger);
        events::Approve { from, spender, amount, live_until_ledger }.publish(&e);
    }

    fn balance(e: Env, id: Address) -> i128 {
        bump_instance(&e);
        read_balance(&e, &id)
    }

    fn transfer(e: Env, from: Address, to_muxed: MuxedAddress, amount: i128) {
        from.require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        let to = to_muxed.address();
        move_tokens(&e, &from, &to, amount);
        events::Transfer { from, to, to_muxed_id: to_muxed.id(), amount }.publish(&e);
    }

    fn transfer_from(e: Env, spender: Address, from: Address, to: Address, amount: i128) {
        spender.require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        spend_allowance(&e, &from, &spender, amount);
        move_tokens(&e, &from, &to, amount);
        events::Transfer { from, to, to_muxed_id: None, amount }.publish(&e);
    }

    /// Burning reduces the supply, so later deposits divide by the smaller number.
    fn burn(e: Env, from: Address, amount: i128) {
        from.require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        require_allowed(&e, &from);
        checkpoint(&e, &from);
        spend_balance(&e, &from, amount);
        e.storage().instance().set(&DataKey::TotalSupply, &(read_supply(&e) - amount));
        events::Burn { from, amount }.publish(&e);
    }

    fn burn_from(e: Env, spender: Address, from: Address, amount: i128) {
        spender.require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        spend_allowance(&e, &from, &spender, amount);
        require_allowed(&e, &from);
        checkpoint(&e, &from);
        spend_balance(&e, &from, amount);
        e.storage().instance().set(&DataKey::TotalSupply, &(read_supply(&e) - amount));
        events::Burn { from, amount }.publish(&e);
    }

    fn decimals(e: Env) -> u32 {
        TokenUtils::new(&e).metadata().get_metadata().decimal
    }

    fn name(e: Env) -> String {
        TokenUtils::new(&e).metadata().get_metadata().name
    }

    fn symbol(e: Env) -> String {
        TokenUtils::new(&e).metadata().get_metadata().symbol
    }
}

#[cfg(test)]
mod test;
```

</details>

<details>
<summary><code>src/test.rs</code> (las 12 pruebas)</summary>

```rust
extern crate std;

use super::*;
use soroban_sdk::{
    testutils::{Address as _, Ledger},
    token::{StellarAssetClient, TokenClient},
    Address, BytesN, Env, String,
};

/// One whole token or one whole USDC, in base units.
const T: i128 = 10_000_000;

/// A panic with `panic_with_error!` reaches the client as a generic `soroban_sdk::Error`.
fn code(e: Error) -> soroban_sdk::Error {
    soroban_sdk::Error::from_contract_error(e as u32)
}

struct Fx<'a> {
    env: Env,
    id: Address,
    vault: BuildingTokenClient<'a>,
    token: TokenClient<'a>,
    usdc: TokenClient<'a>,
    treasury: Address,
}

fn hash(env: &Env, tag: u8) -> BytesN<32> {
    BytesN::from_array(env, &[tag; 32])
}

fn fixture<'a>(supply_tokens: i128) -> Fx<'a> {
    let env = Env::default();
    env.mock_all_auths();
    let admin = Address::generate(&env);
    let treasury = Address::generate(&env);
    let issuer = Address::generate(&env);
    let usdc_sac = env.register_stellar_asset_contract_v2(issuer);
    let usdc_admin = StellarAssetClient::new(&env, &usdc_sac.address());
    usdc_admin.mint(&admin, &(1_000_000 * T));
    let id = env.register(
        BuildingToken,
        (
            admin.clone(),
            usdc_sac.address(),
            treasury.clone(),
            String::from_str(&env, "Torre Equipetrol"),
            String::from_str(&env, "TEQ1"),
            supply_tokens * T,
            2_000u32, // NAV may move at most 20% per update
        ),
    );
    let vault = BuildingTokenClient::new(&env, &id);
    let token = TokenClient::new(&env, &id);
    let usdc = TokenClient::new(&env, &usdc_sac.address());
    Fx { env, id, vault, token, usdc, treasury }
}

impl<'a> Fx<'a> {
    /// New KYC-approved investor holding `tokens` whole tokens bought from the treasury.
    fn investor(&self, tokens: i128) -> Address {
        let who = Address::generate(&self.env);
        self.vault.set_allowed(&who, &true);
        if tokens > 0 {
            self.token.transfer(&self.treasury, &who, &(tokens * T));
        }
        who
    }
    fn deposit(&self, period: u32, usdc: i128) {
        self.vault.deposit_net_rent(&period, &(usdc * T), &hash(&self.env, period as u8));
    }
}

#[test]
fn exposes_the_sep41_surface() {
    let f = fixture(100_000);
    assert_eq!(f.token.decimals(), 7);
    assert_eq!(f.token.name(), String::from_str(&f.env, "Torre Equipetrol"));
    assert_eq!(f.token.symbol(), String::from_str(&f.env, "TEQ1"));
    assert_eq!(f.token.balance(&f.treasury), 100_000 * T);
    assert_eq!(f.vault.total_supply(), 100_000 * T);

    let alice = f.investor(1_000);
    let bob = f.investor(0);
    let spender = Address::generate(&f.env);
    f.token.approve(&alice, &spender, &(300 * T), &(f.env.ledger().sequence() + 100));
    assert_eq!(f.token.allowance(&alice, &spender), 300 * T);
    f.token.transfer_from(&spender, &alice, &bob, &(200 * T));
    assert_eq!(f.token.balance(&alice), 800 * T);
    assert_eq!(f.token.balance(&bob), 200 * T);
    assert_eq!(f.token.allowance(&alice, &spender), 100 * T);
}

/// The original minted tokens on a self-transfer: 1,000 became 32,000 after five of them.
#[test]
fn self_transfer_does_not_mint() {
    let f = fixture(100_000);
    let alice = f.investor(1_000);
    for _ in 0..5 {
        let all = f.token.balance(&alice);
        f.token.transfer(&alice, &alice, &all);
    }
    assert_eq!(f.token.balance(&alice), 1_000 * T);
    assert_eq!(f.token.balance(&alice) + f.token.balance(&f.treasury), 100_000 * T);
}

/// The original had no way to approve an investor, so nobody but the admin could ever hold tokens.
#[test]
fn only_allowed_accounts_can_receive_and_a_revoked_sender_is_frozen() {
    let f = fixture(100_000);
    let stranger = Address::generate(&f.env);
    let res = f.token.try_transfer(&f.treasury, &stranger, &(10 * T));
    assert_eq!(res.err().unwrap().ok().unwrap(), code(Error::NotAllowed));

    let alice = f.investor(100);
    f.vault.set_allowed(&alice, &false);
    let res = f.token.try_transfer(&alice, &f.treasury, &(10 * T));
    assert_eq!(res.err().unwrap().ok().unwrap(), code(Error::NotAllowed));

    f.vault.set_allowed(&alice, &true);
    f.token.transfer(&alice, &f.treasury, &(10 * T));
    assert_eq!(f.token.balance(&alice), 90 * T);
}

/// The document's own example: a holder of 2,500 of 100,000 tokens earns 87.5 USDC from a 3,500 USDC deposit.
#[test]
fn pro_rata_share_matches_the_worked_example() {
    let f = fixture(100_000);
    let alice = f.investor(2_500);
    f.deposit(1, 3_500);
    assert_eq!(f.vault.claimable(&alice), 875_000_000); // 87.5 USDC
    let paid = f.vault.claim_rent(&alice);
    assert_eq!(paid, 875_000_000);
    assert_eq!(f.usdc.balance(&alice), 875_000_000);
    assert_eq!(f.vault.claimable(&alice), 0);
    let again = f.vault.try_claim_rent(&alice);
    assert_eq!(again.err().unwrap().ok().unwrap(), code(Error::NothingToClaim));
}

/// A holds through period 1 and sells to B before period 2: A keeps period 1 rent, B gets only period 2.
#[test]
fn rent_stays_with_the_holder_across_a_mid_period_sale() {
    let f = fixture(100_000);
    let a = f.investor(1_000);
    let b = f.investor(0);
    f.deposit(1, 10_000);
    f.token.transfer(&a, &b, &(1_000 * T));
    assert_eq!(f.vault.claimable(&a), 100 * T, "A keeps the rent accrued before the sale");
    assert_eq!(f.vault.claimable(&b), 0, "B cannot claim rent from before the purchase");
    f.deposit(2, 10_000);
    assert_eq!(f.vault.claimable(&a), 100 * T, "A earns nothing from period 2");
    assert_eq!(f.vault.claimable(&b), 100 * T, "B earns period 2");
    assert_eq!(f.vault.claim_rent(&a), 100 * T);
    assert_eq!(f.vault.claim_rent(&b), 100 * T);
}

#[test]
fn a_period_can_be_deposited_once() {
    let f = fixture(100_000);
    f.deposit(202611, 3_500);
    assert_eq!(f.vault.period_report(&202611), Some(hash(&f.env, 202611u32 as u8)));
    let again = f.vault.try_deposit_net_rent(&202611, &(3_500 * T), &hash(&f.env, 9));
    assert_eq!(again.err().unwrap().ok().unwrap(), code(Error::PeriodAlreadyDeposited));
    assert_eq!(f.usdc.balance(&f.id), 3_500 * T, "the rejected deposit moved no USDC");
}

/// 3-token supply, 1-stroop deposits: the old floor-only index paid nothing; the carry pays all 3 stroops.
#[test]
fn division_remainder_is_carried_forward() {
    let f = fixture(3);
    let a = f.investor(1);
    let b = f.investor(1);
    let c = f.investor(1);
    for period in 1..=3u32 {
        f.vault.deposit_net_rent(&period, &1, &hash(&f.env, period as u8));
    }
    let total = f.vault.claimable(&a) + f.vault.claimable(&b) + f.vault.claimable(&c);
    assert_eq!(total, 3, "all three deposited stroops are claimable");
    assert_eq!(f.vault.claim_rent(&a), 1);
}

#[test]
fn nav_is_computed_from_its_inputs_and_bounded() {
    let f = fixture(105_000);
    assert_eq!(f.vault.nav(), None);
    // 1,000,000 appraisal + 10,000 reserves - 0 liabilities over 105,000 tokens = 9.6190476 USDC
    f.vault.update_nav(&(1_000_000 * T), &(10_000 * T), &0, &hash(&f.env, 1));
    let nav = f.vault.nav().unwrap();
    assert_eq!(nav.nav_per_token, 96_190_476);
    assert_eq!(nav.report_sha256, hash(&f.env, 1));

    // +5%: accepted
    f.vault.update_nav(&(1_060_000 * T), &(10_000 * T), &0, &hash(&f.env, 2));
    assert!(f.vault.nav().unwrap().nav_per_token > 96_190_476);

    // +50%: rejected by the 20% cap
    let big = f.vault.try_update_nav(&(1_600_000 * T), &(10_000 * T), &0, &hash(&f.env, 3));
    assert_eq!(big.err().unwrap().ok().unwrap(), code(Error::NavMoveTooLarge));

    // liabilities larger than assets: rejected
    let neg = f.vault.try_update_nav(&(100 * T), &0, &(200 * T), &hash(&f.env, 4));
    assert_eq!(neg.err().unwrap().ok().unwrap(), code(Error::InvalidNav));

    // a documented large re-appraisal needs the cap raised first, as a separate admin action
    f.vault.set_max_nav_move_bps(&6_000);
    f.vault.update_nav(&(1_600_000 * T), &(10_000 * T), &0, &hash(&f.env, 5));
    assert!(f.vault.nav().unwrap().nav_per_token > 150_000_000);
}

#[test]
fn forced_transfer_settles_rent_on_both_sides() {
    let f = fixture(100_000);
    let lost = f.investor(1_000);
    let heir = f.investor(0);
    f.deposit(1, 10_000);
    f.vault.forced_transfer(&lost, &heir, &(1_000 * T), &hash(&f.env, 7));
    assert_eq!(f.token.balance(&heir), 1_000 * T);
    assert_eq!(f.vault.claimable(&lost), 100 * T, "rent earned before the order stays with the old holder");
    assert_eq!(f.vault.claimable(&heir), 0);
}

#[test]
fn burn_shrinks_supply_and_keeps_accrued_rent() {
    let f = fixture(100_000);
    let alice = f.investor(1_000);
    f.deposit(1, 10_000);
    f.token.burn(&alice, &(1_000 * T));
    assert_eq!(f.vault.total_supply(), 99_000 * T);
    assert_eq!(f.vault.claimable(&alice), 100 * T);
    assert_eq!(f.vault.claim_rent(&alice), 100 * T);
}

#[test]
fn state_survives_a_long_idle_period() {
    let f = fixture(100_000);
    let alice = f.investor(1_000);
    f.deposit(1, 10_000);
    f.env.ledger().with_mut(|l| l.sequence_number += 20 * 17_280); // 20 days
    assert_eq!(f.token.balance(&alice), 1_000 * T);
    assert_eq!(f.vault.claim_rent(&alice), 100 * T);
}

/// Invariant: whatever the sequence of transfers, deposits and claims, holders can never be owed
/// more than was deposited, and at most a few stroops of rounding dust are left unassigned.
#[test]
fn never_owes_more_than_was_deposited() {
    let f = fixture(100_000);
    let holders = [f.investor(7_000), f.investor(13_000), f.investor(333), f.investor(21_111), f.investor(5)];
    let mut seed: u64 = 0x9E37_79B9_7F4A_7C15;
    let mut next = || {
        seed = seed.wrapping_mul(6364136223846793005).wrapping_add(1442695040888963407);
        (seed >> 33) as i128
    };
    let mut deposited: i128 = 0;
    let mut claimed: i128 = 0;
    let mut period = 0u32;
    let mut checkpoints: i128 = 0;
    for step in 0..120 {
        match next() % 4 {
            0 | 1 => {
                let from = &holders[(next() % 5) as usize];
                let to = &holders[(next() % 5) as usize]; // may equal `from`
                let bal = f.token.balance(from);
                if bal > 0 {
                    let amount = 1 + next() % bal;
                    f.token.transfer(from, to, &amount);
                    checkpoints += 2;
                }
            }
            2 => {
                period += 1;
                let amount = 1 + next() % (5_000 * T);
                f.vault.deposit_net_rent(&period, &amount, &hash(&f.env, period as u8));
                deposited += amount;
            }
            _ => {
                let who = &holders[(next() % 5) as usize];
                if f.vault.claimable(who) > 0 {
                    claimed += f.vault.claim_rent(who);
                    checkpoints += 1;
                }
            }
        }
        let owed: i128 = holders.iter().map(|h| f.vault.claimable(h)).sum::<i128>()
            + f.vault.claimable(&f.treasury);
        assert!(owed + claimed <= deposited, "step {step}: owed {owed} + claimed {claimed} > deposited {deposited}");
    }
    let owed: i128 = holders.iter().map(|h| f.vault.claimable(h)).sum::<i128>() + f.vault.claimable(&f.treasury);
    let dust = deposited - owed - claimed;
    std::println!("deposited {deposited} stroops, claimed {claimed}, still owed {owed}, rounding dust {dust}, checkpoints ~{checkpoints}");
    assert!(dust >= 0 && dust <= checkpoints + holders.len() as i128 + 2, "unexpected dust {dust}");
    assert_eq!(f.usdc.balance(&f.id), deposited - claimed, "the contract holds exactly deposits minus claims");
    let supply: i128 = holders.iter().map(|h| f.token.balance(h)).sum::<i128>() + f.token.balance(&f.treasury);
    assert_eq!(supply, 100_000 * T, "token supply is conserved");
}
```

</details>

## Anexo C. Qué cambió respecto del original

Los números de página se refieren a la exportación de Gemini de 18 páginas.

| Página | Original | Esta edición | Sección |
|---|---|---|---|
| 1 | Alcance: una fracción de un solo edificio | Edificios, proyectos en construcción y terrenos sin construir, un token por inmueble o un portafolio. Agrega la valoración de terrenos, la reserva de costos de tenencia, las verificaciones de título, el pago al salir y la regla para agregar inmuebles | 1.6, 1.7, 6.3 |
| 1 | NAV dividido entre los tokens "circulantes" | Dividido entre los tokens en circulación, incluidos los de las cuentas distribuidora y escrow | 1.3 |
| 1 | Precio de lanzamiento de $10 a partir de $1,050,000 entre 105,000 tokens, sin mencionar un costo de entrada | NAV del día 1 de $9.6190, es decir, un costo de entrada de $0.3810 (3.81%) que debe informarse. El impuesto a la transferencia es del vendedor | 1.2 |
| 1 | Tokens atados a metros cuadrados como modelo | No recomendado. Lo que importa es el porcentaje del patrimonio | 1.1 |
| 2 a 4 | Un activo clásico con su SAC, negociación en el DEX y un contrato con saldos en almacenamiento privado | Un solo modelo de token por despliegue. Se recomienda el Modelo A, con los titulares sin autorizar entre transacciones de la plataforma | 2.2, 2.3 |
| 3 a 4 | Una sola transacción atómica desde el pago local hasta los tokens | Solo el último paso, USDC por tokens, es atómico. La rampa tiene varios tramos | 3.3 |
| 4 | El USDC de las compras va a la "Property Distribution Vault" | Cuentas separadas de escrow y de distribución | 3.4 |
| 4 | "Muros de liquidez" de la tesorería alrededor del NAV en el DEX | Eliminados. Sin mercado abierto en v1, y ventanas de rescate con tope si la ley lo permite | 5.4 |
| 5 | Cascada con crédito fiscal cero y una reserva de retención dentro del monto común | Crédito fiscal de los costos facturados, retención por titular, IT compensado en régimen estable, provisión de IUE verificada | 4.1, 4.2 |
| 5 | Ejemplo con 100,000 tokens y 0.35 BOB por token | Un solo ejemplo de referencia con 105,000 tokens: 36,365 BOB en el año 1 y 38,465 en régimen estable | 4.2 |
| 6 | El IMT se aplica cuando el edificio entra al SPV o se vende | Lo debe el vendedor (el propietario registrado). La primera venta de construcción nueva paga IT. La base es el mayor entre el precio y el valor del IPBI | 1.2, 4.3 |
| 6 | IPBI por el RUAT con un descuento por pronto pago de 10% a 15% | Se paga al municipio. Los descuentos varían según el municipio y el año (Cochabamba: 15% y luego 10% en 2026) | 4.3 |
| 6 | El Urubó como parte de Santa Cruz de la Sierra | El Urubó está en el municipio de Porongo, que cobra sus impuestos a la propiedad | 4.3, 4.4 |
| 7 | La brecha entre el tipo oficial y el paralelo como principal riesgo cambiario | El boliviano flota desde el 29 de junio de 2026. BCB: Bs 11.67 el 14 de noviembre. La brecha es de cerca de 1%. El riesgo es la volatilidad | 4.5 |
| 7, 18 | Un token atado al BOB (página 7) y `get_pollar_exchange_rate()` (página 18) | Ninguno existe en el SDK. Use el tipo del BCB y `quoteRamp()`. Hay que confirmar la ruta de BOB a Stellar | 3.2 |
| 8 | S.R.L. frente a fideicomiso, sin una sección regulatoria | El perímetro (Constitución, Ley 1834, DS 5384, Ley 393 art. 486, UIF), cinco opciones de estructura que incluyen un fondo cerrado, preguntas para la asesoría legal | 5 |
| 9 | La depreciación protege el flujo de caja, "que se puede distribuir" | La depreciación reduce la utilidad, no el efectivo. Los pagos sobre derechos patrimoniales suelen limitarse a la utilidad efectiva y líquida después de la reserva legal | 4.3 |
| 9 | El IUE pagado compensa el IT durante "todo el año siguiente" | El crédito empieza el mes siguiente al pago, y más depreciación implica menos IUE y menos crédito. DS 5327 y DS 5563 | 4.3 |
| 9 | "ELFEC/AXS" para la energía de áreas comunes | CRE y SAGUAPAC atienden a Santa Cruz. Ninguna fuente ubica a AXS en ninguna de las dos ciudades | 4.4 |
| 9 a 15 | El contrato del vault | Autotransferencias que crean tokens, inicialización sin autorización, sin forma de aprobar el KYC, no es SEP-41, depósitos repetibles, NAV opaco. Reemplazado por el Anexo B | 6.2, Anexo B |
| 15 | El contrato es "seguro ante transferencias" | Refutado en el entorno de pruebas de Soroban: 1,000 tokens se convirtieron en 32,000 | 6.2 |
| 15 | `ipfs_hash: BytesN<32>` | Se guarda un SHA-256 del informe. El identificador de contenido va en el evento o en el informe | 6.2 |
| 16 | Rendimientos netos de 9.2% (Equipetrol) y 6.8% (Cochabamba) | 3.56% en el año 1 (SC-1) y 2.34% (CB-1), calculados con la cascada | 7.4 |
| 16 | Un optimizador tipo mochila (knapsack) para tres edificios | Plantillas basadas en reglas | 7.2 |
| 17 | $1,400 más $200 al mes, $3,800 en 12 meses. Opción A: 396 tokens, unos $356 al año | 14,000 BOB más 2,000 BOB al mes a 11.67 son $3,256.21. Opción A: 333.35 tokens, $77.33 de alquiler recibido en el año, ritmo anual de unos $118.72 | 7.4 |
| 17 | Opción C: una "ganancia patrimonial inmediata" de $950 | Una proyección: $586.12 con +18%, con escenarios sin cambio y de -10%. Sin alquiler durante la construcción | 7.4 |
| 18 | El contrato libera tokens en cuanto se acredita un pago | La plataforma libera los tramos, porque la cadena no puede ver un pago en BOB. Informe el supuesto de confianza | 7.5 |

## Anexo D. Fuentes

Cada regla de este documento se leyó en la fuente indicada. Las fuentes secundarias están señaladas en el texto. El contenido de las fuentes se parafraseó.

**Stellar y Soroban**

- [Panorama y comparación de activos](https://developers.stellar.org/docs/tokens/anatomy-of-an-asset) (activos clásicos y SAC, SEP-41, SEP-57, compatibilidad con el DEX)
- [SEP-8, activos regulados](https://github.com/stellar/stellar-protocol/blob/master/ecosystem/sep-0008.md)
- [Guía de clawback](https://developers.stellar.org/docs/build/guides/transactions/clawbacks)
- [Lista de operaciones](https://developers.stellar.org/docs/learn/fundamentals/transactions/list-of-operations) (umbrales; `AUTH_CLAWBACK_ENABLED` requiere `AUTH_REVOCABLE`)
- [Migración de Horizon a RPC](https://developers.stellar.org/docs/data/apis/migrate-from-horizon-to-rpc) y el [panorama de APIs](https://developers.stellar.org/docs/data/apis) (Horizon cerca del fin de su vida útil)
- [Token RWA de OpenZeppelin para Stellar](https://docs.openzeppelin.com/stellar-contracts/tokens/rwa/rwa)
- [Ejemplo de token de Soroban](https://github.com/stellar/soroban-examples/tree/main/token) (la referencia SEP-41 que usa el Anexo B)
- [Recetario de la CLI de Stellar, gestión de activos](https://developers.stellar.org/docs/tools/cli/cookbook/asset-management) (códigos de activo de 1 a 12 caracteres)

**Pollar**

- [pollar-xyz/pollar](https://github.com/pollar-xyz/pollar): README, CHANGELOG, `docs/adr/0001-ramp-api-and-lifecycle.md`, `docs/design/dev-8-ramp-adapter-and-user-experience.md` (SDK 0.12.1, Apache-2.0)

**Tipo de cambio**

- [Tabla de cotizaciones del BCB, 14 de noviembre de 2026](https://www.bcb.gob.bo/librerias/indicadores/otras/ultimo.php)
- [Infobae, 29 de junio de 2026](https://www.infobae.com/america/america-latina/2026/06/29/bolivia-flexibiliza-el-dolar-y-cierra-una-era-de-15-anos-de-tipo-de-cambio-fijo/)
- Red Uno: [tipo para el 24 de septiembre de 2026](https://www.reduno.com.bo/economia/nueva-escalada-oficial-el-bcb-eleva-el-tipo-de-cambio-del-dolar-para-este-jueves-2026923202122) y [7 de noviembre de 2026](https://www.reduno.com.bo/economia/a-cuanto-esta-el-dolar-en-bolivia-este-sabado-7-de-noviembre-asi-cotizan-el-cambio-oficial-y-paralelo-202611773627)
- [dolarbolivia.net](https://dolarbolivia.net/) (sitio de seguimiento de cotizaciones, brecha de 1.1% el 10 de noviembre)

**Normas y tributos de Bolivia**

- [Ley 1834, del Mercado de Valores](https://www.lexivox.org/norms/BO-L-1834.xhtml), [DS 5384](https://www.lexivox.org/norms/BO-DS-N5384.xhtml) y [DS 24054](https://www.lexivox.org/norms/BO-DS-24054.xhtml) (reproducciones de Lexivox, no la Gaceta Oficial)
- [Constitución de 2009, copia de la OEA](http://www.oas.org/juridico/PDFs/mesicic4_blv_const.pdf) (arts. 262, 315, 331, 394, 395, 397, 400 y 401)
- [Ley 477 contra el avasallamiento y tráfico de tierras](https://www.lexivox.org/norms/BO-L-N477.xhtml)
- [INRA, saneamiento de la propiedad agraria](https://www.inra.gob.bo/saneamiento-de-la-propiedad-agraria/) y [Los Tiempos sobre las verificaciones del INRA para compradores](https://www.lostiempos.com/actualidad/cochabamba/20220306/instan-verificar-6-requisitos-evitar-estafas-avasalladores)
- [boliviaimpuestos.com, compensación de pérdidas](https://boliviaimpuestos.com/resultados-acumulados-negativos-contabilidad/) (reproduce la RND 10-0008-13 del SIN sobre el art. 48 de la Ley 843)
- [ASFI, actividad financiera ilegal](https://asfi.gob.bo/pb/y-donde-denunciar-actividad-financiera-ilegal) (Ley 393, art. 486)
- [Preguntas frecuentes del SIN sobre retenciones](https://www.impuestos.gob.bo/wp-content/uploads/2025/10/PREGUNTAS-RETENCIONES-25.pdf)
- [YoFacturo, impuestos sobre el alquiler](https://yo-facturo.com/blog/impuestos-alquiler-inmuebles-bolivia/) (secundaria)
- [boliviaimpuestos.com, compensación IUE-IT](https://boliviaimpuestos.com/decreto-supremo-nuevo-marco-normativo-para-la-compensacion-iue-it/) (secundaria, fuente del DS 5327 y del DS 5563)
- [Infoleyes, Código de Comercio art. 169](https://bolivia.infoleyes.com/articulo/26270) y [anexo de depreciaciones del DS 24051](https://bolivia.infoleyes.com/articulo/7033) (reproducciones secundarias)
- [PriFinance, licencias cripto en Bolivia](https://prifinance.com/es/licencia-de-intercambio-de-criptomonedas/bolivia/) (consultora de licencias, fuente secundaria sobre la Resolución 082/2024 del BCB)
- [Los Tiempos, descuentos de impuestos municipales en Cochabamba](https://www.lostiempos.com/actualidad/cochabamba/20260701/amplian-perdonazo-impuestos-municipales-cercado)
- [Metlabs, tokenización de activos en Bolivia](https://metlabs.io/en/tokenization/bolivia/) (un proveedor de tokenización, por lo tanto fuente secundaria)
- [Dentons, modificaciones al reglamento de fintech, 22 de mayo de 2026](https://www.dentons.com/en/insights/articles/2026/may/22/amendments-to-the-regulation-for-financial-technology-companies) (solo el resumen de búsqueda; la página no se pudo leer)
- [Preguntas frecuentes de BNB SAFI](https://www.bnb.com.bo/PortalSAFI/Principal/PreguntasFrecuentes), [Banco Unión SAFI](https://bancounion.com.bo/GrupoUnion/SAFIProductos) y [Fortaleza SAFI](https://www.fortalezasafi.com/productos/) (definiciones de fondos de inversión, supervisión de la ASFI)

**Datos de mercado y servicios**

- Numbeo: [Santa Cruz](https://www.numbeo.com/property-investment/in/Santa-Cruz-Bolivia) y [Cochabamba](https://www.numbeo.com/property-investment/in/Cochabamba), actualizados al 10 de noviembre de 2026. Datos aportados por usuarios.
- [Correo del Sur sobre CRE](https://correodelsur.com/sociedad/20250806/cre-un-orgullo-para-los-bolivianos.html) y [BNamericas sobre SAGUAPAC](https://www.bnamericas.com/es/perfil-empresa/cooperativa-de-servicios-publicos-santa-cruz-ltda)
- [eju.tv sobre Porongo y Colinas del Urubó](https://eju.tv/2024/10/la-hora-de-porongo-y-la-planificacion-urbana/)

**Método.** Las cifras de las secciones 1, 4 y 7 se calcularon con aritmética decimal a partir de los supuestos de la sección 0.3. El contrato del Anexo B se compiló y probó como allí se describe.
