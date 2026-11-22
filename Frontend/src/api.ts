const base = (import.meta.env.VITE_API_URL?.trim() || (import.meta.env.DEV ? 'https://localhost:7339' : '')).replace(/\/$/, '')

export interface Sesion {
  usuarioId: string
  nombre: string
  correo: string
  tenantId: string | null
  esRevisor: boolean
  esAdminGlobal?: boolean
  token: string
  venceEn?: string | null
}

export interface Plan {
  id: string; codigo: string; nombre: string; descripcion?: string; precio: number; moneda: string
  periodicidad: string; max_tokens_propios: number; estado: string
}

export interface Suscripcion {
  tenantId: string; subscripcionId: string; estado: string
  servicios: { servicio: string; estado: string }[]
}

export interface Inmueble {
  id: string; nombre: string; tipo: string; ciudad: string; direccion: string
  superficieM2: number; estadoRevision: string
}

export interface Expediente {
  inmueble: Inmueble
  envioId?: number
  titulares: { nombre: string; desde: string; cuota?: number; verificada_en?: string }[]
  documentos: { documentoId: string; clase: string; version: number; fecha: string }[]
  revisiones: { id: string; decision: string; observacion?: string; revisado_en: string }[]
}

export interface Emision {
  inmuebleId: string; activoId: string; codigo: string; unidades: number; red: string
  estado: string; emisor?: string; hash?: string; ledger?: number
}

export interface Oferta {
  id: string; inmuebleId: string; nombre: string; ciudad: string; tipo: string
  codigo: string; unidades: number; disponibles: number; precio: number; moneda: string; publicadaEn: string
  emisor?: string | null; red?: string | null
}

export interface Compra {
  id: string; ofertaId: string; inmueble: string; codigo: string; unidades: number
  total: number; moneda: string; estado: 'ESPERANDO_PAGO' | 'PENDIENTE' | 'CONFIRMADA' | 'FALLIDA' | 'CANCELADA'; puedeCancelar?: boolean
  sinPago?: boolean
  creadaEn: string; hash?: string; billetera?: string; ledger?: number
  hashPago?: string; ledgerPago?: number; pagadorUsdc?: string; destinoUsdc?: string
  memoPago?: string; totalUsdc?: string; pagoIniciado?: boolean
}

export class ApiError extends Error {
  constructor(public status: number, mensaje: string) { super(mensaje) }
}

export async function api<T>(path: string, options: {
  method?: string; body?: unknown; sesion?: Sesion | null; tenant?: string | null; signal?: AbortSignal
} = {}): Promise<T> {
  const headers = new Headers({ Accept: 'application/json' })
  if (options.sesion) headers.set('Authorization', 'Bearer ' + options.sesion.token)
  const tenant = options.tenant ?? options.sesion?.tenantId
  if (tenant) headers.set('X-Tenant', tenant)
  let body: BodyInit | undefined
  if (options.body instanceof FormData) body = options.body
  else if (options.body !== undefined) {
    headers.set('Content-Type', 'application/json')
    body = JSON.stringify(options.body)
  }
  const response = await fetch(base + path, { method: options.method ?? 'GET', headers, body, signal: options.signal })
  const raw = await response.text()
  let result: unknown = raw
  try { result = JSON.parse(raw) } catch { /* algunas API responden texto */ }
  if (!response.ok) {
    const problem = typeof result === 'object' && result !== null
      ? result as { detail?: string; title?: string; errors?: Record<string, string[]> } : undefined
    const details = problem?.errors && Object.values(problem.errors).flat().join(' ')
    throw new ApiError(response.status, details || problem?.detail || problem?.title ||
      (typeof result === 'string' && result.length < 350 && result) || `No se pudo completar la operación (${response.status}).`)
  }
  return (raw ? result : undefined) as T
}

export function fotoDeOferta(id: string) {
  return base + '/api/Ofertas/Imagen/' + encodeURIComponent(id)
}

export async function descargar(path: string, sesion: Sesion | null, nombre = 'respaldo') {
  const headers = new Headers()
  if (sesion) headers.set('Authorization', 'Bearer ' + sesion.token)
  if (sesion?.tenantId) headers.set('X-Tenant', sesion.tenantId)
  const response = await fetch(base + path, { headers })
  if (!response.ok) throw new ApiError(response.status, nombre === 'respaldo' ? 'No se pudo descargar el respaldo.' : 'No se pudo descargar el informe.')
  const url = URL.createObjectURL(await response.blob())
  const enlace = document.createElement('a')
  enlace.href = url
  enlace.download = nombre
  enlace.click()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

export function moneda(valor: number, divisa = 'BOB') {
  const codigo = divisa.toUpperCase() === 'BS' ? 'BOB' : divisa.toUpperCase()
  if (!/^[A-Z]{3}$/.test(codigo)) return new Intl.NumberFormat('es-BO').format(valor) + ' ' + divisa
  return new Intl.NumberFormat('es-BO', { style: 'currency', currency: codigo, maximumFractionDigits: 2 }).format(valor)
}

export function fecha(valor: string) {
  return new Intl.DateTimeFormat('es-BO', { dateStyle: 'medium' }).format(new Date(valor))
}
