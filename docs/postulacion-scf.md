# Umbral · Stellar Community Fund Build Award

## 1. Evidencia y acceso para el jurado

El jurado puede abrir el repositorio, la oferta, la evidencia de Casa Prado y los comprobantes en Stellar sin iniciar sesión. La compra de prueba con Pollar quedó confirmada el 22 de noviembre de 2026. [Evidencia pública](https://tokenizacion-inmobiliaria.vercel.app/evidencia-stellar/casa-prado) · [Repositorio público](https://github.com/DiegoOrellanaUS/TokenizacionInmobiliaria) · [Oferta de Casa Prado](https://tokenizacion-inmobiliaria.vercel.app/ofertas/32d2ac6f-f288-4dc9-b226-66a2379a518f)

| Evidencia | Qué comprobar | Enlace |
| --- | --- | --- |
| Activo clásico en Stellar Testnet | Código `INM001` y emisor `GBWYOSVB6RZG7Y7GCJW5RGN5PKSBMKQYSRDEOECYLB725PEEO7D5Y6IG` | [Activo exacto](https://stellar.expert/explorer/testnet/asset/INM001-GBWYOSVB6RZG7Y7GCJW5RGN5PKSBMKQYSRDEOECYLB725PEEO7D5Y6IG) |
| Emisión | Pago de 500 unidades desde el emisor a la distribuidora, ledger 5841722 | [Transacción](https://stellar.expert/explorer/testnet/tx/f299ad655df7a6bf485fd4171cc8b9c0bf7eb9d2294b5b2f6d1833b7bcf24910) · [Operaciones en Horizon](https://horizon-testnet.stellar.org/transactions/f299ad655df7a6bf485fd4171cc8b9c0bf7eb9d2294b5b2f6d1833b7bcf24910/operations) |
| Pago con Pollar | 1 USDC de Circle hacia la cuenta receptora, ledger 5.865.992. | [`5de7b12de80bbffa942dcdee78ca5f431e6ccab1b6f98710b7ca7a91b7b12377`](https://stellar.expert/explorer/testnet/tx/5de7b12de80bbffa942dcdee78ca5f431e6ccab1b6f98710b7ca7a91b7b12377) · [Operaciones en Horizon](https://horizon-testnet.stellar.org/transactions/5de7b12de80bbffa942dcdee78ca5f431e6ccab1b6f98710b7ca7a91b7b12377/operations) |
| Entrega de la compra con Pollar | 1 INM001 desde la distribuidora original a la billetera de portafolio del comprador, ledger 5.865.996. | [`31460812d39afe13136d1d7362dd1d2bedba480ddcc6390483aabe270fcbad44`](https://stellar.expert/explorer/testnet/tx/31460812d39afe13136d1d7362dd1d2bedba480ddcc6390483aabe270fcbad44) · [Operaciones en Horizon](https://horizon-testnet.stellar.org/transactions/31460812d39afe13136d1d7362dd1d2bedba480ddcc6390483aabe270fcbad44/operations) |

La orden de Umbral es `eddf51d9-928d-4e4d-8c01-d3c1a99d5a24`. En Stellar Testnet el pago quedó confirmado a las 09:20:01 de Bolivia y la entrega a las 09:20:21. En Horizon, comprueba el importe, el código y el emisor en la operación `payment` de cada transacción. En la entrega, el código y el emisor coinciden con los de la emisión, y la cuenta de origen coincide con la distribuidora que recibió las 500 unidades. El pago usa el USDC de Circle de Testnet y lleva el memo `2VHf7Y2STU6MAdPBqZ1aJA`, vinculado a la orden.

La página pública conserva también la entrega de la prueba anterior, que simulaba el cobro. Los dos comprobantes de esta compra aparecen bajo **Compra con Pollar en Stellar Testnet**. [Evidencia pública](https://tokenizacion-inmobiliaria.vercel.app/evidencia-stellar/casa-prado)

## 3. Tracción y plan de validación

| Indicador | Evidencia disponible al 22 de noviembre de 2026 | Meta propuesta |
| --- | --- | --- |
| Flujo de emisión, pago y entrega | Una oferta pública de demostración, una emisión de 500 INM001 y una compra controlada con dos operaciones confirmadas en Testnet. Pollar envió 1 USDC de prueba y Umbral entregó 1 INM001. | Cinco compras de prueba confirmadas, cada una con su referencia y comprobantes en Stellar, durante el piloto de Testnet. |
| Organizaciones usuarias | Sin organizaciones piloto, acuerdos ni clientes acreditados en el material disponible. | Dos organizaciones externas con contraparte identificada y al menos un expediente de prueba completado por organización entre las semanas 6 y 11. |
| Compradores externos | Sin usuarios externos acreditados en el material disponible. | Cinco participantes externos completan una compra de prueba. Cada sesión queda registrada con un identificador y resultado. |
| Uso en Mainnet | Sin actividad acreditada en el material disponible. | Diez transferencias de entrega confirmadas a cinco billeteras externas distintas en los primeros 30 días de un piloto habilitado para operar. |

El cliente objetivo inicial es el operador inmobiliario que gestiona expedientes, revisiones y ofertas para varios inmuebles. En el piloto, cada operador recorre el flujo de alta y revisión de un inmueble y observa una compra de prueba. Para medir adopción, conserva fecha, organización o participante seudonimizado, paso completado, dificultad observada y referencia de evidencia. Cuenta cada organización una vez y excluye al equipo, cuentas de demo, compras canceladas y reintentos. Publica solo resultados agregados y hashes cuya divulgación corresponda. Usa la tasa de finalización del piloto y el tiempo mediano entre expediente completo y revisión resuelta para evaluar si el flujo le ahorra trabajo al operador.

Para la meta de Mainnet, registra las cuentas emisoras y distribuidoras del piloto. Reconcilia las órdenes entregadas con operaciones `payment` exitosas del activo exacto. Deduplica por hash e ID de operación, excluye cuentas operadas por el equipo y reporta el período, el número de entregas y las billeteras externas únicas. El volumen y la adopción comercial se reportan cuando exista evidencia. [Requisitos de la propuesta Build](https://stellar.gitbook.io/scf-handbook/scf-awards/build-award)

## 4. Integración técnica, entregables y presupuesto

El MVP usa activos clásicos de Stellar. Tras aprobar el expediente, Tokenización prepara el emisor y la distribuidora, crea la línea de confianza cuando hace falta y emite mediante una operación `payment`. Para comprar Casa Prado, Comercio reserva el INM001 y el comprador autoriza con Pollar un pago en USDC de prueba. Tokenización verifica en Horizon el emisor de USDC, el importe, el pagador, el destino y el memo de la orden. Comercio registra el hash del pago una sola vez. Solo con ese pago verificado, Tokenización envía 1 INM001 desde la distribuidora a la billetera de portafolio del comprador. Comercio confirma la orden y muestra ambos hashes y sus ledgers. [Flujo de Pollar](pollar-casa-prado.md) · [Verificador](../Backend/Tokenizacion/Servicios/VerificarPagoPollar.cs) · [Liquidación de compras](../Backend/ComercioInversion/Data/LiquidarComprasDemo.cs)

```text
Expediente aprobado → Tokenización → Emisor → Distribuidora en Stellar Testnet
Reserva en Comercio → Pago USDC con Pollar → Verificación en Horizon por Tokenización
Pago verificado → Distribuidora → Entrega de INM001 en Stellar Testnet
Orden confirmada en Comercio → Hash y ledger del pago y de la entrega
```

**Alcance propuesto.** Dieciséis semanas de desarrollo desde el inicio del proyecto. El piloto de Mainnet depende de contar con un operador participante, un modelo de custodia aprobado para el piloto y la autorización aplicable para operar. La medición cubre los 30 días posteriores a la habilitación y puede continuar después de esas dieciséis semanas. Los importes y la disponibilidad del equipo se deben confirmar antes de usar esta propuesta en el formulario.

| Tramo y plazo | Trabajo por financiar | Criterio de aceptación | Costo de trabajo |
| --- | --- | --- | ---: |
| 1 · MVP verificable · semanas 1–5 | Registrar estados y comprobantes de entrega, endurecer reintentos de compra y conciliación; cubrir cancelación, fallo y respuesta duplicada. | Una instancia limpia permite emitir y entregar en Testnet. Diez reintentos controlados de una compra generan una única entrega confirmada. La cancelación previa a la transferencia restituye el inventario una sola vez. | USD 9 000 · 110 h de ingeniería × 60 + 60 h de QA y documentación × 40 |
| 2 · Piloto Testnet · semanas 6–11 | Instrumentar el recorrido del operador y del comprador y publicar un informe de pruebas con conciliación por hash. | Dos operadores externos completan el recorrido acordado. Cinco participantes externos completan una compra de prueba cada uno. Las cinco compras quedan vinculadas a operaciones confirmadas en Testnet. | USD 9 000 · 120 h de ingeniería × 60 + 45 h de QA y documentación × 40 |
| 3 · Piloto Mainnet · semanas 12–16 | Habilitar y monitorear un despliegue limitado, preparar el manejo de llaves y publicar la guía de operación y el registro de conciliación. | Despliegue verificable y, como meta, diez entregas a cinco billeteras externas durante los primeros 30 días del piloto. La ventana de medición comienza con el piloto habilitado. | USD 12 000 · 160 h de ingeniería × 60 + 60 h de QA y documentación × 40 |

**Solicitud orientativa de USD 30 000.** La estructura de pagos del SCF se aplica así: USD 3 000 al aceptar (10 %, adelanto aplicado al trabajo del primer tramo), USD 6 000 al completar el tramo 1 (20 %), USD 9 000 al completar el tramo 2 (30 %) y USD 12 000 al completar el tramo 3 (40 %). El presupuesto se destina a desarrollo futuro, pruebas y documentación técnica. [Guía oficial de presupuesto y entregables](https://stellar.gitbook.io/scf-handbook/scf-awards/build-award/budget-and-deliverable-guidelines)

**Pruebas.** La suite actual de Playwright incluye el recorrido de registro, revisión, emisión, publicación y compra en Testnet. Ejecútala contra un entorno de pruebas configurado porque crea datos y realiza operaciones de Testnet. El plan añade casos de cancelación, caída tras el envío, reintento y aislamiento entre tenants, con una comprobación de la orden y el ledger para cada caso. [Suite E2E](../Frontend/tests/mvp.spec.ts) · [Flujo reproducible de inmuebles](flujo-inmuebles.md) · [Borrador de arquitectura futura](arquitectura-del-sistema.md)

## 5. Modalidad, comparación y divulgación de IA

**Modalidad propuesta para revisión del equipo: Open Track.** La hipótesis de Umbral es reducir el trabajo de los operadores inmobiliarios al vincular el expediente, la revisión y la entrega en Stellar en un flujo por organización. El piloto debe medir la mejora y demostrar qué valor aporta a Stellar. La elección definitiva depende de la invitación del SCF y de la evidencia del equipo sobre su experiencia en el dominio. [Open Track](https://stellar.gitbook.io/scf-handbook/scf-awards/build-award/open-track)

| Solución | Enfoque de negocio | Enfoque técnico |
| --- | --- | --- |
| Umbral | Flujo para operadores inmobiliarios, con expediente, revisión por rol, publicación y entrega al comprador. Su mercado inicial se validará con operadores piloto. | Servicios .NET con datos por tenant, activos clásicos y pago con Pollar verificado antes de la entrega en Stellar Testnet. [Flujo de inmuebles](flujo-inmuebles.md) · [Compra verificable](https://tokenizacion-inmobiliaria.vercel.app/evidencia-stellar/casa-prado) |
| RedSwan | Mercado de valores digitales inmobiliarios enfocado en bienes inmuebles comerciales. | Oferta de propiedades comerciales tokenizadas en Stellar. [Publicación de Stellar Development Foundation](https://stellar.org/blog/foundation-news/redswan-cre-brings-tokenized-real-estate-to-the-stellar-network) |
| Lofty | Mercado de inversión fraccionada en propiedades de alquiler de EE. UU., con rentas y reventa. | Negociación con contratos inteligentes de Algorand y USD Lofty. [Documentación de Lofty](https://www.lofty.ai/how-it-works) |

La hipótesis se contrasta midiendo el tiempo de revisión y el porcentaje de expedientes completados por dos operadores independientes. Para cada operador se registra como referencia un recorrido equivalente con su proceso habitual, usando expedientes de complejidad comparable. El aporte a Stellar se medirá a través de entregas confirmadas, billeteras externas y emisores atribuibles al piloto.

**Divulgación de IA.** Se utilizó OpenAI Codex para editar el dossier y los README, preparar la página de evidencia y asistir en el checkout con Pollar, el verificador de Horizon y los enlaces de Stellar. Los comprobantes de esta propuesta se contrastaron con los registros públicos de Stellar Testnet. El equipo debe completar el inventario con cualquier otro código, documentación, diseño, imagen o video generado o asistido por IA que incluya en la postulación. [Criterio de divulgación del Open Track](https://stellar.gitbook.io/scf-handbook/scf-awards/build-award/open-track)

**Si la invitación indica Integration Track.** El candidato es Freighter Connect, que aparece en la lista oficial. La propuesta es permitir que el comprador conecte su propia billetera, verifique su red y autorice la línea de confianza desde ella; la entrega se conciliará con esa dirección. Esta integración está propuesta. Antes de postular por esa vía, se requiere acreditar tracción previa y rehacer el presupuesto para que la integración concentre la mayor parte del gasto. El umbral inicial a discutir con el panel es diez entregas exitosas a cinco billeteras externas en 30 días de Mainnet, atribuidas a los emisores y las distribuidoras del piloto. [Lista oficial de integraciones](https://stellar.gitbook.io/scf-handbook/scf-awards/build-award/integration-track/integration-list) · [Integration Track](https://stellar.gitbook.io/scf-handbook/scf-awards/build-award/integration-track)

## Texto para el formulario

**Solución y rol de Stellar.** Umbral ayuda a operadores inmobiliarios a revisar expedientes, publicar ofertas y entregar tokens al comprador. Casa Prado emitió 500 INM001 como activo clásico de Stellar. En la compra de prueba, Pollar envió 1 USDC en Stellar Testnet. Umbral verificó el pago en Horizon antes de transferir 1 INM001 del emisor original a la billetera de portafolio del comprador. El portafolio muestra los dos hashes de la misma orden. [Código público](https://github.com/DiegoOrellanaUS/TokenizacionInmobiliaria) · [Evidencia pública](https://tokenizacion-inmobiliaria.vercel.app/evidencia-stellar/casa-prado) · [Pago en Stellar](https://stellar.expert/explorer/testnet/tx/5de7b12de80bbffa942dcdee78ca5f431e6ccab1b6f98710b7ca7a91b7b12377) · [Entrega en Stellar](https://stellar.expert/explorer/testnet/tx/31460812d39afe13136d1d7362dd1d2bedba480ddcc6390483aabe270fcbad44)

**Tracción.** La evidencia pública actual es una demostración técnica en Testnet con una oferta pública, un pago de prueba con Pollar y la entrega verificable del INM001. Proponemos un piloto con dos operadores y cinco compradores externos que completen el recorrido en Testnet. Para un piloto habilitado en Mainnet, la meta inicial es diez entregas confirmadas a cinco billeteras externas en 30 días.

**Plan y presupuesto.** Proponemos USD 30 000 para dieciséis semanas de desarrollo futuro. Los tres entregables son el MVP verificable con pruebas de recuperación y cancelación, un piloto externo en Testnet con conciliación de compras y un piloto limitado en Mainnet con medición de entregas. La solicitud y las metas quedan sujetas a confirmación del equipo.
