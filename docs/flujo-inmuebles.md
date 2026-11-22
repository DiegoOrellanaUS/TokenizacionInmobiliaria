# Registro y revisión de inmuebles

Para iniciar el flujo, el propietario debe tener un tenant activo, el rol `TITULAR_TENANT` vigente y una suscripción activa para ese tenant. Admin comprueba las tres condiciones. Al contratar, Admin aprovisiona `inmuebles_operacion_{tenantId:N}` y `tokenizacion_{tenantId:N}` como bases separadas por tenant. La suscripción nueva pasa a `ACTIVA` cuando ambos servicios responden `LISTO`. Si alguno está pendiente, repite el alta con el mismo plan, usuario y referencia de pago para completar el aprovisionamiento. Esto también permite preparar bases faltantes para una suscripción anterior.

## Configuración local

- Inicia Admin, Inmuebles, Tokenización, Comercio, Reportes y Gateway para hacer el flujo completo.
- Configura el mismo valor privado de `Aprovisionamiento__ClaveInterna` en Admin, Inmuebles, Tokenización y Reportes. Úsalo solo para la comunicación interna. Admin llama directamente a los servicios. Si cambias los puertos, ajusta `Aprovisionamiento__InmueblesOperacion`, `Aprovisionamiento__Tokenizacion` y `Aprovisionamiento__Reportes` en Admin.
- Inicia Reportes en `https://localhost:7315` y configura `ConnectionStrings__ReportesContext` con una cuenta de PostgreSQL que pueda crear bases de datos. El servicio crea una base `reportes_{tenantId}` para cada tenant.
- Configura la misma variable `Inmuebles__ClaveInterna` en Admin e Inmuebles con un valor privado. `Inmuebles__AdminUrl` en Inmuebles apunta a Admin. Su valor local predeterminado es `https://localhost:7266/`.
- Para habilitar un revisor, asigna a su usuario un rol global vigente `REVISOR_INMUEBLE` o `SUPER_ADMIN` en Admin y agrega su UUID a la configuración de Admin, por ejemplo con `Inmuebles__Revisores__0`.
- Usa HTTPS con los encabezados `X-Tenant` y `Authorization: Basic <base64(usuarioId:contraseña)>`. El UUID es el del usuario de Admin. Para probar desde [Scalar de Gateway](https://localhost:7339/scalar), selecciona **Inmuebles y Operación**.

## Endpoints públicos

Las rutas de la tabla empiezan con `/api/Inmuebles`.

| Operación | Método y ruta | Datos necesarios |
|---|---|---|
| Crear borrador | `POST /Crear` | `nombre`, `tipo`, `ciudad`, `direccion`, `superficieM2` |
| Editar ficha | `PUT /Actualizar/{id}` | Los mismos datos de la ficha |
| Declarar titulares | `PUT /{id}/Titulares` | `titulares`, lista con `nombre`, `desde` y `cuota` opcional entre 0 y 1 |
| Adjuntar respaldo | `POST /{id}/Documentos` | Formulario multipart con `clase` y `archivo`. Usa `TITULARIDAD` para el respaldo principal |
| Reemplazar respaldo | `POST /{id}/Documentos/{documentoId}/Versiones` | Formulario multipart con `archivo` |
| Enviar a revisión | `POST /{id}/EnviarRevision` | La ficha debe tener titulares declarados y al menos un archivo de clase `TITULARIDAD` |
| Resolver revisión | `POST /{id}/ResolverRevision` | Un revisor envía `envioId` tomado de la consulta del expediente, `decision` y, para `CON_OBSERVACIONES`, una `observacion` |
| Consultar expediente | `GET /Buscar/{id}` | Este resultado incluye documentos, titulares y decisiones |
| Descargar respaldo | `GET /{id}/Documentos/{documentoId}/Versiones/{version}` | Identificador del documento y versión |
| Listar fichas | `GET /Listar` o `GET /ListarReal` | Encabezados de acceso |

Las cuotas, cuando se indican, se completan para todos los titulares y suman 1. Los documentos se almacenan en el silo, con un máximo de 5 MiB por versión. Al observar una ficha, el propietario puede corregirla, cargar una nueva versión y volver a enviarla. Al aprobarla se conservan en la misma transacción la decisión, la fecha de verificación de los titulares y el evento de cambio de estado. La aprobación corresponde a la revisión de la plataforma.

## Consumo entre microservicios

Inmuebles guarda los cambios de estado en `operacion.evento_inmueble` dentro del silo, en la misma transacción que actualiza la ficha. Los servicios autorizados pueden consultar directamente a Inmuebles con:

```http
GET /interno/tenants/{tenantId}/inmuebles/eventos?despues=0
X-Inmuebles-Eventos: <valor configurado en Inmuebles__ClaveEventos>
```

La consulta devuelve hasta 100 eventos por página. Cada entrada tiene `eventoId`, `inmuebleId`, `estadoRevision` y `ocurridoEn`. El resultado incluye `tenantId` y `siguienteCursor`. Cada consumidor debe conservar su propio cursor por tenant, avanzarlo después de procesar los eventos y deduplicar por `(tenantId, eventoId)`. Comercio e Inversión puede usar `APROBADO` para habilitar el siguiente flujo de evaluación. La creación de una oferta, su aprobación y la emisión siguen siendo operaciones de sus servicios dueños.

El catálogo de eventos se autentica con una clave distinta a la de Admin. El Gateway enruta las operaciones públicas y su documentación.

## Emitir el token de un inmueble aprobado

Tokenización usa Stellar CLI y conserva las emisiones en la base privada que se prepara al contratar la suscripción. Configura en Tokenización los mismos valores de `Inmuebles__ClaveInterna` que usa Admin y de `Inmuebles__ClaveEventos` que usa Inmuebles. Para instalaciones con otras direcciones, configura `Tokenizacion__AdminUrl` y `Tokenizacion__InmueblesUrl`. La CLI local se busca en `bin/stellar/stellar.exe` y puede configurarse con `Stellar__Ejecutable`.

Con el inmueble aprobado, usa el documento **Tokenizacion** del [Scalar de Gateway](https://localhost:7339/scalar), conserva el `X-Tenant` del titular y usa sus credenciales Basic.

1. Llama a `POST /api/Tokenizacion/Inmuebles/{inmuebleId}/Emitir` con las unidades enteras. Tokenización asigna automáticamente el código de la nueva emisión.
   
   ```json
   { "unidades": 500 }
   ```

2. Si el resultado es `PENDIENTE`, consulta `GET /api/Tokenizacion/Inmuebles/{inmuebleId}/Buscar` y repite el mismo POST para reconciliarlo. El servicio usa el hash y el sobre firmado que ya guardó y devuelve el resultado existente cuando se confirmó.
3. Cuando el resultado es `CONFIRMADA`, guarda el hash y el ledger. La cuenta distribuidora recibe las unidades del activo. Stellar identifica el activo por código y dirección emisora. Las emisiones previas conservan su código, incluido el `INM001` de Casa Prado.

El contrato de emisión usa el cupo del plan en el momento de contratar. Cada inmueble tiene sus propias cuentas de emisión y distribución en Testnet. Comprar un plan en el entorno piloto y aprobar un inmueble son los pasos previos a la emisión. La oferta comercial y la venta de unidades tienen sus propios flujos.

