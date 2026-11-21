import { useCallback, useEffect, useState } from 'react'
import { api, ApiError, type Sesion } from './api'

export function useDatos<T>(path: string | null, sesion?: Sesion | null, intervalo = 0) {
  const [revision, actualizar] = useState(0)
  const [state, setState] = useState<{ key: string; data?: T; error?: string; status?: number; loading: boolean }>({ key: '', loading: true })
  const key = JSON.stringify([path, sesion?.usuarioId, sesion?.tenantId])
  useEffect(() => {
    if (!path) return
    const controller = new AbortController()
    let timer: ReturnType<typeof setTimeout> | undefined
    let ocupado = false
    let sinAcceso = false

    async function cargar() {
      if (!path || ocupado || sinAcceso || controller.signal.aborted) return
      ocupado = true
      clearTimeout(timer)
      try {
        const data = await api<T>(path, { sesion, signal: controller.signal })
        if (!controller.signal.aborted) setState({ key, data, loading: false })
      } catch (error) {
        if (controller.signal.aborted) return
        const status = error instanceof ApiError ? error.status : undefined
        sinAcceso = status === 401 || status === 403
        setState(prev => ({ key, status, error: error instanceof Error ? error.message : 'No se pudo actualizar la información.',
          data: prev.key === key && !sinAcceso && status !== 404 ? prev.data : undefined, loading: false }))
      } finally {
        ocupado = false
        if (intervalo && !sinAcceso && !controller.signal.aborted)
          timer = setTimeout(() => { if (!document.hidden) void cargar() }, intervalo)
      }
    }

    function volver() {
      if (!document.hidden) void cargar()
    }

    void cargar()
    if (intervalo) document.addEventListener('visibilitychange', volver)
    return () => {
      controller.abort()
      clearTimeout(timer)
      document.removeEventListener('visibilitychange', volver)
    }
  }, [intervalo, key, path, sesion, revision])
  const result = state.key === key ? state : { data: undefined, loading: Boolean(path), error: undefined, status: undefined }
  const refresh = useCallback(() => actualizar(n => n + 1), [])
  const recibir = useCallback((data: T) => setState({ key, data, loading: false }), [key])
  return { ...result, refresh, recibir }
}

export function useAccion() {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  async function ejecutar(action: () => Promise<void>) {
    setBusy(true)
    setError('')
    try { await action() }
    catch (e) { setError(e instanceof Error ? e.message : 'No se pudo completar la operación.') }
    finally { setBusy(false) }
  }
  return { busy, error, ejecutar }
}
