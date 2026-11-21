import { readFile } from 'node:fs/promises'
import config from '../vercel.json' with { type: 'json' }

const sitio = 'https://tokenizacion-inmobiliaria.vercel.app'
const logo = sitio + '/imagenes/logo-umbral.jpg'
const destino = config.rewrites.find(ruta => ruta.destination.endsWith('/api/:path*')).destination
const gateway = new URL(destino).origin
let plantilla
const segmento = /^[a-f\d]{8}-(?:[a-f\d]{4}-){3}[a-f\d]{12}$/i
const htmlEscapes = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }
const escapar = valor => String(valor).replace(/[&<>"']/g, signo => htmlEscapes[signo])

function metadata({ titulo, descripcion, url, imagen, alt }) {
  const etiqueta = (property, content) => '<meta property="' + property + '" content="' + escapar(content) + '" />'
  return [
    '<title>' + escapar(titulo) + '</title>',
    '<meta name="description" content="' + escapar(descripcion) + '" />',
    '<link rel="canonical" href="' + escapar(url) + '" />',
    etiqueta('og:type', 'website'),
    etiqueta('og:site_name', 'Umbral'),
    etiqueta('og:locale', 'es_BO'),
    etiqueta('og:title', titulo),
    etiqueta('og:description', descripcion),
    etiqueta('og:url', url),
    etiqueta('og:image', imagen),
    etiqueta('og:image:alt', alt),
    '<meta name="twitter:card" content="summary_large_image" />',
  ].join('\n    ')
}

export default async function vistaOferta(req, res) {
  if (req.method !== 'GET' && req.method !== 'HEAD') {
    res.setHeader('Allow', 'GET, HEAD')
    res.statusCode = 405
    return res.end()
  }

  const { pathname, searchParams } = new URL(req.url, sitio)
  const esPlanes = pathname === '/planes' || (searchParams.get('pagina') ?? req.query?.pagina) === 'planes'
  const id = searchParams.get('id') ?? req.query?.id ?? pathname.match(/^\/ofertas\/([^/]+)$/)?.[1] ?? ''
  const ficha = {
    titulo: esPlanes ? 'Planes | Umbral' : 'Ofertas inmobiliarias | Umbral',
    descripcion: esPlanes ? 'Consulta los planes para gestionar y tokenizar tus inmuebles en Umbral.'
      : 'Explora las ofertas inmobiliarias de Umbral.',
    url: sitio + (esPlanes ? '/planes' : '/ofertas/' + encodeURIComponent(id)),
    imagen: logo,
    alt: 'Logo de Umbral',
  }
  let estado = 200
  let segundos = 60

  if (!esPlanes) {
    if (!segmento.test(id)) {
      estado = 404
    } else {
      try {
        const imagen = '/api/Ofertas/Imagen/' + id
        const signal = AbortSignal.timeout(4000)
        const [oferta, foto] = await Promise.allSettled([
          fetch(gateway + '/api/Ofertas/Buscar/' + id, { signal }),
          fetch(gateway + imagen, { method: 'HEAD', signal }),
        ])

        if (oferta.status === 'fulfilled' && oferta.value.ok) {
          const datos = await oferta.value.json()
          ficha.titulo = datos.nombre + ' | Umbral'
          ficha.descripcion = [datos.tipo, datos.ciudad].filter(Boolean).join(' · ') + ' · Oferta inmobiliaria en Umbral.'
          if (foto.status === 'fulfilled' && foto.value.ok) {
            ficha.imagen = sitio + imagen
            ficha.alt = 'Foto de referencia de ' + datos.nombre
          } else if (foto.status === 'rejected' || foto.value.status !== 404) {
            segundos = 10
          }
        } else if (oferta.status === 'fulfilled' && oferta.value.status === 404) {
          estado = 404
        } else {
          segundos = 10
        }
      } catch {
        segundos = 10
      }
    }
  }

  plantilla ??= await readFile(new URL('../dist/index.html', import.meta.url), 'utf8')
  const html = plantilla.replace(/<!-- umbral:meta -->[\s\S]*?<!-- \/umbral:meta -->/, () => metadata(ficha))
  res.statusCode = estado
  res.setHeader('Content-Type', 'text/html; charset=utf-8')
  if (estado === 404) res.setHeader('X-Robots-Tag', 'noindex')
  res.setHeader('Cache-Control', 'public, max-age=0, s-maxage=' + (estado === 404 ? 10 : segundos))
  res.end(req.method === 'HEAD' ? undefined : html)
}
