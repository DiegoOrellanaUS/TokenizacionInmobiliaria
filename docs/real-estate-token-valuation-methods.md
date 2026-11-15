# Real Estate Tokenization on Stellar: Valuation, Distributions and Investor Flow

Corrected edition for Bolivia (Santa Cruz and Cochabamba)

| | |
|---|---|
| Status | Draft for team review |
| Version | 2.2, 2026-11-14 |
| Replaces | "Real Estate Token Valuation Methods", an 18-page Gemini chat export |
| Assumed stack | Stellar (classic assets, optional Soroban), Pollar SDK 0.12.1, USDC |
| Legal notice | A technical design reference, not legal or tax advice. Items tagged **Counsel** need confirmation from Bolivian securities and tax counsel before launch. |
| Language | English. Spanish version: [metodos-de-valoracion-de-tokens-inmobiliarios.md](metodos-de-valoracion-de-tokens-inmobiliarios.md) |

## How to read this document

- Sections 1 to 8 are the design. Appendix A collects the formulas, Appendix B holds a tested reference contract, Appendix C lists every correction to the original, and Appendix D lists the sources.
- Every worked example uses one running building and one set of assumptions (section 0.3), so the numbers agree across sections. They are illustrations, not market data.
- Legal and tax statements cite the text that was read. Where only a secondary source was available, the text says so.
- The design is stack-neutral. The backend can live in the repository's .NET services. Only the optional contract in Appendix B needs Rust.

## 0. Summary

The platform sells fractions of real estate as tokens: completed buildings, projects under construction and land with no construction yet, offered one property at a time or as a portfolio of several. It values each token from an independently appraised NAV, collects rent in bolivianos where the property earns it, pays net distributable cash to holders in USDC, and uses an AI concierge to turn a buyer's budget into purchase options. The running example is a building. Section 1.6 covers land and section 1.7 covers several properties.

The original text was right to price tokens from NAV, to use integer math, and to keep arithmetic away from the language model. It also had structural problems. Its token contract could create tokens. Its Stellar design mixed token models that cannot work together. The Bolivian securities and fintech rules were missing. Its examples used a fixed exchange rate and yields that its own waterfall could not produce. This edition fixes those points and records each change in Appendix C.

### 0.1 Architecture at a glance

```text
BUY SIDE
  Investor wallet (Pollar)
    1. KYC and approval. The wallet's trustline exists but stays unauthorized
    2. BOB by QR -> ramp order -> USDC in the wallet (several legs, minutes)
    3. One platform-built Stellar transaction: authorize the trustline,
       USDC investor -> ESCROW, tokens DISTRIBUTOR -> investor, remove the authorization

RENT SIDE
  Tenants --BOB--> vehicle bank account
    monthly close: taxes, costs, reserves -> net pool in BOB
    convert at a recorded rate -> USDC -> DISTRIBUTION account
    record-date snapshot -> batched USDC payments -> holders

ACCOUNTS (no account does two jobs)
  Issuer (multisig, service key for trustline flags) | Distributor (unsold tokens)
  Escrow (sale proceeds, reserved tokens)
  Distribution (rent USDC) | Sponsor (network fees)
```

### 0.2 Decisions

| # | Decision | Default in this edition | Status |
|---|---|---|---|
| D1 | Legal wrapper and public-offer route | Not chosen. The design stays wrapper-neutral (section 5) | **Counsel**, decide first |
| D2 | Token model | Classic Stellar asset with issuer flags, "Model A" (section 2.2) | Recommended for v1 |
| D3 | Transfer control | `AUTH_REQUIRED`, `AUTH_REVOCABLE`, clawback. Holders stay unauthorized between platform-built transactions, so tokens move only through the platform (section 2.3) | Recommended |
| D4 | Rent distribution | Record-date snapshot and batched USDC payments (section 6.1) | Recommended for v1 |
| D5 | Secondary liquidity | No open market. Capped redemption windows if the law allows (section 5.4) | **Counsel** |
| D6 | Valuation | Independent appraisal, NAV published with the report hash, bounded change (section 1.3) | Recommended |
| D7 | Currency | USDC on-chain. BOB converted at a recorded rate each period (section 4.5) | To confirm |
| D8 | Pollar | Wallets, sponsored trustlines, KYC flow, ramps. BOB route to be confirmed (section 3) | To confirm with Pollar |
| D9 | AI concierge | Language model for conversation, deterministic tools for every number (section 7) | Recommended |
| D10 | Unit of offering | One token per property. A portfolio token only through a regulated fund or a vehicle built for it (section 1.7) | Recommended for v1 |
| D11 | Land without construction | Holding-cost reserve funded at launch, no distributions until a sale or a lease, urban lots with clean title first (section 1.6) | **Counsel** for rural land |

### 0.3 Running example and assumptions

One building carries all examples. Change an assumption and every figure that depends on it changes.

| ID | Assumption |
|---|---|
| A1 | **SC-1**: ten apartments in Santa Cruz, long-term corporate leases, bought at its appraisal of $1,000,000. |
| A2 | Capital of $1,050,000 = $1,000,000 price + $40,000 closing and structuring costs (spent) + $10,000 initial cash reserve, funded by selling 105,000 tokens at $10.00. Until every token is sold, the party that funded the purchase holds the rest. The transfer tax is not included (section 1.2). |
| A3 | Gross rent invoiced: 70,000 BOB a month, tax-inclusive. IVA 13% and IT 3% are computed on that amount. IPBI accrual 1,800 BOB. Vacant-unit expensas and repairs 3,500 BOB. Management fee 10% of gross rent. CapEx and vacancy reserve 5% of gross rent. All costs are invoiced with IVA (`factura`). |
| A4 | IUE accrual 8,000 BOB a month. Check: with land at 30% of the price and the building depreciated at 2.5% a year, the estimated taxable profit is 370,155 BOB a year, so IUE is 92,539 BOB a year, or 7,712 BOB a month (section 4.2). |
| A5 | Exchange rate 11.67 BOB per USD, the BCB official rate on 14 November 2026, held constant in projections. |
| A6 | **CB-1**: a completed Cochabamba building at $25.00 per token, gross yield 4.5% on its token price, same net-to-gross ratio as SC-1. **SC-2**: a pre-sale at $8.00 per token with a projected +18% delivery value and no rent during construction. |
| A7 | **LT-1**: an urban lot of 2,000 m² with no construction, bought at its appraisal of $300,000 ($150 per m²). Closing and structuring costs of $12,000 (4%, as for SC-1). Holding costs of $6,600 a year: IPBI $600, fencing, security and cleaning $1,800, management fee of 1% of the appraisal $3,000, accounting and audit $1,200. A reserve for 3 years ($19,800) is funded at launch, and the plan is to sell after 3 years. Selling costs are 6% of the sale price (3% transfer tax, 3% brokerage). Illustrative figures, not market data. |

### 0.4 Terms

| Term | Meaning |
|---|---|
| BCB, ASFI, UIF, SIN | Central Bank, financial regulator, financial intelligence unit, tax authority |
| IVA 13%, IT 3% | Value-added tax and transactions tax, both charged on invoiced rent |
| IUE 25%, IUE-BE 12.5% | Corporate income tax, and the same tax withheld on payments to foreign beneficiaries |
| RC-IVA 13% | Income complement tax for individuals |
| IPBI, IMT | Annual municipal property tax, and municipal property transfer tax (3%) |
| NIT, `factura`, SIAT | Tax ID, tax invoice, and the SIN's invoicing and filing system |
| RMV, EDV, `agencia de bolsa` | Securities market register, securities depository, licensed securities broker |
| ETF, PSAV | Financial technology company, and virtual asset service provider (both defined in DS 5384) |
| `expensas`, `anticrético` | Building common-area fees, and a lease where the tenant lends a lump sum instead of paying rent |
| `patrimonio autónomo` | A legally separate pool of assets, used by trusts, securitizations and investment funds |
| UFV | Inflation-indexed unit of account published by the BCB |
| NAV, NOI | Net asset value, net operating income |
| SPV, "the vehicle" | The legal entity or estate that holds the building |
| SAC, SEP | Stellar Asset Contract, Stellar Ecosystem Proposal |
| SAFI, FIC | Investment fund manager, and closed-end investment fund |
| SEGIP, RUAT | National identity registry, and the platform many municipalities use to collect their taxes |
| Derechos Reales, folio real | The property registry, and a property's entry in it |
| INRA, `saneamiento`, `título ejecutorial` | The national agrarian reform institute, its procedure to regularize rural title, and the final title it issues |
| FES | `Función económico social`: the productive use that medium and business rural property must show to keep its title |

## 1. Token price and NAV

### 1.1 What a token represents

A token is a fixed share of the economic rights of the vehicle that holds one property (a building, a project under construction or a plot of land) or a portfolio of properties (section 1.7). Ownership share equals tokens held divided by tokens outstanding. The supply is fixed at issue, so percentages never need to be stored.

| Whole tokens issued | Base units (10^7 per token) | One token is |
|---|---|---|
| 10,000 | 10^11 | 0.01% |
| 100,000 | 10^12 | 0.001% |
| 105,000 (running example) | 1,050,000,000,000 | 0.000952% |

Choose a supply that gives a familiar unit price ($10 to $100). Customers can still hold fractions down to seven decimals.

Avoid pegging tokens to square meters. A peg of 100 tokens per square meter assumes equal value per square meter across floors, views and uses. The equity percentage is what matters, so use area only as a description.

What the token legally is depends on the wrapper (section 5). Do not describe it as ownership of the property unless the structure grants that right.

### 1.2 Primary price

```math
P_0 = \frac{\text{purchase price} + \text{closing and structuring costs} + \text{initial cash reserve} - \text{debt}}{N}
```

Running example (A1, A2): P0 = (1,000,000 + 40,000 + 10,000 - 0) / 105,000 = **$10.00**.

Investors pay $10.00, but the day-1 NAV is (1,000,000 + 10,000 - 0) / 105,000 = **$9.6190**. The $0.3810 difference (3.81% of the price) is an entry cost. Show it before purchase and in the first NAV report.

**Transfer tax.** The municipal transfer tax (IMT, 3%) is owed by the person in whose name the property is registered, which is the seller ([DS 24054](https://www.lexivox.org/norms/BO-DS-24054.xhtml), art. 5). The first sale of new construction pays the national IT instead (art. 2). Who bears the cost in practice is a term of the purchase contract. Add it to the costs above only if the contract shifts it to the vehicle. The base is the higher of the price paid and the value used for IPBI (art. 6).

### 1.3 NAV per token

```math
NAV_t = \frac{1}{\text{tokens outstanding}}
\left(
\begin{aligned}
& \text{appraised value} \\
& + \text{cash and reserves} \\
& + \text{receivables, net of bad-debt provision} \\
& - \text{debt and accrued interest} \\
& - \text{tenant security deposits} \\
& - \text{anticrético capital received} \\
& - \text{taxes payable and accrued (IVA, IT, IUE, IPBI)} \\
& - \text{distributions declared but not yet paid} \\
& - \text{accrued fees}
\end{aligned}
\right)
```

- **Denominator.** "Tokens outstanding" means every token that is a claim on the vehicle: each issued token except any the vehicle holds itself. Tokens in the distributor and escrow accounts count, because the party that funded the purchase paid for that part of the building. The original divided by "circulating" tokens, which leaves them out and overstates NAV per token. If the vehicle itself keeps unsold tokens, treat them like treasury shares: exclude them, and record the money that funded that part of the building (for example a bridge loan) as a liability.
- **Appraisal.** Use an independent appraiser. For Bolivian residential property, comparable sales should lead and the income approach should cross-check. Reappraise at least once a year and after a material event (major CapEx, long vacancy, damage).
- **Governance.** The appraiser must be independent of the asset manager. Publish the SHA-256 of the signed report and record it with each NAV update. Limit the change per update (default 20%). A larger move needs a separate, logged decision. Use a multisig for the NAV key.
- **Display.** Show the NAV date and the appraisal date next to every price.
- **Exit view (optional).** For redemption planning, take NAV_t and subtract selling costs, the transfer tax owed by the seller, and IUE on the gain (25% of sale value minus tax basis). The vehicle is the seller, so it carries these costs.

### 1.4 Income approach as a cross-check

```math
\text{implied value} = \frac{\text{NOI}}{\text{cap rate}}
```

NOI is rent net of IVA, minus IT, vacancy and credit loss, IPBI, insurance, repairs, management, and owner-paid expensas and utilities. Measure it before IUE, debt service, depreciation and CapEx reserve.

Reliable public cap-rate data for Bolivian residential property is thin. Numbeo's crowd-sourced gross yields (updated 10 November 2026) are a rough guide: [Santa Cruz](https://www.numbeo.com/property-investment/in/Santa-Cruz-Bolivia) 6.34% in the centre and 10.20% outside, [Cochabamba](https://www.numbeo.com/property-investment/in/Cochabamba) 4.24% and 3.71%. The samples are small (44 and 38 entries). SC-1's gross yield of 7.20% on its appraisal sits inside the Santa Cruz range.

### 1.5 Price after launch

Primary sales happen at P0, or at the latest NAV plus a disclosed fee. Any trading venue would price above or below NAV with demand. Do not promise redemption at NAV unless a funded redemption mechanism exists (section 5.4).

### 1.6 Land without construction

A plot of land earns no rent, so its token returns only the change in value and what happens at the end: a sale, a lease or a construction project.

**Valuation**

- Use comparable land sales priced per m², adjusted for location, size, frontage, allowed land use and services (water, sewer, electricity). The value the municipality uses for IPBI is not a market value.
- For land that could be built on, cross-check with the residual method:

```math
\begin{aligned}
\text{land value} = {} & \text{value of the finished project} \\
& - \text{construction costs} - \text{fees, permits and taxes} \\
& - \text{financing costs} - \text{developer profit}
\end{aligned}
```

- The income approach (section 1.4) applies only if the land is leased, for example as a parking lot or for farming.
- Land does not depreciate (section 4.3), and with no income the holding costs produce tax losses. A loss can offset taxable profit only in the following three years (Ley 843 art. 48 as amended by Ley 169, per a [SIN resolution](https://boliviaimpuestos.com/resultados-acumulados-negativos-contabilidad/) reproduced by boliviaimpuestos.com). If the sale slips past year 4, the first year's losses expire. Some new productive ventures get five years (**Counsel**).

**Holding costs and the reserve**

With no rent, IPBI, fencing, security, cleaning, insurance and the management fee must come from cash. Fund a reserve at launch for the planned holding period plus a margin, and include it in the price. Do not plan on asking holders for more money later: collecting a capital call from thousands of token holders is hard, and it may count as a new offering (**Counsel**).

**Worked example (LT-1, assumption A7)**

| Item | Value |
|---|---|
| Capital | $331,800 = $300,000 price + $12,000 closing costs + $19,800 reserve, so 33,180 tokens at $10.00 |
| Day-1 NAV | ($300,000 + $19,800) / 33,180 = $9.6383, an entry cost of $0.3617 (3.62%) |
| NAV if the land value stays flat | $9.4394 after year 1, $9.2405 after year 2 and $9.0416 after year 3, as the reserve is spent |
| Break-even sale price | $352,978.72, which is 17.66% above the purchase price, or 5.57% a year over 3 years |

| Sale price after 3 years | Per token to holders | Total result | Per year |
|---|---:|---:|---:|
| -10% ($270,000) | $7.6492 | -23.51% | -8.55% |
| Flat ($300,000) | $8.4991 | -15.01% | -5.28% |
| +10% ($330,000) | $9.3490 | -6.51% | -2.22% |
| Break-even ($352,978.72) | $10.0000 | 0.00% | 0.00% |
| +30% ($390,000) | $10.7866 | +7.87% | +2.56% |

Amounts per token are after 6% selling costs and 25% IUE on the gain, assuming the three years of holding costs offset the gain. Only the +30% case pays IUE ($8,700). With these assumptions the land must gain about 18% in three years before holders get their money back, so the concierge shows this table instead of a yield (section 7.4).

**Title and legal checks**

- **Urban lots.** A title registered in Derechos Reales (folio real) in the seller's name, a municipal cadastral record, no overlap with neighboring plots, and an approved subdivision plan if the lot belongs to an urbanization. If construction is part of the plan, check the allowed land use and building rules. Ley 477 of 2013 makes land invasions (`avasallamiento`) and land trafficking crimes ([Lexivox](https://www.lexivox.org/norms/BO-L-N477.xhtml)), but vacant land still needs fencing and watch, which the holding costs include.
- **Rural land ("fields").** Treat it as a separate product that needs counsel before any offering. Article numbers below follow the 2009 Constitution as promulgated ([OAS copy](http://www.oas.org/juridico/PDFs/mesicic4_blv_const.pdf)).
  - The title must come out of INRA's `saneamiento`, the procedure that regularizes rural property ([INRA](https://www.inra.gob.bo/saneamiento-de-la-propiedad-agraria/)). INRA's Cochabamba office told buyers to ask for the seller's `título ejecutorial`, a cadastral plan and certificate, and a folio real in Derechos Reales ([Los Tiempos](https://www.lostiempos.com/actualidad/cochabamba/20220306/instan-verificar-6-requisitos-evitar-estafas-avasalladores)).
  - Medium and business rural property must meet the FES, and failing it is a ground for reversion to the State (arts. 397 and 401).
  - Small rural property is indivisible (art. 394), and dividing property below the maximum size of the small property is prohibited (art. 400).
  - Earning land rent from speculative use of land is prohibited (art. 395).
  - Legal entities may own land as long as it is used for their purpose, to create jobs and to produce goods or services (art. 315). A vehicle that holds idle land only to sell it later is exposed under arts. 315 and 395. Counsel should also confirm whether art. 315 reaches urban lots.
  - Within 50 km of a border, a foreigner cannot acquire property, alone or through a company (art. 262).
  - These rules make rural land a poor fit for a passive token. Start with urban lots.

**Exit.** The vehicle eventually sells, leases or builds. A sale pays out the net proceeds and closes the token (section 6.3). A lease turns the land into an income property under section 4. A construction project needs new money, so the offering must say before launch who decides, how construction would be financed (new tokens at NAV, debt or a developer partner) and what happens to holders who do not take part.

### 1.7 Several properties

There are two ways to offer more than one property.

| | One token per property (default) | One portfolio token |
|---|---|---|
| What the holder owns | A share of a vehicle that holds one property | A share of a vehicle that holds several properties |
| NAV | Section 1.3, per property | All appraisals, cash and reserves, minus all liabilities, divided by the portfolio's tokens outstanding |
| Distributions | Each property's own net pool (section 4) | One pool: net rent of the income properties minus the holding costs of any land |
| Diversification | The investor builds it by buying several tokens (option B in section 7.4) | Built in |
| Ring-fencing | A problem in one property stays there | A problem in one property reaches every holder |
| Legal fit | Each vehicle and offering stands alone | A closed-end fund (FIC) is the natural wrapper (section 5.2) |
| On Stellar | One asset per property, for example `SC1`, `CB1` and `LT1` | One asset for the portfolio |

An asset code is 1 to 12 characters, and together with the issuer account it identifies the asset ([Stellar CLI cookbook](https://developers.stellar.org/docs/tools/cli/cookbook/asset-management)). Use one issuer account per legal vehicle, so the flags and the register follow the legal owner.

**Adding a property to a portfolio.** Issue the new tokens at the current NAV plus the costs of the new acquisition, so existing holders are not diluted:

```math
\begin{aligned}
& \text{new tokens} && N_{\text{new}} = \frac{V_{\text{new}}}{NAV_t} \\[0.5em]
& \text{issue price} && P_{\text{new}} = NAV_t \times \frac{V_{\text{new}} + C_{\text{new}}}{V_{\text{new}}}
\end{aligned}
```

Example: NAV_t is $9.80 and a new property costs $500,000 plus $20,000 of acquisition costs. Issue 51,020.4082 tokens at $10.1920 to raise $520,000, and NAV per token stays at $9.80. Issuing 53,061.2245 tokens at the $9.80 NAV, ignoring the costs, would lower every holder's NAV to $9.6735, a 1.29% dilution. Appraise the whole portfolio before each issue, because NAV_t must be current.

Mixing land and income properties in one portfolio lowers the payout of the whole token. Disclose the mix and each property's share of NAV.

## 2. The token on Stellar

### 2.1 Precision and supply

Stellar assets use seven decimals. One base unit (a "stroop") is 10^-7 of a token, so a supply of N whole tokens is N x 10^7 base units. Store and compute only integers. Soroban has no floating-point types, so any contract math uses `i128` fixed point. Round payouts down and carry the remainder forward (section 6.1).

Set a minimum order (for example one token) and allow fractions to seven decimals above it.

### 2.2 Choose one token model

The original mixed three models: a classic asset with a Stellar Asset Contract (page 2), trading on the Stellar DEX (page 4), and a contract that kept balances in its own storage (pages 9 to 15). They cannot work together. A balance kept only in a contract's private storage is not a SEP-41 token, so wallets cannot display it and nothing else can trade it. A classic asset traded on the DEX never calls the contract, so the contract's rent checkpoint never runs and accrued rent goes to the wrong holder.

Stellar's [assets comparison](https://developers.stellar.org/docs/tokens/anatomy-of-an-asset) describes the three options this edition uses:

| | **A. Classic asset** (with its SAC) | **B. SEP-57 contract token** (T-REX, ERC-3643) | **C. Custom SEP-41 token** with rent index (Appendix B) |
|---|---|---|---|
| Balances live in | Trustlines | Contract storage | Contract storage |
| Wallets and Stellar DEX | Full wallet support. The DEX works through classic operations, which this design blocks (section 2.3) | Some wallets show balances. Transfers and control functions are not yet supported in wallets. No DEX | Wallets can show the balance through SEP-41. No DEX |
| Compliance controls | Issuer flags and the SEP-8 transaction pattern | On-chain identity and transfer rules, for example [OpenZeppelin's RWA suite](https://docs.openzeppelin.com/stellar-contracts/tokens/rwa/rwa) | An allowlist you write |
| Rent distribution | Off-chain snapshot plus payments (section 6.1) | A module you add. Indexers must be self-built | Built in (on-chain index) |
| Audit burden | No custom contract | Audited library, but your rules and modules are not | All of it |
| Pollar fit | Sponsored trustlines are supported | Contract calls through the SEP-43 signer | Same as B |

**Recommendation.** Start with Model A. The legal register sits off-chain in any compliant structure, so distributions can follow ordinary securities practice: a record date, then payment. Move to B or C only when you need on-chain transfer rules or fully on-chain accrual, and budget an audit before you do.

### 2.3 Compliance controls for Model A

| Flag or feature | What it gives you |
|---|---|
| `AUTH_REQUIRED` | New trustlines start unauthorized. Nobody can receive the token until the issuer authorizes the trustline |
| `AUTH_REVOCABLE` | The issuer can remove an authorization at any time. The pattern below and freezing a holder both need it |
| `AUTH_CLAWBACK_ENABLED` | The platform can recover or burn tokens for court orders, inheritance or lost keys. It requires `AUTH_REVOCABLE`. The flag is visible on the public ledger, so disclose it ([Stellar clawback guide](https://developers.stellar.org/docs/build/guides/transactions/clawbacks)) |

Set the flags before the first trustline exists, and do not set `AUTH_IMMUTABLE`.

**Keep transfers inside the platform.** A holder whose trustline stays authorized can send tokens to any other authorized holder, or post an offer on the Stellar DEX, without the platform. That would open the market that section 5.4 rules out for v1. So every transaction the platform builds follows the pattern in [SEP-8 regulated assets](https://github.com/stellar/stellar-protocol/blob/master/ecosystem/sep-0008.md): the first operation authorizes the holder's trustline, the payments follow, and the last operation removes the authorization. The transaction is atomic, so the holder is never authorized outside it.

- The tokens stay in the holder's account and show in the wallet.
- USDC payouts still arrive, because they use a separate trustline.
- Clawback still works. The clawback guide removes the authorization first and then claws back.
- A public SEP-8 approval server is needed only if holders may start their own transfers, which v1 does not allow. SEP-8 itself advises against regulated assets unless required, because of the operational overhead.

Check that Pollar's wallet flow can sign a platform-built transaction that includes these issuer operations (section 3.5).

### 2.4 Accounts and keys

| Account | Holds | Signing |
|---|---|---|
| Issuer | Nothing. Owns the asset and its flags | Cold multisig for high-threshold operations. A separate operational signer limited by thresholds for day-to-day trustline authorization |
| Distributor | Unsold tokens | Platform service key with spending limits |
| Escrow | Sale proceeds in USDC, and tokens reserved for installment plans | Platform service key, reconciled daily |
| Distribution | USDC for the current rent payout | Platform service key, funded only at payout time |
| Sponsor | XLM for network fees and reserves | Platform service key |

Stellar's [operation list](https://developers.stellar.org/docs/learn/fundamentals/transactions/list-of-operations) sets the thresholds that drive the signer design. Authorizing or revoking a trustline (`SetTrustLineFlags`, `AllowTrust`) is a low-threshold operation. Clawback and changing account flags are medium. Changing signers, thresholds or the master key weight is high. Example for the issuer: three cold keys and one platform service key, each with weight 1, thresholds low 1, medium 2, high 3, and the master key weight set to 0. The service key can then authorize and deauthorize trustlines alone, which every platform-built transaction needs. Clawback needs the service key plus one cold key, or two cold keys. Changing signers needs three keys.

Each holder's trustline costs a 0.5 XLM reserve, which Pollar can sponsor.

## 3. Investor onboarding and purchase (Pollar)

### 3.1 Who does what

This table reflects the [Pollar SDK repository](https://github.com/pollar-xyz/pollar) (`@pollar/core` 0.12.1, Apache-2.0).

| Capability | Pollar provides | The platform keeps |
|---|---|---|
| Login and wallets | Email, Google, GitHub and external-wallet login. Pollar-managed wallets | The decision on whether to require managed wallets |
| Fees and trustlines | App-sponsored fees. Sponsored trustlines | The issuer operations inside each platform-built transaction (section 2.3). Funding the sponsorship budget |
| KYC | Hosted KYC flows and ramp-specific steps, including a SEGIP registry check for Bolivia | The platform's own KYC and AML file, risk scoring, the allowlist decision, UIF reporting |
| Ramps | `getRampRoutes`, `quoteRamp`, `createRampOrder`, `pollRampOrder`. Orders have several legs and public quotes last 15 minutes | Matching orders to purchases, idempotency, refunds |
| Signing | A SEP-43 signer for Soroban authorization entries | Building the purchase transaction and co-signing it |

Pollar's KYC does not replace the issuer's own KYC and AML file.

### 3.2 What the original got wrong about Pollar

- **No exchange-rate function.** `get_pollar_exchange_rate()` does not exist in the SDK. Use the BCB's published official rate as the reference and `quoteRamp()` for the executed rate.
- **No BOB-pegged token** was found in the SDK or its design notes.
- **The BOB route needs confirming.** The README lists the Bolivian BOB ramp (provider Stereum, using Circle's CCTP bridge) under its Solana section. Pollar's ramp ADR (`docs/adr/0001-ramp-api-and-lifecycle.md`) says a Stereum deposit must end as USDC delivered on Stellar, but it labels its examples as illustrations and says real routes need verified support. Confirm in writing, and check `getRampRoutes()` for your app and network, that a BOB route settling in USDC on Stellar is live.
- **Substitute assets.** A Stereum leg can pay out USDT, and a later leg converts it. Treat the order as complete only when USDC has arrived.

### 3.3 Purchase flow

1. **Sign-up.** The investor logs in through Pollar and receives a wallet.
2. **KYC.** The investor completes Pollar's flow. The platform completes its own file.
3. **Approval.** The investor's wallet creates a trustline to the property's asset (Pollar sponsors it). The trustline stays unauthorized (section 2.3). The platform records the investor as approved.
4. **Quote and reserve.** The concierge quotes tokens at the current price and reserves that inventory for the quote's validity (up to Pollar's 15-minute public quote).
5. **Payment.** The investor pays BOB by QR. The ramp order turns it into USDC in the investor's wallet. This takes several legs and minutes. Alternatively the investor pays USDC directly.
6. **Settlement.** One Stellar transaction built by the platform: authorize the investor's trustline, pay USDC from the investor to **escrow**, pay tokens from the **distributor** to the investor, and remove the authorization. The investor, the distributor and the issuer's service key sign it. Only this final step can be atomic. The ramp before it cannot.
7. **Reconcile.** Store the order id, exchange rates, ramp order id and transaction hashes. Move proceeds from escrow to their owner (the vehicle, or the party that funded the purchase) on a schedule agreed with counsel.
8. **Confirm.** Send the receipt and show the entry cost (section 1.2).

| Event | Handling |
|---|---|
| Quote expires before payment | Release the reservation and require a new quote |
| A ramp leg fails or is refunded | No tokens are released. Reconcile against Pollar's order status. The investor can retry |
| The ramp pays USDT instead of USDC | Wait for the converting leg. Check the final asset before building the settlement transaction |
| The investor's payment differs from the quote | The settlement transaction fixes both amounts, so it fails. Refund or top up under the order terms |
| The investor is not approved, or approval was withdrawn | The platform does not build the settlement transaction. Refund the USDC |
| The trustline does not exist | The token payment fails and the whole transaction fails. No partial state |
| The reservation lapsed and the tokens sold elsewhere | Refund the USDC |

### 3.4 Separate sale proceeds from rent

The original sent purchase USDC into the "Property Distribution Vault". That mixes investor capital with rent, so a distribution could pay capital back out as income. Use separate accounts: purchase proceeds go to **escrow**, and only net rent goes to the **distribution** account.

### 3.5 Questions to settle with Pollar

1. Is a BOB deposit route that settles as USDC on Stellar enabled for our app? What are the limits, fees and settlement times?
2. Can the app present a transaction it built, with operations from the investor, the distributor and the issuer, for the user's wallet to sign?
3. What is the custody model of Pollar-managed Stellar wallets, and which legal entity is the custodian?
4. Does the wallet handle an asset whose trustline is unauthorized at rest (`AUTH_REQUIRED`, `AUTH_REVOCABLE`), and could it work with a SEP-8 approval server later?
5. What order status guarantees, webhooks and idempotency does the ramp API offer?
6. What is Pollar's and Stereum's regulatory status in Bolivia (ASFI, UIF)?
7. What KYC evidence must the platform retain, and how is it shared?

## 4. Rent income and distribution waterfall (Bolivia)

### 4.1 The distribution formula

This section applies to properties that earn rent. Land and projects under construction have no rent to distribute: their costs come from a reserve (section 1.6), and holders are paid when the property is sold (section 6.3).

Distribute net distributable cash, never gross rent. Each month the accounting runs off-chain in bolivianos and produces the net pool. The platform then converts the pool to USDC and pays holders (section 6).

```math
\begin{aligned}
D_p = {} & \text{gross rent collected} \\
& - \text{IVA payable} && \text{(13\% of invoiced rent, less the IVA credit on invoiced costs)} \\
& - \text{IT} && \text{(3\% of invoiced rent, until the IUE credit covers it)} \\
& - \text{IPBI accrual} && \text{(annual tax / 12)} \\
& - \text{operating costs} && \text{(expensas on vacant units, repairs, utilities, insurance)} \\
& - \text{CapEx and vacancy reserve} \\
& - \text{asset management fee} \\
& - \text{IUE accrual} && \text{(vehicle level)} \\[1em]
\text{payout}_i = {} & \left\lfloor \frac{\text{balance}_i \times D_p}{\text{tokens outstanding}} \right\rfloor - \text{withholding}_i
\end{aligned}
```

- Withholding is per holder and comes after the split. The original deducted a "withholding reserve" from the shared pool, which makes resident holders pay part of foreign holders' tax. IUE-BE (12.5%) applies only to payments to foreign beneficiaries.
- Tenant security deposits and anticrético capital are liabilities. They never enter D_p.
- Reserves stay in the vehicle as cash. They show up in NAV until spent and are not income to holders.
- Distributable cash is not the same as distributable profit (last row of section 4.3).

### 4.2 Worked example (SC-1)

Assumptions A1 to A5. Amounts are BOB per month and tax-inclusive. "Year 1" means before any IUE has been paid. "Steady state" means the IUE paid for the previous year is large enough to cover all IT.

| Line | Year 1 | Steady state |
|---|---:|---:|
| Gross rent invoiced | 70,000 | 70,000 |
| IVA on rent (13%) | -9,100 | -9,100 |
| IVA credit on invoiced costs (13% of 7,000 + 3,500) | +1,365 | +1,365 |
| IT (3%) | -2,100 | 0 (paid with the IUE credit) |
| IPBI accrual (1/12) | -1,800 | -1,800 |
| Vacant-unit expensas and repairs | -3,500 | -3,500 |
| CapEx and vacancy reserve (5%) | -3,500 | -3,500 |
| Management fee (10%) | -7,000 | -7,000 |
| IUE accrual, vehicle level | -8,000 | -8,000 |
| **Net distributable pool** | **36,365** | **38,465** |
| In USDC at 11.67 | 3,116.11 | 3,296.06 |
| Share of invoiced rent | 51.95% | 54.95% |
| Per token per month (105,000 tokens) | 0.3463 BOB (0.0297 USD) | 0.3663 BOB (0.0314 USD) |
| Net cash yield on the $10.00 price | 3.56% | 3.77% |

A holder of 2.5% (2,625 tokens) receives 909.13 BOB a month in year 1 and 961.63 BOB in steady state, before any holder-level tax. If 12.5% is withheld because the holder is a foreign beneficiary, the amounts are 795.48 and 841.42 BOB.

**Check of the IUE input (A4).** Revenue net of IVA is 730,800 BOB a year. Deductible costs are 156,420 BOB (IT 25,200, IPBI 21,600, repairs 36,540 and management fee 73,080, both net of IVA). Depreciation is 204,225 BOB: the price is 11,670,000 BOB at 11.67, the building is 70% of it (8,169,000 BOB), and it depreciates at 2.5% a year. Taxable profit is 370,155 BOB, so IUE at 25% is 92,539 BOB a year, or 7,712 BOB a month. The 8,000 BOB used is close. The IUE paid in a year (92,539) is larger than a year of IT (25,200), which is why IT drops to zero in steady state.

**Reading the result.** Holders receive about half of invoiced rent. Taxes and the reserve account for most of the rest. The original quoted net yields of 9.2% and 6.8% with no calculation behind them. Its own waterfall paid out 50% of gross rent, so those yields would need gross yields of 18.4% and 13.6%. Quote yields only from this calculation, never from a target.

### 4.3 Tax and cost rules

| Item | Rule | Basis and confidence |
|---|---|---|
| Taxes on rent | IVA 13% (form 200), IT 3% (form 400), IUE 25% on the vehicle's profit (form 500). An individual owner usually files RC-IVA 13% (form 610) | [YoFacturo guide](https://yo-facturo.com/blog/impuestos-alquiler-inmuebles-bolivia/). Secondary source |
| IVA credit | Every cost needs a `factura` issued to the vehicle's NIT. The 13% on those invoices offsets the 13% IVA on rent. The original recommended this on pages 6 and 8 to 9 but assumed zero credit in its example on page 5 | SIN rules. Confirm with the accountant |
| Suppliers without a `factura` | If an individual not registered with the SIN sells goods or services occasionally, the vehicle must withhold and pay: services 13% RC-IVA (form 604) plus 3% IT (form 410), 16% in all. Goods 5% IUE (form 570) plus 3% IT (form 410), 8% in all. Forms are filed monthly through SIAT, by the last digit of the NIT, between the 13th and 22nd | [SIN retentions FAQ](https://www.impuestos.gob.bo/wp-content/uploads/2025/10/PREGUNTAS-RETENCIONES-25.pdf). Official |
| Depreciation | Buildings (`edificaciones`) 2.5% a year (40 years) on construction value, land excluded. It lowers taxable profit, not cash | [DS 24051 annex](https://bolivia.infoleyes.com/articulo/7033). Secondary reproduction |
| IUE credit against IT | Ley 843 art. 77: IUE paid for the year counts as payment on account of IT in each monthly period, from the month after it is paid. For a December year-end the annual IUE falls due about 120 days after closing, so the credit starts in the second year. DS 5327 (Feb 2025) required the IUE to be paid by its due date to use the credit. DS 5563 (Mar 2026) removed that condition. IUE paid cannot offset IT owed on property transfers (DS 21532 art. 7). More depreciation means less IUE and a smaller credit | [boliviaimpuestos.com](https://boliviaimpuestos.com/decreto-supremo-nuevo-marco-normativo-para-la-compensacion-iue-it/). Secondary source for DS 5563. **Counsel** to confirm the current rule |
| IPBI | Annual municipal property tax, paid to the municipal government where the property is registered, often through the RUAT platform. Properties in Urubó pay Porongo, not Santa Cruz de la Sierra (section 4.4). Provision 1/12 a month. Early-payment discounts vary by municipality and year: Cochabamba (Cercado) allowed 15% until 30 June 2026 and 10% until 28 December 2026 for tax year 2025. A Santa Cruz municipal notice (undated) advertised 15% until 1 April | [Los Tiempos](https://www.lostiempos.com/actualidad/cochabamba/20260701/amplian-perdonazo-impuestos-municipales-cercado). Check each municipality every year |
| Transfer tax | See section 1.2. 3% of the higher of the price and the IPBI value. Owed by the seller. Due within 10 business days. Notaries and the registry will not process the deed without proof of payment. IUE is not deductible against it | [DS 24054](https://www.lexivox.org/norms/BO-DS-24054.xhtml), arts. 2, 5, 6, 8, 12. Primary text |
| IUE-BE (foreign beneficiaries) | 12.5% withheld on payments abroad, form 530. The SIN FAQ confirms the rate for services performed abroad for a Bolivian company. How it applies to payouts to foreign token holders depends on the legal form of the token | Planning assumption of 12.5%. **Counsel** |
| Legal reserve | S.A. and S.R.L.: at least 5% of effective, liquid profit until the reserve equals half of paid-in capital | [Código de Comercio art. 169](https://bolivia.infoleyes.com/articulo/26270). Secondary reproduction |
| Cash versus profit | Depreciation lowers profit, not cash. If the token is equity, payouts are generally limited to effective and liquid profit after the legal reserve. The original treated depreciation-sheltered cash as freely distributable. Monthly cash payouts fit a contractual right, or advances reconciled at year end | **Counsel** |

### 4.4 City factors

**Santa Cruz (Equipetrol, Sirari, Urubó).** Heat, humidity and heavy rain raise the cost of air conditioning, waterproofing, mold prevention, elevators and pools. Short-term and corporate demand is strong, with more turnover and cleaning cost. CRE (electricity) and SAGUAPAC (water) are cooperatives that serve the city ([CRE](https://correodelsur.com/sociedad/20250806/cre-un-orgullo-para-los-bolivianos.html), [SAGUAPAC](https://www.bnamericas.com/es/perfil-empresa/cooperativa-de-servicios-publicos-santa-cruz-ltda)). Confirm the providers for each building. The original listed "AXS" for common-area power, and no source places AXS in Santa Cruz or Cochabamba. Urubó lies across the Piraí river in the municipality of Porongo ([eju.tv](https://eju.tv/2024/10/la-hora-de-porongo-y-la-planificacion-urbana/)), so its property and transfer taxes go to Porongo, not to Santa Cruz de la Sierra.

**Cochabamba (Zona Norte, Cala Cala, Recoleta, Tupuraya).** A milder climate lowers HVAC wear. Dry-season water costs include cistern deliveries, pump maintenance and filtration. Long-term residential and professional leases dominate, with lower turnover. The original names ELFEC (electricity) and SEMAPA (water) as the utilities.

**Both cities.** Vacant units still cost expensas, and the pool pays them. Keep anticrético contracts out of a yield pool: they produce no monthly cash and create a large liability when the tenant leaves. Carry that lump sum, and every tenant deposit, as a liability in NAV.

**Rent sanity check.** Numbeo reports average monthly rent for a three-bedroom city-centre apartment of Bs 10,700 in Santa Cruz and Bs 3,675 in Cochabamba. SC-1's 7,000 BOB per apartment is plausible for Santa Cruz. It would be high for Cochabamba.

### 4.5 Currency and exchange rate

- **The regime changed.** The boliviano has floated since 29 June 2026. The BCB moved from a fixed Bs 6.96 to an official rate of Bs 9.73 that day ([Infobae](https://www.infobae.com/america/america-latina/2026/06/29/bolivia-flexibiliza-el-dolar-y-cierra-una-era-de-15-anos-de-tipo-de-cambio-fijo/)). It set Bs 12.26 for 24 September, announced the evening before ([Red Uno](https://www.reduno.com.bo/economia/nueva-escalada-oficial-el-bcb-eleva-el-tipo-de-cambio-del-dolar-para-este-jueves-2026923202122)), and [Bs 11.67 for 14 November](https://www.bcb.gob.bo/librerias/indicadores/otras/ultimo.php). The parallel market quoted Bs 11.73 to 11.93 on 7 November ([Red Uno](https://www.reduno.com.bo/economia/a-cuanto-esta-el-dolar-en-bolivia-este-sabado-7-de-noviembre-asi-cotizan-el-cambio-oficial-y-paralelo-202611773627)). A rate tracker put the gap between the two at 1.1% on 10 November ([dolarbolivia.net](https://dolarbolivia.net/)).
- **What it means.** The original's concern about a gap between official and parallel rates is no longer the main risk. Volatility is. A fixed rent in bolivianos bought 17% fewer dollars on 14 November than on 29 June, and 21% fewer on 24 September.
- **Policy.**
  1. Record the BCB official rate and the executed conversion rate for every distribution.
  2. Convert the net pool on the distribution date and say so in the terms.
  3. Pay USDC on-chain. Offer BOB by bank transfer or QR to holders who prefer it, at the recorded rate.
  4. Consider USD-denominated or UFV-indexed leases where the law allows (**Counsel**).
  5. Purchases paid in monthly BOB installments convert at each payment date's rate (section 7.4).

### 4.6 Monthly close and transparency report

1. Reconcile rent receipts against the invoices issued through SIAT.
2. Close the month: IVA credit, taxes, retentions filed, IPBI accrual, reserves.
3. Compute D_p in BOB. The accountant signs the report.
4. Publish the report (PDF and JSON) and compute its SHA-256.
5. Convert to USDC and record both rates.
6. Distribute (section 6) with the report hash in every payout transaction.

Example JSON (illustrative values):

```json
{
  "building_id": "SC-1",
  "period": "2026-11",
  "fx": { "bcb_official_bob_per_usd": "11.67", "executed_bob_per_usd": "11.67" },
  "gross_rent_bob": "70000.00",
  "lines_bob": {
    "iva_payable": "-7735.00",
    "it": "-2100.00",
    "ipbi_accrual": "-1800.00",
    "operating_costs": "-3500.00",
    "capex_vacancy_reserve": "-3500.00",
    "management_fee": "-7000.00",
    "iue_accrual": "-8000.00"
  },
  "net_pool_bob": "36365.00",
  "net_pool_usdc": "3116.11",
  "tokens_outstanding": "105000.0000000",
  "per_token_usdc": "0.0296772",
  "record_ledger": "<ledger sequence of the snapshot>",
  "report_sha256": "<sha-256 of the signed report PDF>"
}
```

## 5. Legal structure and regulatory perimeter

This section is the largest gap in the original. The statements below come from the legal texts that were read ([Ley 1834](https://www.lexivox.org/norms/BO-L-1834.xhtml), [DS 5384](https://www.lexivox.org/norms/BO-DS-N5384.xhtml), [DS 24054](https://www.lexivox.org/norms/BO-DS-24054.xhtml)) and from the secondary sources named in each row. Lexivox reproduces the texts for convenience and is not the official gazette. Its Ley 1834 page shows the 1998 text, which names the Superintendencia de Valores. ASFI now holds that role (Ley 393 art. 16, as cited in DS 5384), and later amendments may not appear. Confirm everything with counsel.

### 5.1 The perimeter

| Norm | What it says | What it means here |
|---|---|---|
| **Constitution** (CPE), art. 331 | Financial intermediation, financial services and any other activity related to the management, use and investment of savings are of public interest and need prior State authorization (quoted in the recitals of DS 5384) | Even if the token were not a security, collecting the public's money to invest in a building needs authorization. **Counsel** |
| **Constitution**, arts. 262, 315, 394, 395, 397, 400, 401 (land) | Rules on rural property, land owned by legal entities and the border zone (section 1.6) | Rural land needs counsel before any offering. Urban lots come first. **Counsel** |
| **Ley 1834**, Securities Market Law, arts. 2, 3, 6, 10 | A public offer is any invitation or proposal to the public or to specific sectors, by any medium, to carry out a legal transaction in securities (art. 6). The regulator may declare a doubtful offer subject to the law (art. 6). Securities include instruments created under specific regulation that identify the beneficiary of the funds, have an authorized public offer, and represent an effective obligation of the issuer. Securities are freely transferable, and any limit on their circulation is void (art. 2). The only authorized intermediaries are `agencias de bolsa` (art. 3). A joint-stock company with 25 or more shareholders must register its shares in the RMV (art. 10) | A token that pays a share of rent and sale proceeds to retail buyers through an app is very likely an offer of securities, or is treated like one. It needs authorization and registration unless an exemption applies. An allowlist token sits uneasily with the rule against limits on circulation. **Counsel** |
| **DS 5384** (7 May 2025) | Defines a "tokenized asset" as a digital representation of a real or financial asset on a blockchain through tokens. A virtual asset service provider (PSAV) includes exchange, transfer, custody, and participation in or provision of financial services related to an issuer's offer or the sale of a virtual asset (art. 3). Financial technology companies (ETF) must be constituted as financial entities with an ASFI constitution permit and operating license (art. 4). Their categories include tokenized assets and PSAV, and ASFI may require entry into a controlled test environment (art. 5). Existing companies must adapt (second transitory provision) | Selling and holding tokenized assets for customers appears to fall under the ETF regime. Plan for an ASFI license and ask whether the controlled test environment is available |
| ASFI implementing rules | Not reviewed. A Dentons note of 22 May 2026 reports amendments to the fintech regulation clarifying virtual-asset services (summary only, the page could not be retrieved) | Counsel should read ASFI's current compilation of financial-service rules (RNSF) and the 2026 amendment |
| **Ley 393**, Financial Services Law, art. 486 | Prohibits financial intermediation and complementary financial services without authorization ([ASFI](https://asfi.gob.bo/pb/y-donde-denunciar-actividad-financiera-ilegal)) | Money collected from the public in installments or reservations, before tokens are delivered, is the exposed part. **Counsel** |
| UIF | A tokenization vendor reports that the UIF requires registration from providers offering exchange, transfer or custody of virtual assets ([Metlabs](https://metlabs.io/en/tokenization/bolivia/)). Secondary source | Plan an AML program and UIF registration. **Counsel** |
| Código de Comercio | An S.R.L. admits at most 25 partners and its quotas cannot be negotiable instruments (Metlabs). Legal reserve in section 4.3 | The S.R.L. route cannot carry a retail token directly |

### 5.2 Wrapper options

| Option | What the token is | Regulation | Fit |
|---|---|---|---|
| Securitization (`titularización`) | A mirror of a registered security issued by a `patrimonio autónomo` managed by a licensed securitization company | Ley 1834 covers securitization companies and securitization (art. 1). Metlabs cites arts. 76 to 79 | Strongest ring-fence, slowest and most expensive |
| Joint-stock company (S.A.) | A mirror of the share register | 25 or more shareholders must register in the RMV (art. 10). A public offer needs authorization | Familiar corporate form |
| Closed-end investment fund through a licensed fund manager (`FIC`, `SAFI`) | A mirror of fund participation units | A fund is an autonomous estate separate from its manager, set up under Ley 1834 ([BNB SAFI](https://www.bnb.com.bo/PortalSAFI/Principal/PreguntasFrecuentes)), in a market ASFI supervises ([Fortaleza SAFI](https://www.fortalezasafi.com/productos/)). A closed fund does not redeem units except under its rules ([Banco Unión SAFI](https://bancounion.com.bo/GrupoUnion/SAFIProductos)) | An existing regulated wrapper with custody and reporting. Whether a real-estate fund fits tokenized units is a question for counsel. The original did not consider it |
| S.R.L. with participation contracts (`cuentas en participación`) | A private contractual yield right | S.R.L. limits above. A contractual right offered to the public is still a public offer | A small private group only, not a retail product |
| Trust (`fideicomiso`) with a bank as trustee | Beneficiary rights over the estate | Needs a regulated financial entity as trustee (from the original, not verified) | Ring-fenced but costly. Same public-offer question |

**Recommendation.** Do not fix the wrapper in the engineering design. Build the system so that a token points to a register entry and every distribution has per-holder payout lines. Then the wrapper can change without redesign.

### 5.3 What a holder owns

Under each wrapper the holder has a contractual or securities right against the issuer or the estate. The vehicle holds title to the property, not the investor. Marketing, the app and the concierge must describe what the holder receives (a rent share, a share of sale proceeds, votes, information rights) exactly as the wrapper defines it. Do not write "you own 2% of the building".

### 5.4 Secondary trading and liquidity

- **Removed from the original:** treasury "liquidity walls" around NAV on the Stellar DEX. An issuer that supports its own price risks being treated as manipulating the market or operating an unlicensed market (**Counsel**).
- If the tokens are securities, intermediation belongs to `agencias de bolsa` (Ley 1834 art. 3). Because art. 2 voids limits on circulation, ask counsel how to restrict who can hold tokens. Restricting at the intermediary and custody layer may be lawful where a token-level restriction is not.
- **v1 policy:** no open market. Holders stay unauthorized between platform-built transactions (section 2.3), so tokens move only through platform flows: purchases, redemption windows if allowed, and forced transfers. If redemptions are allowed, run periodic windows at the latest NAV, funded from a reserve and capped per window. Property funds elsewhere that promised easy exits have suspended redemptions under stress, so build the cap in from day one.

### 5.5 Questions for counsel

1. Is the token a security (`Valor`) under Ley 1834, and which route fits a single building: securitization, S.A. with RMV registration, or a closed-end fund?
2. Does the platform need ETF status under DS 5384, in which category, and can it start in ASFI's controlled test environment?
3. What are the PSAV, UIF and AML obligations, and when do they start?
4. How can a transfer restriction coexist with Ley 1834 art. 2?
5. May holders receive USDC, or must payouts go through banks in bolivianos? What do BCB and ASFI rules say about virtual assets in this flow? A licensing consultancy, a secondary source, reports that BCB Resolution 082/2024 allows operations with virtual assets through authorized channels ([PriFinance](https://prifinance.com/es/licencia-de-intercambio-de-criptomonedas/bolivia/)).
6. What is the tax treatment of payouts for each holder type (resident individual, resident company, foreign), and what is withheld?
7. How are the vehicle's IVA, IT and IUE treated under the chosen wrapper, and does a `patrimonio autónomo` or fund have its own NIT?
8. Who may intermediate secondary transfers, and may the issuer run redemption windows?
9. Is it lawful to collect installments before tokens are delivered? What escrow is required? Is forfeiting the pre-sale discount valid under consumer-protection law?
10. Who is the legal custodian of tokens held in Pollar-managed wallets?
11. What counts as investment advice for the AI concierge, and what disclosures are mandatory?
12. Can a vehicle hold urban lots or rural land only to sell them later, given Constitution arts. 315 and 395? Which wrapper suits land, and how do the FES, reversion and border-zone rules apply?
13. Can one vehicle or fund hold several properties and issue new tokens when it adds one, and what approval does each new issue need?

## 6. Distribution mechanics

### 6.1 Model A: record-date snapshot and USDC payments

This is ordinary record-date practice with no custom contract.

1. **Close and approve the month** (section 4.6) and hash the report.
2. **Fix the record ledger and snapshot balances.** Every transfer goes through a platform-built transaction (section 2.3), so the platform already knows every holder address. Read the trustline balances in batches with Stellar RPC `getLedgerEntries` (up to 200 keys per call, see the [Horizon to RPC guide](https://developers.stellar.org/docs/data/apis/migrate-from-horizon-to-rpc)) or from your own indexer, and record the ledger number the response returns. Horizon is [nearing end-of-life](https://developers.stellar.org/docs/data/apis) and RPC returns current state rather than history, so keep every snapshot.
3. **Check the snapshot.** Holder balances plus the distributor, escrow and other platform accounts must equal tokens issued minus tokens burned. If they do not (a transfer landed between batches), take the snapshot again.
4. **Compute payouts with integers:** `floor(balance x pool / tokens outstanding)` in base units. Tokens in the distributor and escrow accounts earn rent for their owner, because they are unsold or unreleased fractions of the building. Section 8.2 lists this as a decision. Subtract holder-level withholding. Carry rounding remainders into the next period.
5. **Fund and pay.** Fund the distribution account with exactly the total in USDC. Pay in transactions of up to 100 operations, each carrying the report hash as `MEMO_HASH`. For 300 holders that is 3 transactions, for 1,500 it is 15, and for 5,000 it is 50.
6. **Handle failures.** A Stellar transaction fails as a whole if one operation fails. Check each destination's USDC trustline before batching, and split a failed batch. Keep an unpaid holder's amount as a liability and pay it with the next period or on request.
7. **Publish** the report and a payout file with holder id, gross, withholding, net and transaction hash.

The holder on the record ledger receives the whole period's payout. State that rule in the terms. Time-weighted accrual is possible later, because every transfer is on the ledger, but it adds complexity. Holders pay no network fees. Anyone can recompute the payouts from the published report and public ledger data.

Later options, not needed for v1: publish a Merkle root of the payout list so holders can claim with a proof, or use claimable balances for holders without a USDC trustline.

### 6.2 On-chain accrual (Model C, Appendix B)

Choose it when transfers are frequent and small, you want accrual to settle on-chain at every transfer, and you accept an audit and a token that cannot trade on the Stellar DEX.

```math
\begin{aligned}
& \text{on deposit:} && \text{index} \mathrel{+}= \frac{\text{amount} \times 10^{14} + \text{carry}}{\text{supply}} \\[0.5em]
& \text{before any balance change of account } u\text{:} && \text{earned}_u = \frac{\text{balance}_u \times (\text{index} - \text{last\_index}_u)}{10^{14}} \\
& && \text{unclaimed}_u \mathrel{+}= \text{earned}_u; \quad \text{last\_index}_u = \text{index} \\[0.5em]
& \text{on claim:} && \text{pay } \text{unclaimed}_u
\end{aligned}
```

A new holder starts at the current index, so they cannot claim past rent. A seller keeps the rent accrued up to the sale. The checkpoint must run before every balance change: transfers, forced transfers and burns.

What the original contract got wrong, and what Appendix B does about it:

| Original (pages 9 to 15) | Problem | Appendix B |
|---|---|---|
| `transfer_fraction` read both balances, then wrote both | A self-transfer overwrites the debit with the credit, so the balance grows. On the Soroban test host (SDK 29.0.1), 1,000 tokens became 32,000 after five self-transfers. A holder with a fair 35 USDC share claimed 1,120 USDC from a 3,500 USDC deposit | Writes the sender before reading the recipient, so a self-transfer nets to zero |
| `initialize` without authorization | Anyone can initialize first after deployment | `__constructor`, which runs inside the deploy transaction |
| Nothing wrote the KYC flag except the admin entry | No investor could ever receive tokens | `set_allowed`, with an event |
| Balances in a private map | Not a SEP-41 token, so wallets cannot show it | The full SEP-41 interface |
| `deposit_net_rent` repeatable, remainder lost | Double deposits and lost dust | One deposit per period id with a stored report hash. The remainder carries forward |
| `update_nav(new_nav_per_token)` took one opaque number, and nothing read it | No audit trail and no limit on a bad value | `update_nav(appraisal, reserves, liabilities, report_sha256)` computes NAV and caps the change per update |
| `ipfs_hash: BytesN<32>` | An IPFS content id is longer than 32 bytes | Stores the SHA-256 of the report. Put the content id in the event or the report |
| No events, no TTL extension | Entries can be archived and nothing is auditable | Events, and TTL bumps on every touched entry |
| No forced transfer | No path for court orders or lost keys | `forced_transfer` with a reason hash |
| Release builds wrap on overflow unless configured | Silent overflow | `overflow-checks = true` in the release profile |

### 6.3 Sale proceeds and closing a token

When a property is sold, as land usually is at the end of its plan, holders receive the sale proceeds once, as a return of capital separate from rent.

1. **Approve the sale** as the offering documents require: a holder vote, or a manager decision within stated limits.
2. **Settle the vehicle.** Pay the selling costs, the transfer tax, IUE on the gain and any remaining liabilities. Keep a reserve for the final tax filings and closing costs, and pay out what is left of it at the end.
3. **Pay out** with the same record-date snapshot used for rent (section 6.1). Label the payment as sale proceeds, because its tax treatment may differ from rent (**Counsel**).
4. **Close the token.** In Model A, the issuer claws back the remaining balances after the final payment, as the offering terms allow, and a clawback burns them ([operation list](https://developers.stellar.org/docs/learn/fundamentals/transactions/list-of-operations)). In Model C, the admin moves the balances to the treasury with `forced_transfer` and the treasury burns them.
5. **Publish** the closing report: sale price, costs, taxes, net amount per token and transaction hashes.

In a portfolio, the sale of one property either pays its net proceeds to all holders of the portfolio token or, if the offering allows it, funds another property. The portfolio token stays open.

## 7. AI Investment Concierge

### 7.1 Purpose and limits

The concierge turns upfront cash, a monthly budget and a time horizon into two or three purchase options from live inventory. If the offering is regulated, its recommendations may count as investment advice or intermediation and must follow the licensed entity's rules (**Counsel**). Present options as information with risks, never as promises.

### 7.2 Architecture

The language model handles conversation, intent and explanation. Deterministic tools compute every number. This part of the original stands.

| Tool | Returns |
|---|---|
| `get_inventory()` | Properties with their type (completed building, project under construction, land or portfolio), price, available tokens, NAV date, trailing gross and net yield from published reports (a modeled yield, labeled as such, for a property with no history), for land the sale scenarios and break-even price (section 1.6), and risk notes |
| `get_fx()` | The BCB official rate and its date. At checkout, the ramp's executed quote from `quoteRamp()` |
| `build_options(profile, inventory, fx)` | Two or three options, each with a `calcId`, its inputs, outputs and assumptions |
| `reserve_tokens(optionId, ttl)` | A reservation that expires with the ramp quote |
| `create_order(optionId)` | An order that starts the purchase flow (section 3.3) |

- Every number in a reply must appear in a tool payload. A post-check compares the draft's numbers to the payloads and regenerates on any mismatch.
- Three or four buildings do not need an optimization solver. Use rule-based templates (income now, balanced, pre-sale). They are easy to explain and audit.
- The UI renders the structured payload as cards. The purchase flow receives the `calcId`, never the model's text.

### 7.3 Inputs and suitability

Collect upfront cash (BOB or USDC), monthly budget, number of months, risk tolerance and horizon, residency (it affects withholding), and KYC status. Cap concentration: by default no option puts the whole budget in one property. Offer land only when the customer's horizon covers the planned holding period. Log the inputs, the outputs and the disclosures shown.

### 7.4 Worked example

Customer: "I have 14,000 BOB now and can pay 2,000 BOB a month for 12 months."

At 11.67 BOB per USD (A5), 14,000 BOB is $1,199.66 and 2,000 BOB is $171.38, so the 12-month capital is **$3,256.21**. The original showed $3,800 at 10 BOB per USD. Monthly installments convert at each payment date's rate. If the boliviano weakens to 13.0 right after the first purchase, the 12 installments buy $1,846 instead of $2,057, which is 10.2% less.

Inventory (A6): SC-1 at $10.00 (net cash yield 3.56% in year 1), CB-1 at $25.00 (2.34%), SC-2 pre-sale at $8.00. The CB-1 yield is 4.5% gross times SC-1's 51.95% net-to-gross ratio.

| Option | Mix | Day 1 | After 12 months | Notes |
|---|---|---|---|---|
| A. Income now | 100% SC-1 | 119.97 tokens | 333.35 tokens (0.3175% of the building). Rent received $77.33. Run rate about $118.72 a year | Each month the installment and the rent received buy more tokens |
| B. Two cities | 60% SC-1, 40% CB-1 | 71.98 SC-1 and 19.19 CB-1 tokens | 200.01 SC-1 and 52.91 CB-1 tokens, worth $3,322.82. Rent received $66.61. Blended net cash yield 3.07% | Spreads city risk and lowers income |
| C. Pre-sale | 100% SC-2 at $8.00 | 149.96 tokens, then 21.42 a month | 407.03 tokens for $3,256.21. No rent during construction | Value at delivery: $3,842.33 if +18% (gain $586.12), $3,256.21 if flat, $2,930.59 if -10% (loss $325.62) |

Option C's upfront share is 36.84% of the capital. If the customer stops after four installments they have paid $1,885.18, which is 235.65 tokens at $8.00, or 188.52 tokens if the contract reprices them to $10.00 and forfeits the discount. The forfeiture is a contract term for counsel (section 7.5).

These figures come from `build_options` at the stated assumptions with rent reinvested and the exchange rate held constant. They are projections, not forecasts.

A land token such as LT-1 pays nothing until the sale. For land, the assistant shows the sale scenarios and the break-even price from section 1.6 instead of a yield, and offers it only when the customer's horizon covers the planned holding period.

### 7.5 Installment plans (pay as you vest)

- Reserved tokens stay in the escrow account. After each installment settles, the platform releases that tranche with a platform-built payment that authorizes the investor's trustline for that transaction only (section 2.3), so Model A needs no extra contract.
- The blockchain cannot see a BOB QR payment, so the platform decides when a payment has cleared. The original said the contract "releases the next 25 tokens the moment the payment clears". Disclose this trust assumption.
- Rent follows the legal holder. Reserved but unreleased tokens belong to escrow, so they earn nothing for the buyer.
- If the buyer stops paying, the buyer keeps the released tokens. Whether the buyer also forfeits the pre-sale discount must pass consumer-protection review (**Counsel**).
- A pre-sale carries completion, permit and developer-insolvency risk. Hold pre-sale proceeds in escrow and release them against construction milestones (**Counsel**). Show a flat and a negative scenario next to the base case.

### 7.6 Customer simulation

Pair each option with a slider that calls the same deterministic function with a different yield, exchange rate or horizon.

```math
\begin{aligned}
\text{tokens}(t+1) &= \text{tokens}(t) + \frac{\text{installment\_usdc}(t) + \text{rent}(t)}{\text{price}} \\[0.5em]
\text{rent}(t) &= \frac{\text{tokens}(t) \times \text{price} \times \text{net\_yield}}{12}
\end{aligned}
```

Always display the assumptions beside the chart.

### 7.7 Payload and guardrails

```ts
interface InvestmentOption {
  calcId: string;                  // ties every figure to one tool run
  strategy: "income" | "balanced" | "presale";
  lines: { buildingId: string; tokens: string; priceUsdc: string; sharePct: string }[];
  upfrontUsdc: string;
  monthlyUsdc: string;
  months: number;
  fx: { bcbOfficial: string; asOf: string };
  projection: {
    tokensAtEnd: string;
    rentReceivedUsdc: string;
    runRateUsdcYear: string;
    assumptions: string[];
  };
  scenarios?: { label: string; valueUsdc: string; resultUsdc: string }[];
  disclosures: string[];           // risks, fees, entry cost, "projection, not a promise"
  expiresAt: string;               // inventory reservation expiry
}
```

- **Inventory lock.** Reserve before quoting. Never offer more than is available.
- **Currency.** Show BOB and USD side by side and state which rate applies to later installments.
- **Wording.** No "guaranteed" or "instant gain". Label every projection.
- **Traceability.** Every number in the reply carries a `calcId`.

## 8. Risks and open decisions

### 8.1 Risks

| Risk | Where it shows up | Mitigation |
|---|---|---|
| The offering is regulated and the platform is unlicensed | Sections 5.1 and 5.5 | Counsel first. Ask about ASFI's controlled test environment. Keep the design wrapper-neutral |
| Issuer key compromise or loss | Sections 2.3 and 2.4 | Cold multisig, an operational key limited to low-threshold operations, an alert on any trustline authorization the platform did not build, a recovery procedure, a clawback policy |
| Stale or biased appraisal | Section 1.3 | Independent appraiser, bounded NAV change, report hash, dates next to every price |
| Exchange-rate swings | Section 4.5 | Recorded rates, USDC payouts, disclosure |
| Redemption run | Section 5.4 | Caps, windows, a funded reserve, no promise of redemption at NAV |
| Ramp or provider failure | Section 3.3 | Idempotent orders, reconciliation, escrow before token release, written provider terms |
| Custom contract defect | Appendix B | Model A needs no custom contract. Audit before any production use of Appendix B |
| Tax treatment of payouts | Section 4.3 | A tax opinion per holder type before the first payout |
| Unsuitable or misleading recommendations | Section 7 | Suitability inputs, regulated wording, number checks, logs |
| Concentration in one building or city | Section 7.4 | Default caps and diversified options |
| Land: title defects, overlaps or invasions | Section 1.6 | Title and cadastral checks before buying, fencing and watch |
| Land: holding costs outlast the reserve, or the sale comes late | Section 1.6 | A reserve with a margin, a sale window, the break-even price disclosed |
| Rural land rules (FES, reversion, speculation, border zone) | Sections 1.6 and 5.1 | Counsel before any rural offering. Start with urban lots |
| Portfolio: dilution when a property is added | Section 1.7 | New tokens at NAV plus acquisition costs, after a fresh appraisal |

### 8.2 Decisions to settle before design

1. The legal wrapper, and whether the first offering is public (counsel). Everything else depends on it.
2. The token model: Model A (recommended) or SEP-57.
3. What a token entitles its holder to: rent only, rent and sale proceeds, votes, information rights.
4. Distribution currency and exchange-rate policy: rate source, timing, who bears conversion costs.
5. How tokens in the distributor and escrow accounts are treated in distributions and in NAV. This edition assumes they belong to the party that funded the purchase, so they count in NAV and earn rent.
6. Secondary liquidity: none, redemption windows, or a licensed venue.
7. Appraisal policy: method, frequency, appraiser independence, maximum NAV change per update.
8. Pollar's role and regulatory status, and the BOB-to-Stellar route, confirmed in writing.
9. Fees: the management fee, any platform fee, who receives them and how they are disclosed. The examples include only the management fee (10% of rent for SC-1, 1% of the appraisal a year for LT-1).
10. Which property types to launch with (completed buildings, projects under construction, urban lots, rural land) and in what order.
11. Whether to offer portfolio tokens, and the rule for adding properties.

## Appendix A. Formula reference

```math
\begin{aligned}
& \text{Primary price} && P_0 = \frac{V + C_{\text{close}} + R_0 - D}{N} \\[0.4em]
& \text{Day-1 NAV} && NAV_0 = \frac{V + R_0 - D - \text{other liabilities}}{N} \\[0.4em]
& \text{Entry cost} && P_0 - NAV_0 \\
& \text{Ongoing NAV} && \text{section 1.3, divided by tokens outstanding} \\
& \text{Income approach} && \text{value} = \frac{\text{NOI}}{\text{cap rate}} \\[0.4em]
& \text{Net pool} && D_p = \text{section 4.1} \\
& \text{Payout} && \text{payout}_i = \left\lfloor \frac{\text{balance}_i \times D_p}{N_{\text{out}}} \right\rfloor - \text{withholding}_i \\[0.4em]
& \text{Net cash yield} && \frac{\text{payout per token} \times 12 \text{ (in USD)}}{\text{price}} \\[0.4em]
& \text{Projection} && \text{tokens}(t+1) = \text{tokens}(t) + \frac{\text{installment\_usdc} + \text{rent}(t)}{\text{price}} \\
& && \text{rent}(t) = \frac{\text{tokens}(t) \times \text{price} \times \text{net\_yield}}{12} \\[0.4em]
& \text{Installment FX} && \text{usdc}_t = \frac{\text{installment\_bob}}{fx_t} \\[0.4em]
& \text{Rent index} && \text{index} \mathrel{+}= \frac{\text{amount} \times 10^{14} + \text{carry}}{\text{supply}} \\
& && \text{earned}_u = \frac{\text{balance}_u \times (\text{index} - \text{last}_u)}{10^{14}} \\[0.4em]
& \text{Land, residual} && \text{land} = \text{finished value} - \text{construction} - \text{fees and taxes} - \text{financing} - \text{developer profit} \\
& \text{Land break-even} && S_{\text{be}} = \frac{V + C_{\text{close}} + R_{\text{hold}}}{1 - s} \quad \text{(no IUE is due at break-even)} \\[0.4em]
& \text{Portfolio issue} && N_{\text{new}} = \frac{V_{\text{new}}}{NAV_t}; \quad P_{\text{new}} = NAV_t \times \frac{V_{\text{new}} + C_{\text{new}}}{V_{\text{new}}}
\end{aligned}
```

Symbols: `V` price, `C_close` closing and structuring costs, `R0` initial cash reserve, `D` debt, `N` tokens issued, `N_out` tokens outstanding, `R_hold` holding-cost reserve, `s` selling-cost rate, `V_new` and `C_new` price and acquisition costs of an added property.

## Appendix B. Reference contract for Model C (tested, not audited)

A SEP-41 permissioned building token with an on-chain rent index, as described in section 6.2.

**Status**

- Built and tested on 2026-11-14 with Rust 1.100.0, `soroban-sdk` and `soroban-token-sdk` 29.0.1, and `stellar-cli` 30.0.1.
- 12 tests pass on the Soroban test host. `cargo clippy --all-targets -- -D warnings` is clean. `stellar contract build` produces an 18,128-byte WASM.
- Not deployed to any network, not audited, and not measured against Soroban resource budgets. Treat it as a reference to read and extend, and get an audit before any production use.
- The original contract was tested verbatim on the same host. Four tests reproduce its defects: a self-transfer of the full balance doubles it, five self-transfers turn 1,000 tokens into 32,000, the inflated holder claims 1,120 USDC against a fair 35 USDC, and no investor can receive tokens because nothing sets the KYC flag.

**Design choices**

- The constructor mints the whole supply to the treasury, which is the first holder. Unsold tokens in the treasury earn rent, consistent with section 6.1.
- `update_nav` divides by the total supply. That is right when the party that funded the purchase holds the unsold tokens. If the vehicle keeps them itself, divide by the tokens outside the vehicle instead (section 1.3).
- Both parties to a transfer must be on the allowlist. Revoking a holder freezes their transfers and claims. `forced_transfer` still moves their tokens for court orders or lost keys.
- As written, allowlisted holders can transfer to each other without the platform. To keep transfers inside the platform, as section 2.3 does for Model A, also require the admin's authorization in `transfer` and `transfer_from`.
- `forced_transfer` moves tokens, not rent already accrued. That rent stays claimable by the old address, so it is stuck if the old key is lost. Add a forced claim if your procedures need one.
- The rent index carries the division remainder forward. Each period can be deposited once and stores its signed report hash.
- NAV is computed from its inputs and the change per update is capped. The cap changes only through a separate admin call.
- The contract stores NAV for reporting. It does not sell tokens at NAV. Primary sales happen outside the contract (section 3.3).
- There is a single admin address. Use a multisig account. There is no upgrade function, so a defect needs a migration. Add a timelocked upgrade if the audit calls for one.

**What the tests show**

| Test | What it proves |
|---|---|
| `exposes_the_sep41_surface` | Name, symbol, decimals, balance, approve, allowance and `transfer_from` work through the standard token client |
| `self_transfer_does_not_mint` | Five full-balance self-transfers leave 1,000 tokens at 1,000 and the supply unchanged |
| `only_allowed_accounts_can_receive_and_a_revoked_sender_is_frozen` | A stranger cannot receive. A revoked holder cannot send. Restoring the flag restores transfers |
| `pro_rata_share_matches_the_worked_example` | A holder of 2,500 of 100,000 tokens earns 87.5 USDC from a 3,500 USDC deposit and cannot claim twice |
| `rent_stays_with_the_holder_across_a_mid_period_sale` | The seller keeps rent earned before the sale. The buyer earns only from the next deposit |
| `a_period_can_be_deposited_once` | A repeated period is rejected and moves no USDC. The report hash is stored |
| `division_remainder_is_carried_forward` | Three 1-stroop deposits over a 3-token supply pay out all 3 stroops |
| `nav_is_computed_from_its_inputs_and_bounded` | NAV for the running example is 9.6190476 USDC. A +5% update passes, +50% is rejected, negative equity is rejected, and raising the cap allows the larger move |
| `forced_transfer_settles_rent_on_both_sides` | The old holder keeps rent earned before the order. The new holder starts at the current index |
| `burn_shrinks_supply_and_keeps_accrued_rent` | Burning reduces supply and leaves accrued rent claimable |
| `state_survives_a_long_idle_period` | Balances and claims still work after 20 days of ledgers |
| `never_owes_more_than_was_deposited` | A seeded random sequence of 120 transfers (including self-transfers), deposits and claims never owes more than was deposited. Rounding dust ended at 35 stroops over about 150 checkpoints. The contract holds exactly deposits minus claims, and the token supply is conserved |

**Reproduce**

Create a crate with the three files below, then run:

```bash
cargo test
cargo clippy --all-targets -- -D warnings
stellar contract build   # soroban-sdk 29 refuses a plain cargo build for WASM and asks for stellar-cli
```

Running the tests writes JSON snapshots under `test_snapshots/`. Commit them or add the folder to `.gitignore`.

<details>
<summary><code>Cargo.toml</code></summary>

```toml
[package]
name = "building-token"
description = "Reference SEP-41 permissioned building token with an on-chain rent index"
version = "0.1.0"
edition = "2021"
rust-version = "1.92.0"
publish = false

[lib]
crate-type = ["cdylib"]

[dependencies]
soroban-sdk = { version = "29.0.1" }
soroban-token-sdk = { version = "29.0.1" }

[dev-dependencies]
soroban-sdk = { version = "29.0.1", features = ["testutils"] }

[profile.release]
opt-level = "z"
overflow-checks = true
debug = 0
strip = "symbols"
debug-assertions = false
panic = "abort"
codegen-units = 1
lto = true
```

</details>

<details>
<summary><code>src/lib.rs</code> (the contract)</summary>

```rust
#![no_std]
//! Reference implementation, not audited: a SEP-41 permissioned building token with an
//! on-chain rent index. Balances live in this contract, so wallets can read them through the
//! standard token interface, and every balance change settles accrued rent first.

use soroban_sdk::{
    contract, contracterror, contractevent, contractimpl, contracttype, panic_with_error,
    token::{TokenClient, TokenInterface},
    Address, BytesN, Env, MuxedAddress, String,
};
use soroban_token_sdk::{events, metadata::TokenMetadata, TokenUtils};

/// One whole token = 10^7 base units (7 decimals, like USDC on Stellar).
const TOKEN_UNIT: i128 = 10_000_000;
/// Fixed-point scale of the rent index (10^14).
const INDEX_SCALE: i128 = 100_000_000_000_000;

const DAY_IN_LEDGERS: u32 = 17_280;
const INSTANCE_BUMP: u32 = 7 * DAY_IN_LEDGERS;
const INSTANCE_THRESHOLD: u32 = INSTANCE_BUMP - DAY_IN_LEDGERS;
const ENTRY_BUMP: u32 = 30 * DAY_IN_LEDGERS;
const ENTRY_THRESHOLD: u32 = ENTRY_BUMP - DAY_IN_LEDGERS;

#[contracterror]
#[derive(Copy, Clone, Debug, Eq, PartialEq, PartialOrd, Ord)]
#[repr(u32)]
pub enum Error {
    InvalidAmount = 1,
    NotAllowed = 2,
    InsufficientBalance = 3,
    InsufficientAllowance = 4,
    PeriodAlreadyDeposited = 5,
    NothingToClaim = 6,
    InvalidNav = 7,
    NavMoveTooLarge = 8,
    InvalidSupply = 9,
}

#[contracttype]
#[derive(Clone)]
pub struct AllowanceKey {
    pub from: Address,
    pub spender: Address,
}

#[contracttype]
pub struct AllowanceValue {
    pub amount: i128,
    pub live_until_ledger: u32,
}

/// Valuation inputs and the NAV computed from them, all in USDC base units (7 decimals).
#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct NavSnapshot {
    pub appraisal: i128,
    pub reserves: i128,
    pub liabilities: i128,
    pub nav_per_token: i128,
    pub ledger: u32,
    pub report_sha256: BytesN<32>,
}

#[contracttype]
#[derive(Clone)]
pub enum DataKey {
    Admin,
    Usdc,
    TotalSupply,
    MaxNavMoveBps,
    /// Cumulative USDC per token base unit, scaled by INDEX_SCALE.
    RentIndex,
    /// Division remainder carried into the next deposit so no stroop is lost.
    IndexCarry,
    Nav,
    Balance(Address),
    UserIndex(Address),
    Unclaimed(Address),
    Allowed(Address),
    Period(u32),
    Allowance(AllowanceKey),
}

#[contractevent]
pub struct RentDeposited {
    #[topic]
    pub period: u32,
    pub amount: i128,
    pub report_sha256: BytesN<32>,
}

#[contractevent]
pub struct RentClaimed {
    #[topic]
    pub investor: Address,
    pub amount: i128,
}

#[contractevent]
pub struct NavUpdated {
    pub nav_per_token: i128,
    pub report_sha256: BytesN<32>,
}

#[contractevent(data_format = "single-value")]
pub struct NavCapSet {
    pub max_bps: u32,
}

#[contractevent]
pub struct AllowedSet {
    #[topic]
    pub account: Address,
    pub allowed: bool,
}

#[contractevent]
pub struct ForcedTransfer {
    #[topic]
    pub from: Address,
    #[topic]
    pub to: Address,
    pub amount: i128,
    pub reason_sha256: BytesN<32>,
}

#[contractevent(data_format = "single-value")]
pub struct SetAdmin {
    #[topic]
    pub admin: Address,
    pub new_admin: Address,
}

// ---------------------------------------------------------------- storage helpers

fn bump_instance(e: &Env) {
    e.storage().instance().extend_ttl(INSTANCE_THRESHOLD, INSTANCE_BUMP);
}

fn read_i128(e: &Env, key: &DataKey) -> i128 {
    match e.storage().persistent().get::<DataKey, i128>(key) {
        Some(v) => {
            e.storage().persistent().extend_ttl(key, ENTRY_THRESHOLD, ENTRY_BUMP);
            v
        }
        None => 0,
    }
}

fn write_i128(e: &Env, key: &DataKey, value: i128) {
    e.storage().persistent().set(key, &value);
    e.storage().persistent().extend_ttl(key, ENTRY_THRESHOLD, ENTRY_BUMP);
}

fn read_admin(e: &Env) -> Address {
    e.storage().instance().get::<DataKey, Address>(&DataKey::Admin).unwrap()
}

fn read_supply(e: &Env) -> i128 {
    e.storage().instance().get::<DataKey, i128>(&DataKey::TotalSupply).unwrap()
}

fn read_index(e: &Env) -> i128 {
    e.storage().instance().get::<DataKey, i128>(&DataKey::RentIndex).unwrap_or(0)
}

fn is_allowed(e: &Env, who: &Address) -> bool {
    let key = DataKey::Allowed(who.clone());
    match e.storage().persistent().get::<DataKey, bool>(&key) {
        Some(allowed) => {
            e.storage().persistent().extend_ttl(&key, ENTRY_THRESHOLD, ENTRY_BUMP);
            allowed
        }
        None => false,
    }
}

fn require_allowed(e: &Env, who: &Address) {
    if !is_allowed(e, who) {
        panic_with_error!(e, Error::NotAllowed);
    }
}

fn check_amount(e: &Env, amount: i128) {
    if amount < 0 {
        panic_with_error!(e, Error::InvalidAmount);
    }
}

// ---------------------------------------------------------------- balances and rent accrual

fn read_balance(e: &Env, who: &Address) -> i128 {
    read_i128(e, &DataKey::Balance(who.clone()))
}

fn spend_balance(e: &Env, who: &Address, amount: i128) {
    let balance = read_balance(e, who);
    if balance < amount {
        panic_with_error!(e, Error::InsufficientBalance);
    }
    write_i128(e, &DataKey::Balance(who.clone()), balance - amount);
}

fn receive_balance(e: &Env, who: &Address, amount: i128) {
    // Read after the sender was written: a self-transfer nets to zero instead of minting.
    let balance = read_balance(e, who);
    write_i128(e, &DataKey::Balance(who.clone()), balance + amount);
}

/// Moves the rent earned since the user's last checkpoint into `Unclaimed`.
/// Must run before any change to the user's balance.
fn checkpoint(e: &Env, who: &Address) {
    let index = read_index(e);
    let user_key = DataKey::UserIndex(who.clone());
    let last = read_i128(e, &user_key);
    if index > last {
        let balance = read_balance(e, who);
        if balance > 0 {
            let earned = balance * (index - last) / INDEX_SCALE;
            if earned > 0 {
                let key = DataKey::Unclaimed(who.clone());
                write_i128(e, &key, read_i128(e, &key) + earned);
            }
        }
        write_i128(e, &user_key, index);
    }
}

fn move_tokens(e: &Env, from: &Address, to: &Address, amount: i128) {
    require_allowed(e, from);
    require_allowed(e, to);
    checkpoint(e, from);
    checkpoint(e, to);
    spend_balance(e, from, amount);
    receive_balance(e, to, amount);
}

// ---------------------------------------------------------------- allowances (SEP-41)

fn read_allowance(e: &Env, from: &Address, spender: &Address) -> AllowanceValue {
    let key = DataKey::Allowance(AllowanceKey { from: from.clone(), spender: spender.clone() });
    match e.storage().temporary().get::<DataKey, AllowanceValue>(&key) {
        Some(a) if a.live_until_ledger >= e.ledger().sequence() => a,
        Some(a) => AllowanceValue { amount: 0, live_until_ledger: a.live_until_ledger },
        None => AllowanceValue { amount: 0, live_until_ledger: 0 },
    }
}

fn write_allowance(e: &Env, from: &Address, spender: &Address, amount: i128, live_until: u32) {
    if amount > 0 && live_until < e.ledger().sequence() {
        panic_with_error!(e, Error::InvalidAmount);
    }
    let key = DataKey::Allowance(AllowanceKey { from: from.clone(), spender: spender.clone() });
    e.storage()
        .temporary()
        .set(&key, &AllowanceValue { amount, live_until_ledger: live_until });
    if amount > 0 {
        let live_for = live_until - e.ledger().sequence();
        e.storage().temporary().extend_ttl(&key, live_for, live_for);
    }
}

fn spend_allowance(e: &Env, from: &Address, spender: &Address, amount: i128) {
    let allowance = read_allowance(e, from, spender);
    if allowance.amount < amount {
        panic_with_error!(e, Error::InsufficientAllowance);
    }
    if amount > 0 {
        write_allowance(e, from, spender, allowance.amount - amount, allowance.live_until_ledger);
    }
}

// ---------------------------------------------------------------- contract

#[contract]
pub struct BuildingToken;

#[allow(clippy::too_many_arguments)]
#[contractimpl]
impl BuildingToken {
    #[allow(clippy::too_many_arguments)]
    /// Runs inside the deploy transaction, so nobody can initialise the contract first.
    /// The whole fixed supply is minted to `treasury`, which is the only holder at the start.
    pub fn __constructor(
        e: Env,
        admin: Address,
        usdc: Address,
        treasury: Address,
        name: String,
        symbol: String,
        total_supply: i128,
        max_nav_move_bps: u32,
    ) {
        if total_supply <= 0 {
            panic_with_error!(&e, Error::InvalidSupply);
        }
        let inst = e.storage().instance();
        inst.set(&DataKey::Admin, &admin);
        inst.set(&DataKey::Usdc, &usdc);
        inst.set(&DataKey::TotalSupply, &total_supply);
        inst.set(&DataKey::MaxNavMoveBps, &max_nav_move_bps);
        TokenUtils::new(&e)
            .metadata()
            .set_metadata(&TokenMetadata { decimal: 7, name, symbol });
        e.storage().persistent().set(&DataKey::Allowed(treasury.clone()), &true);
        write_i128(&e, &DataKey::Balance(treasury.clone()), total_supply);
        events::MintWithAmountOnly { to: treasury, amount: total_supply }.publish(&e);
    }

    // ---- compliance (admin)

    /// Allowlist maintained from the KYC provider. Revoking freezes transfers and claims.
    pub fn set_allowed(e: Env, account: Address, allowed: bool) {
        read_admin(&e).require_auth();
        bump_instance(&e);
        e.storage().persistent().set(&DataKey::Allowed(account.clone()), &allowed);
        e.storage().persistent().extend_ttl(
            &DataKey::Allowed(account.clone()),
            ENTRY_THRESHOLD,
            ENTRY_BUMP,
        );
        AllowedSet { account, allowed }.publish(&e);
    }

    /// Court order, inheritance or lost key. Settles rent for both sides first.
    pub fn forced_transfer(
        e: Env,
        from: Address,
        to: Address,
        amount: i128,
        reason_sha256: BytesN<32>,
    ) {
        read_admin(&e).require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        require_allowed(&e, &to);
        checkpoint(&e, &from);
        checkpoint(&e, &to);
        spend_balance(&e, &from, amount);
        receive_balance(&e, &to, amount);
        ForcedTransfer { from, to, amount, reason_sha256 }.publish(&e);
    }

    /// Use a multisig account as admin in production.
    pub fn set_admin(e: Env, new_admin: Address) {
        let admin = read_admin(&e);
        admin.require_auth();
        bump_instance(&e);
        e.storage().instance().set(&DataKey::Admin, &new_admin);
        SetAdmin { admin, new_admin }.publish(&e);
    }

    // ---- rent

    /// Deposits one period's net distributable cash. A period can be deposited once, and its
    /// signed report hash is stored with it. Unsold tokens held by the treasury earn rent too.
    pub fn deposit_net_rent(e: Env, period: u32, amount: i128, report_sha256: BytesN<32>) {
        let admin = read_admin(&e);
        admin.require_auth();
        if amount <= 0 {
            panic_with_error!(&e, Error::InvalidAmount);
        }
        bump_instance(&e);
        let period_key = DataKey::Period(period);
        if e.storage().persistent().has(&period_key) {
            panic_with_error!(&e, Error::PeriodAlreadyDeposited);
        }
        let supply = read_supply(&e);
        if supply <= 0 {
            panic_with_error!(&e, Error::InvalidSupply);
        }
        let usdc = e.storage().instance().get::<DataKey, Address>(&DataKey::Usdc).unwrap();
        TokenClient::new(&e, &usdc).transfer(&admin, e.current_contract_address(), &amount);

        let carry = e.storage().instance().get::<DataKey, i128>(&DataKey::IndexCarry).unwrap_or(0);
        let scaled = amount * INDEX_SCALE + carry;
        e.storage().instance().set(&DataKey::RentIndex, &(read_index(&e) + scaled / supply));
        e.storage().instance().set(&DataKey::IndexCarry, &(scaled % supply));

        e.storage().persistent().set(&period_key, &report_sha256);
        e.storage().persistent().extend_ttl(&period_key, ENTRY_THRESHOLD, ENTRY_BUMP);
        RentDeposited { period, amount, report_sha256 }.publish(&e);
    }

    /// Pays all accrued rent to the investor in USDC.
    pub fn claim_rent(e: Env, investor: Address) -> i128 {
        investor.require_auth();
        bump_instance(&e);
        require_allowed(&e, &investor);
        checkpoint(&e, &investor);
        let key = DataKey::Unclaimed(investor.clone());
        let payout = read_i128(&e, &key);
        if payout <= 0 {
            panic_with_error!(&e, Error::NothingToClaim);
        }
        write_i128(&e, &key, 0);
        let usdc = e.storage().instance().get::<DataKey, Address>(&DataKey::Usdc).unwrap();
        TokenClient::new(&e, &usdc).transfer(&e.current_contract_address(), &investor, &payout);
        RentClaimed { investor, amount: payout }.publish(&e);
        payout
    }

    /// Rent the investor could claim right now, including accrual not yet checkpointed.
    pub fn claimable(e: Env, investor: Address) -> i128 {
        let index = read_index(&e);
        let last = read_i128(&e, &DataKey::UserIndex(investor.clone()));
        let balance = read_balance(&e, &investor);
        let pending = read_i128(&e, &DataKey::Unclaimed(investor));
        if balance > 0 && index > last {
            pending + balance * (index - last) / INDEX_SCALE
        } else {
            pending
        }
    }

    // ---- valuation

    /// NAV per whole token = (appraisal + reserves - liabilities) / tokens outstanding.
    /// All inputs are USDC base units. After the first update, a move larger than
    /// `max_nav_move_bps` is rejected, so a single bad appraisal cannot reprice the token.
    pub fn update_nav(
        e: Env,
        appraisal: i128,
        reserves: i128,
        liabilities: i128,
        report_sha256: BytesN<32>,
    ) {
        read_admin(&e).require_auth();
        bump_instance(&e);
        if appraisal <= 0 || reserves < 0 || liabilities < 0 {
            panic_with_error!(&e, Error::InvalidNav);
        }
        let equity = appraisal + reserves - liabilities;
        let supply = read_supply(&e);
        if equity <= 0 || supply <= 0 {
            panic_with_error!(&e, Error::InvalidNav);
        }
        let nav_per_token = equity * TOKEN_UNIT / supply;
        if nav_per_token <= 0 {
            panic_with_error!(&e, Error::InvalidNav);
        }
        if let Some(prev) = e.storage().instance().get::<DataKey, NavSnapshot>(&DataKey::Nav) {
            let max_bps = e
                .storage()
                .instance()
                .get::<DataKey, u32>(&DataKey::MaxNavMoveBps)
                .unwrap() as i128;
            let moved = (nav_per_token - prev.nav_per_token).abs();
            if moved * 10_000 > prev.nav_per_token * max_bps {
                panic_with_error!(&e, Error::NavMoveTooLarge);
            }
        }
        let snapshot = NavSnapshot {
            appraisal,
            reserves,
            liabilities,
            nav_per_token,
            ledger: e.ledger().sequence(),
            report_sha256: report_sha256.clone(),
        };
        e.storage().instance().set(&DataKey::Nav, &snapshot);
        NavUpdated { nav_per_token, report_sha256 }.publish(&e);
    }

    /// Changes the per-update NAV cap. Raise it deliberately before a large, documented re-appraisal.
    pub fn set_max_nav_move_bps(e: Env, max_bps: u32) {
        read_admin(&e).require_auth();
        bump_instance(&e);
        e.storage().instance().set(&DataKey::MaxNavMoveBps, &max_bps);
        NavCapSet { max_bps }.publish(&e);
    }

    // ---- views

    pub fn nav(e: Env) -> Option<NavSnapshot> {
        e.storage().instance().get::<DataKey, NavSnapshot>(&DataKey::Nav)
    }

    pub fn total_supply(e: Env) -> i128 {
        read_supply(&e)
    }

    pub fn rent_index(e: Env) -> i128 {
        read_index(&e)
    }

    pub fn is_allowed(e: Env, account: Address) -> bool {
        is_allowed(&e, &account)
    }

    pub fn period_report(e: Env, period: u32) -> Option<BytesN<32>> {
        e.storage().persistent().get::<DataKey, BytesN<32>>(&DataKey::Period(period))
    }
}

#[contractimpl(contracttrait)]
impl TokenInterface for BuildingToken {
    fn allowance(e: Env, from: Address, spender: Address) -> i128 {
        bump_instance(&e);
        read_allowance(&e, &from, &spender).amount
    }

    fn approve(e: Env, from: Address, spender: Address, amount: i128, live_until_ledger: u32) {
        from.require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        write_allowance(&e, &from, &spender, amount, live_until_ledger);
        events::Approve { from, spender, amount, live_until_ledger }.publish(&e);
    }

    fn balance(e: Env, id: Address) -> i128 {
        bump_instance(&e);
        read_balance(&e, &id)
    }

    fn transfer(e: Env, from: Address, to_muxed: MuxedAddress, amount: i128) {
        from.require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        let to = to_muxed.address();
        move_tokens(&e, &from, &to, amount);
        events::Transfer { from, to, to_muxed_id: to_muxed.id(), amount }.publish(&e);
    }

    fn transfer_from(e: Env, spender: Address, from: Address, to: Address, amount: i128) {
        spender.require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        spend_allowance(&e, &from, &spender, amount);
        move_tokens(&e, &from, &to, amount);
        events::Transfer { from, to, to_muxed_id: None, amount }.publish(&e);
    }

    /// Burning reduces the supply, so later deposits divide by the smaller number.
    fn burn(e: Env, from: Address, amount: i128) {
        from.require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        require_allowed(&e, &from);
        checkpoint(&e, &from);
        spend_balance(&e, &from, amount);
        e.storage().instance().set(&DataKey::TotalSupply, &(read_supply(&e) - amount));
        events::Burn { from, amount }.publish(&e);
    }

    fn burn_from(e: Env, spender: Address, from: Address, amount: i128) {
        spender.require_auth();
        check_amount(&e, amount);
        bump_instance(&e);
        spend_allowance(&e, &from, &spender, amount);
        require_allowed(&e, &from);
        checkpoint(&e, &from);
        spend_balance(&e, &from, amount);
        e.storage().instance().set(&DataKey::TotalSupply, &(read_supply(&e) - amount));
        events::Burn { from, amount }.publish(&e);
    }

    fn decimals(e: Env) -> u32 {
        TokenUtils::new(&e).metadata().get_metadata().decimal
    }

    fn name(e: Env) -> String {
        TokenUtils::new(&e).metadata().get_metadata().name
    }

    fn symbol(e: Env) -> String {
        TokenUtils::new(&e).metadata().get_metadata().symbol
    }
}

#[cfg(test)]
mod test;
```

</details>

<details>
<summary><code>src/test.rs</code> (the 12 tests)</summary>

```rust
extern crate std;

use super::*;
use soroban_sdk::{
    testutils::{Address as _, Ledger},
    token::{StellarAssetClient, TokenClient},
    Address, BytesN, Env, String,
};

/// One whole token or one whole USDC, in base units.
const T: i128 = 10_000_000;

/// A panic with `panic_with_error!` reaches the client as a generic `soroban_sdk::Error`.
fn code(e: Error) -> soroban_sdk::Error {
    soroban_sdk::Error::from_contract_error(e as u32)
}

struct Fx<'a> {
    env: Env,
    id: Address,
    vault: BuildingTokenClient<'a>,
    token: TokenClient<'a>,
    usdc: TokenClient<'a>,
    treasury: Address,
}

fn hash(env: &Env, tag: u8) -> BytesN<32> {
    BytesN::from_array(env, &[tag; 32])
}

fn fixture<'a>(supply_tokens: i128) -> Fx<'a> {
    let env = Env::default();
    env.mock_all_auths();
    let admin = Address::generate(&env);
    let treasury = Address::generate(&env);
    let issuer = Address::generate(&env);
    let usdc_sac = env.register_stellar_asset_contract_v2(issuer);
    let usdc_admin = StellarAssetClient::new(&env, &usdc_sac.address());
    usdc_admin.mint(&admin, &(1_000_000 * T));
    let id = env.register(
        BuildingToken,
        (
            admin.clone(),
            usdc_sac.address(),
            treasury.clone(),
            String::from_str(&env, "Torre Equipetrol"),
            String::from_str(&env, "TEQ1"),
            supply_tokens * T,
            2_000u32, // NAV may move at most 20% per update
        ),
    );
    let vault = BuildingTokenClient::new(&env, &id);
    let token = TokenClient::new(&env, &id);
    let usdc = TokenClient::new(&env, &usdc_sac.address());
    Fx { env, id, vault, token, usdc, treasury }
}

impl<'a> Fx<'a> {
    /// New KYC-approved investor holding `tokens` whole tokens bought from the treasury.
    fn investor(&self, tokens: i128) -> Address {
        let who = Address::generate(&self.env);
        self.vault.set_allowed(&who, &true);
        if tokens > 0 {
            self.token.transfer(&self.treasury, &who, &(tokens * T));
        }
        who
    }
    fn deposit(&self, period: u32, usdc: i128) {
        self.vault.deposit_net_rent(&period, &(usdc * T), &hash(&self.env, period as u8));
    }
}

#[test]
fn exposes_the_sep41_surface() {
    let f = fixture(100_000);
    assert_eq!(f.token.decimals(), 7);
    assert_eq!(f.token.name(), String::from_str(&f.env, "Torre Equipetrol"));
    assert_eq!(f.token.symbol(), String::from_str(&f.env, "TEQ1"));
    assert_eq!(f.token.balance(&f.treasury), 100_000 * T);
    assert_eq!(f.vault.total_supply(), 100_000 * T);

    let alice = f.investor(1_000);
    let bob = f.investor(0);
    let spender = Address::generate(&f.env);
    f.token.approve(&alice, &spender, &(300 * T), &(f.env.ledger().sequence() + 100));
    assert_eq!(f.token.allowance(&alice, &spender), 300 * T);
    f.token.transfer_from(&spender, &alice, &bob, &(200 * T));
    assert_eq!(f.token.balance(&alice), 800 * T);
    assert_eq!(f.token.balance(&bob), 200 * T);
    assert_eq!(f.token.allowance(&alice, &spender), 100 * T);
}

/// The original minted tokens on a self-transfer: 1,000 became 32,000 after five of them.
#[test]
fn self_transfer_does_not_mint() {
    let f = fixture(100_000);
    let alice = f.investor(1_000);
    for _ in 0..5 {
        let all = f.token.balance(&alice);
        f.token.transfer(&alice, &alice, &all);
    }
    assert_eq!(f.token.balance(&alice), 1_000 * T);
    assert_eq!(f.token.balance(&alice) + f.token.balance(&f.treasury), 100_000 * T);
}

/// The original had no way to approve an investor, so nobody but the admin could ever hold tokens.
#[test]
fn only_allowed_accounts_can_receive_and_a_revoked_sender_is_frozen() {
    let f = fixture(100_000);
    let stranger = Address::generate(&f.env);
    let res = f.token.try_transfer(&f.treasury, &stranger, &(10 * T));
    assert_eq!(res.err().unwrap().ok().unwrap(), code(Error::NotAllowed));

    let alice = f.investor(100);
    f.vault.set_allowed(&alice, &false);
    let res = f.token.try_transfer(&alice, &f.treasury, &(10 * T));
    assert_eq!(res.err().unwrap().ok().unwrap(), code(Error::NotAllowed));

    f.vault.set_allowed(&alice, &true);
    f.token.transfer(&alice, &f.treasury, &(10 * T));
    assert_eq!(f.token.balance(&alice), 90 * T);
}

/// The document's own example: a holder of 2,500 of 100,000 tokens earns 87.5 USDC from a 3,500 USDC deposit.
#[test]
fn pro_rata_share_matches_the_worked_example() {
    let f = fixture(100_000);
    let alice = f.investor(2_500);
    f.deposit(1, 3_500);
    assert_eq!(f.vault.claimable(&alice), 875_000_000); // 87.5 USDC
    let paid = f.vault.claim_rent(&alice);
    assert_eq!(paid, 875_000_000);
    assert_eq!(f.usdc.balance(&alice), 875_000_000);
    assert_eq!(f.vault.claimable(&alice), 0);
    let again = f.vault.try_claim_rent(&alice);
    assert_eq!(again.err().unwrap().ok().unwrap(), code(Error::NothingToClaim));
}

/// A holds through period 1 and sells to B before period 2: A keeps period 1 rent, B gets only period 2.
#[test]
fn rent_stays_with_the_holder_across_a_mid_period_sale() {
    let f = fixture(100_000);
    let a = f.investor(1_000);
    let b = f.investor(0);
    f.deposit(1, 10_000);
    f.token.transfer(&a, &b, &(1_000 * T));
    assert_eq!(f.vault.claimable(&a), 100 * T, "A keeps the rent accrued before the sale");
    assert_eq!(f.vault.claimable(&b), 0, "B cannot claim rent from before the purchase");
    f.deposit(2, 10_000);
    assert_eq!(f.vault.claimable(&a), 100 * T, "A earns nothing from period 2");
    assert_eq!(f.vault.claimable(&b), 100 * T, "B earns period 2");
    assert_eq!(f.vault.claim_rent(&a), 100 * T);
    assert_eq!(f.vault.claim_rent(&b), 100 * T);
}

#[test]
fn a_period_can_be_deposited_once() {
    let f = fixture(100_000);
    f.deposit(202611, 3_500);
    assert_eq!(f.vault.period_report(&202611), Some(hash(&f.env, 202611u32 as u8)));
    let again = f.vault.try_deposit_net_rent(&202611, &(3_500 * T), &hash(&f.env, 9));
    assert_eq!(again.err().unwrap().ok().unwrap(), code(Error::PeriodAlreadyDeposited));
    assert_eq!(f.usdc.balance(&f.id), 3_500 * T, "the rejected deposit moved no USDC");
}

/// 3-token supply, 1-stroop deposits: the old floor-only index paid nothing; the carry pays all 3 stroops.
#[test]
fn division_remainder_is_carried_forward() {
    let f = fixture(3);
    let a = f.investor(1);
    let b = f.investor(1);
    let c = f.investor(1);
    for period in 1..=3u32 {
        f.vault.deposit_net_rent(&period, &1, &hash(&f.env, period as u8));
    }
    let total = f.vault.claimable(&a) + f.vault.claimable(&b) + f.vault.claimable(&c);
    assert_eq!(total, 3, "all three deposited stroops are claimable");
    assert_eq!(f.vault.claim_rent(&a), 1);
}

#[test]
fn nav_is_computed_from_its_inputs_and_bounded() {
    let f = fixture(105_000);
    assert_eq!(f.vault.nav(), None);
    // 1,000,000 appraisal + 10,000 reserves - 0 liabilities over 105,000 tokens = 9.6190476 USDC
    f.vault.update_nav(&(1_000_000 * T), &(10_000 * T), &0, &hash(&f.env, 1));
    let nav = f.vault.nav().unwrap();
    assert_eq!(nav.nav_per_token, 96_190_476);
    assert_eq!(nav.report_sha256, hash(&f.env, 1));

    // +5%: accepted
    f.vault.update_nav(&(1_060_000 * T), &(10_000 * T), &0, &hash(&f.env, 2));
    assert!(f.vault.nav().unwrap().nav_per_token > 96_190_476);

    // +50%: rejected by the 20% cap
    let big = f.vault.try_update_nav(&(1_600_000 * T), &(10_000 * T), &0, &hash(&f.env, 3));
    assert_eq!(big.err().unwrap().ok().unwrap(), code(Error::NavMoveTooLarge));

    // liabilities larger than assets: rejected
    let neg = f.vault.try_update_nav(&(100 * T), &0, &(200 * T), &hash(&f.env, 4));
    assert_eq!(neg.err().unwrap().ok().unwrap(), code(Error::InvalidNav));

    // a documented large re-appraisal needs the cap raised first, as a separate admin action
    f.vault.set_max_nav_move_bps(&6_000);
    f.vault.update_nav(&(1_600_000 * T), &(10_000 * T), &0, &hash(&f.env, 5));
    assert!(f.vault.nav().unwrap().nav_per_token > 150_000_000);
}

#[test]
fn forced_transfer_settles_rent_on_both_sides() {
    let f = fixture(100_000);
    let lost = f.investor(1_000);
    let heir = f.investor(0);
    f.deposit(1, 10_000);
    f.vault.forced_transfer(&lost, &heir, &(1_000 * T), &hash(&f.env, 7));
    assert_eq!(f.token.balance(&heir), 1_000 * T);
    assert_eq!(f.vault.claimable(&lost), 100 * T, "rent earned before the order stays with the old holder");
    assert_eq!(f.vault.claimable(&heir), 0);
}

#[test]
fn burn_shrinks_supply_and_keeps_accrued_rent() {
    let f = fixture(100_000);
    let alice = f.investor(1_000);
    f.deposit(1, 10_000);
    f.token.burn(&alice, &(1_000 * T));
    assert_eq!(f.vault.total_supply(), 99_000 * T);
    assert_eq!(f.vault.claimable(&alice), 100 * T);
    assert_eq!(f.vault.claim_rent(&alice), 100 * T);
}

#[test]
fn state_survives_a_long_idle_period() {
    let f = fixture(100_000);
    let alice = f.investor(1_000);
    f.deposit(1, 10_000);
    f.env.ledger().with_mut(|l| l.sequence_number += 20 * 17_280); // 20 days
    assert_eq!(f.token.balance(&alice), 1_000 * T);
    assert_eq!(f.vault.claim_rent(&alice), 100 * T);
}

/// Invariant: whatever the sequence of transfers, deposits and claims, holders can never be owed
/// more than was deposited, and at most a few stroops of rounding dust are left unassigned.
#[test]
fn never_owes_more_than_was_deposited() {
    let f = fixture(100_000);
    let holders = [f.investor(7_000), f.investor(13_000), f.investor(333), f.investor(21_111), f.investor(5)];
    let mut seed: u64 = 0x9E37_79B9_7F4A_7C15;
    let mut next = || {
        seed = seed.wrapping_mul(6364136223846793005).wrapping_add(1442695040888963407);
        (seed >> 33) as i128
    };
    let mut deposited: i128 = 0;
    let mut claimed: i128 = 0;
    let mut period = 0u32;
    let mut checkpoints: i128 = 0;
    for step in 0..120 {
        match next() % 4 {
            0 | 1 => {
                let from = &holders[(next() % 5) as usize];
                let to = &holders[(next() % 5) as usize]; // may equal `from`
                let bal = f.token.balance(from);
                if bal > 0 {
                    let amount = 1 + next() % bal;
                    f.token.transfer(from, to, &amount);
                    checkpoints += 2;
                }
            }
            2 => {
                period += 1;
                let amount = 1 + next() % (5_000 * T);
                f.vault.deposit_net_rent(&period, &amount, &hash(&f.env, period as u8));
                deposited += amount;
            }
            _ => {
                let who = &holders[(next() % 5) as usize];
                if f.vault.claimable(who) > 0 {
                    claimed += f.vault.claim_rent(who);
                    checkpoints += 1;
                }
            }
        }
        let owed: i128 = holders.iter().map(|h| f.vault.claimable(h)).sum::<i128>()
            + f.vault.claimable(&f.treasury);
        assert!(owed + claimed <= deposited, "step {step}: owed {owed} + claimed {claimed} > deposited {deposited}");
    }
    let owed: i128 = holders.iter().map(|h| f.vault.claimable(h)).sum::<i128>() + f.vault.claimable(&f.treasury);
    let dust = deposited - owed - claimed;
    std::println!("deposited {deposited} stroops, claimed {claimed}, still owed {owed}, rounding dust {dust}, checkpoints ~{checkpoints}");
    assert!(dust >= 0 && dust <= checkpoints + holders.len() as i128 + 2, "unexpected dust {dust}");
    assert_eq!(f.usdc.balance(&f.id), deposited - claimed, "the contract holds exactly deposits minus claims");
    let supply: i128 = holders.iter().map(|h| f.token.balance(h)).sum::<i128>() + f.token.balance(&f.treasury);
    assert_eq!(supply, 100_000 * T, "token supply is conserved");
}
```

</details>

## Appendix C. What changed from the original

Page numbers refer to the 18-page Gemini export.

| Page | Original | This edition | Section |
|---|---|---|---|
| 1 | Scope: a fraction of one building | Buildings, projects under construction and land with no construction, one property per token or a portfolio. Adds land valuation, a holding-cost reserve, title checks, the payout at exit and the rule for adding properties | 1.6, 1.7, 6.3 |
| 1 | NAV divided by "circulating" tokens | Divided by tokens outstanding, including treasury and escrow | 1.3 |
| 1 | $10 launch price from $1,050,000 over 105,000 tokens, no mention of an entry cost | Day-1 NAV $9.6190, so a $0.3810 (3.81%) entry cost to disclose. The transfer tax is the seller's | 1.2 |
| 1 | Square-meter peg as a token model | Not recommended. The equity percentage is what matters | 1.1 |
| 2 to 4 | A classic asset with its SAC, trading on the DEX, and a contract holding balances in private storage | One token model per deployment. Model A recommended, with holders unauthorized between platform-built transactions | 2.2, 2.3 |
| 3 to 4 | One atomic transaction from local payment to tokens | Only the final USDC-for-token step is atomic. The ramp has several legs | 3.3 |
| 4 | Purchase USDC goes into the "Property Distribution Vault" | Separate escrow and distribution accounts | 3.4 |
| 4 | Treasury "liquidity walls" around NAV on the DEX | Removed. No open market in v1, capped redemption windows if lawful | 5.4 |
| 5 | Waterfall with zero IVA credit and a withholding reserve inside the shared pool | IVA credit on invoiced costs, withholding per holder, IT offset in steady state, IUE accrual checked | 4.1, 4.2 |
| 5 | Example with 100,000 tokens and 0.35 BOB per token | One running example with 105,000 tokens: 36,365 BOB in year 1 and 38,465 in steady state | 4.2 |
| 6 | IMT applies when the building enters the SPV or is sold | The seller (registered owner) owes it. First sales of new construction pay IT. The base is the higher of price and IPBI value | 1.2, 4.3 |
| 6 | IPBI through RUAT with a 10% to 15% early-payment discount | Paid to the municipality. Discounts vary by municipality and year (Cochabamba 15%, then 10%, in 2026) | 4.3 |
| 6 | Urubó listed as part of Santa Cruz de la Sierra | Urubó is in the municipality of Porongo, which collects its property taxes | 4.3, 4.4 |
| 7 | The official versus parallel gap as the main exchange-rate risk | The boliviano floats since 29 June 2026. BCB Bs 11.67 on 14 November. The gap is about 1%. Volatility is the risk | 4.5 |
| 7, 18 | A BOB-pegged token (page 7) and `get_pollar_exchange_rate()` (page 18) | Neither exists in the SDK. Use the BCB rate and `quoteRamp()`. The BOB route to Stellar needs confirmation | 3.2 |
| 8 | S.R.L. versus fideicomiso, with no regulatory section | The perimeter (Ley 1834, DS 5384, Ley 393 art. 486, UIF), five wrapper options including a closed-end fund, questions for counsel | 5 |
| 9 | Depreciation shields cash flow, "which can be distributed" | Depreciation lowers profit, not cash. Equity payouts are generally limited to effective and liquid profit after the legal reserve | 4.3 |
| 9 | IUE paid offsets IT for "the entire following year" | The credit starts the month after payment, and more depreciation means less IUE and less credit. DS 5327 and DS 5563 | 4.3 |
| 9 | "ELFEC/AXS" for common-area power | CRE and SAGUAPAC serve Santa Cruz. No source places AXS in either city | 4.4 |
| 9 to 15 | The vault contract | Self-transfer minting, no authorization on initialization, no KYC setter, not SEP-41, repeatable deposits, opaque NAV. Replaced by Appendix B | 6.2, App. B |
| 15 | The contract is "transfer-safe" | Disproved on the Soroban test host. 1,000 tokens became 32,000 | 6.2 |
| 15 | `ipfs_hash: BytesN<32>` | A SHA-256 of the report is stored. The content id goes in the event or the report | 6.2 |
| 16 | Net yields of 9.2% (Equipetrol) and 6.8% (Cochabamba) | 3.56% in year 1 (SC-1) and 2.34% (CB-1), computed from the waterfall | 7.4 |
| 16 | A knapsack optimizer for three buildings | Rule-based templates | 7.2 |
| 17 | $1,400 plus $200 a month, $3,800 in 12 months. Option A: 396 tokens, about $356 a year | 14,000 BOB plus 2,000 BOB at 11.67 is $3,256.21. Option A: 333.35 tokens, $77.33 of rent received in the year, run rate about $118.72 | 7.4 |
| 17 | Option C: a $950 "instant equity gain" | A projection: $586.12 at +18%, with flat and -10% cases. No rent during construction | 7.4 |
| 18 | The contract releases tokens the moment a payment clears | The platform releases tranches, because the chain cannot see a BOB payment. Disclose the trust assumption | 7.5 |

## Appendix D. Sources

Each rule in this document was read in the source named. Secondary sources are marked in the text. Source content was paraphrased.

**Stellar and Soroban**

- [Assets overview and comparison](https://developers.stellar.org/docs/tokens/anatomy-of-an-asset) (classic assets and SAC, SEP-41, SEP-57, DEX compatibility)
- [SEP-8 Regulated Assets](https://github.com/stellar/stellar-protocol/blob/master/ecosystem/sep-0008.md)
- [Clawback guide](https://developers.stellar.org/docs/build/guides/transactions/clawbacks)
- [List of operations](https://developers.stellar.org/docs/learn/fundamentals/transactions/list-of-operations) (thresholds, `AUTH_CLAWBACK_ENABLED` requires `AUTH_REVOCABLE`)
- [Migrate from Horizon to RPC](https://developers.stellar.org/docs/data/apis/migrate-from-horizon-to-rpc) and the [APIs overview](https://developers.stellar.org/docs/data/apis) (Horizon nearing end-of-life)
- [OpenZeppelin RWA token for Stellar](https://docs.openzeppelin.com/stellar-contracts/tokens/rwa/rwa)
- [Soroban token example](https://github.com/stellar/soroban-examples/tree/main/token) (the SEP-41 reference used for Appendix B)
- [Stellar CLI cookbook, asset management](https://developers.stellar.org/docs/tools/cli/cookbook/asset-management) (asset codes of 1 to 12 characters)

**Pollar**

- [pollar-xyz/pollar](https://github.com/pollar-xyz/pollar): README, CHANGELOG, `docs/adr/0001-ramp-api-and-lifecycle.md`, `docs/design/dev-8-ramp-adapter-and-user-experience.md` (SDK 0.12.1, Apache-2.0)

**Exchange rate**

- [BCB rate table, 14 November 2026](https://www.bcb.gob.bo/librerias/indicadores/otras/ultimo.php)
- [Infobae, 29 June 2026](https://www.infobae.com/america/america-latina/2026/06/29/bolivia-flexibiliza-el-dolar-y-cierra-una-era-de-15-anos-de-tipo-de-cambio-fijo/)
- Red Uno: [rate for 24 September 2026](https://www.reduno.com.bo/economia/nueva-escalada-oficial-el-bcb-eleva-el-tipo-de-cambio-del-dolar-para-este-jueves-2026923202122) and [7 November 2026](https://www.reduno.com.bo/economia/a-cuanto-esta-el-dolar-en-bolivia-este-sabado-7-de-noviembre-asi-cotizan-el-cambio-oficial-y-paralelo-202611773627)
- [dolarbolivia.net](https://dolarbolivia.net/) (a rate tracker, gap of 1.1% on 10 November)

**Bolivian law and tax**

- [Ley 1834, Securities Market Law](https://www.lexivox.org/norms/BO-L-1834.xhtml), [DS 5384](https://www.lexivox.org/norms/BO-DS-N5384.xhtml) and [DS 24054](https://www.lexivox.org/norms/BO-DS-24054.xhtml) (Lexivox reproductions, not the official gazette)
- [Constitution of 2009, OAS copy](http://www.oas.org/juridico/PDFs/mesicic4_blv_const.pdf) (arts. 262, 315, 331, 394, 395, 397, 400, 401)
- [Ley 477 against land invasion and land trafficking](https://www.lexivox.org/norms/BO-L-N477.xhtml)
- [INRA, saneamiento of rural property](https://www.inra.gob.bo/saneamiento-de-la-propiedad-agraria/) and [Los Tiempos on INRA's checks for buyers](https://www.lostiempos.com/actualidad/cochabamba/20220306/instan-verificar-6-requisitos-evitar-estafas-avasalladores)
- [boliviaimpuestos.com, tax loss carry-forward](https://boliviaimpuestos.com/resultados-acumulados-negativos-contabilidad/) (reproduces SIN resolution RND 10-0008-13 on Ley 843 art. 48)
- [ASFI, illegal financial activity](https://asfi.gob.bo/pb/y-donde-denunciar-actividad-financiera-ilegal) (Ley 393 art. 486)
- [SIN retentions FAQ](https://www.impuestos.gob.bo/wp-content/uploads/2025/10/PREGUNTAS-RETENCIONES-25.pdf)
- [YoFacturo, taxes on rent](https://yo-facturo.com/blog/impuestos-alquiler-inmuebles-bolivia/) (secondary)
- [boliviaimpuestos.com, IUE-IT compensation](https://boliviaimpuestos.com/decreto-supremo-nuevo-marco-normativo-para-la-compensacion-iue-it/) (secondary, source for DS 5327 and DS 5563)
- [Infoleyes, Código de Comercio art. 169](https://bolivia.infoleyes.com/articulo/26270) and [DS 24051 depreciation annex](https://bolivia.infoleyes.com/articulo/7033) (secondary reproductions)
- [PriFinance, crypto licensing in Bolivia](https://prifinance.com/es/licencia-de-intercambio-de-criptomonedas/bolivia/) (a licensing consultancy, secondary source for BCB Resolution 082/2024)
- [Los Tiempos, Cochabamba municipal tax discounts](https://www.lostiempos.com/actualidad/cochabamba/20260701/amplian-perdonazo-impuestos-municipales-cercado)
- [Metlabs, asset tokenization in Bolivia](https://metlabs.io/en/tokenization/bolivia/) (a tokenization vendor, so a secondary source)
- [Dentons, fintech regulation amendments, 22 May 2026](https://www.dentons.com/en/insights/articles/2026/may/22/amendments-to-the-regulation-for-financial-technology-companies) (search summary only, the page could not be retrieved)
- [BNB SAFI FAQ](https://www.bnb.com.bo/PortalSAFI/Principal/PreguntasFrecuentes), [Banco Unión SAFI](https://bancounion.com.bo/GrupoUnion/SAFIProductos) and [Fortaleza SAFI](https://www.fortalezasafi.com/productos/) (investment fund definitions, ASFI supervision)

**Market data and utilities**

- Numbeo: [Santa Cruz](https://www.numbeo.com/property-investment/in/Santa-Cruz-Bolivia) and [Cochabamba](https://www.numbeo.com/property-investment/in/Cochabamba), updated 10 November 2026. Crowd-sourced.
- [Correo del Sur on CRE](https://correodelsur.com/sociedad/20250806/cre-un-orgullo-para-los-bolivianos.html) and [BNamericas on SAGUAPAC](https://www.bnamericas.com/es/perfil-empresa/cooperativa-de-servicios-publicos-santa-cruz-ltda)
- [eju.tv on Porongo and Colinas del Urubó](https://eju.tv/2024/10/la-hora-de-porongo-y-la-planificacion-urbana/)

**Method.** The figures in sections 1, 4 and 7 were computed with decimal arithmetic from the assumptions in section 0.3. The contract in Appendix B was compiled and tested as described there.
