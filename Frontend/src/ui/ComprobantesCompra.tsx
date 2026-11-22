import type { Compra } from '../api'

const explorador = 'https://stellar.expert/explorer/testnet/tx/'

function Comprobante({ hash, titulo }: { hash: string; titulo: string }) {
  return <div>
    <a className="text-button" href={explorador + hash} target="_blank" rel="noreferrer">{titulo}</a>
    <div className="break-all"><code>{hash}</code></div>
  </div>
}

export function ComprobantesCompra({ compra }: { compra: Compra }) {
  return <>
    {compra.hashPago && <Comprobante hash={compra.hashPago} titulo="Ver pago USDC en Stellar"/>}
    {compra.estado === 'CONFIRMADA' && compra.hash && <Comprobante hash={compra.hash}
      titulo={compra.hashPago ? 'Ver entrega de ' + compra.codigo + ' en Stellar' : 'Ver transferencia'}/>}
  </>
}
