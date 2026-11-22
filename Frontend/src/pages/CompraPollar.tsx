import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import type { PendingConfirmation, PollarClient } from '@pollar/core'
import { api, ApiError, type Compra, type Oferta, type Sesion } from '../api'
import { useAccion, useDatos } from '../hooks'
import { useSesion } from '../session'
import { Button, Campo, ErrorBox, Estado } from '../ui/atoms'
import { ComprobantesCompra } from '../ui/ComprobantesCompra'

type Configuracion = { clavePublica: string; emisor: string; precioUnitario: number }
type Checkout = { configuracion: Configuracion | null; orden: Compra | null }
const esHash = (hash: string) => /^[0-9a-f]{64}$/i.test(hash)

export function CompraPollar({ oferta, actualizarOferta }: { oferta: Oferta; actualizarOferta: () => void }) {
  const { sesion } = useSesion()
  if (sesion?.esRevisor) return null

  return <section className="panel flow-stack">
    <h2>Comprar con Pollar</h2>
    <p>Pago con USDC de prueba en Stellar Testnet. Umbral verificará el pago en la red antes de entregar {oferta.codigo} a tu billetera de portafolio.</p>
    {sesion ? <CheckoutPollar key={sesion.usuarioId} sesion={sesion} oferta={oferta} actualizarOferta={actualizarOferta}/>
      : <Link className="button button-primary" to={'/acceso?volver=' + encodeURIComponent('/ofertas/' + oferta.id)}>
        Acceder para comprar
      </Link>}
  </section>
}

function CheckoutPollar({ sesion, oferta, actualizarOferta }: {
  sesion: Sesion; oferta: Oferta; actualizarOferta: () => void
}) {
  const ruta = '/api/Compras/Ofertas/' + oferta.id
  const checkout = useDatos<Checkout>(ruta + '/Pollar', sesion)
  const configuracion = checkout.data?.configuracion
  const orden = checkout.data?.orden
  const seguimiento = useDatos<Compra>(orden?.estado === 'PENDIENTE' ? '/api/Compras/Buscar/' + orden.id : null, sesion, 3000)
  const resultado = seguimiento.data ?? orden
  const action = useAccion()
  const [cantidad, setCantidad] = useState('1')
  const [hashManual, setHashManual] = useState('')
  const [cliente, setCliente] = useState<PollarClient>()
  const [billetera, setBilletera] = useState('')
  const [mensajeWallet, setMensajeWallet] = useState('')
  const [aviso, setAviso] = useState('')
  const [confirmacion, setConfirmacion] = useState<PendingConfirmation>()
  const [confirmando, setConfirmando] = useState(false)
  const ocupado = useRef(false)
  const pagoActual = useRef<{ compra: Compra; iniciado: boolean; error?: unknown } | null>(null)
  const clavePublica = configuracion?.clavePublica

  useEffect(() => {
    if (!clavePublica) return
    let activo = true
    let limpiar: (() => void) | undefined
    void import('@pollar/core').then(({ PollarClient }) => {
      if (!activo) return
      const sdk = new PollarClient({ apiKey: clavePublica, stellarNetwork: 'testnet', confirmation: 'prompt' })
      function sincronizar() {
        const auth = sdk.getAuthState()
        const wallet = sdk.getWallet()
        const lista = auth.step === 'authenticated' && auth.verified && /^G[A-Z2-7]{55}$/.test(wallet?.address ?? '')
        setBilletera(lista ? wallet!.address : '')
        setMensajeWallet(auth.step === 'error' ? auth.message :
          auth.step === 'wallet_not_installed' ? 'Instala Freighter y selecciona Testnet.' :
          auth.step === 'authenticated' && !lista ? 'Pollar está preparando tu billetera Stellar.' : '')
      }
      const salirAuth = sdk.onAuthStateChange(sincronizar)
      const salirWallet = sdk.onWalletStateChange(sincronizar)
      const salirConfirmacion = sdk.onConfirmationRequest(pendientes => setConfirmacion(pendientes[0]))
      limpiar = () => { salirAuth(); salirWallet(); salirConfirmacion(); sdk.destroy() }
      setCliente(sdk)
      sincronizar()
    }).catch(() => { if (activo) setMensajeWallet('No se pudo cargar el SDK de Pollar.') })
    return () => { activo = false; limpiar?.() }
  }, [clavePublica])

  useEffect(() => {
    if (resultado?.estado === 'CONFIRMADA' || resultado?.estado === 'FALLIDA') actualizarOferta()
  }, [resultado?.estado, actualizarOferta])

  function ejecutar(tarea: () => Promise<void>) {
    if (ocupado.current) return
    ocupado.current = true
    void action.ejecutar(async () => {
      try { await tarea() }
      finally { ocupado.current = false }
    })
  }

  function guardarOrden(compra: Compra) {
    checkout.recibir({ configuracion: configuracion ?? null, orden: compra })
  }

  async function verificar(id: string, hash: string, intentos = 1) {
    for (let intento = 1; ; intento++) {
      try {
        guardarOrden(await api<Compra>('/api/Compras/' + id + '/Pago', { method: 'POST', sesion, body: { hash } }))
        sessionStorage.removeItem('umbral.pollar.' + id)
        return
      } catch (error) {
        if (!(error instanceof ApiError) || error.status !== 425 || intento >= intentos) throw error
        await new Promise(resolve => setTimeout(resolve, 2500))
      }
    }
  }

  async function pagar(compra: Compra) {
    if (!cliente || !configuracion || billetera !== compra.pagadorUsdc || !compra.destinoUsdc ||
      !compra.memoPago || !compra.totalUsdc) return
    setAviso('')
    const saldo = await cliente.getWalletBalance(billetera, 'testnet')
    const disponible = saldo.balances.find(activo => activo.code === 'USDC' && activo.issuer === configuracion.emisor)?.available
    if (disponible == null || !Number.isFinite(Number(disponible)) || Number(disponible) < Number(compra.totalUsdc))
      throw new Error('Comprueba que la billetera tenga suficiente USDC de Testnet y que su línea de confianza esté activa.')

    const intento = { compra, iniciado: false, error: undefined as unknown }
    pagoActual.current = intento
    try {
      const pago = await cliente.sendPayment({
        chain: 'STELLAR',
        destination: compra.destinoUsdc,
        amount: compra.totalUsdc,
        asset: { type: 'credit_alphanum4', code: 'USDC', issuer: configuracion.emisor },
        options: { memo: { type: 'text', value: compra.memoPago }, timeoutSec: 90 },
      })
      if (intento.error) throw intento.error
      const hash = 'hash' in pago ? pago.hash : undefined
      if (!hash || !esHash(hash)) {
        if (!intento.iniciado) throw new Error('La billetera no completó la confirmación del pago.')
        throw new Error('Pollar todavía no devolvió un hash. Revisa el historial de tu billetera y verifica el pago con ese comprobante.')
      }
      sessionStorage.setItem('umbral.pollar.' + compra.id, hash)
      setHashManual(hash)
      await verificar(compra.id, hash, 6)
    } finally {
      pagoActual.current = null
    }
  }

  async function autorizarFirma(solicitud: PendingConfirmation) {
    if (confirmando || solicitud.network !== 'STELLAR') return
    const intento = pagoActual.current
    setConfirmando(true)
    try {
      if (intento && !intento.iniciado) {
        guardarOrden(await api<Compra>('/api/Compras/' + intento.compra.id + '/IniciarPago', { method: 'POST', sesion }))
        intento.iniciado = true
      }
      solicitud.confirm()
    } catch (error) {
      if (intento) intento.error = error
      solicitud.reject()
    } finally {
      setConfirmando(false)
    }
  }

  const recuperado = orden?.pagoIniciado ? sessionStorage.getItem('umbral.pollar.' + orden.id) ?? '' : ''
  const hash = hashManual || recuperado
  const esperandoPago = resultado?.estado === 'ESPERANDO_PAGO'
  const necesitaWallet = !resultado || (esperandoPago && !resultado.pagoIniciado)

  return <>
    <ErrorBox text={checkout.error || action.error || seguimiento.error || mensajeWallet}/>
    {checkout.loading && <p role="status">Consultando compra…</p>}
    {configuracion && necesitaWallet && (!resultado && oferta.disponibles <= 0 ? <p>La oferta se agotó.</p> :
      !billetera ? <div className="flex flex-wrap gap-2">
        <Button type="button" disabled={!cliente} onClick={() => cliente?.login({ provider: 'google' })}>Abrir billetera con Pollar</Button>
        <Button type="button" variant="secondary" disabled={!cliente} onClick={() => cliente?.login({ provider: 'freighter' })}>Conectar Freighter</Button>
      </div> : <>
        <p className="break-all">Billetera pagadora <code>{billetera}</code></p>
        <div className="flex flex-wrap gap-2">
          <Button type="button" variant="secondary" disabled={action.busy} onClick={() => ejecutar(async () => {
            if (!cliente) return
            const linea = await cliente.setTrustline({ code: 'USDC', issuer: configuracion.emisor })
            if (linea.status === 'error') throw new Error(linea.details || 'No pudo abrirse la línea de confianza.')
            setAviso(linea.status === 'success' ? 'Línea de confianza USDC habilitada.' : 'Completa la confirmación en tu billetera.')
          })}>Habilitar USDC</Button>
          <a className="text-button" href="https://faucet.circle.com/" target="_blank" rel="noreferrer">Obtener USDC de prueba</a>
          <Button type="button" variant="secondary" disabled={action.busy} onClick={() => ejecutar(async () => {
            await cliente?.logout()
            setBilletera('')
          })}>Cambiar billetera</Button>
        </div>
      </>)}
    {aviso && <p role="status">{aviso}</p>}
    {confirmacion && <div className="flow-stack" role="group" aria-label="Confirmar firma con Pollar">
      {pagoActual.current ? <p className="break-all">Autoriza el pago de <strong>{pagoActual.current.compra.totalUsdc} USDC de prueba</strong>
        {' a '}<code>{pagoActual.current.compra.destinoUsdc}</code>{' en Stellar Testnet.'}</p>
        : <p>Autoriza la operación de tu billetera en Stellar Testnet.</p>}
      <div className="flex flex-wrap gap-2">
        <Button type="button" disabled={confirmando || confirmacion.network !== 'STELLAR'}
          onClick={() => void autorizarFirma(confirmacion)}>Autorizar firma</Button>
        <Button type="button" variant="secondary" disabled={confirmando} onClick={confirmacion.reject}>Rechazar firma</Button>
      </div>
    </div>}
    {configuracion && billetera && !resultado && oferta.disponibles > 0 &&
      <form className="form-stack" onSubmit={evento => {
        evento.preventDefault()
        ejecutar(async () => {
          const compra = await api<Compra>(ruta, { method: 'POST', sesion,
            body: { referencia: crypto.randomUUID(), unidades: Number(cantidad), pagadorUsdc: billetera } })
          guardarOrden(compra)
          actualizarOferta()
        })
      }}>
        <Campo label="Unidades a comprar"><input type="number" min={1} max={oferta.disponibles} step={1}
          value={cantidad} onChange={evento => setCantidad(evento.target.value)} required/></Campo>
        <p>Total <strong>{(Number(cantidad) * configuracion.precioUnitario).toFixed(2)} USDC de prueba</strong></p>
        <Button disabled={action.busy}>Reservar para pagar</Button>
      </form>}
    {resultado && <div className="flow-stack" aria-live="polite">
      <Estado value={esperandoPago ? 'Esperando pago' : resultado.estado}/>
      {resultado.totalUsdc && <p><strong>{resultado.totalUsdc} USDC de prueba</strong> por {resultado.unidades} {resultado.codigo}</p>}
      {resultado.estado === 'PENDIENTE' && <p>{resultado.hashPago
        ? 'Pago USDC verificado. Estamos confirmando la entrega en Stellar Testnet.'
        : 'Consulta el estado de tu orden anterior en el portafolio.'}</p>}
      {resultado.estado === 'CONFIRMADA' && <p>La entrega figura en tu portafolio.</p>}
      {resultado.estado === 'FALLIDA' && <p>{resultado.hashPago
        ? 'El pago está confirmado y la entrega falló. Conserva el comprobante para la conciliación.'
        : 'La entrega falló.'}</p>}
      <ComprobantesCompra compra={resultado}/>
      {esperandoPago && resultado.pagoIniciado ? <form className="form-stack" onSubmit={evento => {
        evento.preventDefault()
        ejecutar(() => verificar(resultado.id, hash.trim()))
      }}>
        <p>Si Pollar ya envió el pago, verifica el mismo comprobante. Referencia en Stellar <code>{resultado.memoPago}</code>.</p>
        <Campo label="Hash de pago USDC"><input value={hash} onChange={evento => setHashManual(evento.target.value)}
          maxLength={64} required/></Campo>
        <Button disabled={action.busy || !esHash(hash.trim())}>Verificar pago</Button>
      </form> : esperandoPago && billetera === resultado.pagadorUsdc && configuracion &&
        <Button type="button" disabled={action.busy} onClick={() => ejecutar(() => pagar(resultado))}>Pagar con Pollar</Button>}
      {resultado.puedeCancelar && <Button type="button" variant="secondary" disabled={action.busy} onClick={() => ejecutar(async () => {
        await api('/api/Compras/' + resultado.id + '/Cancelar', { method: 'POST', sesion })
        checkout.refresh()
        actualizarOferta()
      })}>Cancelar reserva</Button>}
      {resultado.estado === 'CONFIRMADA' && oferta.disponibles > 0 && <Button type="button" variant="secondary" onClick={() => {
        setHashManual('')
        setCantidad('1')
        checkout.recibir({ configuracion: configuracion ?? null, orden: null })
      }}>Comprar más unidades</Button>}
      <Link className="text-button" to="/portafolio">Ver mi portafolio</Link>
    </div>}
  </>
}
