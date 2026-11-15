# Information from Other Markets: Real Estate Tokenization Abroad

What platforms, regulators and courts in other countries teach the Bolivian design

| | |
|---|---|
| Status | Draft for team review |
| Version | 1.0, 2026-11-15 |
| Companion to | [real-estate-token-valuation-methods.md](real-estate-token-valuation-methods.md), version 2.2 ("the design document") |
| Coverage | United States, United Arab Emirates (Dubai), Brazil, Argentina, El Salvador, Spain, Mexico and Bolivia. Sources dated up to November 2026 |
| Notice | A research summary, not legal, tax or investment advice. Figures are as the named sources report them. Company and competitor claims are marked as such. |
| Language | English. Spanish version: [informacion-de-otros-mercados.md](informacion-de-otros-mercados.md) |

## How to read this document

- Section 1 lists the lessons. Section 2 sizes the market. Section 3 goes case by case, and each case ends with what it means for our design. Section 4 summarizes studies of resale liquidity. Section 5 proposes changes to the design document for the Design Review Gate. Section 6 lists the sources.
- "Design 1.6" means section 1.6 of the [design document](real-estate-token-valuation-methods.md). P1 to P8 are the proposals in section 5.
- Every figure names its source. A figure published by a platform about itself is a company figure. A figure from a rival platform is a competitor's account. Section 6 labels each source by type.
- Figures are as of each source's date and were not audited. Amounts stay in the source's currency, and "$" means US dollars.

## 1. Lessons in brief

| # | Lesson | Evidence | Proposal |
|---|---|---|---|
| L1 | The best-known failure came from the operator, not from the blockchain | A Detroit court declared many RealT properties public nuisances and sent rent to escrow. Taxes went unpaid, payouts stopped and the US structures are being liquidated (section 3.1) | P1, P2 |
| L2 | Investors could not check title, liens or taxes | RealT sold stakes in nearly 100 properties without clear recorded ownership and took mortgages it had said did not exist (section 3.1) | P3 |
| L3 | High advertised returns are a warning sign | RealT touted 6% to 16% a year (section 3.1). A study finds that higher token returns come mainly from picking high-yield property segments (section 4). The design's net cash yield for SC-1 is 3.56% in year 1 (design 4.2) | P4 |
| L4 | The property registry decides how close a token can get to title | Dubai's land registry runs its own token program (section 3.2). Brazilian state courts bar registries from linking titles to tokens (section 3.3) | P3 |
| L5 | The models that work sit inside a regulator's perimeter | Dubai under VARA (section 3.2), Argentina's farmland trust under the CNV (section 3.4), El Salvador's issues under the CNAD (section 3.5) | P5 |
| L6 | Resale stays thin, even with tokens | Studies of RealT and other platforms (section 4). Mexico has no organized resale (section 3.7) | P7 |
| L7 | Headline market figures overstate what is tokenized | Brazil: a R$20 billion project pipeline against R$40 million to R$50 million actually tokenized (section 3.3). Real estate tokens on public blockchains total about $213 million worldwide (section 2) | P8 |
| L8 | Leasing land while holding it is a tested source of income | Landtoken's farmland earns lease income, paid in soybeans in its first stage (section 3.4) | P6 |
| L9 | Bolivia has no tokenization precedent | None of the 11 companies cleared to enter ASFI's test environment works with tokenized assets (section 3.8) | P5 |

## 2. Market size

| Measure | Figure | Source | Note |
|---|---|---|---|
| Real estate tokens on public blockchains | $213.29 million in 96 assets from 11 countries, held by about 17,560 wallets, as of 5 November 2026. Two platforms, Groma ($95.2 million) and Reental ($79.9 million), hold about 82% of it | RWA.xyz data quoted by a vendor ([Pixel Web Solutions](https://www.pixelwebsolutions.com/best-real-estate-tokenization-platforms/)) | The narrowest measure: tokens anyone can see on-chain |
| All tokenized real-world assets | About $33.5 billion distributed on-chain as of 8 July 2026. Tokenized US Treasuries are the largest part, at $12.9 billion to $16.2 billion | [Crypto Briefing](https://cryptobriefing.com/rwa-tokenization-surges-five-sectors/), citing RWA.xyz | Real estate is a small slice |
| Deloitte forecast for tokenized real estate | Above $4 trillion by 2035, from under $300 billion in 2024, about 27% a year. Of the 2035 figure, $2.39 trillion is loans and securitizations, $1 trillion private real estate funds and $501 billion undeveloped land and projects under construction | Deloitte Center for Financial Services, April 2025, as reported by [Cointelegraph](https://cointelegraph.com/news/real-estate-tokenization-forecast-4-trillion-2035) and [PaySpace Magazine](https://payspacemagazine.com/news/real-estate-worth-4t-to-be-tokenized-by-2035-deloitte/) | Mostly tokenized debt and fund units, not tokens for shares of a building |

The forecast and the on-chain count measure different things, so they cannot be compared. For planning, use the on-chain count and the platform results in section 3.

## 3. Cases

The table compares the cases. The subsections give the details and the sources.

| Case | Assets | What the holder gets | Oversight | Status |
|---|---|---|---|---|
| RealT, United States | Detroit rental homes | Interests in the companies that held the homes, sold only outside the US | Offered under Regulation S, per a competitor | A court-approved fiduciary since April 2026. US structures in liquidation since July 2026 |
| RedSwan, United States | Multifamily and hospitality properties | Digital securities | A FINRA-regulated marketplace, per the Stellar Development Foundation | $100 million on Stellar, September 2025 |
| PRYPCO Mint, Dubai | Dubai properties | A fractional stake in a specific property | A land registry (DLD) program. Platform licensed by VARA | Resale open since 20 February 2026 |
| Landtoken, Argentina | Farmland | Units of a financial trust with a public offer authorized by the CNV | CNV. Allaria Fiduciaria S.A. as trustee | Public trust placed from December 2025 |
| LEAD issues, El Salvador | Real estate and construction projects | The economic rights each issue defines | CNAD | 55 issues for $633 million by September 2026 |
| Reental, Spain | Properties and real estate credit | A fraction of a property or of a credit right | Not in the CNMV crowdfunding register, per Reental | $79.9 million on public blockchains, November 2026 (vendor figure) |
| Urbanitae, Spain | Real estate projects | Investments through a registered crowdfunding platform. No secondary market | Registered with the CNMV | 11.7% average IRR on repaid projects (company figure, reported by Reental) |

### 3.1 United States

#### RealT in Detroit

**What it was.** RealT bought rental homes in Detroit and sold tokens for interests in the companies that held them, only to investors outside the United States ([Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf); [Outlier Media](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/)). Lofty, a competitor, says the offerings relied on Regulation S, the US exemption for sales made outside the country ([Lofty](https://www.lofty.ai/blog/realt-liquidation-explained)). The tokens ran on the public blockchains Ethereum and Gnosis Chain ([Cointribune](https://www.cointribune.com/en/realt-revolutionizes-real-estate-investment-with-blockchain/)). RealT sold about $94 million of tokens on roughly 700 Detroit properties ([Outlier Media](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/)) to about 22,000 investors ([Reiss, Bizub and Peralta](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=7220758)). Detroit's complaint says RealT touted yearly returns of 6% to 16% from rent ([Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf)). Lofty says advertised yields often exceeded 10%.

**What went wrong.** Detroit's complaint described unsafe occupied units, hundreds of unsecured vacant homes, rentals run without the certificates of compliance the city requires, and unpaid fines and taxes ([Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf)).

| Date | Event | Source |
|---|---|---|
| February 2021 | RealT takes a first mortgage of $458,000 on 18 properties | [Outlier Media](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/) |
| October 2021 | A co-founder tells investors there are no mortgages on RealT properties | [Outlier Media](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/) |
| 2025 | Mortgages reach nearly $4.8 million on 33 properties, although investors had paid enough to buy them outright. RealT has sold stakes in nearly 100 properties where it has not shown clear recorded ownership, and 4 of them are listed for sale by other people. 141 properties are vacant and the tax debt is above $3.6 million. Taxes are overdue on 30 of the mortgaged properties, which have received 571 blight tickets since they were tokenized | [Outlier Media](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/) |
| 22 July 2025 | Judge Annette J. Berry of the Wayne County Circuit Court grants Detroit a temporary restraining order and declares many of the properties public nuisances. RealT must secure vacant properties within 30 days and repair code violations in occupied ones within 90 days. It may not collect rent or evict tenants until a property has a certificate of compliance, and tenants pay rent into an escrow account overseen by the city | [Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf) |
| 23 July 2025 | The city announces the case: more than 400 properties, RealT's two founders and 165 affiliated companies. The escrow can be used only for repairs | [City of Detroit](https://detroitmi.gov/es/news/la-ciudad-obtiene-una-importante-victoria-para-los-inquilinos-que-viven-en-propiedades-de-real-token) |
| 23 September 2025 | The court turns the order into a preliminary injunction | [Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf) |
| 20 March 2026 | Payouts to investors have almost all stopped. More than 300 properties could go to tax foreclosure. RealT's property manager is down to five employees | [Michigan Public](https://www.michiganpublic.org/social-justice/2026-03-20/the-model-no-longer-works-crypto-landlords-detroit-enterprise-is-crumbling) |
| 22 April 2026 | A court-approved settlement puts a fiduciary, Charles Bullock, in charge of about 700 properties. The escrow holds under $640,000 | [Lofty](https://www.lofty.ai/blog/realt-liquidation-explained) (competitor) |
| 2 July 2026 | RealT announces the voluntary liquidation of its US structures | [Lofty](https://www.lofty.ai/blog/realt-liquidation-explained) (competitor) |
| 2026 | The Delomel law firm brings a class action in France for about 14,000 investors, and a criminal complaint is filed in Paris | [Lofty](https://www.lofty.ai/blog/realt-liquidation-explained) (competitor) |

**What it means for us.**

- The token did not change who was responsible for the buildings. Detroit used ordinary housing and nuisance law and argued that the token structure was irrelevant to liability, and the court granted its order ([Roosevelt Institute](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf)). Expect Bolivian municipal, tax and tenant rules to apply to our vehicle in the same way.
- When the operator failed, no independent party could take over. A fiduciary arrived only through a court settlement, nine months after the restraining order (P1).
- The blockchain showed token balances, not the state of the property. Holders could not see unpaid taxes, mortgages, vacancies or code violations (P2, P3).
- The advertised returns depended on rent that a court later stopped (P4).

#### RedSwan on Stellar

In September 2025 the Stellar Development Foundation announced that RedSwan, which it describes as a FINRA-regulated digital securities marketplace, had brought $100 million of commercial real estate to Stellar: institutional-grade multifamily and hospitality properties ([Stellar](https://stellar.org/press/redswan-digital-real-estate-brings-usd100m-of-tokenized-real-estate-to-the-stellar-network)). This is a company announcement. It gives no investor numbers or trading data.

**What it means for us.** Regulated real estate securities already run on Stellar, sold through a regulated intermediary. That supports the choice of network (design 2). It says nothing about which token model RedSwan uses.

### 3.2 United Arab Emirates: the Dubai Land Department and PRYPCO Mint

**What it is.** A program of Dubai's land registry, the Dubai Land Department (DLD), with PRYPCO Mint as the platform. VARA, Dubai's virtual assets regulator, licenses and supervises the platform ([Gulf News, February 2026](https://gulfnews.com/business/property/prypco-rolls-out-dubai-marketplace-after-nod-for-property-stakes-trading-1.500438076)).

- **Launch.** The DLD started the pilot with VARA in March 2025 ([The National](https://www.thenationalnews.com/business/property/2026/02/09/dubai-property-tokenisation-fractional-investing/)). PRYPCO Mint opened to investors in May 2025, for UAE residents with an Emirates ID, from AED 2,000 per investment ([Gulf News, May 2025](https://gulfnews.com/your-money/saving-investment/how-to-invest-in-dubai-property-with-just-dh2000-1.500140848); [Invezz](https://invezz.com/es/noticias/2025/05/26/la-primera-plataforma-inmobiliaria-tokenizada-de-dubai-se-lanza-bajo-la-supervision-de-vara/)). That is about $545 at the dirham's fixed rate of 3.6725 per dollar.
- **Demand.** By February 2026 the pilot had raised more than AED 18.5 million from investors of more than 50 nationalities, and one offering filled in 1 minute 58 seconds ([Gulf News, February 2026](https://gulfnews.com/business/property/prypco-rolls-out-dubai-marketplace-after-nod-for-property-stakes-trading-1.500438076)).
- **Resale.** A resale market opened on 20 February 2026 for about 7.8 million tokens ([The National](https://www.thenationalnews.com/business/property/2026/02/09/dubai-property-tokenisation-fractional-investing/)).
- **Target.** The DLD aims for AED 60 billion ($16.33 billion) of tokenized property by 2033, about 7% of Dubai's property transactions ([The National](https://www.thenationalnews.com/business/property/2026/02/09/dubai-property-tokenisation-fractional-investing/)).
- **Fees.** On entry, PRYPCO Mint charges a 2% DLD fee instead of the standard 4% transfer fee, plus a 2% platform fee and a 0.1% tokenization fee capped at AED 850 ([PRYPCO](https://prypco.com/blogs/real-estate-tokenization-fees-dubai-explained), company).

**What it means for us.**

- In Dubai the land registry itself sponsors the program, so the link between token and property is official. Derechos Reales has no such program, so our token stays a right against the vehicle, not a registry entry (design 5.3).
- Entry costs are similar. PRYPCO's entry fees come to about 4.1% of the amount invested. The design's entry cost is 3.81% of the price for SC-1 and 3.62% for LT-1 (design 1.2 and 1.6).
- Resale opened about nine months after PRYPCO Mint opened to investors, once the regulator approved it. That matches the design's v1 policy of no open market until a licensed route exists (design 5.4, D5).

### 3.3 Brazil

**Market.** Exame reports about 450 developers working with tokenization, whose projects have a combined sales value (VGV) of about R$20 billion. It also counts 30,000 digital wallets and 9,000 investors reported to the federal tax authority. The stock of property actually tokenized is only R$40 million to R$50 million ([Exame](https://exame.com/future-of-money/tokenizacao-imobiliaria-alcanca-r-20-bilhoes-no-brasil/), 2026). The R$20 billion is the value of the developers' projects, not tokens sold.

**Registries.** Brazil's property registries (`cartórios`) and the state court bodies that oversee them have refused to link titles to tokens.

- At the registrars' request, the Corregedoria Geral da Justiça of São Paulo, the state court body that oversees registries, issued Provimento CGJ 54/2025, published on 23 January 2026. It bars registrars from making any entry that links a property record (`matrícula`) to a digital token or a blockchain representation ([Valor Econômico](https://valor.globo.com/financas/criptomoedas/noticia/2026/01/27/em-vitria-dos-cartrios-corregedoria-de-so-paulo-probe-o-registro-de-tokens-imobilirios.ghtml); [Migalhas](https://www.migalhas.com.br/depeso/452304/cgj-sp-veda-tokenizacao-imobiliaria-e-edita-provimento-54-25)).
- Rio Grande do Sul, Santa Catarina, Mato Grosso do Sul and Espírito Santo issued similar rules ([Jota](https://www.jota.info/justica/corregedorias-de-justica-proibem-que-matriculas-de-imoveis-sejam-vinculadas-a-tokens-digitais)). Santa Catarina's dates from 18 August 2025 ([Coinspeaker](https://www.coinspeaker.com/pt/santa-catarina-veta-registros-de-imoveis-na-blockchain/)).
- The federal council of real estate brokers (Cofeci) set up a system for digital real estate transactions with Resolution 1.551/2025 in August 2025. On 13 October 2025 the 21st Federal Civil Court in Brasília suspended it at the request of the national electronic registry operator (ONR), finding that it created a parallel registry without legal basis ([Valor Econômico](https://valor.globo.com/financas/criptomoedas/noticia/2025/10/13/justia-suspende-resoluo-do-cofeci-sobre-tokenizao-imobiliria.ghtml); [Migalhas](https://www.migalhas.com.br/quentes/442418/token-juiz-suspende-resolucao-sobre-transacoes-imobiliarias-digitais)).

**Securities.** The securities regulator, the CVM, published Parecer de Orientação 40 on 11 October 2022. It sets out the CVM's view of the rules that apply to crypto-assets that are securities ([CVM](https://www.gov.br/cvm/pt-br/assuntos/noticias/2022/cvm-divulga-parecer-de-orientacao-sobre-criptoativos-e-o-mercado-de-valores-mobiliarios)).

**What it means for us.**

- A registry can refuse to recognize tokens. The design already keeps title with the vehicle in Derechos Reales and makes the token a right against the vehicle (design 5.3). Keep it that way, and never market the token as registered ownership.
- Measure the market by tokens sold and money raised, not by project pipelines (P8).
- In Brazil the securities regulator's view decides when a token is a security. The Bolivian equivalent is the Ley 1834 question already on the counsel list (design 5.5, question 1).

### 3.4 Argentina

**Rules.** The securities regulator, the Comisión Nacional de Valores (CNV), built a regime for tokenizing real-world assets in steps, each published in the official gazette.

| Resolution | Published | What it did |
|---|---|---|
| [RG 1069/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/326947/20250613) | 13 June 2025 | Created the first regime for tokenizing real-world assets, with a one-year regulatory sandbox |
| [RG 1081/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/330173/20250821) | 21 August 2025 | Second stage of the regime |
| [RG 1087/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/333326/20251023) | 23 October 2025 | Widened the regime |
| [RG 1150/2026](https://www.boletinoficial.gob.ar/detalleAviso/primera/343010/20260611) | 11 June 2026 | Widened it again and extended the sandbox to 31 December 2027 |

**Landtoken.** Landtoken, founded in 2023, tokenizes farmland. The agribusiness company Adecoagro operates the fields and Justoken provides the technology ([Agrofy](https://news.agrofy.com.ar/noticia/214878/hito-ahora-cualquiera-puede-invertir-campos-us-50-y-importante-renta-proyectada)). All figures below come from the company, as reported by agricultural trade press.

| | First stage | Second stage (from December 2025) |
|---|---|---|
| Vehicle | A private administration trust, bought directly on Landtoken's platform | The first tokenized financial trust for farmland with a public offer authorized by the CNV. Allaria Fiduciaria S.A. is trustee and issuer, and Nicholson y Cano is legal counsel |
| Minimum | $100 | $50, through brokers, banks and crypto exchanges, starting with Ripio |
| Reach | About 1,000 investors, its first two tokenized fields, and more than 100 trades between investors | Company totals at launch: more than 13,000 users, more than 1,400 investments and $2.2 million transacted |
| Projected return | About 10% a year: about 4% from the lease, paid in soybean quintals, plus 5% to 6% from appreciation | 3% to 4% gross lease income plus 5% to 6% appreciation a year |
| Source | [Agrositio](https://www.agrositio.com.ar/noticia/243155-tokenizacion-de-campos-como-invertir-en-tierras-productivas-con-solo-100-dolares.html) | [Agrofy](https://news.agrofy.com.ar/noticia/214878/hito-ahora-cualquiera-puede-invertir-campos-us-50-y-importante-renta-proyectada) |

**What it means for us.**

- Landtoken is the closest model to our land product (design 1.6, D11): farmland held in a trust, lease income while the land is held, small tickets, sale through brokers and banks, and an established company farming the land.
- Leasing land to a producer could also bear on the productive-use (FES) and anti-speculation (art. 395) questions in design 1.6. That is a question for counsel (P6).
- The design lists a bank trust as a wrapper option and notes that its requirements were not verified (design 5.2). Landtoken moved from a private trust to a financial trust with an authorized public offer once the regulator had rules for it. Ask counsel whether a Bolivian `fideicomiso` or a securitization through a `patrimonio autónomo` can do the same (P5).

### 3.5 El Salvador

El Salvador's Digital Assets Issuance Law (Ley de Emisión de Activos Digitales, LEAD) has its own regulator, the National Commission of Digital Assets (CNAD). From the CNAD's start in 2023 to 4 September 2026, 55 real estate and construction issues were registered, for $633 million. The tokens carry the economic rights each issue defines, and the law has no closed list of assets that can be tokenized. The newspaper notes that the amount of an issue is not the amount actually invested ([El Diario de Hoy](https://www.elsalvador.com/dinero-y-negocios/entorno-economico/proyectos-inmobiliarios-activos-digitales/1293650/2026/)).

**What it means for us.** A dedicated law and regulator made issuance routine. As in Brazil, the issued amount still overstates the money raised. Bolivia has no such law, so our tokens fall under the securities and fintech rules in design 5.1.

### 3.6 Spain

Reental's own blog compares it with Urbanitae, so these are a company's statements about itself and about a rival ([Reental](https://www.reental.co/blog/reental-vs-urbanitae)):

- A Reental token is a fraction of a property or of a credit right. Reental is not in the register of crowdfunding providers kept by the CNMV, Spain's securities regulator.
- Urbanitae has been in that register since 23 December 2022. It reports an average IRR of 11.7% on fully repaid projects and has no secondary market.

The vendor page in section 2 puts Reental at $79.9 million of tokenized real estate on public blockchains, the second largest platform it tracks.

**What it means for us.** Two platforms in the same country chose different legal routes, and the route decides what the holder owns: a share of a property, a credit right or a crowdfunding investment. The design already requires describing the holder's right exactly as the wrapper defines it (design 5.3).

### 3.7 Mexico

About $400 million has been tokenized in Mexico, and an estimate by Tutellus puts about 25% of it in real estate. Real estate tokens resell only between individuals who agree on a trade, with no formal secondary market yet. In an EY survey, 49% named regulatory uncertainty as the main barrier ([Expansión](https://expansion.mx/mercados/2026/03/02/mexico-registra-400-mdd-en-tokenizacion-institucionalizacion), 2 March 2026).

**What it means for us.** Without clear rules, resale stays informal. That supports keeping v1 without an open market (design 5.4).

### 3.8 Bolivia

ASFI approved its regulation for financial technology companies with Resolución ASFI/540/2025 of 3 July 2025. It includes a controlled test environment (`Entorno Controlado de Pruebas`), and existing companies had until 31 December 2025 to adapt ([ASFI](https://asfi.gob.bo/node/1176), 15 July 2025).

ASFI's list as of 9 October 2026 shows 11 companies with its no-objection to enter the test environment, all S.R.L. ([ASFI list](https://asfi.gob.bo/sites/default/files/2026-10/Entidades%20con%20No%20Objeci%C3%B3n%20para%20el%20Ingreso%20al%20Entorno%20Controlado%20de%20Pruebas%20al%209.10.2026.pdf)):

| Service | Companies |
|---|---|
| Payment gateway | Kite Xperience, Tu Cuate, Libélula, Banipay, Smartfastpay Company Bolivia, PAYIN & PAYOUT |
| Payments and payment platform | DFINANCE |
| Confirming (supplier payments) | Taspri Soluciones Digitales |
| Cross-border payments | AV Finance |
| Conversion of virtual assets to bolivianos | Banexcoin Bolivia |
| Automatic currency conversion | Salteños Bolivia |

None works with tokenized assets.

Derechos Reales issues the `certificado alodial`, which certifies a property's current legal status: the owner's right, mortgages, preventive annotations and court cases ([Consejo de la Magistratura](https://cm.organojudicial.gob.bo/consejo/requisitosddrr/45_-certificado-alodial.html)).

**What it means for us.**

- The platform would be the first tokenized-asset entrant, with no precedent to copy. Ask ASFI early (P5, design 5.5 question 2).
- Banexcoin converts virtual assets to bolivianos, which is the leg the purchase flow needs (design 3.3). Ask Pollar whether its boliviano route uses a company on this list (design 3.5, question 1).
- The `certificado alodial` is the document to publish for title and liens (P3).

## 4. Resale liquidity: what studies found

| Study | Data | Finding |
|---|---|---|
| [Swinkels, Financial Innovation, 2023](https://jfin-swufe.springeropen.com/articles/10.1186/s40854-022-00427-5) | 58 tokenized US rental homes, mostly in Detroit | 254 owners per property on average. Ownership changes about once a year, more often on decentralized exchanges. Token prices move with the house price index |
| [Augustin, Chen, Qian and Shin, SSRN, 2026](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=6251598) | 644 tokenized US homes, with on-chain prices, rents, ownership and trades linked to housing records | Despite frequent trading, token prices add little information and follow housing fundamentals. Liquidity improves only modestly. Higher returns come mainly from high rental yields, because the tokenized homes are concentrated in high-yield segments |
| [BIS Working Paper 1311, 2025, revised 2026](https://www.bis.org/publ/work1311.htm) | Several US platforms, 2019 to 2025 | Tokenized homes cluster where prices are lower and demand and liquidity weaker. After a natural disaster, trading rises 35% over two days. That advantage depends on platforms offering buybacks, which come with higher insolvency risk |
| [Mafrur, arXiv, 2025](https://arxiv.org/abs/2508.11651) | Tokenized real-world assets | Most tokens show low volumes, long holding periods and few active participants |
| [Mafrur, arXiv, 2026](https://arxiv.org/abs/2606.01131) | Ethereum tokens for Treasuries, gold and private credit | Being on-chain is not the same as being liquid. Gold tokens have more holders and steadier activity than many Treasury and private-credit tokens, and a token's outstanding value does not reliably predict its liquidity |

**What it means for us.** A token makes a transfer cheaper, but it does not create buyers. Keep the design's position: no promise of liquidity, and capped redemption windows only if the law allows them (design 5.4). The BIS finding shows why the cap matters: a platform that buys back tokens under stress keeps trading alive but puts its own solvency at risk (P7).

## 5. Proposed changes to the design document

These are proposals for the Design Review Gate. None is in the design document yet.

| ID | Proposed change | Design section | Based on |
|---|---|---|---|
| P1 | **Operator-failure plan.** Name a backup property manager and a successor administrator in the offering documents. Rent goes to an account of the vehicle that the property manager cannot move alone. Holders can replace the manager by vote (**Counsel**) | 3.4, 5.2, 8.1 | RealT (3.1) |
| P2 | **Property facts in the monthly report.** IPBI paid (RUAT receipt), utilities and `expensas` paid, insurance in force, occupied and vacant units, and open municipal notices, each with its receipt or document | 4.6 | RealT (3.1) |
| P3 | **Title and lien proof.** Before each offering and every year after, publish the folio real and a `certificado alodial` from Derechos Reales for each property, with their hashes. No new mortgage or lien without the approval the offering documents require, and publication within a set number of days (**Counsel**) | 1.6, 5.3, 8.1 | RealT (3.1), Brazil (3.3), Bolivia (3.8) |
| P4 | **Yield guardrail.** The concierge quotes only yields computed by the waterfall (design 4.2) and flags any listing or customer message that cites a higher one. When it shows a high yield, it explains that in other markets higher token yields came from cheaper, weaker locations (section 4). Marketing follows the same rules | 7.7 | RealT (3.1), studies (4) |
| P5 | **Regulator first.** Ask ASFI early whether a tokenized-asset company can enter the controlled test environment and which wrapper it expects. Ask counsel whether a trust or a securitization can carry an authorized public offer, as in Argentina | 5.1, 5.2, 5.5 | Dubai (3.2), Argentina (3.4), Bolivia (3.8) |
| P6 | **Land lease route.** For lots like LT-1, plan an interim lease (parking or storage on urban lots, farming on rural land) when the plot allows it, and show the lease income next to the sale scenarios (**Counsel** for rural land) | 1.6 | Landtoken (3.4) |
| P7 | **Liquidity disclosure.** State before purchase that resale may be rare and slow, and publish the number of transfers each period. Size any redemption reserve so that a run cannot threaten the vehicle | 5.4, 7.7 | Studies (4), Mexico (3.7) |
| P8 | **Realistic targets.** Base business targets and marketing figures on tokens sold and money raised, never on project pipelines, issue amounts or market forecasts | 8.2 | Brazil (3.3), El Salvador (3.5), market size (2) |

P1 and P3 would also add two rows to the risk table in design 8.1: operator failure, and undisclosed liens or unpaid taxes.

## 6. Sources

Each fact was read in the source named. Sources marked "search summary" could not be opened directly, so the facts come from the summary that the search engine showed. Source content was paraphrased.

Types: official (government, regulator, court or gazette), academic, think tank, press, company (a platform about itself), competitor (a rival platform) and vendor (a seller of tokenization services).

**United States**

- [Outlier Media, "Undisclosed mortgages, missing deeds: RealT's problems mount"](https://outliermedia.org/realt-detroit-mortgages-real-estate-crypto-blockchain/), 2025. Press
- [City of Detroit, news release of 23 July 2025](https://detroitmi.gov/es/news/la-ciudad-obtiene-una-importante-victoria-para-los-inquilinos-que-viven-en-propiedades-de-real-token), Spanish version. Official
- [Roosevelt Institute, Brad Lipton, "Detroit's Case Against Real Token", December 2025](https://rooseveltinstitute.org/wp-content/uploads/2025/11/RI_Detroit-Real-Token-Model-Landlords_Memo_202512.pdf). Think tank. It summarizes the complaint and the court orders
- [Michigan Public, "The model no longer works", 20 March 2026](https://www.michiganpublic.org/social-justice/2026-03-20/the-model-no-longer-works-crypto-landlords-detroit-enterprise-is-crumbling). Press
- [Lofty, "The RealT Liquidation Explained"](https://www.lofty.ai/blog/realt-liquidation-explained), 2026. Competitor
- [Reiss, Bizub and Peralta, "Who Controls the Block? How States Can Regulate Tokenized Residential Real Estate", SSRN](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=7220758). Academic, search summary
- [Cointribune on RealT, September 2025](https://www.cointribune.com/en/realt-revolutionizes-real-estate-investment-with-blockchain/). Press
- [Stellar Development Foundation, RedSwan announcement, September 2025](https://stellar.org/press/redswan-digital-real-estate-brings-usd100m-of-tokenized-real-estate-to-the-stellar-network). Company

**United Arab Emirates**

- [Gulf News, "How to invest in Dubai property with just Dh2,000", May 2025](https://gulfnews.com/your-money/saving-investment/how-to-invest-in-dubai-property-with-just-dh2000-1.500140848). Press, search summary
- [Invezz, 26 May 2025](https://invezz.com/es/noticias/2025/05/26/la-primera-plataforma-inmobiliaria-tokenizada-de-dubai-se-lanza-bajo-la-supervision-de-vara/), Spanish edition. Press, search summary
- [Gulf News, "PRYPCO rolls out Dubai marketplace after nod for property stakes trading", February 2026](https://gulfnews.com/business/property/prypco-rolls-out-dubai-marketplace-after-nod-for-property-stakes-trading-1.500438076). Press
- [The National, 9 February 2026](https://www.thenationalnews.com/business/property/2026/02/09/dubai-property-tokenisation-fractional-investing/). Press
- [PRYPCO, "Real Estate Tokenization Fees in Dubai, UAE Explained"](https://prypco.com/blogs/real-estate-tokenization-fees-dubai-explained), 2026. Company

**Brazil**

- [Exame, "Tokenização imobiliária alcança R$ 20 bilhões no Brasil"](https://exame.com/future-of-money/tokenizacao-imobiliaria-alcanca-r-20-bilhoes-no-brasil/), 2026. Press
- [Valor Econômico, 27 January 2026](https://valor.globo.com/financas/criptomoedas/noticia/2026/01/27/em-vitria-dos-cartrios-corregedoria-de-so-paulo-probe-o-registro-de-tokens-imobilirios.ghtml). Press
- [Migalhas, "CGJ/SP veda tokenização imobiliária e edita provimento 54/25", 23 March 2026](https://www.migalhas.com.br/depeso/452304/cgj-sp-veda-tokenizacao-imobiliaria-e-edita-provimento-54-25). Legal press
- [Jota, February 2026](https://www.jota.info/justica/corregedorias-de-justica-proibem-que-matriculas-de-imoveis-sejam-vinculadas-a-tokens-digitais). Legal press
- [Coinspeaker, August 2025](https://www.coinspeaker.com/pt/santa-catarina-veta-registros-de-imoveis-na-blockchain/). Press, search summary
- [Valor Econômico, 13 October 2025](https://valor.globo.com/financas/criptomoedas/noticia/2025/10/13/justia-suspende-resoluo-do-cofeci-sobre-tokenizao-imobiliria.ghtml). Press
- [Migalhas, October 2025](https://www.migalhas.com.br/quentes/442418/token-juiz-suspende-resolucao-sobre-transacoes-imobiliarias-digitais). Legal press
- [CVM, 11 October 2022](https://www.gov.br/cvm/pt-br/assuntos/noticias/2022/cvm-divulga-parecer-de-orientacao-sobre-criptoativos-e-o-mercado-de-valores-mobiliarios). Official

**Argentina**

- Boletín Oficial: [RG 1069/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/326947/20250613), [RG 1081/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/330173/20250821), [RG 1087/2025](https://www.boletinoficial.gob.ar/detalleAviso/primera/333326/20251023) and [RG 1150/2026](https://www.boletinoficial.gob.ar/detalleAviso/primera/343010/20260611). Official
- [Agrofy News on Landtoken's public trust](https://news.agrofy.com.ar/noticia/214878/hito-ahora-cualquiera-puede-invertir-campos-us-50-y-importante-renta-proyectada), late 2025. Trade press, figures from the company
- [Agrositio on Landtoken's private trust](https://www.agrositio.com.ar/noticia/243155-tokenizacion-de-campos-como-invertir-en-tierras-productivas-con-solo-100-dolares.html), 2025. Trade press, figures from the company

**El Salvador**

- [El Diario de Hoy, "Proyectos inmobiliarios suman $633 millones en emisiones", September 2026](https://www.elsalvador.com/dinero-y-negocios/entorno-economico/proyectos-inmobiliarios-activos-digitales/1293650/2026/). Press

**Spain**

- [Reental, "Reental vs Urbanitae 2026"](https://www.reental.co/blog/reental-vs-urbanitae), November 2026. Company, also describing a rival

**Mexico**

- [Expansión, 2 March 2026](https://expansion.mx/mercados/2026/03/02/mexico-registra-400-mdd-en-tokenizacion-institucionalizacion). Press

**Bolivia**

- [ASFI, press note of 15 July 2025](https://asfi.gob.bo/node/1176). Official
- [ASFI, companies with no-objection to enter the controlled test environment, 9 October 2026](https://asfi.gob.bo/sites/default/files/2026-10/Entidades%20con%20No%20Objeci%C3%B3n%20para%20el%20Ingreso%20al%20Entorno%20Controlado%20de%20Pruebas%20al%209.10.2026.pdf) (PDF). Official
- [Consejo de la Magistratura, requirements for the certificado alodial](https://cm.organojudicial.gob.bo/consejo/requisitosddrr/45_-certificado-alodial.html). Official

**Studies and market data**

- [Swinkels, "Empirical evidence on the ownership and liquidity of real estate tokens", Financial Innovation 9:45, 2023](https://jfin-swufe.springeropen.com/articles/10.1186/s40854-022-00427-5). Academic, search summary
- [Augustin, Chen, Qian and Shin, "Tokenization and the Financialization of Real Estate", SSRN, 2026](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=6251598). Academic, search summary
- [BIS Working Paper 1311, "When bricks meet bytes: does tokenisation fill gaps in traditional real estate markets?", November 2025, revised June 2026](https://www.bis.org/publ/work1311.htm). Official research
- [Mafrur, "Tokenize Everything, But Can You Sell It? RWA Liquidity Challenges and the Road Ahead", arXiv, August 2025](https://arxiv.org/abs/2508.11651). Academic
- [Mafrur, "Tokenized but Illiquid? Evidence from Real-World Asset Markets", arXiv, May 2026, revised July 2026](https://arxiv.org/abs/2606.01131). Academic, also published in FinTech 2026, 5, 62
- [Pixel Web Solutions, real estate tokenization platforms, data as of 5 November 2026](https://www.pixelwebsolutions.com/best-real-estate-tokenization-platforms/). Vendor, quoting RWA.xyz
- [Crypto Briefing, "RWA tokenization surges across five sectors", July 2026](https://cryptobriefing.com/rwa-tokenization-surges-five-sectors/). Press, citing RWA.xyz
- Deloitte forecast of 24 April 2025, as reported by [Cointelegraph](https://cointelegraph.com/news/real-estate-tokenization-forecast-4-trillion-2035) (press) and [PaySpace Magazine](https://payspacemagazine.com/news/real-estate-worth-4t-to-be-tokenized-by-2035-deloitte/) (press, search summary). Deloitte's own page could not be read
