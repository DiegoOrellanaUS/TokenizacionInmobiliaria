import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, ApiError, type Sesion } from './api'
import { cerrarSesion, renovarSesion } from './oauth'

type Modo = 'cargando' | 'oauth' | 'demo' | 'error'
type Proveedores = { gitHub: boolean; facebook?: boolean; contrasena?: boolean }
const Context = createContext<{
  sesion: Sesion | null
  modo: Modo
  proveedores: Proveedores
  entrar: (correo: string, contrasenia?: string) => Promise<Sesion>
  tenant: (id: string) => void
  salir: () => Promise<void>
} | null>(null)

export function SesionProvider({ children }: { children: ReactNode }) {
  const [sesion, guardar] = useState<Sesion | null>(null)
  const [modo, definirModo] = useState<Modo>('cargando')
  const [proveedores, definirProveedores] = useState<Proveedores>({ gitHub: false })

  useEffect(() => {
    let vigente = true
    async function cargar() {
      try {
        const estado = await api<Proveedores>('/api/SesionDemo/Modo')
        if (!vigente) return
        const oauth = estado.gitHub || estado.facebook
        const nueva = oauth ? await renovarSesion() : null
        if (vigente) {
          guardar(nueva)
          definirProveedores(estado)
          definirModo(oauth ? 'oauth' : 'demo')
        }
      } catch (error) {
        if (vigente) definirModo(error instanceof ApiError && error.status === 404 ? 'demo' : 'error')
      }
    }
    void cargar()
    return () => { vigente = false }
  }, [])

  useEffect(() => {
    const vence = sesion?.venceEn
    if (modo !== 'oauth' || !vence) return
    const objetivo = Date.parse(vence) - 60_000
    let timer: number
    let vigente = true
    let enCurso = false

    async function renovar() {
      if (!vigente || enCurso || document.hidden) return
      enCurso = true
      window.clearTimeout(timer)
      try {
        const nueva = await renovarSesion()
        if (vigente) guardar(anterior => anterior && nueva?.usuarioId === anterior.usuarioId
          ? { ...nueva, tenantId: anterior.tenantId ?? nueva.tenantId } : nueva)
      } catch {
        if (vigente) timer = window.setTimeout(renovar, 15_000)
      } finally { enCurso = false }
    }

    function alVolver() {
      if (Date.now() >= objetivo) void renovar()
    }

    timer = window.setTimeout(renovar, Math.max(0, objetivo - Date.now()))
    document.addEventListener('visibilitychange', alVolver)
    return () => {
      vigente = false
      window.clearTimeout(timer)
      document.removeEventListener('visibilitychange', alVolver)
    }
  }, [modo, sesion?.venceEn])

  async function entrar(correo: string, contrasenia?: string) {
    const nueva = await api<Sesion>('/api/SesionDemo/Iniciar', { method: 'POST', body: { correo, contrasenia } })
    if (sesion?.venceEn) await cerrarSesion()
    guardar(nueva)
    return nueva
  }

  function tenant(id: string) {
    guardar(anterior => anterior ? { ...anterior, tenantId: id } : null)
  }

  async function salir() {
    if (sesion?.venceEn) await cerrarSesion()
    guardar(null)
  }

  return <Context.Provider value={{ sesion, modo, proveedores, entrar, tenant, salir }}>{children}</Context.Provider>
}

export function useSesion() {
  const contexto = useContext(Context)
  if (!contexto) throw new Error('Falta el proveedor de sesión')
  return contexto
}
