import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError, fecha, moneda, type Compra, type Oferta } from '../api'
import { useAccion, useDatos } from '../hooks'
import { useSesion } from '../session'
import { Button, Campo, ErrorBox, Estado } from '../ui/atoms'
import { Empty, PageTitle } from '../ui/molecules'
import { ComprobantesCompra } from '../ui/ComprobantesCompra'
import { idCasaPrado } from '../ui/Stellar'
import { CompraPollar } from './CompraPollar'
import { RentasPortafolio } from './Rentas'

export function CompraOferta(props: { oferta: Oferta; actualizarOferta: () => void }) {
  const { sesion } = useSesion()
  const demo = useDatos<{ habilitada: boolean }>(sesion?.esRevisor ? null : '/api/Compras/Demo')
  if (sesion?.esRevisor) return null
  return <>
    {demo.data?.habilitada && <CompraDemo key={sesion?.usuarioId} {...props}/>}
    {props.oferta.id === idCasaPrado && <CompraPollar {...props}/>}
  </>
}

function CompraDemo({ oferta, actualizarOferta }: { oferta: Oferta; actualizarOferta: () => void }) {
  const { sesion } = useSesion()
  const action = useAccion()
  const [cantidad, setCantidad] = useState('1')
  const [compra, setCompra] = useState<Compra>()
  const intento = useRef<{ referencia: string; unidades: number } | null>(null)
  const lock = useRef(false)
  const seguimiento = useDatos<Compra>(compra?.estado === 'PENDIENTE' ? '/api/Compras/Buscar/' + compra.id : null, sesion, 3000)
  const resultado = compra?.estado === 'CANCELADA' ? compra : seguimiento.data ?? compra
  const estado = resultado?.estado

  useEffect(() => {
    if (estado === 'CONFIRMADA' || estado === 'FALLIDA' || estado === 'CANCELADA') actualizarOferta()
  }, [estado, actualizarOferta])

  return <section className="panel flow-stack"><h2>Compra de demostración</h2>
    <p>Recibe {oferta.codigo} en Stellar Testnet sin enviar un pago.</p>
    {!sesion ? <Link className="button button-primary" to={'/acceso?volver=' + encodeURIComponent('/ofertas/' + oferta.id)}>
      Compra demo
    </Link> : resultado ? <>
      <div aria-live="polite"><Estado value={resultado.estado}/>
        <p>{resultado.estado === 'PENDIENTE' ? 'Compra demo registrada. Estamos confirmando la entrega en Stellar Testnet.'
          : resultado.estado === 'CONFIRMADA' ? 'Las unidades ya están en tu portafolio.'
            : resultado.estado === 'CANCELADA' ? 'Cancelaste la compra y se liberaron las unidades reservadas.'
              : 'La transferencia falló y se liberaron las unidades reservadas.'}</p>
      </div>
      <ErrorBox text={seguimiento.error}/>
      <ComprobantesCompra compra={resultado}/>
      {resultado.puedeCancelar && <Button type="button" variant="secondary" disabled={action.busy} onClick={() => {
        void action.ejecutar(async () => {
          try {
            setCompra(await api<Compra>('/api/Compras/' + resultado.id + '/Cancelar', { method: 'POST', sesion }))
          } finally { seguimiento.refresh() }
        })
      }}>Cancelar compra</Button>}
      <Link className="button button-primary" to="/portafolio">Ver mi portafolio</Link>
      {resultado.estado !== 'PENDIENTE' && oferta.disponibles > 0 && <Button type="button" variant="secondary" onClick={() => {
        intento.current = null
        setCompra(undefined)
        setCantidad('1')
      }}>Comprar más unidades</Button>}
    </> : oferta.disponibles > 0 || action.error ? <form className="form-stack" onSubmit={e => {
      e.preventDefault()
      if (lock.current) return
      const unidades = Number(cantidad)
      if (!Number.isSafeInteger(unidades) || unidades < 1) return
      lock.current = true
      const solicitud = intento.current ?? { referencia: crypto.randomUUID(), unidades }
      intento.current = solicitud
      void action.ejecutar(async () => {
        try {
          setCompra(await api<Compra>('/api/Compras/Ofertas/' + oferta.id + '/Demo', { method: 'POST', sesion, body: solicitud }))
        } catch (error) {
          if (error instanceof ApiError && [400, 401, 403, 404, 409].includes(error.status)) intento.current = null
          throw error
        } finally {
          lock.current = false
          actualizarOferta()
        }
      })
    }}>
      <Campo label="Unidades para la compra demo"><input type="number" min={1} max={intento.current ? undefined : oferta.disponibles} step={1}
        value={cantidad} onChange={e => setCantidad(e.target.value)} disabled={action.busy || intento.current !== null} required/></Campo>
      <Button disabled={action.busy}>{action.busy ? 'Registrando compra…' : intento.current
        ? 'Consultar o reintentar la compra' : 'Compra demo'}</Button>
    </form> : <p>La oferta se agotó.</p>}
    <ErrorBox text={action.error}/>
  </section>
}

const numero = new Intl.NumberFormat('es-BO')

function PosicionPortafolio({ compra, unidades }: { compra: Compra; unidades: number }) {
  const { data, loading } = useDatos<{ codigo: string; unidadesEmitidas?: number | null }>(
    '/api/Global/Activos/' + compra.ofertaId)
  const emitidas = data?.codigo === compra.codigo ? data.unidadesEmitidas : null
  const cantidad = numero.format(unidades)
  return <li>
    <Link to={'/ofertas/' + compra.ofertaId}>{compra.inmueble}</Link>
    {' · '}{compra.codigo}{' · '}
    {emitidas != null && emitidas >= unidades
      ? <strong>{cantidad} de {numero.format(emitidas)} unidades emitidas</strong>
      : <><strong>{cantidad} {unidades === 1 ? 'unidad confirmada' : 'unidades confirmadas'}</strong>
        <small className="muted">{loading ? ' · Consultando total emitido…' : ' · Total emitido no disponible'}</small></>}
  </li>
}

export function Portafolio() {
  const { sesion } = useSesion()
  const { data, loading, error } = useDatos<Compra[]>('/api/Compras/Portafolio', sesion, 3000)
  const posiciones = new Map<string, { compra: Compra; unidades: number }>()
  for (const compra of data ?? []) {
    if (compra.estado !== 'CONFIRMADA') continue
    const posicion = posiciones.get(compra.ofertaId)
    posiciones.set(compra.ofertaId, { compra, unidades: compra.unidades + (posicion?.unidades ?? 0) })
  }

  return <section className="inner flow-stack">
    <PageTitle title="Mi portafolio" description="Consulta tus unidades confirmadas y el estado de tus compras en Stellar Testnet."/>
    <ErrorBox text={error}/>
    {loading && <p role="status">Cargando portafolio…</p>}
    {data?.length === 0 && <Empty title="Todavía no tienes compras" description="Explora el mercado para elegir una oferta."
      href="/" label="Ver ofertas"/>}
    {!!data?.length && <>
      <section className="panel"><h2>Saldo de unidades</h2>
        {posiciones.size ? <ul className="plain-list" aria-live="polite">
          {[...posiciones.values()].map(posicion => <PosicionPortafolio key={posicion.compra.ofertaId} {...posicion}/>)}
        </ul> : <p role="status">Todavía no hay unidades confirmadas.</p>}
      </section>
      <section className="flow-stack" aria-label="Historial de compras"><h2>Historial de compras</h2>
        {data.map(compra => <article className="panel flow-stack" key={compra.id}>
          <div className="flex items-center gap-2"><strong>{compra.inmueble}</strong><Estado value={compra.estado === 'ESPERANDO_PAGO' ? 'Esperando pago' : compra.estado}/></div>
          <p>{compra.unidades} {compra.codigo} · {compra.sinPago ? 'Compra demo sin cobro' :
            compra.totalUsdc ? compra.totalUsdc + ' USDC de prueba' : moneda(compra.total, compra.moneda)} · {fecha(compra.creadaEn)}</p>
          <p className="break-all">Referencia de compra <code>{compra.id}</code></p>
          {compra.estado === 'CONFIRMADA' && compra.billetera && <p className="break-all">Billetera Testnet <code>{compra.billetera}</code></p>}
          {compra.estado === 'ESPERANDO_PAGO' && <Link to={'/ofertas/' + compra.ofertaId}>Continuar pago</Link>}
          <ComprobantesCompra compra={compra}/>
        </article>)}
      </section>
    </>}
    <RentasPortafolio/>
  </section>
}
