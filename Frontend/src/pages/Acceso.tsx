import { useState, type FormEvent } from 'react'
import { ArrowRight } from 'lucide-react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../api'
import { useAccion } from '../hooks'
import { useSesion } from '../session'
import { Button, Campo, ErrorBox } from '../ui/atoms'

const piloto = (import.meta.env.VITE_PILOTO ?? String(import.meta.env.PROD)) === 'true'

export function Acceso() {
  const [registro, editar] = useState(false)
  const { entrar } = useSesion()
  const navigate = useNavigate()
  const [query] = useSearchParams()
  const action = useAccion()
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const fields = new FormData(event.currentTarget)
    const correo = String(fields.get('correo'))
    const contrasenia = piloto ? String(fields.get('contrasenia')) : undefined
    void action.ejecutar(async () => {
      if (registro) await api('/api/Usuarios/crear', {
        method: 'POST', body: {
          ci: fields.get('ci'), nombre: fields.get('nombre'), apellidos: fields.get('apellidos'),
          correoElectronico: correo, contrasenia: contrasenia ?? crypto.randomUUID() + '!Dz',
          invitacion: piloto ? fields.get('invitacion') : undefined,
        },
      })
      const sesion = await entrar(correo, contrasenia)
      const volver = query.get('volver') ?? ''
      const origen = /^\/ofertas\/[a-f\d-]{36}$/i.test(volver) ? volver : '/espacio'
      navigate(sesion.esRevisor ? '/revision' : origen)
    })
  }

  return <section className="access-wrap">
    <div className="access-image" aria-hidden="true" />
    <div className="access-body">
      <h1>{registro ? 'Crea tu perfil' : 'Entra a tu espacio'}</h1>
      <p>{piloto ? 'Accede al piloto de Umbral con tu correo y contraseña.' : 'Usa tu correo en esta sesión de demostración.'}</p>
      <div className="segmented" aria-label="Tipo de acceso">
        <button type="button" aria-pressed={!registro} onClick={() => editar(false)}>Entrar</button>
        <button type="button" aria-pressed={registro} onClick={() => editar(true)}>Registrarme</button>
      </div>
      <form className="form-stack" onSubmit={submit}>
        {registro && <>
          <Campo label="Nombre"><input name="nombre" required autoComplete="given-name"/></Campo>
          <Campo label="Apellidos"><input name="apellidos" autoComplete="family-name"/></Campo>
          <Campo label="Documento de identidad"><input name="ci" required/></Campo>
        </>}
        <Campo label="Correo electrónico"><input name="correo" type="email" required autoComplete="email" placeholder="nombre@correo.com"/></Campo>
        {piloto && <Campo label="Contraseña"><input name="contrasenia" type="password" minLength={12} maxLength={128}
          autoComplete={registro ? 'new-password' : 'current-password'} required/></Campo>}
        {piloto && registro && <Campo label="Código de invitación"><input name="invitacion" maxLength={128} required/></Campo>}
        <Button disabled={action.busy}>{registro ? 'Crear perfil' : 'Continuar'} <ArrowRight size={17}/></Button>
        <ErrorBox text={action.error}/>
      </form>
    </div>
  </section>
}
