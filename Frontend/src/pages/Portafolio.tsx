import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError, fecha, moneda, type Compra, type Oferta } from '../api'
import { useAccion, useDatos } from '../hooks'
import { useSesion } from '../session'
import { Button, Campo, ErrorBox, Estado } from '../ui/atoms'
import { Empty, PageTitle } from '../ui/molecules'

function Tx({ compra }: { compra: Compra }) {
  return compra.hash && compra.estado === 'CONFIRMADA' ? <a className="text-button"
    href={'https://stellar.expert/explorer/testnet/tx/' + compra.hash} target="_blank" rel="noreferrer">Ver transferencia</a> : null
}

export function CompraOferta({ oferta, actualizarOferta }: { oferta: Oferta; actualizarOferta: () => void }) {
  const { sesion } = useSesion()
  const action = useAccion()
  const [cantidad, setCantidad] = useState('1')
  const [compra, setCompra] = useState<Compra>()
  const intento = useRef<{ referencia: string; unidades: number } | null>(null)
  const lock = useRef(false)
  const seguimiento = useDatos<Compra>(compra?.estado === 'PENDIENTE' ? '/api/Compras/Buscar/' + compra.id : null, sesion, 3000)
  const resultado = seguimiento.data ?? compra
  const estado = seguimiento.data?.estado

  useEffect(() => {
    if (estado === 'CONFIRMADA' || estado === 'FALLIDA') actualizarOferta()
  }, [estado, actualizarOferta])

  if (sesion?.esRevisor) return null
  return <section className="panel flow-stack"><h2>Comprar unidades</h2>
    {!sesion ? <Link className="button button-primary" to={'/acceso?volver=' + encodeURIComponent('/ofertas/' + oferta.id)}>
      Acceder para comprar
    </Link> : resultado ? <>
      <div aria-live="polite"><Estado value={resultado.estado}/>
        <p>{resultado.estado === 'PENDIENTE' ? 'Pago simulado registrado. Estamos confirmando la entrega en Stellar Testnet.'
          : resultado.estado === 'CONFIRMADA' ? 'Las unidades ya están en tu portafolio.'
            : 'La transferencia falló y se liberaron las unidades reservadas.'}</p>
      </div>
      <ErrorBox text={seguimiento.error}/>
      <Tx compra={resultado}/>
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
          setCompra(await api<Compra>('/api/Compras/Ofertas/' + oferta.id, { method: 'POST', sesion, body: solicitud }))
        } catch (error) {
          if (error instanceof ApiError && [400, 401, 403, 404, 409].includes(error.status)) intento.current = null
          throw error
        } finally {
          lock.current = false
          actualizarOferta()
        }
      })
    }}>
      <Campo label="Unidades a comprar"><input type="number" min={1} max={intento.current ? undefined : oferta.disponibles} step={1}
        value={cantidad} onChange={e => setCantidad(e.target.value)} disabled={action.busy || intento.current !== null} required/></Campo>
      <p>Total de prueba <strong>{moneda((Number(cantidad) || 0) * oferta.precio, oferta.moneda)}</strong></p>
      <Button disabled={action.busy}>{action.busy ? 'Registrando compra…' : intento.current
        ? 'Consultar o reintentar la compra' : 'Confirmar compra de prueba'}</Button>
    </form> : <p>La oferta se agotó.</p>}
    <ErrorBox text={action.error}/>
  </section>
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
          {[...posiciones.values()].map(({ compra, unidades }) => <li key={compra.ofertaId}>
            <Link to={'/ofertas/' + compra.ofertaId}>{compra.inmueble}</Link>
            {' · '}{compra.codigo}{' · '}<strong>{unidades} {unidades === 1 ? 'unidad confirmada' : 'unidades confirmadas'}</strong>
          </li>)}</ul> : <p role="status">Todavía no hay unidades confirmadas.</p>}
      </section>
      <section className="flow-stack" aria-label="Historial de compras"><h2>Historial de compras</h2>
        {data.map(compra => <article className="panel flow-stack" key={compra.id}>
          <div className="flex items-center gap-2"><strong>{compra.inmueble}</strong><Estado value={compra.estado}/></div>
          <p>{compra.unidades} {compra.codigo} · {moneda(compra.total, compra.moneda)} · {fecha(compra.creadaEn)}</p>
          {compra.estado === 'CONFIRMADA' && compra.billetera && <p className="break-all">Billetera Testnet <code>{compra.billetera}</code></p>}
          <Tx compra={compra}/>
        </article>)}
      </section>
    </>}
  </section>
}
