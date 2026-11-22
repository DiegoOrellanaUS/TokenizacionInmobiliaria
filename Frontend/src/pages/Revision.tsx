import { useState } from 'react'
import { ArrowRight, ClipboardCheck, FileText, FolderSearch } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { api, descargar, fecha, type Expediente, type Inmueble } from '../api'
import { useAccion, useDatos } from '../hooks'
import { useSesion } from '../session'
import { Button, Campo, ErrorBox, Estado } from '../ui/atoms'
import { Empty, PageTitle } from '../ui/molecules'

const intervalo = 4000
const filtros = [
  { id: 'EN_REVISION', label: 'Por revisar' },
  { id: 'CON_OBSERVACIONES', label: 'Con observaciones' },
  { id: 'APROBADO', label: 'Aprobados' },
] as const

type EspacioRevision = { tenantId: string; nombre: string }

export function RevisionBandeja() {
  const { sesion, tenant } = useSesion()
  const [id, setId] = useState(sesion?.tenantId ?? '')
  const [filtro, setFiltro] = useState<string>('EN_REVISION')
  const [cambiar, setCambiar] = useState(false)
  const espacios = useDatos<EspacioRevision[]>('/api/SesionDemo/EspaciosRevision', sesion, intervalo)
  const seleccionado = espacios.data?.find(e => e.tenantId === sesion?.tenantId)
  const bandeja = useDatos<Inmueble[]>(seleccionado ? '/api/Inmuebles/Listar' : null, sesion, intervalo)
  const expedientes = bandeja.data ?? []
  const visibles = expedientes.filter(i => i.estadoRevision === filtro)
  return <section className="inner">
    <PageTitle title="Bandeja de revisión" description="Consulta expedientes, revisa el respaldo y registra tu decisión.">
      {sesion?.tenantId && <Button variant="secondary" type="button" onClick={() => setCambiar(!cambiar)}>
        {cambiar ? 'Cerrar selector' : 'Cambiar espacio'}
      </Button>}
    </PageTitle>
    <ErrorBox text={espacios.error}/>
    {espacios.loading && <p role="status">Cargando espacios de revisión…</p>}
    {espacios.data?.length === 0 && <Empty title="Sin espacios para revisar" description="Los espacios habilitados aparecerán aquí."/>}
    {!!espacios.data?.length && (!sesion?.tenantId || cambiar || !seleccionado) && <form className="panel form-stack compact mb-6" onSubmit={e => {
      e.preventDefault()
      if (!espacios.data?.some(e => e.tenantId === id)) return
      tenant(id)
      setFiltro('EN_REVISION')
      setCambiar(false)
    }}>
      <Campo label="Espacio para revisar"><select value={id} onChange={e => setId(e.target.value)} required>
        <option value="">Selecciona un espacio</option>
        {espacios.data.map(e => <option key={e.tenantId} value={e.tenantId}>{e.nombre} · {e.tenantId.slice(0, 8)}</option>)}
      </select></Campo>
      <Button>Abrir expedientes <ArrowRight size={16}/></Button>
    </form>}
    {seleccionado && <>
      <p className="muted mb-4">Espacio de {seleccionado.nombre}</p>
      <div className="review-queue" role="group" aria-label="Filtrar expedientes por estado">
        {filtros.map(x => <button type="button" aria-pressed={x.id === filtro}
          onClick={() => setFiltro(x.id)} key={x.id}>
          {x.label} <strong>{expedientes.filter(i => i.estadoRevision === x.id).length}</strong>
        </button>)}
      </div>
      <ErrorBox text={bandeja.error}/>
      {bandeja.loading && <p role="status">Cargando expedientes…</p>}
      {bandeja.data && (visibles.length ? <ul className="property-list">
        {visibles.map(i => <li key={i.id}>
          <Link to={'/revision/expedientes/' + i.id}>
            <span className="property-icon"><FolderSearch size={20}/></span>
            <span className="property-main"><strong>{i.nombre}</strong><small>{i.ciudad} · {i.tipo}</small></span>
            <Estado value={i.estadoRevision}/><ArrowRight className="property-arrow" size={17}/>
          </Link>
        </li>)}
      </ul> : <Empty title="No hay expedientes en esta bandeja"
         description="La lista se actualiza automáticamente cuando cambian los estados." />)}
    </>}
  </section>
}

export function RevisionDetalle() {
  const { id } = useParams()
  const { sesion } = useSesion()
  const ruta = '/api/Inmuebles/' + id
  const ficha = useDatos<Expediente>('/api/Inmuebles/Buscar/' + id, sesion, intervalo)
  const action = useAccion()
  const expediente = ficha.data
  const estado = expediente?.inmueble.estadoRevision
  return <section className="inner">
    <Link className="back-link" to="/revision">← Bandeja de revisión</Link>
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
                 descargar(`${ruta}/Documentos/${d.documentoId}/Versiones/${d.version}`, sesion))}>
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
                    await api(ruta + '/ResolverRevision', { method: 'POST', sesion,
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
