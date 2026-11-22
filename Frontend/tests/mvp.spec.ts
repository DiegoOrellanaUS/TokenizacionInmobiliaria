import { expect, test, type Page } from '@playwright/test'

async function entrar(page: Page, correo: string, contrasenia?: string) {
  await page.goto('/acceso')
  await page.getByLabel('Correo electrónico').fill(correo)
  if (contrasenia) await page.getByLabel('Contraseña').fill(contrasenia)
  const respuesta = page.waitForResponse(r => r.url().endsWith('/api/SesionDemo/Iniciar') && r.ok())
  await page.getByRole('button', { name: 'Continuar' }).click()
  return (await respuesta).json()
}

async function comprobarDescarga(page: Page, nombre: string, extension: string) {
  const descarga = page.waitForEvent('download')
  await page.getByRole('button', { name: nombre, exact: true }).click()
  const archivo = await descarga
  expect(archivo.suggestedFilename().endsWith('.' + extension)).toBe(true)
  expect(await archivo.failure()).toBeNull()
}

test('móvil: el catálogo abre y no desborda horizontalmente', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/')
  await expect(page.getByRole('heading', { name: /Un nuevo lugar/ })).toBeVisible()
  await page.getByRole('link', { name: 'Explorar ofertas' }).click()
  await expect(page.getByRole('heading', { name: 'Ofertas inmobiliarias' })).toBeVisible()
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBe(390)
})

test('MVP: registro, revisión, emisión, publicación y compra en Testnet', async ({ page, browser, baseURL }) => {
  test.setTimeout(360_000)
  const correoRevisor = process.env.E2E_REVIEWER_EMAIL
  if (!correoRevisor) throw new Error('Configura E2E_REVIEWER_EMAIL con un revisor local habilitado.')
  const gateway = process.env.E2E_API_URL || 'https://localhost:7339'

  const contrasenia = process.env.E2E_CONTRASENIA
  const contraseniaRevisor = process.env.E2E_CONTRASENIA_REVISOR
  if (process.env.VITE_PILOTO === 'true' && (!contrasenia || !contraseniaRevisor))
    throw new Error('Configura las contraseñas de prueba para el piloto.')
  const serie = Date.now().toString()
  const nombre = 'Casa Prado · ' + serie.slice(-6)
  const correo = `titular.${serie}@example.test`
  await page.goto('/acceso')
  await page.getByRole('button', { name: 'Crear cuenta', exact: true }).click()
  await page.getByLabel('Nombre', { exact: true }).fill('Alex')
  await page.getByLabel('Apellidos').fill('Demo')
  await page.getByLabel('Documento de identidad').fill(serie)
  await page.getByLabel('Correo electrónico').fill(correo)
  if (contrasenia) await page.getByLabel('Contraseña').fill(contrasenia)
  await page.getByRole('button', { name: 'Crear perfil' }).click()
  await page.getByRole('link', { name: 'Ver planes' }).click()
  const plan = process.env.E2E_PLAN
    ? page.getByRole('button', { name: 'Elegir ' + process.env.E2E_PLAN, exact: true })
    : page.getByRole('button', { name: /^Elegir / }).first()
  await expect(plan).toBeVisible()
  await plan.click()
  const suscripcion = page.waitForResponse(r => r.url().endsWith('/api/Subscripciones/crear') && r.request().method() === 'POST')
  await page.getByRole('button', { name: 'Confirmar pago de prueba' }).click()
  const pago = await (await suscripcion).json()
  expect(pago.estado).toBe('ACTIVA')
  expect([...pago.servicios].sort((a, b) => a.servicio.localeCompare(b.servicio))).toEqual([
    { servicio: 'inmuebles', estado: 'LISTO' },
    { servicio: 'reportes', estado: 'LISTO' },
    { servicio: 'tokenizacion', estado: 'LISTO' },
  ])
  await page.getByRole('button', { name: 'Ir a mis inmuebles' }).click()
  await expect(page.getByText(pago.tenantId, { exact: true })).toBeVisible()
  await page.getByRole('link', { name: 'Nuevo inmueble' }).click()
  await page.getByLabel('Nombre del inmueble').fill(nombre)
  await page.getByLabel('Ciudad').fill('Cochabamba')
  await page.getByLabel('Superficie en m²').fill('120')
  await page.getByLabel('Dirección').fill('Calle de ejemplo 123')
  await page.getByRole('button', { name: 'Guardar borrador' }).click()
  await expect(page.getByRole('heading', { name: nombre })).toBeVisible()
  const id = new URL(page.url()).pathname.split('/').at(-1)
  await page.getByLabel('Nombre completo').fill('Alex Demo')
  await page.getByLabel('Desde', { exact: true }).fill('2025-01-01')
  await page.getByLabel('Porcentaje').fill('100')
  await page.getByRole('button', { name: 'Guardar titulares' }).click()
  await expect(page.getByText('Alex Demo · 100%', { exact: true })).toBeVisible()
  await page.getByLabel('Respaldo de titularidad').setInputFiles({
    name: 'respaldo-demo.txt', mimeType: 'text/plain', buffer: Buffer.from('Documento ficticio de prueba')
  })
  await page.getByRole('button', { name: 'Adjuntar respaldo' }).click()
  await expect(page.getByText('TITULARIDAD · versión 1')).toBeVisible()
  const otro = await browser.newContext({ baseURL })
  try {
    const revisar = await otro.newPage()
    const perfil = await entrar(revisar, correoRevisor, contraseniaRevisor)
    expect(perfil).toMatchObject({ esRevisor: true, tenantId: null })
    const headers = { Authorization: 'Bearer ' + perfil.token }
    const [contratar, comprar, publicar] = await Promise.all([
      revisar.request.post(gateway + '/api/Subscripciones/crear', {
        headers, data: { usuarioId: perfil.usuarioId, codigoPlan: 'PRUEBA', referenciaPago: serie },
      }),
      revisar.request.post(gateway + '/api/Compras/Ofertas/' + pago.tenantId, {
        headers, data: { referencia: crypto.randomUUID(), unidades: 1 },
      }),
      revisar.request.post(gateway + '/api/Ofertas/' + id + '/Publicar', {
        headers: { ...headers, 'X-Tenant': pago.tenantId }, data: { precio: 10, moneda: 'BOB', unidades: 1 },
      }),
    ])
    expect([contratar.status(), comprar.status(), publicar.status()]).toEqual([403, 403, 403])
    await expect(revisar.getByRole('heading', { name: 'Muro de solicitudes' })).toBeVisible()
    await revisar.getByRole('link', { name: 'Transacciones', exact: true }).click()
    await expect(revisar.getByRole('heading', { name: 'Transacciones de la plataforma' })).toBeVisible()
    await revisar.getByRole('link', { name: 'Reportes globales' }).click()
    await expect(revisar.getByRole('heading', { name: 'Indicadores de la plataforma' })).toBeVisible()
    await revisar.getByRole('link', { name: 'Muro de solicitudes', exact: true }).click()
    await expect(revisar.getByRole('table')).toBeVisible()
    await expect(revisar.getByRole('link', { name: nombre })).toHaveCount(0)
    await page.bringToFront()
    await page.getByRole('button', { name: 'Enviar a revisión' }).click()
    await expect(page.getByText('En revisión', { exact: true })).toBeVisible()
    await revisar.bringToFront()
    await revisar.getByRole('link', { name: new RegExp(nombre) }).click({ timeout: 15000 })
    await expect(revisar.getByRole('heading', { name: 'Dictamen' })).toBeVisible()
    await revisar.getByLabel('Decisión').selectOption('CON_OBSERVACIONES')
    await revisar.getByLabel('Observación').fill('Adjunta una versión corregida.')
    await revisar.getByRole('button', { name: 'Registrar decisión' }).click()
    await page.bringToFront()
    await expect(page.getByText('Adjunta una versión corregida.')).toBeVisible({ timeout: 15000 })
    await page.getByLabel('Nueva versión del respaldo').setInputFiles({
      name: 'respaldo-corregido.txt', mimeType: 'text/plain', buffer: Buffer.from('Nueva versión de prueba')
    })
    await page.getByRole('button', { name: 'Guardar nueva versión' }).click()
    await expect(page.getByText('TITULARIDAD · versión 2')).toBeVisible()
    await page.getByRole('button', { name: 'Enviar a revisión' }).click()
    await revisar.bringToFront()
    await expect(revisar.getByRole('button', { name: 'Registrar decisión' })).toBeVisible({ timeout: 15000 })
    await revisar.getByLabel('Decisión').selectOption('APROBADO')
    await revisar.getByRole('button', { name: 'Registrar decisión' }).click()
    await expect(revisar.getByText('Aprobado', { exact: true }).first()).toBeVisible()
  } finally { await otro.close() }

  await page.bringToFront()
  await expect(page.getByRole('heading', { name: 'Tokenización' })).toBeVisible({ timeout: 15000 })
  await page.getByRole('button', { name: 'Preparar emisión' }).click()
  const emisionRegistrada = page.waitForResponse(r => r.url().endsWith('/' + id + '/Emitir') && r.request().method() === 'POST')
  await page.getByRole('button', { name: 'Confirmar emisión en Testnet' }).click()
  const emision = await (await emisionRegistrada).json()
  expect(emision.codigo).toMatch(/^I[A-Z0-9]{11}$/)
  await expect(page.getByRole('link', { name: 'Ver transacción' })).toBeVisible({ timeout: 95_000 })
  await expect(page.getByRole('heading', { name: 'Publicar oferta' })).toBeVisible({ timeout: 15_000 })
  await page.getByLabel('Precio por unidad').fill('60.50')
  await page.getByLabel('Unidades ofrecidas').fill('125')
  await page.getByRole('button', { name: 'Publicar en el catálogo' }).click()
  await page.getByRole('link', { name: 'Ver oferta pública' }).click()
  await expect(page.getByRole('heading', { name: nombre })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Ver token en Stellar' })).toHaveAttribute('href',
    new RegExp('/asset/' + emision.codigo + '-'), { timeout: 30_000 })
  const oferta = new URL(page.url()).pathname.split('/').at(-1)
  await expect(page.getByRole('status', { name: 'Unidades disponibles' })).toHaveText('125')
  await page.getByRole('button', { name: 'Cerrar sesión' }).click()
  await page.getByRole('textbox', { name: 'Buscar por nombre o ciudad' }).fill(nombre)
  await expect(page.getByRole('link', { name: new RegExp(nombre) })).toBeVisible()
  const comprobacion = await page.evaluate(async ({ correo, id, tenant, oferta, gateway, contrasenia }) => {
    const credencial = await fetch(gateway + '/api/SesionDemo/Iniciar', {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ correo, contrasenia }),
    })
    const { token } = await credencial.json()
    const publicar = gateway + '/api/Ofertas/' + id + '/Publicar'
    const headers = { Authorization: 'Bearer ' + token, 'Content-Type': 'application/json' }
    const body = JSON.stringify({ precio: 60.50, moneda: 'BOB', unidades: 125 })
    const sinTenant = await fetch(publicar, { method: 'POST', headers, body })
    const repetir = await fetch(publicar, { method: 'POST', headers: { ...headers, 'X-Tenant': tenant }, body })
    const ajeno = await fetch(publicar, { method: 'POST', headers: { ...headers, 'X-Tenant': crypto.randomUUID() }, body })
    const publica = await fetch(gateway + '/api/Ofertas/Buscar/' + oferta)
    return { acceso: credencial.ok, sinTenant: sinTenant.status, repetir: repetir.status,
      ajeno: ajeno.status, detalles: await publica.json() }
  }, { correo, id, tenant: pago.tenantId, oferta, gateway, contrasenia })
  expect(comprobacion.acceso).toBe(true)
  expect(comprobacion.sinTenant).toBe(400)
  expect(comprobacion.repetir).toBe(200)
  expect(comprobacion.ajeno).toBe(403)
  expect(comprobacion.detalles).not.toHaveProperty('tenantId')
  expect(comprobacion.detalles).not.toHaveProperty('direccion')
  const vendedor = page
  await vendedor.getByRole('link', { name: new RegExp(nombre) }).click()
  const compradores = await browser.newContext({ baseURL })
  try {
    const comprador = await compradores.newPage()
    const correoComprador = 'comprador.' + serie + '@example.test'
    await comprador.goto('/ofertas/' + oferta)
    await comprador.getByRole('link', { name: 'Compra demo', exact: true }).click()
    await comprador.getByRole('button', { name: 'Crear cuenta', exact: true }).click()
    await comprador.getByLabel('Nombre', { exact: true }).fill('Cris')
    await comprador.getByLabel('Documento de identidad').fill('C' + serie)
    await comprador.getByLabel('Correo electrónico').fill(correoComprador)
    if (contrasenia) await comprador.getByLabel('Contraseña').fill(contrasenia)
    await comprador.getByRole('button', { name: 'Crear perfil' }).click()
    await comprador.getByLabel('Unidades para la compra demo').fill('2')
    const ordenCreada = comprador.waitForResponse(r => r.url().includes('/api/Compras/Ofertas/') && r.request().method() === 'POST')
    await comprador.getByRole('button', { name: 'Compra demo', exact: true }).click()
    const respuesta = await ordenCreada
    expect(respuesta.status()).toBe(202)
    const orden = await respuesta.json()
    expect(orden).toMatchObject({ estado: 'PENDIENTE', unidades: 2, total: 0, sinPago: true })
    await expect(comprador.getByText('Compra demo registrada.', { exact: false })).toBeVisible()
    await expect(comprador.getByRole('status', { name: 'Unidades disponibles' })).toHaveText('123')
    await comprador.getByRole('link', { name: 'Ver mi portafolio' }).click()
    await expect(comprador.getByRole('heading', { name: 'Mi portafolio' })).toBeVisible()
    await expect(comprador.getByText('Pendiente', { exact: true })).toBeVisible()
    await expect(comprador.getByText('2 de 500 unidades emitidas', { exact: true })).toBeVisible({ timeout: 120_000 })
    await expect(comprador.getByText(orden.id, { exact: true })).toBeVisible()
    await expect(comprador.getByRole('link', { name: 'Ver entrega de ' + emision.codigo + ' en Stellar' }))
      .toHaveAttribute('href', /\/testnet\/tx\/[a-f0-9]{64}$/)

    const verificacion = await comprador.evaluate(async ({ correoComprador, correo, oferta, orden, gateway, contrasenia }) => {
      async function iniciar(correo: string) {
        const r = await fetch(gateway + '/api/SesionDemo/Iniciar', {
          method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ correo, contrasenia }),
        })
        return r.json()
      }
      const { token, tenantId } = await iniciar(correoComprador)
      const vendedor = await iniciar(correo)
      const headers = { Authorization: 'Bearer ' + token, 'Content-Type': 'application/json' }
      const cabeceraVendedor = { Authorization: 'Bearer ' + vendedor.token }
      const path = gateway + '/api/Compras/Buscar/' + orden.id
      const compra = await fetch(path, { headers })
      const sinToken = await fetch(path)
      const ajeno = await fetch(path, { headers: cabeceraVendedor })
      const revisor = await fetch(gateway + '/api/SesionDemo/MuroRevision', { headers })
      const rutaCompra = gateway + '/api/Compras/Ofertas/' + oferta + '/Demo'
      const reintento = await fetch(rutaCompra, {
        method: 'POST', headers, body: JSON.stringify({ referencia: orden.id, unidades: 2 }),
      })
      const exceso = await fetch(rutaCompra, {
        method: 'POST', headers, body: JSON.stringify({ referencia: crypto.randomUUID(), unidades: 124 }),
      })
      const propia = await fetch(rutaCompra, {
        method: 'POST', headers: { ...cabeceraVendedor, 'Content-Type': 'application/json' },
        body: JSON.stringify({ referencia: crypto.randomUUID(), unidades: 1 }),
      })
      const disponible = await fetch(gateway + '/api/Ofertas/Buscar/' + oferta)
      return { tenantId, compra: await compra.json(), sinToken: sinToken.status, ajeno: ajeno.status,
        revisor: revisor.status, reintento: reintento.status, exceso: exceso.status, propia: propia.status,
        disponible: (await disponible.json()).disponibles }
    }, { correoComprador, correo, oferta, orden, gateway, contrasenia })
    expect(verificacion).toMatchObject({ tenantId: null, sinToken: 401, ajeno: 404, revisor: 403, reintento: 200,
      exceso: 409, propia: 400, disponible: 123, compra: { estado: 'CONFIRMADA', unidades: 2, id: orden.id } })
    expect(verificacion.compra.hash).toMatch(/^[a-f\d]{64}$/)
    expect(verificacion.compra.billetera).toMatch(/^G[A-Z2-7]{55}$/)
    expect(verificacion.compra.ledger).toBeGreaterThan(0)
    await vendedor.bringToFront()
    await expect(vendedor.getByRole('status', { name: 'Unidades disponibles' })).toHaveText('123')

    if (process.env.E2E_RENTAS === 'true') await test.step('Descargas y cierre de alquiler en Stellar', async () => {
      const titular = await entrar(vendedor, correo, contrasenia)
      const navegacion = vendedor.getByRole('navigation', { name: 'Navegación principal' })
      await navegacion.getByRole('link', { name: 'Mis reportes', exact: true }).click()
      await expect(vendedor.locator('.kpi-graficas figure')).toHaveCount(3)
      await comprobarDescarga(vendedor, 'Excel de mi espacio', 'xlsx')
      await comprobarDescarga(vendedor, 'PDF de mi espacio', 'pdf')
      await navegacion.getByRole('link', { name: 'Cierres de alquiler', exact: true }).click()

      const mes = new Date(Date.now() - 4 * 60 * 60 * 1000)
      mes.setUTCDate(1)
      mes.setUTCMonth(mes.getUTCMonth() - 1)
      const periodo = mes.toISOString().slice(0, 7)
      await vendedor.locator('select[name="oferta"]').selectOption(oferta)
      await vendedor.getByLabel('Mes cerrado').fill(periodo)
      const campos = {
        'Alquiler cobrado': '2000', 'Base facturada para IVA': '2000', 'Crédito fiscal IVA admisible': '30',
        'Base imponible del IT': '2000', 'IUE pagado aplicado como crédito al IT': '10',
        'Utilidad fiscal estimada para provisionar IUE': '400', 'Impuesto municipal anual de referencia': '1200',
        'Reserva del mes': '120', 'Concepto del gasto 1': 'Mantenimiento E2E', 'Importe en Bs del gasto 1': '200',
        'Comprobante del gasto 1': 'FACTURA-E2E', 'Importe en Bs del pago 1': '230',
        'Fecha del pago 1': periodo + '-15', 'Referencia de la boleta 1': 'BOLETA-E2E',
      }
      for (const [label, valor] of Object.entries(campos)) await vendedor.getByLabel(label, { exact: true }).fill(valor)
      const respuestaCierre = vendedor.waitForResponse(r => r.url().endsWith('/Rentas/Ofertas/' + oferta) && r.request().method() === 'POST', { timeout: 20000 })
      await vendedor.getByRole('button', { name: 'Enviar cierre al revisor' }).click()
      const respuesta = await respuestaCierre
      expect(respuesta.ok(), await respuesta.text()).toBe(true)
      const cierre = await respuesta.json()
      expect(cierre).toMatchObject({ asignado: 4.8, aprobadoEn: null, contabilidad: { distribuible: 1200 } })
      const rutaCierre = gateway + '/api/Compras/Rentas/' + cierre.id
      const sinPermiso = await vendedor.request.post(rutaCierre + '/Aprobar', {
        headers: { Authorization: 'Bearer ' + titular.token },
      })
      expect(sinPermiso.status()).toBe(403)

      const revisar = await browser.newPage({ baseURL })
      try {
        const revisor = await entrar(revisar, correoRevisor, contraseniaRevisor)
        await revisar.getByRole('link', { name: 'Reportes globales', exact: true }).click()
        await expect(revisar.locator('.kpi-graficas figure')).toHaveCount(4)
        await comprobarDescarga(revisar, 'Excel', 'xlsx')
        await comprobarDescarga(revisar, 'PDF', 'pdf')
        await revisar.getByRole('link', { name: 'Repartos de alquiler', exact: true }).click()
        const detalle = revisar.getByRole('region', { name: 'Cierre de ' + nombre, exact: true })
        await expect(detalle.getByText('FACTURA-E2E')).toBeVisible()
        await expect(detalle.getByText('BOLETA-E2E')).toBeVisible()
        const headers = { Authorization: 'Bearer ' + revisor.token }
        expect((await revisar.request.post(rutaCierre + '/Pagar', { headers })).status()).toBe(409)
        const acciones = detalle.locator('..')
        await acciones.getByRole('button', { name: 'Aprobar reparto', exact: true }).click()
        await acciones.getByRole('button', { name: 'Enviar pagos Testnet', exact: true }).click()
        await expect(detalle.getByText('1 pago confirmado', { exact: false })).toBeVisible({ timeout: 90_000 })
        expect((await (await revisar.request.post(rutaCierre + '/Pagar', { headers })).json()).restantes).toBe(0)
      } finally { await revisar.close() }

      await comprador.bringToFront()
      const rentas = comprador.getByRole('region', { name: 'Mis rentas', exact: true })
      await expect(rentas.getByText('4,80', { exact: false })).toBeVisible({ timeout: 15000 })
      const enlace = rentas.getByRole('link', { name: 'Ver pago de renta en Stellar' })
      await expect(enlace).toHaveCount(1, { timeout: 15000 })
      const hashRenta = (await enlace.getAttribute('href'))!.split('/').at(-1)
      expect(hashRenta).toMatch(/^[a-f0-9]{64}$/)
      const horizonte = 'https://horizon-testnet.stellar.org/transactions/'
      await expect(async () => {
        const consulta = await comprador.request.get(horizonte + hashRenta + '/operations')
        expect(consulta.ok()).toBe(true)
        const { _embedded } = await consulta.json()
        expect(_embedded.records).toContainEqual(expect.objectContaining({
          type: 'payment', transaction_successful: true, asset_type: 'native',
          amount: '0.0480000', to: verificacion.compra.billetera,
        }))
      }).toPass({ timeout: 25000 })
      console.log(JSON.stringify({ hashRenta }))
    })

    console.log(JSON.stringify({ tenant: pago.tenantId, suscripcion: pago.subscripcionId, inmueble: id, oferta,
      compra: verificacion.compra }))
  } finally { await compradores.close() }
})
