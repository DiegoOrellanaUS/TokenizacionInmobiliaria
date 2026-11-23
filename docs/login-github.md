# Login de Umbral con GitHub

## Registro en GitHub

En **Settings → Developer settings → OAuth Apps**, crea una OAuth App para Umbral.

- **Homepage URL**. `https://tokenizacion-inmobiliaria.vercel.app`
- **Authorization callback URL**. `https://tokenizacion-inmobiliaria.vercel.app/api/Auth/GitHub/Callback`
- **Device Flow**. Desactivado. El SPA usa el flujo de autorización web con PKCE.

Para otro dominio, registra el callback exacto que termina en `/api/Auth/GitHub/Callback` y desactiva la coincidencia por comodines.

## Servicios

`Backend/Login` es un proceso independiente. Autentica con GitHub, provisiona el perfil y emite los tokens de Umbral. Comparte con Admin la base central de identidades y sesiones. Inmuebles, Comercio y Tokenización siguen verificando la identidad contra Admin. No necesitan las credenciales OAuth ni el token de GitHub.

La base guarda solo hashes de los tokens de sesión. El access token dura diez minutos y el refresh token tiene una vigencia total de siete días. La rotación revoca la familia si vuelve a aparecer un refresh token usado. El refresh token se guarda en una cookie segura HttpOnly. El SPA guarda el access token en memoria.

## Railway

Crea un servicio privado `login` desde el repositorio, con **Root Directory** `/Backend` y **RAILWAY_DOCKERFILE_PATH** `Login/Dockerfile`. Usa puerto 8080 y `/salud`. Adjunta un volumen en `/data` para conservar las claves de protección del desafío OAuth entre despliegues.

Variables del servicio `login`:

```dotenv
ASPNETCORE_HTTP_PORTS=8080
PORT=8080
GitHubOAuth__Habilitado=true
GitHubOAuth__Origen=https://tokenizacion-inmobiliaria.vercel.app
GitHubOAuth__ClientId=ID_DE_LA_OAUTH_APP
GitHubOAuth__ClientSecret=SECRETO_NUEVO_DE_GITHUB
GitHubOAuth__DirectorioClaves=/data/login
ConnectionStrings__ArchivosMultitenenciaAdministracionContext=CADENA_PRIVADA_DE_ADMIN
```

En `gateway` apunta el nuevo cluster a Login:

```dotenv
ReverseProxy__Clusters__login__Destinations__principal__Address=http://${{login.RAILWAY_PRIVATE_DOMAIN}}:8080/
```

Despliega primero el código actualizado para que Admin aplique la migración `SesionesOAuth`. Comprueba que `/api/Auth/GitHub/Estado` llega a Login. Activa finalmente `GitHubOAuth__Habilitado=true` en **Admin**. En **Piloto**, el acceso con contraseña permanece disponible junto a los proveedores configurados.

Para Facebook, registra `https://tokenizacion-inmobiliaria.vercel.app/api/Auth/Facebook/Callback` como URI válido de redirección. Configura `FacebookOAuth__AppId`, `FacebookOAuth__AppSecret`, `FacebookOAuth__Origen` y `FacebookOAuth__Habilitado=true` en **Login**. Una vez comprobado el acceso al proveedor, activa `FacebookOAuth__Habilitado=true` en **Admin**.

Un perfil previo se conserva cuando un administrador vincula explícitamente el **ID numérico de GitHub** al UUID de ese usuario en `GitHubOAuth__Vinculos__ID_NUMERICO`, en **Login**. El correo por sí solo no trata un perfil anterior como propio. Para conservar la revisión de inmuebles, vincula la cuenta GitHub del revisor y configura `Inmuebles__Revisores__0` con su UUID de Umbral en Login. Admin mantiene su lista de revisores para autorizar las operaciones.

## Vincular Facebook a un perfil de GitHub

Con tu sesión de GitHub abierta, entra a **Conexiones → Vincular Facebook**. Autoriza Facebook y confirma en Umbral la cuenta de Facebook mostrada. Después puedes usar cualquiera de los dos botones de acceso con el mismo perfil y portafolio. Si Facebook ya pertenece a otro perfil de Umbral, la vinculación se rechaza.

Admin debe aplicar la migración `VinculosOAuth` antes de habilitar esta versión de Login. La migración agrega una tabla de vínculos junto a los perfiles existentes.

## Desarrollo local

Login escucha en `http://localhost:7320` y el proxy de Vite reenvía las rutas de autenticación. Configura la cadena de conexión y las credenciales mediante secretos de .NET. Para completar la autorización en local, usa un origen de frontend HTTPS y registra exactamente su callback en GitHub.
