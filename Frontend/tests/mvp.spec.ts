import { expect, test } from '@playwright/test'

test('móvil: el catálogo abre y no desborda horizontalmente', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/')
  await expect(page.getByRole('heading', { name: /Un nuevo lugar/ })).toBeVisible()
  await page.getByRole('link', { name: 'Explorar ofertas' }).click()
  await expect(page.getByRole('heading', { name: 'Ofertas inmobiliarias' })).toBeVisible()
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBe(390)
})

test('MVP: registro, revisión, emisión, publicación y compra en Testnet', async ({ page, browser, baseURL }) => {
  test.setTimeout(300_000)
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
  await page.getByRole('button', { name: 'Registrarme' }).click()
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
  expect(pago.servicios).toEqual([
    { servicio: 'inmuebles', estado: 'LISTO' },
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
    await revisar.goto('/acceso')
    await revisar.getByLabel('Correo electrónico').fill(correoRevisor)
    if (contraseniaRevisor) await revisar.getByLabel('Contraseña').fill(contraseniaRevisor)
    await revisar.getByRole('button', { name: 'Continuar' }).click()
    await expect(revisar.getByRole('heading', { name: 'Bandeja de revisión' })).toBeVisible()
    await revisar.getByLabel('Espacio para revisar').selectOption(pago.tenantId)
    await revisar.getByRole('button', { name: 'Abrir expedientes' }).click()
    await expect(revisar.getByText('No hay expedientes en esta bandeja')).toBeVisible()
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
  await page.getByRole('button', { name: 'Confirmar emisión en Testnet' }).click()
  await expect(page.getByRole('link', { name: 'Ver transacción' })).toBeVisible({ timeout: 95_000 })
  await expect(page.getByRole('heading', { name: 'Publicar oferta' })).toBeVisible({ timeout: 15_000 })
  await page.getByLabel('Precio por unidad').fill('60.50')
  await page.getByLabel('Unidades ofrecidas').fill('125')
  await page.getByRole('button', { name: 'Publicar en el catálogo' }).click()
  await page.getByRole('link', { name: 'Ver oferta pública' }).click()
  await expect(page.getByRole('heading', { name: nombre })).toBeVisible()
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
    await comprador.getByRole('link', { name: 'Acceder para comprar' }).click()
    await comprador.getByRole('button', { name: 'Registrarme' }).click()
    await comprador.getByLabel('Nombre', { exact: true }).fill('Cris')
    await comprador.getByLabel('Documento de identidad').fill('C' + serie)
    await comprador.getByLabel('Correo electrónico').fill(correoComprador)
    if (contrasenia) await comprador.getByLabel('Contraseña').fill(contrasenia)
    await comprador.getByRole('button', { name: 'Crear perfil' }).click()
    await comprador.getByLabel('Unidades a comprar').fill('2')
    const ordenCreada = comprador.waitForResponse(r => r.url().includes('/api/Compras/Ofertas/') && r.request().method() === 'POST')
    await comprador.getByRole('button', { name: 'Confirmar compra de prueba' }).click()
    const respuesta = await ordenCreada
    expect(respuesta.status()).toBe(202)
    const orden = await respuesta.json()
    expect(orden).toMatchObject({ estado: 'PENDIENTE', unidades: 2, total: 121 })
    await expect(comprador.getByText('Pago simulado registrado.', { exact: false })).toBeVisible()
    await expect(comprador.getByRole('status', { name: 'Unidades disponibles' })).toHaveText('123')
    await comprador.getByRole('link', { name: 'Ver mi portafolio' }).click()
    await expect(comprador.getByRole('heading', { name: 'Mi portafolio' })).toBeVisible()
    await expect(comprador.getByText('Pendiente', { exact: true })).toBeVisible()
    await expect(comprador.getByText('2 unidades confirmadas', { exact: true })).toBeVisible({ timeout: 120_000 })
    await expect(comprador.getByRole('link', { name: 'Ver transferencia' })).toHaveAttribute('href', /\/testnet\/tx\/[a-f0-9]{64}$/)

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
      const revisor = await fetch(gateway + '/api/SesionDemo/EspaciosRevision', { headers })
      const rutaCompra = gateway + '/api/Compras/Ofertas/' + oferta
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
    console.log(JSON.stringify({ tenant: pago.tenantId, suscripcion: pago.subscripcionId, inmueble: id, oferta,
      compra: verificacion.compra }))
  } finally { await compradores.close() }
})
