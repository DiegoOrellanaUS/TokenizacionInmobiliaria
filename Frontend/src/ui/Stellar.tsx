import { ArrowUpRight } from 'lucide-react'
import { useDatos } from '../hooks'

export const idCasaPrado = '32d2ac6f-f288-4dc9-b226-66a2379a518f'
export const rutaEvidencia = '/evidencia-stellar/casa-prado'
const explorador = 'https://stellar.expert/explorer/'

export const esHashStellar = (hash: string) => /^[0-9a-f]{64}$/i.test(hash)
export const enlaceTransaccionTestnet = (hash: string) => explorador + 'testnet/tx/' + hash

type IdentidadActivo = { codigo: string; emisor?: string | null; red?: string | null }

const activoCasaPrado = {
  codigo: 'INM001',
  emisor: 'GBWYOSVB6RZG7Y7GCJW5RGN5PKSBMKQYSRDEOECYLB725PEEO7D5Y6IG',
  red: 'testnet',
}

export function enlaceActivo({ codigo, emisor, red }: IdentidadActivo) {
  if (!/^[a-zA-Z0-9]{1,12}$/.test(codigo) || !emisor || !/^G[A-Z2-7]{55}$/.test(emisor)) return null
  const redNormalizada = red?.toLowerCase()
  if (redNormalizada !== 'testnet' && redNormalizada !== 'public') return null
  return explorador + redNormalizada + '/asset/' + codigo + '-' + emisor
}

export function EnlaceActivo({ oferta, resolver = false }: {
  oferta: IdentidadActivo & { id: string }; resolver?: boolean
}) {
  const conocido = enlaceActivo(oferta) || (oferta.id === idCasaPrado && oferta.codigo === activoCasaPrado.codigo
    ? enlaceActivo(activoCasaPrado) : null)
  const { data } = useDatos<IdentidadActivo>(
    resolver && !conocido ? '/api/Global/Activos/' + oferta.id : null, undefined, 4000)
  const url = conocido || (data?.codigo === oferta.codigo ? enlaceActivo(data) : null)
  return url ? <a className="text-button" href={url} target="_blank" rel="noreferrer">
    Ver token en Stellar <ArrowUpRight size={15} aria-hidden="true"/>
  </a> : null
}
