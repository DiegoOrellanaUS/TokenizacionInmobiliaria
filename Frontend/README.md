# Umbral

Interfaz de demostración para TokenizacionInmobiliaria. El catálogo es público. El espacio privado usa la sesión simulada de Admin y envía `X-Tenant` a los servicios de negocio.

## Puesta en marcha

1. Inicia Admin, Inmuebles, Tokenización, Comercio y Gateway con sus perfiles HTTPS de desarrollo. El perfil de Admin activa `SesionDemo__Habilitada` para conexiones locales.
2. Usa PostgreSQL local y la configuración de silos del backend. En los secretos de Comercio, configura `ConnectionStrings:CatalogoContext` para la base central `catalogo_inmobiliario`. Comercio aplica su migración al abrir el catálogo.
3. La emisión usa Stellar Testnet. Tokenización necesita la CLI instalada y sus claves internas de comunicación. Admin debe tener un plan activo y un usuario revisor habilitado por rol y por `Inmuebles:Revisores`.
4. En esta carpeta ejecuta `npm ci` y `npm run dev`. Abre `http://localhost:5173`. Para cambiar la URL de Gateway, usa `VITE_API_URL` y configura `Cors:FrontendOrigins` en el proxy con el origen del frontend.

El flujo de la interfaz es registro con correo, plan piloto, nuevo inmueble, titulares, archivo, revisión, emisión y publicación. El revisor accede por su correo desde otra ventana y entra a la bandeja de revisión. Selecciona el espacio del propietario en la bandeja para abrir sus expedientes. La bandeja y el expediente consultan los cambios periódicamente. Mientras el propietario tiene abierto un expediente con la emisión pendiente, Umbral reconcilia la misma transacción en Stellar y actualiza el resultado. La sesión de demo dura hasta dos horas y se conserva en memoria mientras la página está abierta.

## Compilación y pruebas

- `npm run build` compila TypeScript y Vite.
- Con la aplicación en marcha, configura `E2E_REVIEWER_EMAIL` con el correo de un revisor local habilitado y, opcionalmente, `E2E_PLAN` con el nombre del plan que quieres probar. Ejecuta `npm test`. Las pruebas crean un usuario, un inmueble y una oferta ficticios en la base que usa el backend local.
- La suite usa Microsoft Edge por defecto. `E2E_BROWSER` permite seleccionar otro canal de Playwright instalado.

La fotografía de referencia se obtuvo de [Unsplash](https://images.unsplash.com/photo-1600607687939-ce8a6c25118c). En el catálogo aparece como ilustración de referencia.
