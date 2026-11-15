# Información de otros mercados: tokenización inmobiliaria en el exterior

Lo que plataformas, reguladores y tribunales de otros países enseñan al diseño boliviano

| | |
|---|---|
| Estado | Borrador para revisión del equipo |
| Versión | 1.0, 2026-11-15 |
| Complementa a | [metodos-de-valoracion-de-tokens-inmobiliarios.md](metodos-de-valoracion-de-tokens-inmobiliarios.md), versión 2.2 ("el documento de diseño") |
| Cobertura | Estados Unidos, Emiratos Árabes Unidos (Dubái), Brasil, Argentina, El Salvador, España, México y Bolivia. Fuentes con fecha hasta noviembre de 2026 |
| Aviso | Es un resumen de investigación, no asesoría legal, tributaria ni de inversión. Las cifras son las que reportan las fuentes citadas. Las afirmaciones de empresas y de competidores están marcadas como tales. |
| Idioma | Español. Versión en inglés: [information-from-other-markets.md](information-from-other-markets.md) |

## Cómo leer este documento

- La sección 1 enumera las lecciones. La sección 2 dimensiona el mercado. La sección 3 recorre caso por caso, y cada caso termina con lo que significa para nuestro diseño. La sección 4 resume estudios sobre la liquidez de reventa. La sección 5 propone cambios al documento de diseño para la revisión de diseño (Design Review Gate). La sección 6 lista las fuentes.
- "Diseño 1.6" significa la sección 1.6 del [documento de diseño](metodos-de-valoracion-de-tokens-inmobiliarios.md). P1 a P8 son las propuestas de la sección 5.
- Cada cifra nombra su fuente. Una cifra que una plataforma publica sobre sí misma es una cifra de la empresa. Una cifra de una plataforma rival es la versión de un competidor. La sección 6 clasifica cada fuente por tipo.
- Las cifras corresponden a la fecha de cada fuente y no fueron auditadas. Los montos se mantienen en la moneda de la fuente, y "$" significa dólares estadounidenses. Los números usan punto decimal y coma para los miles, como el documento de diseño.

## 1. Lecciones en breve

| # | Lección | Evidencia | Propuesta |
|---|---|---|---|
| L1 | La falla más conocida vino del operador, no de la blockchain | Un tribunal de Detroit declaró muchos inmuebles de RealT un perjuicio público (public nuisance) y envió los alquileres a una cuenta de garantía. Los impuestos quedaron sin pagar, los pagos se detuvieron y las estructuras de EE. UU. están en liquidación (sección 3.1) | P1, P2 |
| L2 | Los inversionistas no podían verificar el título, los gravámenes ni los impuestos | RealT vendió participaciones en casi 100 inmuebles sin propiedad registrada clara y tomó hipotecas que había dicho que no existían (sección 3.1) | P3 |
| L3 | Los rendimientos altos anunciados son una señal de alerta | RealT promocionaba del 6% al 16% anual (sección 3.1). Un estudio encuentra que los mayores rendimientos de los tokens vienen sobre todo de elegir segmentos inmobiliarios de alto rendimiento (sección 4). El rendimiento neto en efectivo del diseño para SC-1 es 3.56% en el año 1 (diseño 4.2) | P4 |
| L4 | El registro de la propiedad decide cuánto puede acercarse un token al título | El registro de tierras de Dubái tiene su propio programa de tokens (sección 3.2). Tribunales estatales de Brasil prohíben a los registros vincular títulos con tokens (sección 3.3) | P3 |
| L5 | Los modelos que funcionan están dentro del perímetro de un regulador | Dubái bajo la VARA (sección 3.2), el fideicomiso de campos de Argentina bajo la CNV (sección 3.4), las emisiones de El Salvador bajo la CNAD (sección 3.5) | P5 |
| L6 | La reventa sigue siendo escasa, aun con tokens | Estudios sobre RealT y otras plataformas (sección 4). México no tiene reventa organizada (sección 3.7) | P7 |
| L7 | Las cifras de mercado que se anuncian exageran lo tokenizado | Brasil: una cartera de proyectos de R$20 mil millones frente a R$40 a R$50 millones realmente tokenizados (sección 3.3). Los tokens inmobiliarios en blockchains públicas suman unos $213 millones en todo el mundo (sección 2) | P8 |
| L8 | Arrendar el terreno mientras se mantiene es una fuente de ingresos probada | Los campos de Landtoken generan ingresos por arrendamiento, pagados en soja en su primera etapa (sección 3.4) | P6 |
| L9 | Bolivia no tiene precedentes de tokenización | Ninguna de las 11 empresas habilitadas para ingresar al entorno de pruebas de la ASFI trabaja con activos tokenizados (sección 3.8) | P5 |

## 2. Tamaño del mercado

| Medida | Cifra | Fuente | Nota |
|---|---|---|---|
| Tokens inmobiliarios en blockchains públicas | $213.29 millones en 96 activos de 11 países, en manos de unas 17,560 billeteras, al 5 de noviembre de 2026. Dos plataformas, Groma ($95.2 millones) y Reental ($79.9 millones), concentran cerca del 82% | Datos de RWA.xyz citados por un proveedor ([Pixel Web Solutions](https://www.pixelwebsolutions.com/best-real-estate-tokenization-platforms/)) | La medida más estrecha: tokens que cualquiera puede ver on-chain |
| Todos los activos reales tokenizados | Unos $33.5 mil millones distribuidos on-chain al 8 de julio de 2026. Los bonos del Tesoro de EE. UU. tokenizados son la mayor parte, con $12.9 mil millones a $16.2 mil millones | [Crypto Briefing](https://cryptobriefing.com/rwa-tokenization-surges-five-sectors/), con datos de RWA.xyz | Lo inmobiliario es una porción pequeña |
| Pronóstico de Deloitte para inmuebles tokenizados | Más de $4 billones (millones de millones) para 2035, desde menos de $300 mil millones en 2024, cerca de 27% anual. De la cifra de 2035, $2.39 billones son préstamos y titularizaciones, $1 billón fondos inmobiliarios privados y $501 mil millones terrenos sin desarrollar y proyectos en construcción | Deloitte Center for Financial Services, abril de 2025, según [Cointelegraph](https://cointelegraph.com/news/real-estate-tokenization-forecast-4-trillion-2035) y [PaySpace Magazine](https://payspacemagazine.com/news/real-estate-worth-4t-to-be-tokenized-by-2035-deloitte/) | Sobre todo deuda y cuotas de fondos tokenizadas, no tokens por partes de un edificio |

El pronóstico y el conteo on-chain miden cosas distintas, así que no se pueden comparar. Para planificar, use el conteo on-chain y los resultados de las plataformas de la sección 3.

## 3. Casos

La tabla compara los casos. Las subsecciones dan los detalles y las fuentes.

| Caso | Activos | Qué recibe el titular | Supervisión | Situación |
|---|---|---|---|---|
| RealT, Estados Unidos | Casas de alquiler en Detroit | Participaciones en las sociedades dueñas de las casas, vendidas solo fuera de EE. UU. | Ofrecido bajo la Regulación S, según un competidor | Un fiduciario aprobado por el tribunal desde abril de 2026. Estructuras de EE. UU. en liquidación desde julio de 2026 |
| RedSwan, Estados Unidos | Inmuebles multifamiliares y hoteleros | Valores digitales | Un mercado regulado por FINRA, según la Stellar Development Foundation | $100 millones en Stellar, septiembre de 2025 |
| PRYPCO Mint, Dubái | Inmuebles en Dubái | Una participación fraccionaria en un inmueble específico | Un programa del registro de tierras (DLD). Plataforma con licencia de la VARA | Reventa abierta desde el 20 de febrero de 2026 |
| Landtoken, Argentina | Campos agrícolas | Valores de un fideicomiso financiero con oferta pública autorizada por la CNV | CNV. Allaria Fiduciaria S.A. como fiduciario | Fideicomiso público colocado desde diciembre de 2025 |
| Emisiones LEAD, El Salvador | Proyectos inmobiliarios y de construcción | Los derechos económicos que define cada emisión | CNAD | 55 emisiones por $633 millones a septiembre de 2026 |
| Reental, España | Inmuebles y crédito inmobiliario | Una fracción de un inmueble o de un derecho de crédito | No figura en el registro de financiación participativa de la CNMV, según Reental | $79.9 millones en blockchains públicas, noviembre de 2026 (cifra de un proveedor) |
| Urbanitae, España | Proyectos inmobiliarios | Inversiones a través de una plataforma de financiación participativa registrada. Sin mercado secundario | Registrada en la CNMV | TIR promedio de 11.7% en proyectos devueltos (cifra de la empresa, reportada por Reental) |

### 3.1 Estados Unidos

#### RealT en Detroit

**Qué era.** RealT compraba casas de alquiler en Detroit y vendía tokens por participaciones en las sociedades que las poseían, solo a inversionistas fuera de Estados Unidos ([Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf); [Outlier Media](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/)). Lofty, un competidor, dice que las ofertas se apoyaron en la Regulación S, la exención estadounidense para ventas hechas fuera del país ([Lofty](https://www.lofty.ai/blog/realt-liquidation-explained)). Los tokens funcionaban en las blockchains públicas Ethereum y Gnosis Chain ([Cointribune](https://www.cointribune.com/en/realt-revolutionizes-real-estate-investment-with-blockchain/)). RealT vendió unos $94 millones en tokens sobre cerca de 700 inmuebles de Detroit ([Outlier Media](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/)) a unos 22,000 inversionistas ([Reiss, Bizub y Peralta](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=7220758)). La demanda de Detroit dice que RealT promocionaba rendimientos anuales del 6% al 16% por alquileres ([Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf)). Lofty dice que los rendimientos anunciados a menudo superaban el 10%.

**Qué salió mal.** La demanda de Detroit describió unidades ocupadas inseguras, cientos de casas vacías sin asegurar, alquileres sin los certificados de cumplimiento que exige la ciudad, y multas e impuestos impagos ([Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf)).

| Fecha | Hecho | Fuente |
|---|---|---|
| Febrero de 2021 | RealT toma una primera hipoteca de $458,000 sobre 18 inmuebles | [Outlier Media](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/) |
| Octubre de 2021 | Un cofundador dice a los inversionistas que no hay hipotecas sobre los inmuebles de RealT | [Outlier Media](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/) |
| 2025 | Las hipotecas llegan a casi $4.8 millones sobre 33 inmuebles, aunque los inversionistas habían pagado lo suficiente para comprarlos al contado. RealT vendió participaciones en casi 100 inmuebles en los que no demostró propiedad registrada clara, y 4 de ellos están a la venta por otras personas. 141 inmuebles están vacíos y la deuda tributaria supera $3.6 millones. Hay impuestos atrasados en 30 de los inmuebles hipotecados, que recibieron 571 multas por deterioro (blight tickets) desde que se tokenizaron | [Outlier Media](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/) |
| 22 de julio de 2025 | La jueza Annette J. Berry, del Tribunal de Circuito del condado de Wayne, concede a Detroit una orden de restricción temporal y declara muchos de los inmuebles un perjuicio público. RealT debe asegurar los inmuebles vacíos en 30 días y reparar las infracciones al código en los ocupados en 90 días. No puede cobrar alquileres ni desalojar inquilinos hasta que el inmueble tenga un certificado de cumplimiento, y los inquilinos depositan el alquiler en una cuenta de garantía (escrow) supervisada por la ciudad | [Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf) |
| 23 de julio de 2025 | La ciudad anuncia el caso: más de 400 inmuebles, los dos fundadores de RealT y 165 sociedades afiliadas. La cuenta de garantía solo puede usarse para reparaciones | [Ciudad de Detroit](https://detroitmi.gov/es/news/la-ciudad-obtiene-una-importante-victoria-para-los-inquilinos-que-viven-en-propiedades-de-real-token) |
| 23 de septiembre de 2025 | El tribunal convierte la orden en una medida cautelar preliminar | [Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf) |
| 20 de marzo de 2026 | Los pagos a los inversionistas casi se detuvieron por completo. Más de 300 inmuebles podrían terminar en ejecución por deudas tributarias. El administrador de inmuebles de RealT queda con cinco empleados | [Michigan Public](https://www.michiganpublic.org/social-justice/2026-03-20/the-model-no-longer-works-crypto-landlords-detroit-enterprise-is-crumbling) |
| 22 de abril de 2026 | Un acuerdo aprobado por el tribunal pone a un fiduciario, Charles Bullock, a cargo de unos 700 inmuebles. La cuenta de garantía tiene menos de $640,000 | [Lofty](https://www.lofty.ai/blog/realt-liquidation-explained) (competidor) |
| 2 de julio de 2026 | RealT anuncia la liquidación voluntaria de sus estructuras en EE. UU. | [Lofty](https://www.lofty.ai/blog/realt-liquidation-explained) (competidor) |
| 2026 | El estudio jurídico Delomel presenta una demanda colectiva en Francia por unos 14,000 inversionistas, y se presenta una denuncia penal en París | [Lofty](https://www.lofty.ai/blog/realt-liquidation-explained) (competidor) |

**Qué significa para nosotros.**

- El token no cambió quién era responsable de los edificios. Detroit usó el derecho ordinario de vivienda y de perjuicio público, sostuvo que la estructura de tokens era irrelevante para la responsabilidad, y el tribunal concedió su orden ([Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf)). Espere que las normas bolivianas municipales, tributarias y de alquileres se apliquen a nuestro vehículo de la misma manera.
- Cuando el operador falló, ninguna parte independiente pudo tomar el control. Un fiduciario llegó solo mediante un acuerdo judicial, nueve meses después de la orden de restricción (P1).
- La blockchain mostraba los saldos de tokens, no el estado del inmueble. Los titulares no podían ver impuestos impagos, hipotecas, inmuebles vacíos ni infracciones (P2, P3).
- Los rendimientos anunciados dependían de alquileres que luego un tribunal detuvo (P4).

#### RedSwan en Stellar

En septiembre de 2025 la Stellar Development Foundation anunció que RedSwan, a la que describe como un mercado de valores digitales regulado por FINRA, había llevado a Stellar $100 millones en inmuebles comerciales: inmuebles multifamiliares y hoteleros de nivel institucional ([Stellar](https://stellar.org/press/redswan-digital-real-estate-brings-usd100m-of-tokenized-real-estate-to-the-stellar-network)). Es un anuncio de la empresa. No da cifras de inversionistas ni datos de negociación.

**Qué significa para nosotros.** Ya hay valores inmobiliarios regulados en Stellar, vendidos a través de un intermediario regulado. Eso respalda la elección de red (diseño 2). No dice nada sobre qué modelo de token usa RedSwan.

### 3.2 Emiratos Árabes Unidos: el Dubai Land Department y PRYPCO Mint

**Qué es.** Un programa del registro de tierras de Dubái, el Dubai Land Department (DLD), con PRYPCO Mint como plataforma. La VARA, el regulador de activos virtuales de Dubái, otorga la licencia y supervisa la plataforma ([Gulf News, febrero de 2026](https://gulfnews.com/business/property/prypco-rolls-out-dubai-marketplace-after-nod-for-property-stakes-trading-1.500438076)).

- **Lanzamiento.** El DLD inició el piloto con la VARA en marzo de 2025 ([The National](https://www.thenationalnews.com/business/property/2026/02/09/dubai-property-tokenisation-fractional-investing/)). PRYPCO Mint abrió a los inversionistas en mayo de 2025, para residentes de los Emiratos con un Emirates ID, desde AED 2,000 por inversión ([Gulf News, mayo de 2025](https://gulfnews.com/your-money/saving-investment/how-to-invest-in-dubai-property-with-just-dh2000-1.500140848); [Invezz](https://invezz.com/es/noticias/2025/05/26/la-primera-plataforma-inmobiliaria-tokenizada-de-dubai-se-lanza-bajo-la-supervision-de-vara/)). Son unos $545 al tipo de cambio fijo del dírham, 3.6725 por dólar.
- **Demanda.** Para febrero de 2026 el piloto había recaudado más de AED 18.5 millones de inversionistas de más de 50 nacionalidades, y una oferta se completó en 1 minuto 58 segundos ([Gulf News, febrero de 2026](https://gulfnews.com/business/property/prypco-rolls-out-dubai-marketplace-after-nod-for-property-stakes-trading-1.500438076)).
- **Reventa.** Un mercado de reventa abrió el 20 de febrero de 2026 para unos 7.8 millones de tokens ([The National](https://www.thenationalnews.com/business/property/2026/02/09/dubai-property-tokenisation-fractional-investing/)).
- **Meta.** El DLD apunta a AED 60 mil millones ($16.33 mil millones) en inmuebles tokenizados para 2033, cerca del 7% de las transacciones inmobiliarias de Dubái ([The National](https://www.thenationalnews.com/business/property/2026/02/09/dubai-property-tokenisation-fractional-investing/)).
- **Comisiones.** Al entrar, PRYPCO Mint cobra una tasa del DLD de 2% en lugar de la tasa de transferencia estándar de 4%, más una comisión de plataforma de 2% y una comisión de tokenización de 0.1% con tope de AED 850 ([PRYPCO](https://prypco.com/blogs/real-estate-tokenization-fees-dubai-explained), empresa).

**Qué significa para nosotros.**

- En Dubái el propio registro de tierras patrocina el programa, así que el vínculo entre token e inmueble es oficial. Derechos Reales no tiene un programa así, por lo que nuestro token sigue siendo un derecho frente al vehículo, no una inscripción en el registro (diseño 5.3).
- Los costos de entrada son parecidos. Las comisiones de entrada de PRYPCO suman cerca de 4.1% del monto invertido. El costo de entrada del diseño es 3.81% del precio para SC-1 y 3.62% para LT-1 (diseño 1.2 y 1.6).
- La reventa abrió unos nueve meses después de que PRYPCO Mint abriera a los inversionistas, una vez que el regulador la aprobó. Eso coincide con la política del diseño para la v1: sin mercado abierto hasta que exista una vía autorizada (diseño 5.4, D5).

### 3.3 Brasil

**Mercado.** Exame reporta unas 450 desarrolladoras que trabajan con tokenización, cuyos proyectos suman un valor general de ventas (VGV) de unos R$20 mil millones. También cuenta 30,000 billeteras digitales y 9,000 inversionistas declarados a la autoridad tributaria federal. El stock de inmuebles realmente tokenizado es de solo R$40 a R$50 millones ([Exame](https://exame.com/future-of-money/tokenizacao-imobiliaria-alcanca-r-20-bilhoes-no-brasil/), 2026). Los R$20 mil millones son el valor de los proyectos de las desarrolladoras, no tokens vendidos.

**Registros.** Los registros de la propiedad de Brasil (`cartórios`) y los órganos de los tribunales estatales que los supervisan se han negado a vincular títulos con tokens.

- A pedido de los registradores, la Corregedoria Geral da Justiça de São Paulo, el órgano del tribunal estatal que supervisa los registros, emitió el Provimento CGJ 54/2025, publicado el 23 de enero de 2026. Prohíbe a los registradores hacer cualquier inscripción que vincule la ficha registral de un inmueble (`matrícula`) con un token digital o una representación en blockchain ([Valor Econômico](https://valor.globo.com/financas/criptomoedas/noticia/2026/01/27/em-vitria-dos-cartrios-corregedoria-de-so-paulo-probe-o-registro-de-tokens-imobilirios.ghtml); [Migalhas](https://www.migalhas.com.br/depeso/452304/cgj-sp-veda-tokenizacao-imobiliaria-e-edita-provimento-54-25)).
- Rio Grande do Sul, Santa Catarina, Mato Grosso do Sul y Espírito Santo emitieron normas similares ([Jota](https://www.jota.info/justica/corregedorias-de-justica-proibem-que-matriculas-de-imoveis-sejam-vinculadas-a-tokens-digitais)). La de Santa Catarina es del 18 de agosto de 2025 ([Coinspeaker](https://www.coinspeaker.com/pt/santa-catarina-veta-registros-de-imoveis-na-blockchain/)).
- El consejo federal de corredores inmobiliarios (Cofeci) creó un sistema de transacciones inmobiliarias digitales con la Resolución 1.551/2025 en agosto de 2025. El 13 de octubre de 2025, el 21.º Juzgado Federal Civil de Brasilia la suspendió a pedido del operador nacional del registro electrónico de inmuebles (ONR), por considerar que creaba un registro paralelo sin base legal ([Valor Econômico](https://valor.globo.com/financas/criptomoedas/noticia/2025/10/13/justia-suspende-resoluo-do-cofeci-sobre-tokenizao-imobiliria.ghtml); [Migalhas](https://www.migalhas.com.br/quentes/442418/token-juiz-suspende-resolucao-sobre-transacoes-imobiliarias-digitais)).

**Valores.** El regulador de valores, la CVM, publicó el Parecer de Orientação 40 el 11 de octubre de 2022. Expone la visión de la CVM sobre las normas que se aplican a los criptoactivos que son valores ([CVM](https://www.gov.br/cvm/pt-br/assuntos/noticias/2022/cvm-divulga-parecer-de-orientacao-sobre-criptoativos-e-o-mercado-de-valores-mobiliarios)).

**Qué significa para nosotros.**

- Un registro puede negarse a reconocer los tokens. El diseño ya mantiene el título a nombre del vehículo en Derechos Reales y hace del token un derecho frente al vehículo (diseño 5.3). Hay que mantenerlo así, y nunca promocionar el token como propiedad registrada.
- Mida el mercado por tokens vendidos y dinero recaudado, no por carteras de proyectos (P8).
- En Brasil la visión del regulador de valores decide cuándo un token es un valor. El equivalente boliviano es la pregunta sobre la Ley 1834 que ya está en la lista para la asesoría legal (diseño 5.5, pregunta 1).

### 3.4 Argentina

**Normas.** El regulador de valores, la Comisión Nacional de Valores (CNV), armó por etapas un régimen para tokenizar activos reales, y publicó cada etapa en el Boletín Oficial.

| Resolución | Publicada | Qué hizo |
|---|---|---|
| [RG 1069/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/326947/20250613) | 13 de junio de 2025 | Creó el primer régimen para tokenizar activos reales, con un entorno de pruebas regulatorio (sandbox) de un año |
| [RG 1081/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/330173/20250821) | 21 de agosto de 2025 | Segunda etapa del régimen |
| [RG 1087/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/333326/20251023) | 23 de octubre de 2025 | Amplió el régimen |
| [RG 1150/2026](https://www.boletinoficial.gob.ar/detalleAviso/primera/343010/20260611) | 11 de junio de 2026 | Lo volvió a ampliar y extendió el sandbox hasta el 31 de diciembre de 2027 |

**Landtoken.** Landtoken, fundada en 2023, tokeniza campos agrícolas. La empresa agroindustrial Adecoagro opera los campos y Justoken provee la tecnología ([Agrofy](https://news.agrofy.com.ar/noticia/214878/hito-ahora-cualquiera-puede-invertir-campos-us-50-y-importante-renta-proyectada)). Todas las cifras siguientes vienen de la empresa, según la prensa agropecuaria.

| | Primera etapa | Segunda etapa (desde diciembre de 2025) |
|---|---|---|
| Vehículo | Un fideicomiso privado de administración, comprado directamente en la plataforma de Landtoken | El primer fideicomiso financiero tokenizado de campos con oferta pública autorizada por la CNV. Allaria Fiduciaria S.A. es fiduciario y emisor, y Nicholson y Cano es el asesor legal |
| Mínimo | $100 | $50, a través de brokers, bancos y exchanges de criptoactivos, empezando por Ripio |
| Alcance | Unos 1,000 inversionistas, sus dos primeros campos tokenizados y más de 100 operaciones entre inversionistas | Totales de la empresa al lanzamiento: más de 13,000 usuarios, más de 1,400 inversiones y $2.2 millones operados |
| Rendimiento proyectado | Cerca de 10% anual: cerca de 4% por el arrendamiento, pagado en quintales de soja, más 5% a 6% por valorización | 3% a 4% de renta bruta por arrendamiento más 5% a 6% de valorización anual |
| Fuente | [Agrositio](https://www.agrositio.com.ar/noticia/243155-tokenizacion-de-campos-como-invertir-en-tierras-productivas-con-solo-100-dolares.html) | [Agrofy](https://news.agrofy.com.ar/noticia/214878/hito-ahora-cualquiera-puede-invertir-campos-us-50-y-importante-renta-proyectada) |

**Qué significa para nosotros.**

- Landtoken es el modelo más cercano a nuestro producto de terrenos (diseño 1.6, D11): campos en un fideicomiso, ingresos por arrendamiento mientras se mantiene la tierra, montos pequeños, venta a través de brokers y bancos, y una empresa establecida que trabaja la tierra.
- Arrendar la tierra a un productor también podría influir en las preguntas sobre la función económico social (FES) y la prohibición de la especulación (art. 395) del diseño 1.6. Es una pregunta para la asesoría legal (P6).
- El diseño incluye un fideicomiso con un banco como opción de estructura legal y aclara que sus requisitos no se verificaron (diseño 5.2). Landtoken pasó de un fideicomiso privado a un fideicomiso financiero con oferta pública autorizada cuando el regulador tuvo normas para ello. Pregunte a la asesoría legal si un `fideicomiso` boliviano o una titularización mediante un `patrimonio autónomo` puede hacer lo mismo (P5).

### 3.5 El Salvador

La Ley de Emisión de Activos Digitales (LEAD) de El Salvador tiene su propio regulador, la Comisión Nacional de Activos Digitales (CNAD). Desde el inicio de la CNAD en 2023 hasta el 4 de septiembre de 2026 se registraron 55 emisiones inmobiliarias y de construcción, por $633 millones. Los tokens dan los derechos económicos que define cada emisión, y la ley no tiene una lista cerrada de activos que se pueden tokenizar. El periódico advierte que el monto de una emisión no es el monto realmente invertido ([El Diario de Hoy](https://www.elsalvador.com/dinero-y-negocios/entorno-economico/proyectos-inmobiliarios-activos-digitales/1293650/2026/)).

**Qué significa para nosotros.** Una ley y un regulador propios hicieron rutinaria la emisión. Como en Brasil, el monto emitido igual exagera el dinero recaudado. Bolivia no tiene una ley así, así que nuestros tokens caen bajo las normas de valores y de tecnología financiera del diseño 5.1.

### 3.6 España

El blog de Reental la compara con Urbanitae, así que son afirmaciones de una empresa sobre sí misma y sobre un rival ([Reental](https://www.reental.co/blog/reental-vs-urbanitae)):

- Un token de Reental es una fracción de un inmueble o de un derecho de crédito. Reental no figura en el registro de proveedores de financiación participativa que lleva la CNMV, el regulador de valores de España.
- Urbanitae figura en ese registro desde el 23 de diciembre de 2022. Reporta una TIR promedio de 11.7% en proyectos totalmente devueltos y no tiene mercado secundario.

La página del proveedor de la sección 2 sitúa a Reental en $79.9 millones de inmuebles tokenizados en blockchains públicas, la segunda plataforma más grande de las que sigue.

**Qué significa para nosotros.** Dos plataformas del mismo país eligieron vías legales distintas, y la vía decide lo que tiene el titular: una parte de un inmueble, un derecho de crédito o una inversión de financiación participativa. El diseño ya exige describir el derecho del titular exactamente como lo define la estructura legal (diseño 5.3).

### 3.7 México

En México se tokenizaron unos $400 millones, y una estimación de Tutellus ubica cerca del 25% en inmuebles. Los tokens inmobiliarios se revenden solo entre particulares que acuerdan la operación, sin un mercado secundario formal todavía. En una encuesta de EY, el 49% señaló la incertidumbre regulatoria como la principal barrera ([Expansión](https://expansion.mx/mercados/2026/03/02/mexico-registra-400-mdd-en-tokenizacion-institucionalizacion), 2 de marzo de 2026).

**Qué significa para nosotros.** Sin reglas claras, la reventa sigue siendo informal. Eso respalda mantener la v1 sin mercado abierto (diseño 5.4).

### 3.8 Bolivia

La ASFI aprobó su reglamento para empresas de tecnología financiera con la Resolución ASFI/540/2025 del 3 de julio de 2025. Incluye un entorno controlado de pruebas, y las empresas existentes tenían plazo hasta el 31 de diciembre de 2025 para adecuarse ([ASFI](https://asfi.gob.bo/node/1176), 15 de julio de 2025).

La lista de la ASFI al 9 de octubre de 2026 muestra 11 empresas con su no objeción para ingresar al entorno de pruebas, todas S.R.L. ([lista de la ASFI](https://asfi.gob.bo/sites/default/files/2026-10/Entidades%20con%20No%20Objeci%C3%B3n%20para%20el%20Ingreso%20al%20Entorno%20Controlado%20de%20Pruebas%20al%209.10.2026.pdf)):

| Servicio | Empresas |
|---|---|
| Pasarela de pagos | Kite Xperience, Tu Cuate, Libélula, Banipay, Smartfastpay Company Bolivia, PAYIN & PAYOUT |
| Pagos y plataforma de pagos | DFINANCE |
| Confirming (pago a proveedores) | Taspri Soluciones Digitales |
| Pagos transfronterizos | AV Finance |
| Conversión de activos virtuales a bolivianos | Banexcoin Bolivia |
| Conversión automática entre divisas | Salteños Bolivia |

Ninguna trabaja con activos tokenizados.

Derechos Reales emite el `certificado alodial`, que certifica la situación jurídica actual de un inmueble: el derecho del propietario, las hipotecas, las anotaciones preventivas y los procesos judiciales ([Consejo de la Magistratura](https://cm.organojudicial.gob.bo/consejo/requisitosddrr/45_-certificado-alodial.html)).

**Qué significa para nosotros.**

- La plataforma sería la primera en ingresar con activos tokenizados, sin un precedente que copiar. Consulte pronto a la ASFI (P5, diseño 5.5 pregunta 2).
- Banexcoin convierte activos virtuales a bolivianos, que es el tramo que necesita el flujo de compra (diseño 3.3). Pregunte a Pollar si su vía en bolivianos usa una empresa de esta lista (diseño 3.5, pregunta 1).
- El `certificado alodial` es el documento que hay que publicar para el título y los gravámenes (P3).

## 4. Liquidez de reventa: lo que encontraron los estudios

| Estudio | Datos | Hallazgo |
|---|---|---|
| [Swinkels, Financial Innovation, 2023](https://jfin-swufe.springeropen.com/articles/10.1186/s40854-022-00427-5) | 58 casas de alquiler tokenizadas en EE. UU., sobre todo en Detroit | 254 propietarios por inmueble en promedio. La propiedad cambia de manos cerca de una vez al año, más a menudo en exchanges descentralizados. Los precios de los tokens siguen el índice de precios de la vivienda |
| [Augustin, Chen, Qian y Shin, SSRN, 2026](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=6251598) | 644 casas tokenizadas en EE. UU., con precios, alquileres, propiedad y operaciones on-chain vinculados a registros de vivienda | Pese a la negociación frecuente, los precios de los tokens aportan poca información y siguen a los fundamentos de la vivienda. La liquidez mejora solo un poco. Los mayores rendimientos vienen sobre todo de alquileres altos, porque las casas tokenizadas se concentran en segmentos de alto rendimiento |
| [Documento de trabajo 1311 del BIS, 2025, revisado en 2026](https://www.bis.org/publ/work1311.htm) | Varias plataformas de EE. UU., 2019 a 2025 | Las casas tokenizadas se concentran donde los precios son más bajos y la demanda y la liquidez más débiles. Tras un desastre natural, la negociación sube 35% en dos días. Esa ventaja depende de que las plataformas ofrezcan recompras, que traen un mayor riesgo de insolvencia |
| [Mafrur, arXiv, 2025](https://arxiv.org/abs/2508.11651) | Activos reales tokenizados | La mayoría de los tokens muestra volúmenes bajos, tenencias largas y pocos participantes activos |
| [Mafrur, arXiv, 2026](https://arxiv.org/abs/2606.01131) | Tokens en Ethereum de bonos del Tesoro, oro y crédito privado | Estar on-chain no es lo mismo que ser líquido. Los tokens de oro tienen más titulares y una actividad más constante que muchos tokens de bonos del Tesoro y de crédito privado, y el valor en circulación de un token no predice de forma fiable su liquidez |

**Qué significa para nosotros.** Un token abarata la transferencia, pero no crea compradores. Mantenga la posición del diseño: ninguna promesa de liquidez, y ventanas de rescate con tope solo si la ley las permite (diseño 5.4). El hallazgo del BIS muestra por qué importa el tope: una plataforma que recompra tokens bajo estrés mantiene viva la negociación, pero arriesga su propia solvencia (P7).

## 5. Cambios propuestos al documento de diseño

Son propuestas para la revisión de diseño (Design Review Gate). Ninguna está aún en el documento de diseño.

| ID | Cambio propuesto | Sección del diseño | Basado en |
|---|---|---|---|
| P1 | **Plan ante la falla del operador.** Nombrar en los documentos de la oferta un administrador de inmuebles de respaldo y un administrador sucesor. Los alquileres van a una cuenta del vehículo que el administrador de inmuebles no puede mover solo. Los titulares pueden reemplazar al administrador por votación (**Asesoría legal**) | 3.4, 5.2, 8.1 | RealT (3.1) |
| P2 | **Datos del inmueble en el informe mensual.** IPBI pagado (comprobante del RUAT), servicios y `expensas` pagados, seguro vigente, unidades ocupadas y vacías, y notificaciones municipales abiertas, cada uno con su comprobante o documento | 4.6 | RealT (3.1) |
| P3 | **Prueba de título y gravámenes.** Antes de cada oferta y cada año después, publicar el folio real y un `certificado alodial` de Derechos Reales de cada inmueble, con sus hashes. Ninguna hipoteca o gravamen nuevo sin la aprobación que exigen los documentos de la oferta, y publicación dentro de un plazo fijo en días (**Asesoría legal**) | 1.6, 5.3, 8.1 | RealT (3.1), Brasil (3.3), Bolivia (3.8) |
| P4 | **Salvaguarda de rendimientos.** El asistente cita solo rendimientos calculados por la cascada (diseño 4.2) y marca cualquier publicación o mensaje del cliente que cite uno mayor. Cuando muestra un rendimiento alto, explica que en otros mercados los rendimientos más altos de los tokens vinieron de ubicaciones más baratas y débiles (sección 4). El marketing sigue las mismas reglas | 7.7 | RealT (3.1), estudios (4) |
| P5 | **Primero el regulador.** Consultar pronto a la ASFI si una empresa de activos tokenizados puede ingresar al entorno controlado de pruebas y qué estructura legal espera. Preguntar a la asesoría legal si un fideicomiso o una titularización puede llevar una oferta pública autorizada, como en Argentina | 5.1, 5.2, 5.5 | Dubái (3.2), Argentina (3.4), Bolivia (3.8) |
| P6 | **Vía del arrendamiento para terrenos.** Para lotes como LT-1, planificar un arrendamiento interino (estacionamiento o depósito en lotes urbanos, cultivo en tierra rural) cuando el terreno lo permita, y mostrar el ingreso por arrendamiento junto a los escenarios de venta (**Asesoría legal** para tierra rural) | 1.6 | Landtoken (3.4) |
| P7 | **Advertencia sobre liquidez.** Informar antes de la compra que la reventa puede ser poco frecuente y lenta, y publicar el número de transferencias de cada período. Dimensionar cualquier reserva para rescates de modo que una corrida no ponga en riesgo al vehículo | 5.4, 7.7 | Estudios (4), México (3.7) |
| P8 | **Metas realistas.** Basar las metas del negocio y las cifras de marketing en tokens vendidos y dinero recaudado, nunca en carteras de proyectos, montos de emisión ni pronósticos de mercado | 8.2 | Brasil (3.3), El Salvador (3.5), tamaño del mercado (2) |

P1 y P3 también agregarían dos filas a la tabla de riesgos del diseño 8.1: la falla del operador, y los gravámenes no informados o impuestos impagos.

## 6. Fuentes

Cada dato se leyó en la fuente citada. Las fuentes marcadas "resumen de búsqueda" no se pudieron abrir directamente, así que los datos vienen del resumen que mostró el buscador. El contenido de las fuentes se parafraseó.

Tipos: oficial (gobierno, regulador, tribunal o gaceta), académica, centro de estudios, prensa, empresa (una plataforma sobre sí misma), competidor (una plataforma rival) y proveedor (quien vende servicios de tokenización).

**Estados Unidos**

- [Outlier Media, "Undisclosed mortgages, missing deeds: RealT's problems mount"](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/), 2025. Prensa
- [Ciudad de Detroit, comunicado del 23 de julio de 2025](https://detroitmi.gov/es/news/la-ciudad-obtiene-una-importante-victoria-para-los-inquilinos-que-viven-en-propiedades-de-real-token), versión en español. Oficial
- [Roosevelt Institute, Brad Lipton, "Detroit's Case Against Real Token", diciembre de 2025](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf). Centro de estudios. Resume la demanda y las órdenes judiciales
- [Michigan Public, "The model no longer works", 20 de marzo de 2026](https://www.michiganpublic.org/social-justice/2026-03-20/the-model-no-longer-works-crypto-landlords-detroit-enterprise-is-crumbling). Prensa
- [Lofty, "The RealT Liquidation Explained"](https://www.lofty.ai/blog/realt-liquidation-explained), 2026. Competidor
- [Reiss, Bizub y Peralta, "Who Controls the Block? How States Can Regulate Tokenized Residential Real Estate", SSRN](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=7220758). Académica, resumen de búsqueda
- [Cointribune sobre RealT, septiembre de 2025](https://www.cointribune.com/en/realt-revolutionizes-real-estate-investment-with-blockchain/). Prensa
- [Stellar Development Foundation, anuncio de RedSwan, septiembre de 2025](https://stellar.org/press/redswan-digital-real-estate-brings-usd100m-of-tokenized-real-estate-to-the-stellar-network). Empresa

**Emiratos Árabes Unidos**

- [Gulf News, "How to invest in Dubai property with just Dh2,000", mayo de 2025](https://gulfnews.com/your-money/saving-investment/how-to-invest-in-dubai-property-with-just-dh2000-1.500140848). Prensa, resumen de búsqueda
- [Invezz, 26 de mayo de 2025](https://invezz.com/es/noticias/2025/05/26/la-primera-plataforma-inmobiliaria-tokenizada-de-dubai-se-lanza-bajo-la-supervision-de-vara/), edición en español. Prensa, resumen de búsqueda
- [Gulf News, "PRYPCO rolls out Dubai marketplace after nod for property stakes trading", febrero de 2026](https://gulfnews.com/business/property/prypco-rolls-out-dubai-marketplace-after-nod-for-property-stakes-trading-1.500438076). Prensa
- [The National, 9 de febrero de 2026](https://www.thenationalnews.com/business/property/2026/02/09/dubai-property-tokenisation-fractional-investing/). Prensa
- [PRYPCO, "Real Estate Tokenization Fees in Dubai, UAE Explained"](https://prypco.com/blogs/real-estate-tokenization-fees-dubai-explained), 2026. Empresa

**Brasil**

- [Exame, "Tokenização imobiliária alcança R$ 20 bilhões no Brasil"](https://exame.com/future-of-money/tokenizacao-imobiliaria-alcanca-r-20-bilhoes-no-brasil/), 2026. Prensa
- [Valor Econômico, 27 de enero de 2026](https://valor.globo.com/financas/criptomoedas/noticia/2026/01/27/em-vitria-dos-cartrios-corregedoria-de-so-paulo-probe-o-registro-de-tokens-imobilirios.ghtml). Prensa
- [Migalhas, "CGJ/SP veda tokenização imobiliária e edita provimento 54/25", 23 de marzo de 2026](https://www.migalhas.com.br/depeso/452304/cgj-sp-veda-tokenizacao-imobiliaria-e-edita-provimento-54-25). Prensa jurídica
- [Jota, febrero de 2026](https://www.jota.info/justica/corregedorias-de-justica-proibem-que-matriculas-de-imoveis-sejam-vinculadas-a-tokens-digitais). Prensa jurídica
- [Coinspeaker, agosto de 2025](https://www.coinspeaker.com/pt/santa-catarina-veta-registros-de-imoveis-na-blockchain/). Prensa, resumen de búsqueda
- [Valor Econômico, 13 de octubre de 2025](https://valor.globo.com/financas/criptomoedas/noticia/2025/10/13/justia-suspende-resoluo-do-cofeci-sobre-tokenizao-imobiliria.ghtml). Prensa
- [Migalhas, octubre de 2025](https://www.migalhas.com.br/quentes/442418/token-juiz-suspende-resolucao-sobre-transacoes-imobiliarias-digitais). Prensa jurídica
- [CVM, 11 de octubre de 2022](https://www.gov.br/cvm/pt-br/assuntos/noticias/2022/cvm-divulga-parecer-de-orientacao-sobre-criptoativos-e-o-mercado-de-valores-mobiliarios). Oficial

**Argentina**

- Boletín Oficial: [RG 1069/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/326947/20250613), [RG 1081/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/330173/20250821), [RG 1087/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/333326/20251023) y [RG 1150/2026](https://www.boletinoficial.gob.ar/detalleAviso/primera/343010/20260611). Oficial
- [Agrofy News sobre el fideicomiso público de Landtoken](https://news.agrofy.com.ar/noticia/214878/hito-ahora-cualquiera-puede-invertir-campos-us-50-y-importante-renta-proyectada), fines de 2025. Prensa agropecuaria, cifras de la empresa
- [Agrositio sobre el fideicomiso privado de Landtoken](https://www.agrositio.com.ar/noticia/243155-tokenizacion-de-campos-como-invertir-en-tierras-productivas-con-solo-100-dolares.html), 2025. Prensa agropecuaria, cifras de la empresa

**El Salvador**

- [El Diario de Hoy, "Proyectos inmobiliarios suman $633 millones en emisiones", septiembre de 2026](https://www.elsalvador.com/dinero-y-negocios/entorno-economico/proyectos-inmobiliarios-activos-digitales/1293650/2026/). Prensa

**España**

- [Reental, "Reental vs Urbanitae 2026"](https://www.reental.co/blog/reental-vs-urbanitae), noviembre de 2026. Empresa, que también describe a un rival

**México**

- [Expansión, 2 de marzo de 2026](https://expansion.mx/mercados/2026/03/02/mexico-registra-400-mdd-en-tokenizacion-institucionalizacion). Prensa

**Bolivia**

- [ASFI, nota de prensa del 15 de julio de 2025](https://asfi.gob.bo/node/1176). Oficial
- [ASFI, entidades con no objeción para el ingreso al entorno controlado de pruebas, 9 de octubre de 2026](https://asfi.gob.bo/sites/default/files/2026-10/Entidades%20con%20No%20Objeci%C3%B3n%20para%20el%20Ingreso%20al%20Entorno%20Controlado%20de%20Pruebas%20al%209.10.2026.pdf) (PDF). Oficial
- [Consejo de la Magistratura, requisitos del certificado alodial](https://cm.organojudicial.gob.bo/consejo/requisitosddrr/45_-certificado-alodial.html). Oficial

**Estudios y datos de mercado**

- [Swinkels, "Empirical evidence on the ownership and liquidity of real estate tokens", Financial Innovation 9:45, 2023](https://jfin-swufe.springeropen.com/articles/10.1186/s40854-022-00427-5). Académica, resumen de búsqueda
- [Augustin, Chen, Qian y Shin, "Tokenization and the Financialization of Real Estate", SSRN, 2026](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=6251598). Académica, resumen de búsqueda
- [Documento de trabajo 1311 del BIS, "When bricks meet bytes: does tokenisation fill gaps in traditional real estate markets?", noviembre de 2025, revisado en junio de 2026](https://www.bis.org/publ/work1311.htm). Investigación oficial
- [Mafrur, "Tokenize Everything, But Can You Sell It? RWA Liquidity Challenges and the Road Ahead", arXiv, agosto de 2025](https://arxiv.org/abs/2508.11651). Académica
- [Mafrur, "Tokenized but Illiquid? Evidence from Real-World Asset Markets", arXiv, mayo de 2026, revisado en julio de 2026](https://arxiv.org/abs/2606.01131). Académica, publicado también en FinTech 2026, 5, 62
- [Pixel Web Solutions, plataformas de tokenización inmobiliaria, datos al 5 de noviembre de 2026](https://www.pixelwebsolutions.com/best-real-estate-tokenization-platforms/). Proveedor, cita a RWA.xyz
- [Crypto Briefing, "RWA tokenization surges across five sectors", julio de 2026](https://cryptobriefing.com/rwa-tokenization-surges-five-sectors/). Prensa, con datos de RWA.xyz
- Pronóstico de Deloitte del 24 de abril de 2025, según [Cointelegraph](https://cointelegraph.com/news/real-estate-tokenization-forecast-4-trillion-2035) (prensa) y [PaySpace Magazine](https://payspacemagazine.com/news/real-estate-worth-4t-to-be-tokenized-by-2035-deloitte/) (prensa, resumen de búsqueda). No se pudo leer la página de Deloitte
