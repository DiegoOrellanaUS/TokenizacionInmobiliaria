import { createContext, useContext, useState, type ReactNode } from 'react'
import { api, type Sesion } from './api'

const Context = createContext<{
  sesion: Sesion | null
  entrar: (correo: string, contrasenia?: string) => Promise<Sesion>
  tenant: (id: string) => void
  salir: () => void
} | null>(null)

export function SesionProvider({ children }: { children: ReactNode }) {
  const [sesion, guardar] = useState<Sesion | null>(null)
  async function entrar(correo: string, contrasenia?: string) {
    const nueva = await api<Sesion>('/api/SesionDemo/Iniciar', { method: 'POST', body: { correo, contrasenia } })
    guardar(nueva)
    return nueva
  }
  function tenant(id: string) {
    guardar(anterior => anterior ? { ...anterior, tenantId: id } : null)
  }
  return <Context.Provider value={{ sesion, entrar, tenant, salir: () => guardar(null) }}>
    {children}
  </Context.Provider>
}

export function useSesion() {
  const contexto = useContext(Context)
  if (!contexto) throw new Error('Falta el proveedor de sesión')
  return contexto
}
