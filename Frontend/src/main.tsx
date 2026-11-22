import { StrictMode, useEffect, useState } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Link, NavLink, Navigate, Outlet, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { ArrowUpRight, Blocks, ChartNoAxesCombined, ClipboardList, FolderSearch, LogOut, ShieldCheck } from 'lucide-react'
import { SesionProvider, useSesion } from './session'
import { Acceso } from './pages/Acceso'
import { Espacio, NuevoInmueble, Planes } from './pages/Espacio'
import { Expediente } from './pages/Expediente'
import { RevisionBandeja, RevisionDetalle, RevisionTransacciones } from './pages/Revision'
import { DetalleOferta, Mercado } from './pages/Mercado'
import { Portafolio } from './pages/Portafolio'
import { Reportes } from './pages/Reportes'
import { PanelGlobal } from './pages/PanelGlobal'
import { RentasPropias, RentasRevision } from './pages/Rentas'
import { useDatos } from './hooks'
import './style.css'

function Shell() {
  const { sesion, modo, salir } = useSesion()
  const rentas = useDatos<{ habilitadas: boolean }>(sesion ? '/api/Compras/Rentas/Disponible' : null)
  const [errorSalida, setErrorSalida] = useState('')
  const navigate = useNavigate()
  const { pathname } = useLocation()
  useEffect(() => {
    const name = pathname.startsWith('/administracion') ? 'Administración global' : pathname === '/' ? 'Inmuebles tokenizados' : pathname === '/espacio/reportes' ? 'Mis reportes' : pathname.startsWith('/espacio') ? 'Mi espacio' :
      pathname.startsWith('/revision') ? 'Revisión de inmuebles' : pathname.startsWith('/ofertas') ? 'Oferta inmobiliaria' : pathname === '/portafolio' ? 'Portafolio' : pathname === '/planes' ? 'Planes' : 'Acceso'
    document.title = 'Umbral | ' + name
    const privateRoute = pathname.startsWith('/administracion') || pathname.startsWith('/espacio') || pathname.startsWith('/revision') || pathname === '/acceso' || pathname === '/portafolio'
    let robots = document.querySelector<HTMLMetaElement>('meta[name="robots"]')
    if (!robots) { robots = document.createElement('meta'); robots.name = 'robots'; document.head.append(robots) }
    robots.content = privateRoute ? 'noindex, nofollow' : 'index, follow'
  }, [pathname])

  function cerrar() {
    setErrorSalida('')
    void salir().then(() => navigate('/'), () => setErrorSalida('No se pudo cerrar la sesión. Intenta otra vez.'))
  }

  if (sesion?.esAdminGlobal && pathname.startsWith('/administracion/'))
    return <div className="site global-site">
      <a href="#contenido" className="skip">Saltar al contenido</a>
      <header className="review-header global-header">
        <Link className="review-brand" to="/administracion/indicadores">
          <ChartNoAxesCombined aria-hidden="true"/>
          <span>Administración global <small>Umbral · {sesion.nombre}</small></span>
        </Link>
        <nav aria-label="Navegación de administración global">
          <Link to="/">Mercado</Link>
        </nav>
        <button type="button" className="review-account" onClick={cerrar}>
          Cerrar sesión <LogOut size={16} aria-hidden="true"/>
        </button>
      </header>
      <main id="contenido">
        {errorSalida && <p className="alert" role="alert">{errorSalida}</p>}
        <Outlet/>
      </main>
    </div>

  if (sesion?.esRevisor) {
    if (pathname !== '/revision' && !pathname.startsWith('/revision/')) return <Navigate to="/revision" replace/>
    return <div className="review-site">
      <a href="#contenido" className="skip">Saltar al contenido</a>
      <header className="review-header">
        <Link className="review-brand" to="/revision">
          <ShieldCheck aria-hidden="true"/>
          <span>Panel de revisor <small>Umbral · {sesion.nombre}</small></span>
        </Link>
        <button type="button" className="review-account" onClick={cerrar}>
          Cerrar sesión <LogOut size={16} aria-hidden="true"/>
        </button>
      </header>
      <div className="review-layout">
        <nav className="review-nav" aria-label="Herramientas de revisión">
          <NavLink to="/revision" end><FolderSearch size={18} aria-hidden="true"/> Muro de solicitudes</NavLink>
          <NavLink to="/revision/transacciones"><ClipboardList size={18} aria-hidden="true"/> Transacciones</NavLink>
          <NavLink to="/revision/reportes"><ChartNoAxesCombined size={18} aria-hidden="true"/> Reportes globales</NavLink>
          {rentas.data?.habilitadas && <NavLink to="/revision/rentas">Repartos de alquiler</NavLink>}
        </nav>
        <main id="contenido">
          {errorSalida && <p className="alert" role="alert">{errorSalida}</p>}
          <Outlet/>
        </main>
      </div>
    </div>
  }

  return <div className="site">
    <a href="#contenido" className="skip">Saltar al contenido</a>
    <header className="topbar"><div className="topbar-inner">
      <Link className="brand" to="/" aria-label="Umbral, inicio"><i><Blocks size={19}/></i>umbral<span>.</span></Link>
      <nav aria-label="Navegación principal">
        <NavLink to="/" end>Mercado</NavLink><NavLink to="/planes">Planes</NavLink>
        {sesion && <><NavLink to="/espacio">Mi espacio</NavLink><NavLink to="/portafolio">Portafolio</NavLink>
          <NavLink to="/espacio/reportes">Mis reportes</NavLink>
          {sesion.tenantId && rentas.data?.habilitadas && <NavLink to="/espacio/rentas">Cierres de alquiler</NavLink>}</>}
        {sesion?.esAdminGlobal && <NavLink to="/administracion/indicadores">Indicadores globales</NavLink>}
      </nav>
      {sesion ? <button className="account" type="button" aria-label="Cerrar sesión" onClick={cerrar}>
        <span>{sesion.nombre.split(' ')[0]}</span> <LogOut size={17}/>
      </button> : <Link className="account" to="/acceso">Acceder <ArrowUpRight size={17}/></Link>}
    </div></header>
    <main id="contenido">
      {errorSalida && <p className="alert" role="alert">{errorSalida}</p>}
      {modo === 'cargando' && (pathname === '/planes' || pathname === '/' || pathname.startsWith('/ofertas/'))
        ? <p role="status" className="inner">Consultando sesión…</p> : <Outlet/>}
    </main>
    <footer><div><Link className="brand" to="/">umbral<span>.</span></Link><p>Mercado de demostración · Stellar Testnet</p></div></footer>
  </div>
}

function Privado({ requiereTenant = false, revisor = false, admin = false }:
  { requiereTenant?: boolean; revisor?: boolean; admin?: boolean }) {
  const { sesion, modo } = useSesion()
  if (modo === 'cargando') return <p role="status">Consultando sesión…</p>
  if (!sesion) return <Navigate to="/acceso" replace/>
  const inicio = sesion.esRevisor ? '/revision' : '/espacio'
  if (admin) return sesion.esAdminGlobal ? <Outlet/> : <Navigate to={inicio} replace/>
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
      <Route element={<Privado/>}>
        <Route path="espacio" element={<Espacio/>}/>
        <Route path="portafolio" element={<Portafolio/>}/>
        <Route path="espacio/reportes" element={<Reportes/>}/>
      </Route>
      <Route element={<Privado requiereTenant/>}>
        <Route path="espacio/nuevo" element={<NuevoInmueble/>}/>
        <Route path="espacio/inmuebles/:id" element={<Expediente/>}/>
        <Route path="espacio/rentas" element={<RentasPropias/>}/>
      </Route>
      <Route element={<Privado admin/>}><Route path="administracion/indicadores" element={<PanelGlobal/>}/></Route>
      <Route element={<Privado revisor/>}>
        <Route path="revision" element={<RevisionBandeja/>}/>
        <Route path="revision/expedientes/:tenantId/:id" element={<RevisionDetalle/>}/>
        <Route path="revision/transacciones" element={<RevisionTransacciones/>}/>
        <Route path="revision/reportes" element={<PanelGlobal/>}/>
        <Route path="revision/rentas" element={<RentasRevision/>}/>
      </Route>
      <Route path="*" element={<Navigate to="/" replace/>}/>
    </Route>
  </Routes></SesionProvider></BrowserRouter></StrictMode>,
)
