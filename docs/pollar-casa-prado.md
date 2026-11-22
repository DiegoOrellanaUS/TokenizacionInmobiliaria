# Compra de Casa Prado con Pollar y Stellar Testnet

Casa Prado usa Pollar para firmar un pago de USDC de prueba. Tokenización verifica el pago en Horizon antes de entregar `INM001` a la billetera de portafolio de Umbral. La billetera que paga se vincula a la orden al crear la reserva. El detalle de compra y el portafolio muestran por separado el hash del pago y el de la entrega.

## Configuración de Tokenización

1. Crea una aplicación en Stellar Testnet en el dashboard de Pollar. Habilita Google o Freighter, autoriza el origen de Umbral y agrega el USDC oficial de Circle para Testnet. Configura el fondeo inicial si habilitas billeteras integradas. [Pollar Docs](https://docs.pollar.xyz/) · [Dashboard](https://dashboard.pollar.xyz/) · [Emisor USDC de Testnet](https://developers.circle.com/stablecoins/usdc-contract-addresses)
2. Prepara una cuenta receptora en Stellar Testnet con una línea de confianza para `USDC:GBBD47IF6LWK7P7MDEVSCWR7DPUWV3NY3DTQEVFL4NAT4AQH3ZLLFLA5`. Usa una cuenta distinta a la billetera pagadora. [Stellar Lab](https://lab.stellar.org/)
3. En Tokenización configura `Pollar__ClavePublica` con la clave publicable del dashboard y `Pollar__Destino` con la dirección pública receptora. `Pollar__PrecioUsdc` es el precio unitario de prueba (por defecto, 1 USDC). La cotización queda guardada al reservar. Comercio y Tokenización comparten la clave interna existente `Comercio__ClaveLiquidacionDemo`. La migración del catálogo se aplica al iniciar su flujo de aprovisionamiento.

El checkout está habilitado para la oferta identificada por `32d2ac6f-f288-4dc9-b226-66a2379a518f`, en los entornos Development y Piloto. El inmueble requiere una emisión confirmada y saldo en su distribuidora. La clave que usa el frontend es publicable. El SDK no requiere compartir claves privadas de las billeteras con Umbral.

## Inicio de sesión con Google

El SDK envía el origen del navegador como `redirect_uri`. Registra `http://localhost:5173` para el desarrollo local en la lista de **Redirect URIs** de la aplicación Testnet vinculada a `Pollar__ClavePublica`. Cuando se publique el checkout, registra también `https://tokenizacion-inmobiliaria.vercel.app`. Usa el origen exacto, sin agregar una ruta de callback.

Pollar valida por separado los dominios permitidos en **Build → Domains** y las URLs de retorno de OAuth. Si la ventana de Google muestra `APPLICATION_HAS_NO_REDIRECT_URIS`, la app de Pollar todavía no tiene URLs de retorno registradas. El checkout puede crear una sesión y mostrar ese error antes de que se abra Google. [Dashboard de Pollar](https://dashboard.pollar.xyz/)

Para probar el pago sin usar el inicio de sesión con Google, selecciona **Conectar Freighter** con la extensión en Stellar Testnet. Usa una cuenta distinta de la receptora y con saldo de USDC de prueba. **Compra demo** sigue disponible como flujo separado sin cobro.

## Verificación del recorrido

1. Abre Casa Prado con una cuenta compradora y conecta Pollar. Habilita USDC y fondea la billetera con el faucet de Circle si hace falta. [Oferta de Casa Prado](https://tokenizacion-inmobiliaria.vercel.app/ofertas/32d2ac6f-f288-4dc9-b226-66a2379a518f) · [Faucet de Circle](https://faucet.circle.com/)
2. Reserva una unidad, selecciona **Pagar con Pollar** y autoriza el pago. Tokenización comprueba en Horizon que la transacción exitosa contenga un `payment` por el importe y el emisor exactos, desde la billetera reservada hasta el destino, con el memo de la orden. Comercio guarda el hash y el ledger verificados en una transacción de base de datos. Un hash solo puede aplicarse a una compra.
3. Abre los dos comprobantes en el portafolio. El primero debe mostrar USDC hacia la cuenta receptora. El segundo debe mostrar el activo inmobiliario hacia la billetera de portafolio.

La reserva se puede cancelar antes de iniciar el pago. Después de ese punto, usa **Verificar pago** con el hash de Pollar si hubo una interrupción o si Horizon todavía no lo muestra. La orden queda asociada a la cuenta de Umbral y se puede retomar desde el portafolio. Si la firma se interrumpió sin hash o una entrega falla con pago verificado, la orden requiere conciliación operativa. Conserva la referencia y el comprobante cuando esté disponible para evitar un segundo pago.
