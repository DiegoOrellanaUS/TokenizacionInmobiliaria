import { useMemo, useState } from 'react'
import { ClipboardCheck, FileText, FolderSearch } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { api, descargar, fecha, moneda, type Compra, type Expediente } from '../api'
import { useAccion, useDatos } from '../hooks'
import { useSesion } from '../session'
import { Button, Campo, ErrorBox, Estado } from '../ui/atoms'
import { ComprobantesCompra } from '../ui/ComprobantesCompra'
import { Empty, PageTitle } from '../ui/molecules'

const intervalo = 4000
const ordenEstado = ['EN_REVISION', 'APROBADO', 'CON_OBSERVACIONES']
const fechaSolicitud = new Intl.DateTimeFormat('es-BO', {
  dateStyle: 'short', timeStyle: 'short', timeZone: 'America/La_Paz',
})

type Caso = { id: string; nombre: string; estadoRevision: string; solicitadoEn: string | null }
type FilaRevision = {
  persona: { id: string; nombres: string; apellidos: string | null; tenantId: string | null; espacio: string | null }
  inmuebles: Caso[]
  noDisponible: boolean
}

function ordenCaso(a: Caso, b: Caso) {
  const fechaA = a.solicitadoEn ? Date.parse(a.solicitadoEn) : Number.MAX_SAFE_INTEGER
  const fechaB = b.solicitadoEn ? Date.parse(b.solicitadoEn) : Number.MAX_SAFE_INTEGER
  return ordenEstado.indexOf(a.estadoRevision) - ordenEstado.indexOf(b.estadoRevision) || fechaA - fechaB
}

export function RevisionBandeja() {
  const { sesion } = useSesion()
  const bandeja = useDatos<FilaRevision[]>('/api/SesionDemo/MuroRevision', sesion, 12000)
  const filas = (bandeja.data ?? [])
    .map(fila => ({ ...fila, inmuebles: [...fila.inmuebles].sort((a, b) =>
      ordenCaso(a, b) || a.nombre.localeCompare(b.nombre, 'es-BO') || a.id.localeCompare(b.id)) }))
    .sort((a, b) => {
      const primero = a.inmuebles[0]
      const segundo = b.inmuebles[0]
      if (!primero) return segundo ? 1 : 0
      return segundo ? ordenCaso(primero, segundo) : -1
    })
  return <section className="inner flow-stack">
    <PageTitle title="Muro de solicitudes"
      description="Consulta los inmuebles por persona. Las solicitudes pendientes de aprobación más antiguas aparecen primero."/>
    <ErrorBox text={bandeja.error}/>
    {bandeja.loading && <p role="status">Cargando solicitudes…</p>}
    {bandeja.data && (filas.length ? <div className="panel kpi-tabla">
      <table className="review-table">
        <thead><tr><th scope="col">Persona</th><th scope="col">Inmuebles y expedientes</th></tr></thead>
        <tbody>{filas.map(({ persona, inmuebles, noDisponible }) => <tr key={persona.id}>
          <th scope="row">{[persona.nombres, persona.apellidos].filter(Boolean).join(' ')}
            {persona.espacio && <small>{persona.espacio}</small>}
          </th>
          <td>{persona.tenantId && inmuebles.length ? <ul className="review-cases">
            {inmuebles.map(caso => <li key={caso.id}>
              <Link className="button button-secondary"
                to={'/revision/expedientes/' + persona.tenantId + '/' + caso.id}>
                <FolderSearch size={16} aria-hidden="true"/> {caso.nombre}
              </Link>
              <Estado value={caso.estadoRevision}/>
              {caso.solicitadoEn && <time dateTime={caso.solicitadoEn}>{fechaSolicitud.format(Date.parse(caso.solicitadoEn))}</time>}
            </li>)}
          </ul> : noDisponible ? <span role="status">Expedientes temporalmente no disponibles</span> : 'Sin solicitudes'}</td>
        </tr>)}</tbody>
      </table>
    </div> : <Empty title="Sin personas registradas" description="Las personas y sus solicitudes aparecerán aquí."/>)}
  </section>
}

export function RevisionTransacciones() {
  const { sesion } = useSesion()
  const [pagina, cambiarPagina] = useState(1)
  const { data, error, loading } = useDatos<{ compras: Compra[]; haySiguiente: boolean }>(
    '/api/Compras/Revision?pagina=' + pagina, sesion, 12000)
  return <section className="inner flow-stack">
    <PageTitle title="Transacciones de la plataforma"
      description="Consulta el estado de las compras y sus comprobantes en Stellar Testnet."/>
    <ErrorBox text={error}/>
    {loading && <p role="status">Cargando transacciones…</p>}
    {data && (data.compras.length ? <div className="panel kpi-tabla">
      <table className="review-table">
        <thead><tr><th scope="col">Inmueble y activo</th><th scope="col">Fecha</th><th scope="col">Unidades</th>
          <th scope="col">Total</th><th scope="col">Estado</th><th scope="col">Comprobantes</th></tr></thead>
        <tbody>{data.compras.map(compra => <tr key={compra.id}>
          <th scope="row">{compra.inmueble}<small>{compra.codigo}</small></th>
          <td><time dateTime={compra.creadaEn}>{fechaSolicitud.format(Date.parse(compra.creadaEn))}</time></td>
          <td>{compra.unidades}</td><td>{moneda(compra.total, compra.moneda)}</td>
          <td><Estado value={compra.estado}/></td>
          <td>{compra.hashPago || (compra.estado === 'CONFIRMADA' && compra.hash)
            ? <ComprobantesCompra compra={compra}/> : 'Sin comprobantes'}</td>
        </tr>)}</tbody>
      </table>
    </div> : <p>No hay transacciones en esta página.</p>)}
    {data && (pagina > 1 || data.haySiguiente) && <nav className="kpi-descargas" aria-label="Páginas de transacciones">
      <Button type="button" variant="secondary" disabled={pagina === 1} onClick={() => cambiarPagina(pagina - 1)}>Anterior</Button>
      <span>Página {pagina}</span>
      <Button type="button" variant="secondary" disabled={!data.haySiguiente} onClick={() => cambiarPagina(pagina + 1)}>Siguiente</Button>
    </nav>}
  </section>
}

export function RevisionDetalle() {
  const { id, tenantId } = useParams()
  const { sesion } = useSesion()
  const acceso = useMemo(() => sesion && tenantId ? { ...sesion, tenantId } : null, [sesion, tenantId])
  const ruta = '/api/Inmuebles/' + id
  const ficha = useDatos<Expediente>(acceso?.tenantId ? '/api/Inmuebles/Buscar/' + id : null, acceso, intervalo)
  const action = useAccion()
  const expediente = ficha.data
  const estado = expediente?.inmueble.estadoRevision
  return <section className="inner">
    <Link className="back-link" to="/revision">← Muro de solicitudes</Link>
    <ErrorBox text={action.error || ficha.error}/>
    {ficha.loading && <p role="status">Cargando expediente…</p>}
    {expediente && <>
      <PageTitle title={expediente.inmueble.nombre}
        description={`${expediente.inmueble.ciudad} · ${expediente.inmueble.superficieM2} m² · ${expediente.inmueble.tipo}`}>
        <span role="status"><Estado value={estado ?? ''}/></span>
      </PageTitle>
      <div className="expediente-grid">
        <div className="flow-stack">
          <section className="panel"><h2>Ficha y titulares</h2><p>{expediente.inmueble.direccion}</p>
            <ul className="plain-list">{expediente.titulares.map((t, index) => <li key={index}>
              {t.nombre}{t.cuota != null ? ` · ${t.cuota * 100}%` : ''} · Desde {t.desde}
            </li>)}</ul>
          </section>
          <section className="panel"><h2>Documentos de respaldo</h2>
            <ul className="plain-list">{expediente.documentos.map(d => <li key={d.documentoId + ':' + d.version}>
              <button className="text-button" type="button" onClick={() => void action.ejecutar(() =>
                 descargar(`${ruta}/Documentos/${d.documentoId}/Versiones/${d.version}`, acceso))}>
                <FileText size={16}/> {d.clase} · versión {d.version}
              </button>
            </li>)}</ul>
          </section>
        </div>
        <aside className="flow-stack">
          <section className="panel review-decision"><h2 className="flex items-center gap-2"><ClipboardCheck size={21}/> Dictamen</h2>
            {estado === 'EN_REVISION' && expediente.envioId ? <form className="form-stack" key={expediente.envioId}
               onSubmit={e => {
                e.preventDefault()
                const f = new FormData(e.currentTarget)
                const decision = String(f.get('decision') ?? '')
                const observacion = String(f.get('observacion') ?? '').trim()
                void action.ejecutar(async () => {
                  if (decision === 'CON_OBSERVACIONES' && !observacion)
                    throw new Error('Indica qué debe corregir el propietario.')
                  try {
                    await api(ruta + '/ResolverRevision', { method: 'POST', sesion: acceso,
                      body: { envioId: expediente.envioId, decision, observacion } })
                  } finally { ficha.refresh() }
                })
               }}>
              <Campo label="Decisión"><select name="decision"><option value="APROBADO">Aprobar</option>
                <option value="CON_OBSERVACIONES">Solicitar correcciones</option></select></Campo>
              <Campo label="Observación"><textarea name="observacion" rows={3} maxLength={2000}/></Campo>
              <Button disabled={action.busy}>Registrar decisión</Button>
            </form> : <p>{estado === 'CON_OBSERVACIONES' ? 'El expediente está a la espera de correcciones.' : 'La revisión ya se resolvió.'}</p>}
          </section>
          <section className="panel"><h2>Historial de revisión</h2>
            {expediente.revisiones.length ? <ol className="plain-list">{expediente.revisiones.map(r => <li key={r.id}>
              <Estado value={r.decision}/> <time dateTime={r.revisado_en}>{fecha(r.revisado_en)}</time>
              {r.observacion && <p>{r.observacion}</p>}
            </li>)}</ol> : <p>Aún no hay decisiones registradas.</p>}
          </section>
        </aside>
      </div>
    </>}
  </section>
}
