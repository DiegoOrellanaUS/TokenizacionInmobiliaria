import { Download, FileSpreadsheet } from 'lucide-react'
import { descargar, moneda } from '../api'
import { useAccion, useDatos } from '../hooks'
import { useSesion } from '../session'
import { Button, ErrorBox } from '../ui/atoms'
import { GraficoKpi, type Grafica } from '../ui/GraficoKpi'
import { PageTitle } from '../ui/molecules'

type Informe = {
  generadoEn: string
  listo: boolean
  fuentes: { fuente: string; actualizadoEn: string | null }[]
  indicadores: { etiqueta: string; valor: number | null; unidad: string }[]
  graficas: Grafica[]
  importes: { moneda: string; importe: number }[]
  meses: { mesRegistro: string; moneda: string; comprasConfirmadas: number; importe: number }[]
}

const ruta = '/api/Global/Panel'
const numero = new Intl.NumberFormat('es-BO')
const fecha = new Intl.DateTimeFormat('es-BO', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'America/La_Paz' })
const nombres: Record<string, string> = {
  admin: 'Administración', inmuebles: 'Inmuebles', tokenizacion: 'Tokenización', comercio: 'Comercio',
}

export function PanelGlobal() {
  const { sesion } = useSesion()
  const { data, error, loading } = useDatos<Informe>(ruta, sesion, 8000)
  const descarga = useAccion()
  const desactualizadas = data?.fuentes.some(f => !f.actualizadoEn
    || Date.parse(data.generadoEn) - Date.parse(f.actualizadoEn) > 60_000)
  function exportar(formato: 'excel' | 'pdf') {
    const extension = formato === 'excel' ? 'xlsx' : 'pdf'
    void descarga.ejecutar(() => descargar(ruta + '/' + formato, sesion, 'indicadores-globales-umbral.' + extension))
  }

  return <section className="inner flow-stack kpi-pagina">
    <PageTitle title="Indicadores de la plataforma"
      description="Actividad de todos los espacios de Umbral. Los importes corresponden a compras de prueba.">
      <div className="kpi-descargas">
        <Button type="button" variant="secondary" disabled={!data?.listo || descarga.busy} onClick={() => exportar('excel')}>
          <FileSpreadsheet size={17} aria-hidden="true"/> Excel
        </Button>
        <Button type="button" variant="secondary" disabled={!data?.listo || descarga.busy} onClick={() => exportar('pdf')}>
          <Download size={17} aria-hidden="true"/> PDF
        </Button>
      </div>
    </PageTitle>
    <ErrorBox text={error || descarga.error}/>
    {loading && <p role="status">Consultando indicadores globales…</p>}
    {data && <>
      {!data.listo && <p className="panel" role="status">La carga inicial está en curso. Las descargas se habilitarán cuando todas las fuentes estén sincronizadas.</p>}
      {data.listo && <>
        {desactualizadas && <p className="alert" role="status">Hay fuentes pendientes de actualización. Consulta el corte por fuente.</p>}
        <dl className="kpi-resumen kpi-resumen-global">
          {data.indicadores.map(i => <div className="panel" key={i.etiqueta}>
            <dt>{i.etiqueta}</dt><dd>{i.valor === null ? 'Sin oferta' : numero.format(i.valor) + (i.unidad === '%' ? ' %' : '')}</dd>
          </div>)}
        </dl>
        <div className="kpi-graficas kpi-graficas-global">{data.graficas.map(g => <GraficoKpi key={g.titulo} dato={g}/>)}</div>
        <div className="kpi-compras-global">
          <section className="panel flow-stack">
            <h2>Importes confirmados por moneda</h2>
            {data.importes.length ? <div className="kpi-tabla"><table>
              <thead><tr><th scope="col">Moneda</th><th scope="col">Importe</th></tr></thead>
              <tbody>{data.importes.map(i => <tr key={i.moneda}>
                <th scope="row">{i.moneda}</th><td>{moneda(i.importe, i.moneda)}</td>
              </tr>)}</tbody>
            </table></div> : <p>Todavía no hay compras confirmadas.</p>}
          </section>
          <section className="panel flow-stack">
            <h2>Compras confirmadas por mes de registro</h2>
            {data.meses.length ? <div className="kpi-tabla"><table>
              <thead><tr><th scope="col">Mes</th><th scope="col">Moneda</th><th scope="col">Compras</th><th scope="col">Importe</th></tr></thead>
              <tbody>{data.meses.map(m => <tr key={m.mesRegistro + m.moneda}>
                <th scope="row">{m.mesRegistro}</th><td>{m.moneda}</td><td>{numero.format(m.comprasConfirmadas)}</td>
                <td>{moneda(m.importe, m.moneda)}</td>
              </tr>)}</tbody>
            </table></div> : <p>Los totales aparecerán al confirmar las primeras compras.</p>}
          </section>
        </div>
      </>}
      <section className="panel flow-stack">
        <h2>Corte por fuente</h2>
        <div className="kpi-tabla"><table>
          <thead><tr><th scope="col">Fuente</th><th scope="col">Última sincronización (Bolivia)</th></tr></thead>
          <tbody>{data.fuentes.map(f => <tr key={f.fuente}><th scope="row">{nombres[f.fuente] ?? f.fuente}</th>
            <td>{f.actualizadoEn ? <time dateTime={f.actualizadoEn}>{fecha.format(Date.parse(f.actualizadoEn))}</time> : 'En espera'}</td>
          </tr>)}</tbody>
        </table></div>
      </section>
    </>}
  </section>
}
