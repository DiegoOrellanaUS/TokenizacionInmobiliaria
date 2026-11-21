import { StrictMode, useEffect } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Link, NavLink, Navigate, Outlet, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { ArrowUpRight, Blocks, LogOut } from 'lucide-react'
import { SesionProvider, useSesion } from './session'
import { Acceso } from './pages/Acceso'
import { Espacio, NuevoInmueble, Planes } from './pages/Espacio'
import { Expediente } from './pages/Expediente'
import { RevisionBandeja, RevisionDetalle } from './pages/Revision'
import { DetalleOferta, Mercado } from './pages/Mercado'
import { Portafolio } from './pages/Portafolio'
import './style.css'

function Shell() {
  const { sesion, salir } = useSesion()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  useEffect(() => {
    const name = pathname === '/' ? 'Inmuebles tokenizados' : pathname.startsWith('/espacio') ? 'Mi espacio' :
      pathname.startsWith('/revision') ? 'Revisión de inmuebles' : pathname.startsWith('/ofertas') ? 'Oferta inmobiliaria' : pathname === '/portafolio' ? 'Portafolio' : pathname === '/planes' ? 'Planes' : 'Acceso'
    document.title = 'Umbral | ' + name
    const privateRoute = pathname.startsWith('/espacio') || pathname.startsWith('/revision') || pathname === '/acceso' || pathname === '/portafolio'
    let robots = document.querySelector<HTMLMetaElement>('meta[name="robots"]')
    if (!robots) { robots = document.createElement('meta'); robots.name = 'robots'; document.head.append(robots) }
    robots.content = privateRoute ? 'noindex, nofollow' : 'index, follow'
  }, [pathname])

  return <div className="site">
    <a href="#contenido" className="skip">Saltar al contenido</a>
    <header className="topbar"><div className="topbar-inner">
      <Link className="brand" to="/" aria-label="Umbral, inicio"><i><Blocks size={19}/></i>umbral<span>.</span></Link>
      <nav aria-label="Navegación principal">
        {sesion?.esRevisor ? <NavLink to="/revision">Revisión</NavLink> : <>
          <NavLink to="/" end>Mercado</NavLink><NavLink to="/planes">Planes</NavLink>
          {sesion && <><NavLink to="/espacio">Mi espacio</NavLink><NavLink to="/portafolio">Portafolio</NavLink></>}
        </>}
      </nav>
      {sesion ? <button className="account" type="button" aria-label="Cerrar sesión" onClick={() => { salir(); navigate('/') }}>
        <span>{sesion.nombre.split(' ')[0]}</span> <LogOut size={17}/>
      </button> : <Link className="account" to="/acceso">Acceder <ArrowUpRight size={17}/></Link>}
    </div></header>
    <main id="contenido"><Outlet/></main>
    <footer><div><Link className="brand" to="/">umbral<span>.</span></Link><p>Mercado de demostración · Stellar Testnet</p></div></footer>
  </div>
}

function Privado({ requiereTenant = false, revisor = false }: { requiereTenant?: boolean; revisor?: boolean }) {
  const { sesion } = useSesion()
  if (!sesion) return <Navigate to="/acceso" replace/>
  const inicio = sesion.esRevisor ? '/revision' : '/espacio'
  if (sesion.esRevisor !== revisor || requiereTenant && !sesion.tenantId) return <Navigate to={inicio} replace/>
  return <Outlet/>
}

createRoot(document.getElementById('root')!).render(
  <StrictMode><BrowserRouter><SesionProvider><Routes>
    <Route element={<Shell/>}>
      <Route index element={<Mercado/>}/>
      <Route path="acceso" element={<Acceso/>}/>
      <Route path="planes" element={<Planes/>}/>
      <Route path="ofertas/:id" element={<DetalleOferta/>}/>
      <Route element={<Privado/>}><Route path="espacio" element={<Espacio/>}/><Route path="portafolio" element={<Portafolio/>}/></Route>
      <Route element={<Privado requiereTenant/>}>
        <Route path="espacio/nuevo" element={<NuevoInmueble/>}/>
        <Route path="espacio/inmuebles/:id" element={<Expediente/>}/>
      </Route>
      <Route element={<Privado revisor/>}><Route path="revision" element={<RevisionBandeja/>}/></Route>
      <Route element={<Privado revisor requiereTenant/>}>
        <Route path="revision/expedientes/:id" element={<RevisionDetalle/>}/>
      </Route>
      <Route path="*" element={<Navigate to="/" replace/>}/>
    </Route>
  </Routes></SesionProvider></BrowserRouter></StrictMode>,
)
