import { useEffect, useState } from 'react'
import { ArrowDown, ArrowRight, Building2, Search } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { fecha, moneda, type Oferta } from '../api'
import { useDatos } from '../hooks'
import { ErrorBox, Estado } from '../ui/atoms'
import { Empty, OfertaCard, PageTitle } from '../ui/molecules'
import { CompraOferta } from './Portafolio'

export function Mercado() {
  const { data, error, loading } = useDatos<Oferta[]>('/api/Ofertas/Catalogo', undefined, 8000)
  const [busqueda, buscar] = useState('')
  const [tipo, filtrar] = useState('')
  const items = data?.filter(p =>
    (!tipo || p.tipo === tipo) && (p.nombre + ' ' + p.ciudad).toLowerCase().includes(busqueda.toLowerCase())) || []
  const tipos = [...new Set(data?.map(o => o.tipo).filter(Boolean))]
  return <>
    <section className="hero" aria-labelledby="hero-title">
      <div className="hero-inner">
        <p className="eyebrow"><Building2 size={15}/> Mercado inmobiliario</p>
        <h1 id="hero-title">Un nuevo lugar<br />para <em>invertir.</em></h1>
        <p>Explora inmuebles tokenizados y compara sus ofertas. Si eres propietario, gestiona tu inmueble desde el registro hasta la publicación.</p>
        <div className="hero-actions">
          <a href="#catalogo" className="button button-primary">Explorar ofertas <ArrowDown size={16}/></a>
          <Link to="/acceso" className="button button-secondary">Mi espacio <ArrowRight size={16}/></Link>
        </div>
      </div>
      <figure className="hero-image">
        <img src="/imagenes/arquitectura.jpg" alt="Interior contemporáneo con acabados naturales" fetchPriority="high"/>
      </figure>
    </section>
    <section className="catalog" id="catalogo" aria-labelledby="catalogo-title">
      <header className="section-title"><div><h2 id="catalogo-title">Ofertas inmobiliarias</h2>
        <p>Consulta el precio por unidad y las cantidades publicadas.</p></div>
        {data && <span className="muted">{items.length} {items.length === 1 ? 'resultado' : 'resultados'}</span>}
      </header>
      <div className="filters">
        <label className="search"><Search size={18}/><input aria-label="Buscar por nombre o ciudad"
           value={busqueda} onChange={e => buscar(e.target.value)} placeholder="Busca por nombre o ciudad"/></label>
        <select value={tipo} onChange={e => filtrar(e.target.value)} aria-label="Tipo de inmueble">
          <option value="">Todos los tipos</option>{tipos.map(t => <option key={t}>{t}</option>)}
        </select>
      </div>
      <ErrorBox text={error}/>
      {loading && <p role="status">Cargando ofertas…</p>}
      {data && (items.length ? <div className="offer-grid">{items.map(o => <OfertaCard key={o.id} oferta={o}/>)}</div> :
        <Empty title={data.length ? 'Sin resultados para esta búsqueda' : 'Aún no hay ofertas publicadas'}
           description={data.length ? 'Prueba con otro nombre o ciudad.' : 'Las ofertas aparecerán aquí después de publicarse.'} />)}
    </section>
  </>
}

export function DetalleOferta() {
  const { id } = useParams()
  const { data: oferta, error, loading, refresh } = useDatos<Oferta>('/api/Ofertas/Buscar/' + id, undefined, 4000)
  useEffect(() => {
    if (!oferta) return
    document.title = `${oferta.nombre} | Umbral`
    const description = document.querySelector<HTMLMetaElement>('meta[name="description"]')
    const anterior = description?.content
    if (description) description.content = `Consulta las unidades ofrecidas y el precio de ${oferta.nombre} en ${oferta.ciudad}.`
    const schema = document.createElement('script')
    schema.type = 'application/ld+json'
    schema.text = JSON.stringify({
      '@context': 'https://schema.org', '@type': 'Offer', name: oferta.nombre,
      price: oferta.precio, priceCurrency: oferta.moneda, url: window.location.href,
    })
    document.head.append(schema)
    return () => { schema.remove(); if (description && anterior) description.content = anterior }
  }, [oferta])

  return <section className="inner">
    <Link className="back-link" to="/">← Volver al mercado</Link>
    <ErrorBox text={error}/>
    {loading && <p role="status">Cargando oferta…</p>}
    {oferta && <>
      <PageTitle title={oferta.nombre} description={`${oferta.tipo} · ${oferta.ciudad}`}><Estado value="PUBLICADA"/></PageTitle>
      <div className="detail-grid">
        <figure className="relative m-0 h-fit">
          <img className="detail-image" src="/imagenes/arquitectura.jpg" alt="Fotografía de referencia arquitectónica"/>
          <figcaption className="image-label">Imagen referencial</figcaption>
        </figure>
        <aside className="flow-stack"><section className="panel"><h2>Datos de la oferta</h2>
          <dl className="facts"><div><dt>Precio por unidad</dt><dd>{moneda(oferta.precio, oferta.moneda)}</dd></div>
            <div><dt>Unidades ofrecidas</dt><dd>{oferta.unidades}</dd></div>
            <div><dt>Disponibles</dt><dd role="status" aria-label="Unidades disponibles">{oferta.disponibles}</dd></div>
            <div><dt>Activo</dt><dd>{oferta.codigo}</dd></div>
            <div><dt>Fecha de publicación</dt><dd>{fecha(oferta.publicadaEn)}</dd></div>
          </dl></section>
          <CompraOferta key={oferta.id} oferta={oferta} actualizarOferta={refresh}/>
        </aside>
      </div>
    </>}
  </section>
}
