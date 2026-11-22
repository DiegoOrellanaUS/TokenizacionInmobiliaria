import type { Compra } from '../api'
import { enlaceTransaccionTestnet } from './Stellar'

function Comprobante({ hash, titulo }: { hash: string; titulo: string }) {
  return <div>
    <a className="text-button" href={enlaceTransaccionTestnet(hash)} target="_blank" rel="noreferrer">{titulo}</a>
    <div className="break-all"><code>{hash}</code></div>
  </div>
}

export function ComprobantesCompra({ compra }: { compra: Compra }) {
  return <>
    {compra.hashPago && <Comprobante hash={compra.hashPago} titulo="Ver pago USDC en Stellar"/>}
    {compra.estado === 'CONFIRMADA' && compra.hash && <Comprobante hash={compra.hash}
      titulo={'Ver entrega de ' + compra.codigo + ' en Stellar'}/>}
  </>
}
