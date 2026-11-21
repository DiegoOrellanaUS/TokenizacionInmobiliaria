# Desplegar Umbral en Railway y Vercel

Usa el mismo repositorio de GitHub para ambos proveedores. Railway ejecuta los cinco servicios del backend y un solo servidor PostgreSQL. Vercel compila el SPA de React y sirve los archivos estáticos. El navegador solo llama al Gateway público. Los servicios de Railway se comunican por la red privada.

## 1. Publicar los cambios en GitHub

Revisa `git status` y confirma los archivos que quieras desplegar. Incluye `Frontend/package.json`, `Frontend/package-lock.json`, el código del SPA, `Frontend/vercel.json`, los cambios de backend y los Dockerfiles. Si el frontend se sube por primera vez, verifica que en GitHub aparezca la carpeta `Frontend` de la rama que vas a conectar.

Las conexiones y contraseñas de Railway se configuran en Variables. Si una credencial real estuvo en Git, rótala antes de utilizarla en la nube.

## 2. Crear los servicios en Railway

Añade el servicio **PostgreSQL** y usa el nombre **Postgres** para que funcionen los ejemplos de variables. Conecta los servicios al mismo repositorio y a la rama publicada. Usa estos nombres si quieres copiar las referencias tal como se muestran:

| Servicio en Railway | Root Directory |
| --- | --- |
| gateway | `/Backend/Gateway` |
| admin | `/Backend/Adminstracion` |
| inmuebles | `/Backend/InmueblesOperacion` |
| tokenizacion | `/Backend/Tokenizacion` |
| comercio | `/Backend/ComercioInversion` |

Puedes reutilizar el servicio ya conectado como `gateway`. En cada servicio, Railway detecta el `Dockerfile` de su carpeta. Deja vacío **Start Command** para usar el comando de la imagen. Usa una sola réplica por servicio para el piloto.

En los cinco servicios configura estas variables y usa `/salud` como **Healthcheck Path**.

```dotenv
ASPNETCORE_ENVIRONMENT=Piloto
ASPNETCORE_HTTP_PORTS=8080
PORT=8080
```

Mantén privados **admin**, **inmuebles**, **tokenizacion**, **comercio** y **Postgres**. Genera un dominio público en **gateway**, puerto **8080**. El navegador usa HTTPS y la red privada de Railway usa HTTP.

## 3. Configurar las variables por servicio

Crea cuatro variables compartidas de Railway con valores aleatorios distintos. Usa nombres `CLAVE_APROVISIONAMIENTO`, `CLAVE_ACCESO_INMUEBLES`, `CLAVE_EVENTOS_INMUEBLES` y `CLAVE_LIQUIDACION`. Configura referencias a cada variable compartida, tal como aparecen abajo. Esas claves se usan entre servidores.

### admin

Monta un volumen en `/data` y configura los siguientes valores. El inicio en modo Piloto aplica las migraciones de Admin y prepara el plan y la cuenta revisora. Dale al revisor una contraseña única de 12 a 128 caracteres. Distribuye el código de invitación solo a quienes deban probar el registro.

```dotenv
ConnectionStrings__ArchivosMultitenenciaAdministracionContext=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}
Aprovisionamiento__InmueblesOperacion=http://${{inmuebles.RAILWAY_PRIVATE_DOMAIN}}:8080
Aprovisionamiento__Tokenizacion=http://${{tokenizacion.RAILWAY_PRIVATE_DOMAIN}}:8080
Aprovisionamiento__ClaveInterna=${{shared.CLAVE_APROVISIONAMIENTO}}
Inmuebles__ClaveInterna=${{shared.CLAVE_ACCESO_INMUEBLES}}
SesionDemo__DirectorioClaves=/data/sesion
Piloto__CodigoInvitacion=REEMPLAZAR_CON_UN_CODIGO_DE_16_A_128_CARACTERES
Piloto__RevisorEmail=REEMPLAZAR_CON_EL_CORREO_DEL_REVISOR
Piloto__RevisorContrasenia=REEMPLAZAR_CON_UNA_CONTRASENIA_UNICA
```

### inmuebles

```dotenv
ConnectionStrings__InmueblesOperacionContext=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}
Inmuebles__AdminUrl=http://${{admin.RAILWAY_PRIVATE_DOMAIN}}:8080
Inmuebles__ClaveInterna=${{shared.CLAVE_ACCESO_INMUEBLES}}
Inmuebles__ClaveEventos=${{shared.CLAVE_EVENTOS_INMUEBLES}}
Aprovisionamiento__ClaveInterna=${{shared.CLAVE_APROVISIONAMIENTO}}
```

### tokenizacion

Monta un volumen en `/data`. Las identidades de Stellar Testnet se escriben ahí y se reutilizan después de cada despliegue.

```dotenv
ConnectionStrings__TokenizacionContext=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}
Tokenizacion__AdminUrl=http://${{admin.RAILWAY_PRIVATE_DOMAIN}}:8080
Tokenizacion__InmueblesUrl=http://${{inmuebles.RAILWAY_PRIVATE_DOMAIN}}:8080
Inmuebles__ClaveInterna=${{shared.CLAVE_ACCESO_INMUEBLES}}
Inmuebles__ClaveEventos=${{shared.CLAVE_EVENTOS_INMUEBLES}}
Aprovisionamiento__ClaveInterna=${{shared.CLAVE_APROVISIONAMIENTO}}
Comercio__ClaveLiquidacionDemo=${{shared.CLAVE_LIQUIDACION}}
Stellar__DirectorioDatos=/data/stellar
```

### comercio

El catálogo usa otra base en el mismo servidor. Los contextos crean sus bases con el usuario de PostgreSQL del proyecto, que debe tener permiso `CREATEDB`.

```dotenv
ConnectionStrings__CatalogoContext=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=umbral_catalogo;Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}
Comercio__AdminUrl=http://${{admin.RAILWAY_PRIVATE_DOMAIN}}:8080
Comercio__InmueblesUrl=http://${{inmuebles.RAILWAY_PRIVATE_DOMAIN}}:8080
Comercio__TokenizacionUrl=http://${{tokenizacion.RAILWAY_PRIVATE_DOMAIN}}:8080
Comercio__ClaveLiquidacionDemo=${{shared.CLAVE_LIQUIDACION}}
```

Si la plantilla tiene **Serverless** habilitado, deja **comercio** activo mientras pruebas. Su proceso en segundo plano confirma las transferencias pendientes.

### gateway

```dotenv
ReverseProxy__Clusters__administracion__Destinations__principal__Address=http://${{admin.RAILWAY_PRIVATE_DOMAIN}}:8080/
ReverseProxy__Clusters__inmuebles__Destinations__principal__Address=http://${{inmuebles.RAILWAY_PRIVATE_DOMAIN}}:8080/
ReverseProxy__Clusters__tokenizacion__Destinations__principal__Address=http://${{tokenizacion.RAILWAY_PRIVATE_DOMAIN}}:8080/
ReverseProxy__Clusters__comercio__Destinations__principal__Address=http://${{comercio.RAILWAY_PRIVATE_DOMAIN}}:8080/
Cors__FrontendOrigins__0=https://tu-proyecto.vercel.app
```

Reemplaza el origen de Vercel por el dominio exacto, sin barra final. Para otro dominio conocido agrega `Cors__FrontendOrigins__1`. La interfaz pública del piloto expone solo las rutas que usa el SPA.

## 4. Importar el frontend en Vercel

En Vercel usa **Add New → Project**, importa el mismo repositorio y selecciona **Root Directory → Edit → Frontend**. En **Framework Preset**, usa **Vite**. Si no lo detecta, comprueba que GitHub tenga `Frontend/package.json` con la dependencia `vite` y luego selecciona **Vite** manualmente.

Usa **Node.js 24.x** con el `package-lock.json` versionado. El comando es `npm run build` y la salida es `dist`. `Frontend/vercel.json` ya configura las rutas del SPA, como `/ofertas/...` y `/portafolio`.

En **Environment Variables** de Vercel configura:

```dotenv
VITE_API_URL=https://tu-gateway.up.railway.app
VITE_PILOTO=true
```

Asigna estas variables al entorno que vayas a desplegar. Vite incorpora sus valores durante la compilación. Si cambian, aplica un nuevo despliegue. Las variables con prefijo `VITE_` son públicas, así que el código de invitación, contraseñas y claves internas se mantienen en Railway.

## 5. Verificar el piloto

1. Abre `https://tu-gateway.up.railway.app/salud` y luego `/api/Ofertas/Catalogo`. La primera ruta debe responder 200. El catálogo puede comenzar vacío.
2. Abre el dominio Vercel y prueba entrar directamente a `/portafolio`. La aplicación debe abrir y redirigir al acceso si no hay sesión.
3. Registra una cuenta con la invitación, selecciona el plan piloto, crea el inmueble y entra con el usuario revisor desde otra sesión. Aprueba el expediente, emite, publica y completa una compra de prueba desde una segunda cuenta.

El piloto usa una base nueva y Stellar Testnet. Para trasladar ofertas ya emitidas desde otra instalación hay que conservar sus bases y las identidades de Stellar correspondientes.
