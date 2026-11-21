import { useEffect, useState, type FormEvent } from 'react'
import { ArrowUpRight, FileText, Plus, X } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiError, descargar, fecha, type Emision, type Expediente as Detalle, type Oferta } from '../api'
import { useAccion, useDatos } from '../hooks'
import { useSesion } from '../session'
import { Button, Campo, ErrorBox, Estado } from '../ui/atoms'
import { PageTitle, Progreso } from '../ui/molecules'

export function Expediente() {
  const { id } = useParams()
  const { sesion } = useSesion()
  const ruta = '/api/Inmuebles/' + id
  const rutaToken = '/api/Tokenizacion/Inmuebles/' + id
  const ficha = useDatos<Detalle>('/api/Inmuebles/Buscar/' + id, sesion, 4000)
  const expediente = ficha.data
  const estado = expediente?.inmueble.estadoRevision
  const editable = estado === 'BORRADOR' || estado === 'CON_OBSERVACIONES'
  const puedeEmitir = estado === 'APROBADO'
  const token = useDatos<Emision>(puedeEmitir ? rutaToken + '/Buscar' : null, sesion, 4000)
  const catalogo = useDatos<Oferta[]>(token.data?.estado === 'CONFIRMADA' ? '/api/Ofertas/Catalogo' : null, undefined, 8000)
  const oferta = catalogo.data?.find(o => o.inmuebleId === id)
  const action = useAccion()
  const [titulares, definirTitulares] = useState<number[]>([0])
  const [siguiente, setSiguiente] = useState(1)
  const [modificarFicha, setModificarFicha] = useState(false)
  const [confirmar, setConfirmar] = useState(false)
  const pendiente = token.data?.estado === 'PENDIENTE'
  const refrescarToken = token.refresh
  const recibirToken = token.recibir

  useEffect(() => {
    if (!pendiente || !sesion) return
    const controller = new AbortController()
    let timer: ReturnType<typeof setTimeout> | undefined
    let ocupado = false
    let terminar = false

    async function reconciliar() {
      if (ocupado || terminar || controller.signal.aborted || document.hidden) return
      ocupado = true
      clearTimeout(timer)
      try {
        const resultado = await api<Emision>(rutaToken + '/Reconciliar', {
          method: 'POST', sesion, signal: controller.signal,
        })
        if (!controller.signal.aborted && resultado.estado !== 'PENDIENTE') {
          terminar = true
          recibirToken(resultado)
          refrescarToken()
        }
      } catch (error) {
        if (error instanceof ApiError && [401, 403, 404].includes(error.status)) {
          terminar = true
          if (!controller.signal.aborted) refrescarToken()
        }
      } finally {
        ocupado = false
        if (!terminar && !controller.signal.aborted) timer = setTimeout(() => void reconciliar(), 5000)
      }
    }

    timer = setTimeout(() => void reconciliar(), 1000)
    document.addEventListener('visibilitychange', reconciliar)
    return () => {
      controller.abort()
      clearTimeout(timer)
      document.removeEventListener('visibilitychange', reconciliar)
    }
  }, [pendiente, recibirToken, refrescarToken, rutaToken, sesion])

  function guardar(path: string, method: string, body?: unknown) {
    void action.ejecutar(async () => {
      try {
        await api(path, { method, body, sesion })
        setConfirmar(false)
      } finally {
        ficha.refresh()
        if (path.includes('/Tokenizacion/')) token.refresh()
        if (path.includes('/Ofertas/')) catalogo.refresh()
      }
    })
  }

  function enviarTitulares(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const f = new FormData(e.currentTarget)
    guardar(ruta + '/Titulares', 'PUT', { titulares: titulares.map(t => {
      const valor = f.get('cuota' + t)
      return { nombre: f.get('nombre' + t), desde: f.get('desde' + t), cuota: valor ? Number(valor) / 100 : null }
    }) })
  }

  if (!expediente) return <section className="inner">
    <Link className="back-link" to="/espacio">← Volver al espacio</Link>
    {ficha.loading && <p role="status">Cargando expediente…</p>}<ErrorBox text={ficha.error}/>
  </section>

  const i = expediente.inmueble
  const documento = expediente.documentos.find(d => d.clase === 'TITULARIDAD')
  const puedeEnviar = expediente.titulares.length > 0 && Boolean(documento)
  return <section className="inner">
    <Link className="back-link" to="/espacio">← Volver al espacio</Link>
    <PageTitle title={i.nombre} description={`${i.ciudad} · ${i.superficieM2} m² · ${i.tipo}`}>
      <span role="status"><Estado value={estado || ''}/></span>
    </PageTitle>
    <Progreso current={oferta ? 3 : estado === 'APROBADO' ? 2 : estado === 'BORRADOR' ? 0 : 1}/>
    <ErrorBox text={action.error || ficha.error}/>
    <div className="expediente-grid">
      <div className="flow-stack">
        <section className="panel">
          <h2>Ficha del inmueble</h2><p>{i.direccion}</p>
          {editable && <Button type="button" variant="secondary" onClick={() => setModificarFicha(!modificarFicha)}>{modificarFicha ? 'Cerrar edición' : 'Editar ficha'}</Button>}
          {editable && modificarFicha && <form className="form-stack" onSubmit={e => {
            e.preventDefault()
            const f = new FormData(e.currentTarget)
            guardar('/api/Inmuebles/Actualizar/' + id, 'PUT', { ...Object.fromEntries(f), superficieM2: Number(f.get('superficieM2')) })
            }}>
            <Campo label="Nombre"><input name="nombre" defaultValue={i.nombre} required/></Campo>
            <Campo label="Tipo"><select name="tipo" defaultValue={i.tipo}>
              {[...new Set([i.tipo, 'CASA', 'DEPARTAMENTO', 'TERRENO', 'EDIFICIO'])].map(x => <option key={x}>{x}</option>)}
            </select></Campo>
            <Campo label="Ciudad"><input name="ciudad" defaultValue={i.ciudad} required/></Campo>
            <Campo label="Dirección"><input name="direccion" defaultValue={i.direccion} required/></Campo>
            <Campo label="Superficie en m²"><input name="superficieM2" defaultValue={i.superficieM2} type="number" step="0.01" min="0.01" required/></Campo>
            <Button disabled={action.busy}>Guardar ficha</Button>
          </form>}
        </section>
        <section className="panel">
          <h2>Titulares</h2>
          <ul className="plain-list">{expediente.titulares.map((t, index) => <li key={index}>
            {t.nombre}{t.cuota != null ? ` · ${t.cuota * 100}%` : ''}{t.verificada_en ? ' · Verificado' : ''}
          </li>)}</ul>
          {editable && <form className="form-stack" onSubmit={enviarTitulares}>
            {titulares.map((t, index) => <fieldset className="titular" key={t}>
              <legend>Titular {index + 1}</legend>
              <Campo label="Nombre completo"><input name={'nombre' + t} required/></Campo>
              <div className="form-grid"><Campo label="Desde"><input name={'desde' + t} type="date" required/></Campo>
                <Campo label="Porcentaje"><input name={'cuota' + t} type="number" min="0.01" max="100" step="0.01"/></Campo></div>
              {titulares.length > 1 && <Button type="button" variant="secondary" onClick={() => definirTitulares(titulares.filter(x => x !== t))}><X size={15}/> Quitar titular</Button>}
            </fieldset>)}
            <div className="actions"><Button type="button" variant="secondary" onClick={() => {
              definirTitulares([...titulares, siguiente]); setSiguiente(siguiente + 1)
            }}><Plus size={15}/> Agregar titular</Button>
            <Button disabled={action.busy}>Guardar titulares</Button></div>
          </form>}
        </section>
        <section className="panel">
          <h2>Documentos de respaldo</h2>
          <ul className="plain-list">{expediente.documentos.map(d => <li key={d.documentoId + ':' + d.version}>
            <button className="text-button" type="button" onClick={() => void action.ejecutar(() =>
              descargar(`${ruta}/Documentos/${d.documentoId}/Versiones/${d.version}`, sesion))}>
              <FileText size={16}/> {d.clase} · versión {d.version}
            </button>
          </li>)}</ul>
          {editable && <form className="form-stack" onSubmit={e => {
            e.preventDefault()
            const form = new FormData(e.currentTarget)
            if (!documento) form.append('clase', 'TITULARIDAD')
            guardar(documento ? `${ruta}/Documentos/${documento.documentoId}/Versiones` : ruta + '/Documentos', 'POST', form)
          }}>
            <Campo label={documento ? 'Nueva versión del respaldo' : 'Respaldo de titularidad'}>
              <input name="archivo" type="file" required />
            </Campo>
            <Button disabled={action.busy} variant="secondary">{documento ? 'Guardar nueva versión' : 'Adjuntar respaldo'}</Button>
          </form>}
        </section>
      </div>
      <aside className="flow-stack">
        <section className="panel">
          <h2>Revisión</h2>
          {expediente.revisiones.length ? <ol className="plain-list">{expediente.revisiones.map(r => <li key={r.id}>
            <Estado value={r.decision}/> <time>{fecha(r.revisado_en)}</time>{r.observacion && <p>{r.observacion}</p>}
          </li>)}</ol> : <p>Aún no hay decisiones registradas.</p>}
          {editable && <Button disabled={!puedeEnviar || action.busy} onClick={() => guardar(ruta + '/EnviarRevision', 'POST')}>
            Enviar a revisión
          </Button>}
          {editable && !puedeEnviar && <p>Agrega un titular y un documento de respaldo para enviar.</p>}
          {estado === 'EN_REVISION' && <p>La decisión aparecerá aquí automáticamente.</p>}
        </section>
        {puedeEmitir && <section className="panel">
          <h2>Tokenización</h2>
          {token.loading && <p role="status">Consultando emisión…</p>}
          {pendiente && <p role="status">Consultando la confirmación en Stellar Testnet…</p>}
          {!token.data && token.status !== 404 && <ErrorBox text={token.error}/>}
          {token.data && <><span role="status"><Estado value={token.data.estado}/></span><p>{token.data.unidades} {token.data.codigo} · Stellar Testnet</p>
            {token.data.hash && <a className="text-button" target="_blank" rel="noreferrer" href={'https://stellar.expert/explorer/testnet/tx/' + token.data.hash}>Ver transacción <ArrowUpRight size={16}/></a>}</>}
          {!token.data && token.status === 404 && <form className="form-stack" onSubmit={e => {
            e.preventDefault()
            const f = new FormData(e.currentTarget)
            if (!confirmar) { setConfirmar(true); return }
            guardar(rutaToken + '/Emitir', 'POST', { codigo: f.get('codigo'), unidades: Number(f.get('unidades')) })
          }}>
            <Campo label="Código del token"><input name="codigo" pattern="[A-Z0-9]{1,12}" defaultValue="INM001" required/></Campo>
            <Campo label="Unidades"><input name="unidades" type="number" defaultValue={500} step="1" min="1" required/></Campo>
            <Button disabled={action.busy}>{action.busy ? 'Enviando a Stellar…' : confirmar ? 'Confirmar emisión en Testnet' : 'Preparar emisión'}</Button>
          </form>}
        </section>}
        {token.data?.estado === 'CONFIRMADA' && <section className="panel">
          <h2>Publicar oferta</h2>
          <ErrorBox text={catalogo.error}/>
          {oferta ? <Link className="button button-secondary" to={'/ofertas/' + oferta.id}>Ver oferta pública <ArrowUpRight size={16}/></Link>
          : <form className="form-stack" onSubmit={e => {
            e.preventDefault()
            const f = new FormData(e.currentTarget)
            guardar('/api/Ofertas/' + id + '/Publicar', 'POST',
              { precio: Number(f.get('precio')), unidades: Number(f.get('unidades')), moneda: f.get('moneda') })
          }}>
            <Campo label="Precio por unidad"><input name="precio" type="number" min="0.01" step="0.01" required/></Campo>
            <Campo label="Moneda"><select name="moneda"><option>BOB</option><option>USD</option></select></Campo>
            <Campo label="Unidades ofrecidas"><input name="unidades" type="number" min="1" step="1" max={token.data.unidades} required/></Campo>
            <Button disabled={action.busy}>Publicar en el catálogo</Button>
          </form>}
        </section>}
      </aside>
    </div>
  </section>
}
