import { BarChart3 } from 'lucide-react'

export type Grafica = { titulo: string; unidad: string; explicacion: string; barras: { etiqueta: string; valor: number }[] }

const numero = new Intl.NumberFormat('es-BO')

export function GraficoKpi({ dato }: { dato: Grafica }) {
  const maximo = Math.max(1, ...dato.barras.map(x => x.valor))
  return <figure className="panel kpi-grafica">
    <h2><BarChart3 size={20} aria-hidden="true"/>{dato.titulo}</h2>
    <ul aria-label={'Valores en ' + dato.unidad}>
      {dato.barras.map(({ etiqueta, valor }) => <li key={etiqueta}>
        <span>{etiqueta}</span><meter min={0} max={maximo} value={valor} aria-label={etiqueta}/>
        <strong>{numero.format(valor)}</strong>
      </li>)}
    </ul>
    <figcaption>{dato.explicacion}</figcaption>
  </figure>
}
