import { useState, type FormEvent } from 'react'
import { api, fecha, moneda } from '../api'
import { useAccion, useDatos } from '../hooks'
import { useSesion } from '../session'
import { Button, Campo, ErrorBox } from '../ui/atoms'
import { PageTitle } from '../ui/molecules'
import { enlaceTransaccionTestnet } from '../ui/Stellar'

const ruta = '/api/Compras/Rentas'
const bolivianos = (valor: number) => moneda(valor, 'BOB')
const xlm = new Intl.NumberFormat('es-BO', { maximumFractionDigits: 7 })
const fechaCorte = new Intl.DateTimeFormat('es-BO', {
  dateStyle: 'short', timeStyle: 'short', timeZone: 'America/La_Paz',
})
const campos = [
  ['cobrado', 'Alquiler cobrado'], ['baseIva', 'Base facturada para IVA'],
  ['creditoIva', 'Crédito fiscal IVA admisible'], ['baseIt', 'Base imponible del IT'],
  ['iueAplicado', 'IUE pagado aplicado como crédito al IT'],
  ['utilidadFiscalEstimada', 'Utilidad fiscal estimada para provisionar IUE'],
  ['ipbiAnual', 'Impuesto municipal anual de referencia'], ['reserva', 'Reserva del mes'],
  ['bobPorXlm', 'Bs por XLM de prueba (conversión simulada)'],
] as const

type Gasto = { concepto: string; importe: number; referencia: string }
type Tributo = { impuesto: string; importe: number; fecha: string; referencia: string }
type Contabilidad = {
  registro: Record<typeof campos[number][0], number> & { gastos: Gasto[]; pagosTributarios: Tributo[] }
  emitidas: number; debitoIva: number; iva: number; itDeterminado: number; it: number
  provisionIue: number; provisionIpbi: number; totalGastos: number; neto: number; distribuible: number
}
type Cierre = {
  id: string; inmueble: string; periodo: string; corte: string; aprobadoEn: string | null
  contabilidad: Contabilidad; asignado: number; sinAsignar: number; pendientes: number; pagados: number
}
type Pago = {
  referencia: string; inmueble: string; periodo: string; unidades: number
  bob: number; xlm: number; estado: 'PENDIENTE' | 'PAGADO' | 'FALLIDO' | 'SIN_PAGO'; hash?: string | null
}

export function RentasPropias() {
  const { sesion } = useSesion()
  const lista = useDatos<{ ofertas: { id: string; nombre: string }[]; cierres: Cierre[] }>(ruta + '/Propias', sesion, 10000)
  const accion = useAccion()
  const [gastos, setGastos] = useState(1)
  const [tributos, setTributos] = useState(1)

  function registrar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault()
    const form = evento.currentTarget
    const formData = new FormData(form)
    const texto = (nombre: string) => String(formData.get(nombre) ?? '').trim()
    const monto = (nombre: string) => Number(texto(nombre))
    const gastosCapturados = Array.from({ length: gastos }, (_, i) => ({
      concepto: texto('gasto' + i), importe: monto('importeG' + i), referencia: texto('refG' + i),
    })).filter(g => g.concepto || g.importe || g.referencia)
    const pagosTributarios = Array.from({ length: tributos }, (_, i) => ({
      impuesto: texto('tributo' + i), importe: monto('importeT' + i), fecha: texto('fechaT' + i), referencia: texto('refT' + i),
    })).filter(p => p.importe || p.fecha || p.referencia)
    if (accion.busy) return
    void accion.ejecutar(async () => {
      await api(ruta + '/Ofertas/' + texto('oferta'), { method: 'POST', sesion, body: {
        periodo: texto('periodo'), ...Object.fromEntries(campos.map(([nombre]) => [nombre, monto(nombre)])),
        gastos: gastosCapturados, pagosTributarios,
      } })
      form.reset()
      setGastos(1)
      setTributos(1)
      lista.refresh()
    })
  }

  return <section className="inner flow-stack">
    <PageTitle title="Cierres mensuales de alquiler"
      description="Registra una liquidación de demostración en bolivianos y envíala al revisor."/>
    <ErrorBox text={lista.error || accion.error}/>
    {lista.loading && <p role="status">Consultando cierres…</p>}
    {lista.data && (lista.data.ofertas.length ? <form className="panel form-stack" onSubmit={registrar}>
      <div className="form-grid">
        <Campo label="Inmueble"><select name="oferta" required>
          <option value="">Elige una oferta publicada</option>
          {lista.data.ofertas.map(o => <option key={o.id} value={o.id}>{o.nombre}</option>)}
        </select></Campo>
        <Campo label="Mes cerrado"><input type="month" name="periodo" required/></Campo>
      </div>
      <div className="form-grid">{campos.map(([nombre, label]) => <Campo label={label} key={nombre}>
        <input type="number" min={nombre === 'bobPorXlm' ? '0.01' : '0'} step="0.01" name={nombre}
          defaultValue={nombre === 'bobPorXlm' ? 100 : 0} required/>
      </Campo>)}</div>
      <fieldset className="flow-stack">
        <legend>Gastos del período</legend>
        {Array.from({ length: gastos }, (_, i) => <div key={i} className="form-grid">
          <Campo label={'Concepto del gasto ' + (i + 1)}><input name={'gasto' + i} maxLength={120}/></Campo>
          <Campo label={'Importe en Bs del gasto ' + (i + 1)}><input name={'importeG' + i} type="number" min="0" step="0.01"/></Campo>
          <Campo label={'Comprobante del gasto ' + (i + 1)}><input name={'refG' + i} maxLength={120}/></Campo>
        </div>)}
        <Button type="button" variant="secondary" disabled={gastos >= 60} onClick={() => setGastos(gastos + 1)}>Agregar gasto</Button>
      </fieldset>
      <fieldset className="flow-stack">
        <legend>Pagos tributarios registrados para la demo</legend>
        <p>Registra el importe pagado en Bs, la fecha y la referencia de la boleta. El cierre descuenta el impuesto calculado una sola vez.</p>
        {Array.from({ length: tributos }, (_, i) => <div key={i} className="form-grid">
          <Campo label={'Impuesto del pago ' + (i + 1)}><select name={'tributo' + i}>
            {['IVA', 'IT', 'IUE', 'IPBI'].map(t => <option key={t}>{t}</option>)}
          </select></Campo>
          <Campo label={'Importe en Bs del pago ' + (i + 1)}><input name={'importeT' + i} type="number" min="0" step="0.01"/></Campo>
          <Campo label={'Fecha del pago ' + (i + 1)}><input name={'fechaT' + i} type="date"/></Campo>
          <Campo label={'Referencia de la boleta ' + (i + 1)}><input name={'refT' + i} maxLength={120}/></Campo>
        </div>)}
        <Button type="button" variant="secondary" disabled={tributos >= 24} onClick={() => setTributos(tributos + 1)}>Agregar pago tributario</Button>
      </fieldset>
      <Button disabled={accion.busy}>Enviar cierre al revisor</Button>
    </form> : <p>Publica la oferta de un inmueble con emisión confirmada para registrar su cierre.</p>)}
    {lista.data?.cierres.map(cierre => <DetalleCierre key={cierre.id} cierre={cierre}/>)}
  </section>
}

function DetalleCierre({ cierre }: { cierre: Cierre }) {
  const c = cierre.contabilidad
  const r = c.registro
  const resumen: [string, number][] = [
    ['Alquiler cobrado', r.cobrado], ['Gastos operativos', c.totalGastos], ['Base facturada para IVA', r.baseIva],
    ['Débito IVA (13 % de la base informada)', c.debitoIva], ['Crédito fiscal IVA', r.creditoIva],
    ['IVA neto del período', c.iva], ['Base imponible del IT', r.baseIt], ['IT determinado (3 % de la base informada)', c.itDeterminado],
    ['Crédito de IUE aplicado al IT', r.iueAplicado], ['IT por pagar', c.it],
    ['Utilidad fiscal estimada', r.utilidadFiscalEstimada],
    ['Provisión de IUE (25 % de la utilidad fiscal estimada)', c.provisionIue],
    ['IPBI anual informado', r.ipbiAnual], ['Provisión de IPBI municipal', c.provisionIpbi], ['Reserva del mes', r.reserva],
    ['Resultado neto', c.neto], ['Neto distribuible', c.distribuible],
    ['Asignado a compradores', cierre.asignado], ['Sin asignar y redondeo', cierre.sinAsignar],
  ]
  return <section className="panel flow-stack" aria-label={'Cierre de ' + cierre.inmueble}>
    <h2>{cierre.inmueble} · {cierre.periodo}</h2>
    <p>{cierre.aprobadoEn ? 'Aprobado el ' + fecha(cierre.aprobadoEn) : 'En revisión'}
      {' · '}Corte al registrar el cierre {fechaCorte.format(Date.parse(cierre.corte))} (Bolivia) · {c.emitidas} unidades emitidas</p>
    <div className="kpi-tabla"><table>
      <caption className="text-left font-semibold">Liquidación en bolivianos</caption>
      <thead><tr><th scope="col">Concepto</th><th scope="col">Importe</th></tr></thead>
      <tbody>{resumen.map(([concepto, importe]) => <tr key={concepto}><th scope="row">{concepto}</th><td>{bolivianos(importe)}</td></tr>)}</tbody>
    </table></div>
    <h3>Gastos y respaldos</h3>
    {r.gastos.length ? <ul className="plain-list">{r.gastos.map((g, i) => <li key={i}>
      {g.concepto} · {bolivianos(g.importe)} · <code>{g.referencia}</code>
    </li>)}</ul> : <p>Sin gastos registrados.</p>}
    <h3>Boletas tributarias de demostración</h3>
    {r.pagosTributarios.length ? <ul className="plain-list">{r.pagosTributarios.map((p, i) => <li key={i}>
      {p.impuesto} · {bolivianos(p.importe)} · <time dateTime={p.fecha}>{p.fecha}</time> · <code>{p.referencia}</code>
    </li>)}</ul> : <p>Sin boletas registradas.</p>}
    <p>{cierre.pagados} {cierre.pagados === 1 ? 'pago confirmado' : 'pagos confirmados'} · {cierre.pendientes} por enviar en Testnet</p>
  </section>
}

export function RentasRevision() {
  const { sesion } = useSesion()
  const lista = useDatos<Cierre[]>(ruta + '/Revision', sesion, 10000)
  const accion = useAccion()
  function decidir(id: string, pagar: boolean) {
    if (accion.busy) return
    void accion.ejecutar(async () => {
      try {
        if (!pagar) await api(ruta + '/' + id + '/Aprobar', { method: 'POST', sesion })
        else {
          let restantes: number
          do {
            const resultado = await api<{ restantes: number }>(ruta + '/' + id + '/Pagar', { method: 'POST', sesion })
            restantes = resultado.restantes
            lista.refresh()
          } while (restantes > 0)
        }
      } finally { lista.refresh() }
    })
  }
  return <section className="inner flow-stack">
    <PageTitle title="Revisión de repartos de alquiler"
      description="Revisa el cierre, los gastos y las boletas antes de aprobar. Después de aprobar, puedes enviar XLM de prueba."/>
    <ErrorBox text={lista.error || accion.error}/>
    {lista.loading && <p role="status">Consultando cierres…</p>}
    {accion.busy && <p role="status">Procesando el cierre…</p>}
    {lista.data?.length === 0 && <p>Todavía no hay cierres presentados.</p>}
    {lista.data?.map(cierre => <div className="flow-stack" key={cierre.id}>
      <DetalleCierre cierre={cierre}/>
      {!cierre.aprobadoEn && <Button type="button" disabled={accion.busy} onClick={() => decidir(cierre.id, false)}>Aprobar reparto</Button>}
      {cierre.aprobadoEn && cierre.pendientes > 0 &&
        <Button type="button" disabled={accion.busy} onClick={() => decidir(cierre.id, true)}>Enviar pagos Testnet</Button>}
    </div>)}
  </section>
}

export function RentasPortafolio() {
  const { sesion } = useSesion()
  const lista = useDatos<Pago[]>(ruta + '/Portafolio', sesion, 10000)
  if (!lista.data) return null
  const estados = { PENDIENTE: 'Aprobado, pendiente de pago', PAGADO: 'Confirmado en Testnet',
    FALLIDO: 'Pendiente de reintento', SIN_PAGO: 'Sin importe distribuible' }
  return <section className="panel flow-stack" aria-label="Mis rentas">
    <h2>Mis rentas</h2>
    {!lista.data.length && <p>Tus repartos aprobados aparecerán aquí.</p>}
    {lista.data.map(p => <article className="flow-stack" key={p.referencia}>
      <h3>{p.inmueble} · {p.periodo}</h3>
      <p><strong>{bolivianos(p.bob)}</strong> por {p.unidades} unidades · {xlm.format(p.xlm)} XLM de prueba</p>
      <p>{estados[p.estado]}</p>
      <p className="break-all">Referencia del reparto <code>{p.referencia}</code></p>
      {p.hash && <a className="text-button" target="_blank" rel="noreferrer"
        href={enlaceTransaccionTestnet(p.hash)}>Ver pago de renta en Stellar</a>}
    </article>)}
  </section>
}
