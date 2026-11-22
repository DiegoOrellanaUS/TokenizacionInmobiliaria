# Umbral

MVP de tokenización inmobiliaria con activos clásicos de Stellar Testnet.

## Evidencia para el jurado

[Postulación al Stellar Community Fund](docs/postulacion-scf.md) con los enlaces para el jurado, el estado de tracción, el plan técnico por tramos y la divulgación del uso de IA.

[Evidencia pública de Casa Prado](https://tokenizacion-inmobiliaria.vercel.app/evidencia-stellar/casa-prado) · [Oferta de Casa Prado](https://tokenizacion-inmobiliaria.vercel.app/ofertas/32d2ac6f-f288-4dc9-b226-66a2379a518f)

En la compra de prueba del 22 de noviembre de 2026, Pollar envió **1 USDC** en Stellar Testnet. Tokenización verificó el pago y 20 segundos después la distribuidora entregó **1 INM001** del emisor original. Umbral registró ambos hashes en la orden `eddf51d9-928d-4e4d-8c01-d3c1a99d5a24`.

| Operación | Ledger | Hash |
| --- | ---: | --- |
| Pago de 1 USDC con Pollar | 5.865.992 | [`5de7b12de80bbffa942dcdee78ca5f431e6ccab1b6f98710b7ca7a91b7b12377`](https://stellar.expert/explorer/testnet/tx/5de7b12de80bbffa942dcdee78ca5f431e6ccab1b6f98710b7ca7a91b7b12377) |
| Entrega de 1 INM001 | 5.865.996 | [`31460812d39afe13136d1d7362dd1d2bedba480ddcc6390483aabe270fcbad44`](https://stellar.expert/explorer/testnet/tx/31460812d39afe13136d1d7362dd1d2bedba480ddcc6390483aabe270fcbad44) |

- [Activo INM001 en Stellar Expert](https://stellar.expert/explorer/testnet/asset/INM001-GBWYOSVB6RZG7Y7GCJW5RGN5PKSBMKQYSRDEOECYLB725PEEO7D5Y6IG)
- [Emisión original de 500 INM001](https://stellar.expert/explorer/testnet/tx/f299ad655df7a6bf485fd4171cc8b9c0bf7eb9d2294b5b2f6d1833b7bcf24910)
- [Configuración y flujo de Pollar en Testnet](docs/pollar-casa-prado.md)

Para ejecutar la aplicación, consulta el [README del frontend](Frontend/README.md) y la [guía del flujo de inmuebles](docs/flujo-inmuebles.md).
