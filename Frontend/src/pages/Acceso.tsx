import { useEffect, useState, type FormEvent } from 'react'
import { ArrowRight, ShieldCheck } from 'lucide-react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../api'
import { useAccion } from '../hooks'
import { useSesion } from '../session'
import { rutaFacebook, rutaGitHub } from '../oauth'
import { Button, Campo, ErrorBox } from '../ui/atoms'

const piloto = (import.meta.env.VITE_PILOTO ?? String(import.meta.env.PROD)) === 'true'

export function Acceso() {
  const { entrar, modo, proveedores, sesion } = useSesion()
  const navigate = useNavigate()
  const [query] = useSearchParams()
  const errorOAuth = query.get('error')
  const [registro, editar] = useState(errorOAuth === 'invitacion')
  const [revisor, elegirRevisor] = useState(errorOAuth === 'invitacion')
  const [codigo, setCodigo] = useState('')
  const [demo, setDemo] = useState(false)
  const [buscandoCodigo, setBuscandoCodigo] = useState(false)
  const [intentoDemo, repetirDemo] = useState(0)
  const action = useAccion()
  const volver = query.get('volver') ?? ''
  const origen = /^\/ofertas\/[a-f\d-]{36}$/i.test(volver) ? volver : '/espacio'
  const mostrarInvitacion = registro && revisor
  const proveedoresOAuth = [
    ...(proveedores.gitHub ? [{ nombre: 'GitHub', ruta: rutaGitHub }] : []),
    ...(proveedores.facebook ? [{ nombre: 'Facebook', ruta: rutaFacebook }] : []),
  ]

  useEffect(() => {
    if (modo === 'oauth' && sesion && (query.get('github') === 'ok' || query.get('facebook') === 'ok'))
      navigate(sesion.esAdminGlobal ? '/administracion/indicadores' : sesion.esRevisor ? '/revision' : origen, { replace: true })
  }, [modo, sesion, query, navigate, origen])

  useEffect(() => {
    if (!mostrarInvitacion || (modo !== 'oauth' && modo !== 'demo')) return
    const controller = new AbortController()
    setBuscandoCodigo(true)
    void api<{ codigo: string | null }>('/api/SesionDemo/CodigoRevisionDemo', { signal: controller.signal })
      .then(({ codigo: disponible }) => {
        if (controller.signal.aborted) return
        if (disponible) {
          setDemo(true)
          setCodigo(actual => actual || disponible)
        }
      }, () => {})
      .finally(() => { if (!controller.signal.aborted) setBuscandoCodigo(false) })
    return () => controller.abort()
  }, [mostrarInvitacion, modo, intentoDemo])

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const fields = new FormData(event.currentTarget)
    const correo = String(fields.get('correo'))
    const contrasenia = String(fields.get('contrasenia') ?? '') || undefined
    void action.ejecutar(async () => {
      if (registro) await api('/api/Usuarios/crear', {
        method: 'POST', body: {
          ci: fields.get('ci'), nombre: fields.get('nombre'), apellidos: fields.get('apellidos'),
          correoElectronico: correo, contrasenia: contrasenia ?? crypto.randomUUID() + '!Dz',
          codigoRevisor: mostrarInvitacion ? codigo : undefined,
        },
      })
      const nueva = await entrar(correo, contrasenia)
      navigate(nueva.esAdminGlobal ? '/administracion/indicadores' : nueva.esRevisor ? '/revision' : origen)
    })
  }

  const invitacion = registro && <div className="review-invitation">
    <label className="review-choice">
      <input type="checkbox" checked={revisor} onChange={e => elegirRevisor(e.target.checked)}/>
      <ShieldCheck size={19} aria-hidden="true"/> Crear cuenta como revisor
    </label>
    {mostrarInvitacion && <>
      <Campo label="Código de un solo uso">
        <input name="codigoRevisor" value={codigo} onChange={e => setCodigo(e.target.value)}
          required maxLength={128} autoComplete="off" spellCheck={false}/>
      </Campo>
      {buscandoCodigo && <small role="status">Buscando un código de demo…</small>}
      {demo && <div className="review-code-help">
        <small>El código de demo se consume al crear la cuenta.</small>
        <button className="text-button" type="button" disabled={buscandoCodigo} onClick={() => {
          setCodigo('')
          repetirDemo(n => n + 1)
        }}>Actualizar código</button>
      </div>}
    </>}
  </div>

  return <section className="access-wrap">
    <div className="access-image" aria-hidden="true" />
    <div className="access-body">
      <h1>{registro ? 'Crea tu cuenta' : 'Entra a tu espacio'}</h1>
      {modo === 'cargando' && <p role="status">Consultando sesión…</p>}
      {modo === 'error' && <ErrorBox text="No se pudo consultar el acceso. Recarga la página para volver a intentar."/>}
      {(modo === 'demo' || modo === 'oauth') && <div className="segmented" aria-label="Tipo de acceso">
        <button type="button" aria-pressed={!registro} onClick={() => editar(false)}>Entrar</button>
        <button type="button" aria-pressed={registro} onClick={() => editar(true)}>Crear cuenta</button>
      </div>}
      {modo === 'oauth' && <>
        {errorOAuth && <ErrorBox text={errorOAuth === 'invitacion'
          ? 'El código de revisor no es válido, ya fue utilizado o la cuenta del proveedor ya está registrada.'
          : errorOAuth === 'cuenta'
          ? 'No pudimos asociar tu cuenta. Comprueba que el proveedor comparte un correo o contacta al administrador si ya tienes un perfil.'
          : 'No pudimos completar el acceso. Intenta de nuevo.'}/>}
        <form className="form-stack" method="post" onSubmit={e => {
          if (!mostrarInvitacion || !codigo.trim()) e.preventDefault()
        }}>
          {invitacion}
          {mostrarInvitacion && <input type="hidden" name="volver" value={origen}/>}
          {proveedoresOAuth.map(({ nombre, ruta }) => mostrarInvitacion
            ? <Button key={ruta} formAction={ruta + '/RegistrarRevisor'}>Crear cuenta con {nombre} <ArrowRight size={17}/></Button>
            : <a key={ruta} className="button button-primary" href={ruta + '/Iniciar?volver=' + encodeURIComponent(origen)}>
              {registro ? 'Crear cuenta' : 'Continuar'} con {nombre} <ArrowRight size={17}/>
            </a>)}
        </form>
      </>}
      {modo === 'demo' && <form className="form-stack" onSubmit={submit}>
        {registro && <>
          <Campo label="Nombre"><input name="nombre" required autoComplete="given-name"/></Campo>
          <Campo label="Apellidos"><input name="apellidos" autoComplete="family-name"/></Campo>
          <Campo label="Documento de identidad"><input name="ci" required/></Campo>
        </>}
        <Campo label="Correo electrónico"><input name="correo" type="email" required autoComplete="email" placeholder="nombre@correo.com"/></Campo>
        {(piloto || !registro || mostrarInvitacion) && <Campo label={
          !piloto && !registro ? 'Contraseña (obligatoria para revisores y administradores)' : 'Contraseña'
        }><input name="contrasenia" type="password" minLength={piloto || registro ? 12 : undefined} maxLength={128}
          autoComplete={registro ? 'new-password' : 'current-password'} required={piloto || mostrarInvitacion}/></Campo>}
        {invitacion}
        <Button disabled={action.busy}>{registro ? 'Crear perfil' : 'Continuar'} <ArrowRight size={17}/></Button>
        <ErrorBox text={action.error}/>
      </form>}
    </div>
  </section>
}
