import type { Sesion } from './api'

export const rutaGitHub = '/api/Auth/GitHub'
export const rutaFacebook = '/api/Auth/Facebook'
const rutaSesion = '/api/Auth/Sesion'
let pendiente: Promise<Sesion | null> | undefined

function conBloqueo<T>(accion: () => Promise<T>) {
  return navigator.locks ? navigator.locks.request('umbral-oauth', accion) : accion()
}

export function renovarSesion() {
  if (!pendiente) {
    pendiente = conBloqueo(async () => {
      const respuesta = await fetch(rutaSesion + '/Refrescar', { method: 'POST', cache: 'no-store' })
      if (respuesta.status === 401) return null
      if (!respuesta.ok) throw new Error('No se pudo renovar la sesión.')
      return await respuesta.json() as Sesion
    }).finally(() => { pendiente = undefined })
  }
  return pendiente
}

export async function cerrarSesion() {
  await conBloqueo(async () => {
    const respuesta = await fetch(rutaSesion + '/Salir', { method: 'POST', cache: 'no-store' })
    if (!respuesta.ok) throw new Error('No se pudo cerrar la sesión. Intenta otra vez.')
  })
}
