import { useState, type FormEvent } from 'react'
import { ArrowRight, Building2, CirclePlus, Copy, MapPin } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'
import { api, moneda, type Inmueble, type Plan, type Suscripcion } from '../api'
import { useAccion, useDatos } from '../hooks'
import { useSesion } from '../session'
import { Button, Campo, ErrorBox, Estado } from '../ui/atoms'
import { Empty, PageTitle } from '../ui/molecules'

export function Espacio() {
  const { sesion } = useSesion()
  const [copiado, setCopiado] = useState(false)
  const { data, error, loading } = useDatos<Inmueble[]>(sesion?.tenantId ? '/api/Inmuebles/Listar' : null, sesion, 4000)
  if (!sesion) return null
  if (!sesion.tenantId) return <section className="inner">
    <PageTitle title={`Hola, ${sesion.nombre}`} description="Elige un plan para crear tu espacio de trabajo."/>
    <Link to="/planes" className="button button-primary">Ver planes <ArrowRight size={16}/></Link>
  </section>
  return <section className="inner">
    <PageTitle title="Mis inmuebles" description="Consulta el estado de cada inmueble desde el registro hasta la publicación.">
      <Link className="button button-primary" to="/espacio/nuevo"><CirclePlus size={18}/> Nuevo inmueble</Link>
    </PageTitle>
    <div className="tenant-bar">
      <Button variant="secondary" type="button" onClick={() => {
        void navigator.clipboard.writeText(sesion.tenantId || '').then(() => setCopiado(true), () => setCopiado(false))
      }}><Copy size={15}/>{copiado ? 'Tenant copiado' : 'Copiar ID del tenant'}</Button>
      <code>{sesion.tenantId}</code>
    </div>
    <ErrorBox text={error}/>
    {loading && <p role="status">Cargando inmuebles…</p>}
    {data && (data.length ? <ul className="property-list">
      {data.map(i => <li key={i.id}>
        <Link to={'/espacio/inmuebles/' + i.id}>
          <span className="property-icon"><Building2 size={22}/></span>
          <span className="property-main"><strong>{i.nombre}</strong><small><MapPin size={14}/>{i.ciudad} · {i.superficieM2} m²</small></span>
          <Estado value={i.estadoRevision}/><ArrowRight className="property-arrow" size={17}/>
        </Link>
      </li>)}
    </ul> : <Empty title="Aún no hay inmuebles abiertos" description="Los expedientes de este espacio aparecerán aquí."
      href="/espacio/nuevo" label="Registrar inmueble"/>)}
  </section>
}

export function Planes() {
  const list = useDatos<Plan[]>('/api/Planes/Listar')
  const { sesion, tenant } = useSesion()
  const [elegido, elegir] = useState<Plan | null>(null)
  const [ref, fijarRef] = useState('')
  const [suscripcion, guardar] = useState<Suscripcion | null>(null)
  const action = useAccion()
  const navigate = useNavigate()
  function pagar() {
    if (!elegido || !sesion) return
    const referenciaPago = ref || 'web-mvp-' + crypto.randomUUID()
    fijarRef(referenciaPago)
    void action.ejecutar(async () => {
      const result = await api<Suscripcion>('/api/Subscripciones/crear', {
        method: 'POST', sesion, body: { usuarioId: sesion.usuarioId, codigoPlan: elegido.codigo, referenciaPago },
      })
      guardar(result)
      if (result.estado === 'ACTIVA') tenant(result.tenantId)
    })
  }
  return <section className="inner">
    <PageTitle title="Elige tu plan" description="Tu suscripción habilita tu espacio de trabajo y define cuántos inmuebles puedes tokenizar."/>
    <ErrorBox text={list.error}/>
    {list.loading && <p role="status">Cargando planes…</p>}
    <div className="offer-grid">{list.data?.filter(p => p.estado === 'ACTIVO').map(p =>
      <article className="panel plan" key={p.id} data-selected={elegido?.id === p.id}>
        <h2>{p.nombre}</h2><p className="plan-price">{moneda(p.precio, p.moneda)} <small> / {p.periodicidad === 'ANUAL' ? 'año' : 'mes'}</small></p>
        <p>{p.descripcion}</p><p>{p.max_tokens_propios} emisiones propias</p>
        <Button variant={elegido?.id === p.id ? 'primary' : 'secondary'} type="button" disabled={action.busy}
          onClick={() => { elegir(p); fijarRef(''); guardar(null) }}>Elegir {p.nombre}</Button>
      </article>)}</div>
    {elegido && <section className="panel checkout">
      <h2>Confirmar {elegido.nombre}</h2>
      <p>Pago de demostración · {moneda(elegido.precio, elegido.moneda)}</p>
      {suscripcion && <><Estado value={suscripcion.estado}/><ul className="service-list">
        {suscripcion.servicios.map(x => <li key={x.servicio}>{x.servicio} <Estado value={x.estado}/></li>)}
      </ul></>}
      {suscripcion?.estado === 'ACTIVA'
        ? <Button onClick={() => navigate('/espacio')}>Ir a mis inmuebles <ArrowRight size={16}/></Button>
        : <Button disabled={action.busy} onClick={() => sesion ? pagar() : navigate('/acceso')}>
            {action.busy ? 'Preparando servicios…' : suscripcion ? 'Reintentar aprovisionamiento' : sesion ? 'Confirmar pago de prueba' : 'Acceder para continuar'}
          </Button>}
      <ErrorBox text={action.error}/>
    </section>}
  </section>
}

export function NuevoInmueble() {
  const { sesion } = useSesion()
  const action = useAccion()
  const navigate = useNavigate()
  function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const fields = new FormData(e.currentTarget)
    void action.ejecutar(async () => {
      const i = await api<{ id: string }>('/api/Inmuebles/Crear', {
        method: 'POST', sesion, body: { ...Object.fromEntries(fields), superficieM2: Number(fields.get('superficieM2')) },
      })
      navigate('/espacio/inmuebles/' + i.id)
    })
  }
  return <section className="inner">
    <Link className="back-link" to="/espacio">← Mis inmuebles</Link>
    <PageTitle title="Registrar inmueble" description="Completa la ficha. Luego podrás agregar titulares y documentos."/>
    <form className="panel form-stack compact" onSubmit={submit}>
      <Campo label="Nombre del inmueble"><input name="nombre" required maxLength={200}/></Campo>
      <Campo label="Tipo"><select name="tipo" required><option value="CASA">Casa</option><option value="DEPARTAMENTO">Departamento</option><option value="TERRENO">Terreno</option><option value="EDIFICIO">Edificio</option></select></Campo>
      <div className="form-grid"><Campo label="Ciudad"><input name="ciudad" required/></Campo>
        <Campo label="Superficie en m²"><input name="superficieM2" type="number" min="0.01" step="0.01" required/></Campo></div>
      <Campo label="Dirección"><input name="direccion" required/></Campo>
      <Button disabled={action.busy}>{action.busy ? 'Guardando…' : 'Guardar borrador'}</Button>
      <ErrorBox text={action.error}/>
    </form>
  </section>
}
