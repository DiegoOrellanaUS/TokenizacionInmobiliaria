import { Navigate, useSearchParams } from 'react-router-dom'
import { api } from '../api'
import { useAccion, useDatos } from '../hooks'
import { rutaFacebook } from '../oauth'
import { useSesion } from '../session'
import { Button, ErrorBox } from '../ui/atoms'
import { PageTitle } from '../ui/molecules'

export const rutaConexiones = '/cuenta/conexiones'

export function Conexiones() {
  const { sesion, modo, proveedores } = useSesion()
  if (modo === 'cargando') return <p role="status">Consultando sesión…</p>
  if (!sesion) return <Navigate to="/acceso" replace/>
  if (!sesion.venceEn || !proveedores.facebook) return <Navigate to={sesion.esRevisor ? '/revision' : '/espacio'} replace/>
  return <VinculoFacebook key={sesion.usuarioId}/>
}

function VinculoFacebook() {
  const [query] = useSearchParams()
  const accion = useAccion()
  const vinculo = useDatos<{ vinculado: boolean; puedeVincular: boolean; facebookPendiente: string | null }>(rutaFacebook + '/Vinculo')
  const datos = vinculo.data

  function confirmar() {
    void accion.ejecutar(async () => {
      try { await api(rutaFacebook + '/ConfirmarVinculo', { method: 'POST' }) }
      finally { vinculo.refresh() }
    })
  }

  return <section className="inner flow-stack">
    <PageTitle title="Conexiones de mi cuenta"/>
    <section className="panel flow-stack" aria-label="Facebook">
      <h2>Facebook</h2>
      <ErrorBox text={accion.error || vinculo.error || (query.get('facebook') === 'fallo' && !datos?.vinculado
        ? 'Facebook no completó la vinculación. Puedes volver a intentarlo.' : undefined)}/>
      {vinculo.loading && <p role="status">Consultando conexión…</p>}
      {datos?.vinculado && <p role="status">Facebook está vinculado a este perfil de Umbral.</p>}
      {datos && !datos.vinculado && !datos.puedeVincular && <p>Inicia sesión con GitHub para vincular Facebook.</p>}
      {datos?.puedeVincular && <>
        {datos.facebookPendiente && <>
          <p>Confirma que quieres vincular <strong>{datos.facebookPendiente}</strong> a este perfil. Conservarás tus compras y tu portafolio.</p>
          <Button type="button" disabled={accion.busy} onClick={confirmar}>Confirmar vínculo con Facebook</Button>
        </>}
        <form method="post" action={rutaFacebook + '/Vincular'}>
          <Button type="submit" variant={datos.facebookPendiente ? 'secondary' : 'primary'} disabled={accion.busy}>
            {datos.facebookPendiente ? 'Elegir otra cuenta de Facebook' : 'Vincular Facebook'}
          </Button>
        </form>
      </>}
    </section>
  </section>
}
