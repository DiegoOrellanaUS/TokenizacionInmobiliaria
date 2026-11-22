import { Download, FileSpreadsheet } from 'lucide-react'
import { Link } from 'react-router-dom'
import { descargar, moneda, type Compra } from '../api'
import { useAccion, useDatos } from '../hooks'
import { useSesion } from '../session'
import { Button, ErrorBox } from '../ui/atoms'
import { PageTitle } from '../ui/molecules'
import { GraficoKpi, type Grafica } from '../ui/GraficoKpi'

type Informe = {
  generadoEn: string
  listo: boolean
  fuentes: { nombre: string; sincronizadoEn: string | null }[]
  indicadores: { etiqueta: string; valor: number | null; unidad: string }[]
  graficas: Grafica[]
  ventas: { moneda: string; unidades: number; importe: number; precioMedio: number | null }[]
  meses: { mes: string; moneda: string; compras: number; importe: number }[]
  ofertas: { id: string; inmueble: string; codigo: string; red: string | null; emisor: string | null; emitidas: number;
    ofrecidas: number; vendidas: number; moneda: string; precioPublicado: number; importe: number; precioMedio: number | null }[]
}

const ruta = '/api/Reportes/Kpi'
const numero = new Intl.NumberFormat('es-BO')
const fecha = new Intl.DateTimeFormat('es-BO', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'America/La_Paz' })

export function Reportes() {
  const { sesion } = useSesion()
  const { data, loading, error } = useDatos<Informe>(sesion?.tenantId ? ruta : null, sesion, 6000)
  const compras = useDatos<Compra[]>('/api/Compras/Portafolio', sesion, 12000)
  const descarga = useAccion()
  const ultimaFuente = data?.fuentes.reduce((min, f) => f.sincronizadoEn
    ? Math.min(min, Date.parse(f.sincronizadoEn)) : min, Number.POSITIVE_INFINITY)
  const atrasado = data?.listo && ultimaFuente !== undefined && Date.parse(data.generadoEn) - ultimaFuente > 60_000

  return <section className="inner flow-stack kpi-pagina">
    <PageTitle title="Mis reportes" description="Consulta tu actividad de compra y los indicadores de tu espacio.">
      {sesion?.tenantId && <div className="kpi-descargas">
        <Button type="button" variant="secondary" disabled={!data?.listo || descarga.busy} onClick={() => void descarga.ejecutar(() =>
          descargar(ruta + '/excel', sesion, 'kpi-umbral.xlsx'))}><FileSpreadsheet size={17}/> Excel de mi espacio</Button>
        <Button type="button" variant="secondary" disabled={!data?.listo || descarga.busy} onClick={() => void descarga.ejecutar(() =>
          descargar(ruta + '/pdf', sesion, 'kpi-umbral.pdf'))}><Download size={17}/> PDF de mi espacio</Button>
      </div>}
    </PageTitle>
    <section className="flow-stack">
      <h2>Actividad de compra</h2>
      <ErrorBox text={compras.error}/>
      {compras.loading && <p role="status">Consultando tus compras…</p>}
      {compras.data && (compras.data.length ? <GraficoKpi dato={{
        titulo: 'Mis compras por estado', unidad: 'compras',
        explicacion: 'Cada barra cuenta las órdenes en ese estado. Las órdenes canceladas y fallidas quedan fuera de las compras confirmadas.',
        barras: [
          { etiqueta: 'Confirmadas', valor: compras.data.filter(c => c.estado === 'CONFIRMADA').length },
          { etiqueta: 'Pendientes', valor: compras.data.filter(c => c.estado === 'PENDIENTE' || c.estado === 'ESPERANDO_PAGO').length },
          { etiqueta: 'Canceladas', valor: compras.data.filter(c => c.estado === 'CANCELADA').length },
          { etiqueta: 'Fallidas', valor: compras.data.filter(c => c.estado === 'FALLIDA').length },
        ],
      }}/> : <p>Tus compras aparecerán aquí cuando registres una orden.</p>)}
      <Link className="text-button" to="/portafolio">Ver órdenes y comprobantes en mi portafolio</Link>
    </section>
    {sesion?.tenantId && <h2>Indicadores de mi espacio</h2>}
    <ErrorBox text={error || descarga.error}/>
    {loading && <p role="status">Consultando indicadores…</p>}
    {data && !data.listo && <p className="panel" role="status">Estamos cargando los datos de tus inmuebles, emisiones y compras.</p>}
    {data?.listo && <>
      <p className="muted">{atrasado ? 'Hay fuentes pendientes de actualización. ' : ''}
        {Number.isFinite(ultimaFuente) ? 'Fuentes sincronizadas hasta ' + fecha.format(ultimaFuente) + ' (Bolivia)' : ''}
      </p>
      <dl className="kpi-resumen">
        {data.indicadores.map(i => <div className="panel" key={i.etiqueta}>
          <dt>{i.etiqueta}</dt><dd>{i.valor === null ? 'Sin oferta' : numero.format(i.valor) + (i.unidad === '%' ? ' %' : '')}</dd>
        </div>)}
      </dl>
      <div className="kpi-graficas">{data.graficas.map(g => <GraficoKpi key={g.titulo} dato={g}/>)}</div>
      <section className="panel flow-stack">
        <h2>Ventas confirmadas por moneda</h2>
        {data.ventas.length ? <div className="kpi-tabla"><table>
          <thead><tr><th scope="col">Moneda</th><th scope="col">Unidades</th><th scope="col">Importe</th><th scope="col">Precio medio por token</th></tr></thead>
          <tbody>{data.ventas.map(v => <tr key={v.moneda}>
            <th scope="row">{v.moneda}</th><td>{numero.format(v.unidades)}</td><td>{moneda(v.importe, v.moneda)}</td>
            <td>{v.precioMedio === null ? 'Sin datos' : moneda(v.precioMedio, v.moneda)}</td>
          </tr>)}</tbody>
        </table></div> : <p>Las compras confirmadas aparecerán aquí.</p>}
      </section>
      <section className="panel flow-stack">
        <h2>Compras confirmadas por mes de registro</h2>
        {data.meses.length ? <div className="kpi-tabla"><table>
          <thead><tr><th scope="col">Mes</th><th scope="col">Moneda</th><th scope="col">Compras</th><th scope="col">Importe</th></tr></thead>
          <tbody>{data.meses.map(m => <tr key={m.mes + m.moneda}>
            <th scope="row">{m.mes}</th><td>{m.moneda}</td><td>{numero.format(m.compras)}</td><td>{moneda(m.importe, m.moneda)}</td>
          </tr>)}</tbody>
        </table></div> : <p>Las compras confirmadas aparecerán en su mes de registro.</p>}
      </section>
      <section className="panel flow-stack">
        <h2>Procedencia y precio de los tokens</h2>
        {data.ofertas.length ? <div className="kpi-tabla"><table>
          <thead><tr><th scope="col">Inmueble y activo</th><th scope="col">Emitidos</th><th scope="col">Ofrecidos</th>
            <th scope="col">Vendidos</th><th scope="col">Precio publicado</th><th scope="col">Importe confirmado</th><th scope="col">Precio medio</th></tr></thead>
          <tbody>{data.ofertas.map(o => <tr key={o.id}>
            <th scope="row"><strong>{o.inmueble}</strong><small>{o.codigo}{o.red && ' · ' + o.red}</small>
              {o.emisor && <small className="kpi-emisor">{o.emisor}</small>}</th>
            <td>{numero.format(o.emitidas)}</td><td>{numero.format(o.ofrecidas)}</td><td>{numero.format(o.vendidas)}</td>
            <td>{moneda(o.precioPublicado, o.moneda)}</td><td>{moneda(o.importe, o.moneda)}</td>
            <td>{o.precioMedio === null ? 'Sin datos' : moneda(o.precioMedio, o.moneda)}</td>
          </tr>)}</tbody>
        </table></div> : <p>Las ofertas publicadas aparecerán aquí.</p>}
      </section>
    </>}
  </section>
}
