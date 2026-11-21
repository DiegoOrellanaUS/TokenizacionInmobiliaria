import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { ArrowUpRight, MapPin, Plus } from 'lucide-react'
import { fotoDeOferta, moneda, type Oferta } from '../api'
import { Estado } from './atoms'

export function PageTitle({ title, description, children }: { title: string; description?: string; children?: ReactNode }) {
  return <header className="section-title">
    <div><h1>{title}</h1>{description && <p>{description}</p>}</div>
    {children}
  </header>
}

export function Empty({ title, description, href, label }: { title: string; description: string; href?: string; label?: string }) {
  return <section className="empty">
    <h2>{title}</h2><p>{description}</p>
    {href && <Link className="button button-secondary" to={href}><Plus size={16}/>{label}</Link>}
  </section>
}

export function FotoOferta({ oferta, detalle = false }: { oferta: Oferta; detalle?: boolean }) {
  const [usarReferencia, setReferencia] = useState(false)
  return <>
    <img className={detalle ? 'detail-image' : undefined} src={usarReferencia ? '/imagenes/arquitectura.jpg' : fotoDeOferta(oferta.id)}
      alt={usarReferencia ? 'Fotografía de referencia arquitectónica' : 'Foto de ' + oferta.nombre}
      loading={detalle ? 'eager' : 'lazy'} onError={() => setReferencia(true)}/>
    {usarReferencia && <span className="image-label">Imagen referencial</span>}
  </>
}

export function OfertaCard({ oferta }: { oferta: Oferta }) {
  return <article className="offer-card">
    <div className="offer-image">
      <FotoOferta key={oferta.id} oferta={oferta}/>
      <Estado value="PUBLICADA" />
    </div>
    <div className="offer-body">
      <p className="location"><MapPin size={15}/>{oferta.ciudad}</p>
      <h3><Link to={'/ofertas/' + oferta.id}>{oferta.nombre} <ArrowUpRight size={19}/></Link></h3>
      <dl><div><dt>Por unidad</dt><dd>{moneda(oferta.precio, oferta.moneda)}</dd></div>
        <div><dt>Disponibles</dt><dd>{oferta.disponibles}</dd></div></dl>
    </div>
  </article>
}

export function Progreso({ current }: { current: number }) {
  const pasos = ['Expediente', 'Revisión', 'Tokenización', 'Publicación']
  return <ol className="progress" aria-label="Progreso del inmueble">
    {pasos.map((paso, i) => <li key={paso} data-done={i <= current} aria-current={i === current ? 'step' : undefined}>
      <span>{i + 1}</span>{paso}
    </li>)}
  </ol>
}
