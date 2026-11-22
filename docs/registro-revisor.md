# Registro de revisores de Umbral

En **Acceder → Crear cuenta**, marca **Crear cuenta como revisor**. En la demo el formulario precarga un código de un solo uso. Tras el alta se abre el **Panel de revisor**, donde se elige un espacio, se consultan sus expedientes y documentos y se registra el dictamen.

- Activa el banco precargado en Admin y Login con `Revision__Demostracion=true` en el entorno de demo. En Development viene habilitado. El banco trae ocho códigos y el formulario muestra el primero disponible. Si otro juez ya usó el código mostrado, **Actualizar código** ofrece el siguiente.
- Para habilitar invitaciones privadas, configura los mismos valores `Revision__Codigos__0`, `Revision__Codigos__1`, etc., en Admin y Login. Se ingresan manualmente y quedan consumidos en la base incluso si se reinician los servicios.
- En modo OAuth, el botón de alta utiliza GitHub o Facebook según los proveedores habilitados. La invitación se aplica al crear una identidad nueva. Un revisor que ya creó su cuenta puede volver a entrar con su proveedor.
- Aplica la migración `InvitacionesRevision` de Admin en la base compartida con Login antes de usar el nuevo registro.
