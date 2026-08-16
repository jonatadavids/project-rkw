# Spike M0-T08 — Checklist legal preliminar

## Aviso e registro

- Data da consulta: **2026-08-16**
- Este documento é checklist de engenharia/produto e **não substitui consultoria jurídica**.
- Não contém Política de Privacidade, Termos de Uso ou redação legal definitiva.
- Fontes usadas: legislação, reguladores e plataformas oficiais.

## Mudança regulatória crítica

A Lei brasileira **15.211/2025 (ECA Digital)** está em vigor desde março de 2026 e alcança jogos eletrônicos conectados direcionados a crianças/adolescentes ou de acesso provável por eles. Ela exige proteção por padrão, medidas de aferição de idade/experiência adequada, supervisão parental, avaliação de riscos, controles de compra/tempo e veda loot boxes e publicidade perfilada para crianças/adolescentes nos casos abrangidos.

Isso torna Q-PV-02 uma decisão jurídica/produto prioritária antes de lançamento; uma simples caixa “tenho 13 anos” não deve ser adotada sem parecer.

Q-PV-02 **não bloqueia a fundação técnica do M1**, mas bloqueia o fluxo definitivo de idade e conta infantil, anúncios reais, IAP real, Alpha externo e publicação nas lojas. Não implementar checkbox, ano de nascimento ou qualquer autodeclaração de idade sem parecer jurídico.

## Checklist de governança

| Item | Responsável | Status |
|---|---|---|
| Definir entidade controladora, contato e encarregado/DPO aplicável | Fundador + jurídico | Pendente consultor |
| Mapear dados, finalidades, bases legais, fornecedores e países | Interno, revisado pelo jurídico | Pendente |
| Inventário de SDKs: UGS, Photon, Firebase, AdMob, Apple/Google, IAP | Engenharia | Pode fazer internamente |
| Firmar/revisar DPAs e transferências internacionais | Jurídico | Pendente consultor |
| Definir matriz de retenção e descarte por categoria | Jurídico + engenharia | Pendente consultor |
| Elaborar RIPD/DPIA, especialmente para menores, ads e perfilamento | Jurídico + privacidade | Pendente consultor |
| Definir processo de incidentes, registros e comunicação à ANPD/titulares | Jurídico + operação | Pendente consultor |

## Privacidade e direitos do titular

| Item | Dono | Status/critério |
|---|---|---|
| Política de Privacidade pt-BR clara, dentro do app e nas lojas | Jurídico | Texto definitivo pendente |
| Termos de Uso | Jurídico | Texto definitivo pendente |
| Aviso de primeiro boot por finalidade | Produto + jurídico | Design pendente |
| Consentimento apenas quando for base adequada; retirada tão fácil quanto concessão | Produto + jurídico | Base por finalidade pendente |
| Acesso, correção, portabilidade e oposição/revogação aplicáveis | Backend + suporte | Processo pendente |
| Exclusão de conta e dados in-app + web | Backend | Protocolo em `data-deletion-procedure.md` |
| Minimização: não coletar peso corporal, localização precisa ou PII desnecessária | Produto | Guardrail definido |
| Analytics pseudonimizada; não chamar Player ID de “anônimo” | Engenharia | Guardrail definido |
| Registro de consentimento/base legal e versão do aviso | Backend | Planejar implementação |

## Crianças, adolescentes e idade — decisão obrigatória

| Item | Status |
|---|---|
| Definir público/faixa etária real e classificação indicativa | Pendente fundador + jurídico |
| Avaliar “acesso provável” sob ECA Digital, não apenas público declarado | Pendente parecer |
| Definir mecanismo de aferição/sinal de idade minimizando dados | Pendente parecer e regulamentação aplicável |
| Configurações mais protetivas por padrão para menores | Requisito a projetar |
| Supervisão parental: privacidade, contatos, compras e tempo de uso | Requisito a projetar se aplicável |
| Consentimento parental verificável quando exigido | Pendente parecer |
| COPPA (<13 nos EUA) e GDPR art. 8 (limiar europeu variável por país) | Pendente escopo geográfico/jurídico |
| Google Families/ads SDKs certificados se crianças fizerem parte do target | Pendente decisão de audiência |
| Desabilitar publicidade comportamental/perfilada para menores | Guardrail obrigatório |
| Loot boxes para público infantil/adolescente | **Vedadas; projeto já não prevê loot boxes pagas** |

## Monetização, anúncios e lojas

| Item | Responsável | Status |
|---|---|---|
| Compras exclusivamente cosméticas; sem pay-to-win | Produto | Definido |
| Preços, moeda virtual, reembolso e restauração transparentes | Produto + jurídico | Pendente |
| Consentimento parental/restrição de compras para menores | Produto + plataformas | Pendente |
| AdMob fora de corrida e sem perfilamento de menores | Engenharia | Requisito definido; validar config |
| Google Play Data Safety e target audience | Fundador + engenharia | Pré-publicação |
| Apple App Privacy labels | Fundador + engenharia | Pré-publicação |
| Conta criada → exclusão integral in-app; Google também exige URL externa | Engenharia | Obrigatório |
| Política acessível no app e metadata de ambas as lojas | Fundador | Obrigatório |

## Conteúdo, comunidade e propriedade intelectual

| Item | Status |
|---|---|
| Classificação indicativa brasileira e ratings das lojas | Pendente avaliação |
| Moderação/report/block para nomes, UGC e futuras comunicações | Pendente design antes da feature |
| Pistas fictícias e ausência de marcas/logos reais sem licença | Guardrail definido |
| Registro de licença/proveniência de todo asset, fonte, áudio e SDK | Processo interno a criar |
| Direito de imagem/voz em playtests e materiais promocionais | Formulário jurídico pendente |
| Termos de campeonatos, premiação e regras promocionais | Fora do MVP casual; jurídico antes de ativar |

## Segurança operacional

| Item | Status |
|---|---|
| Privacy/security by design, criptografia em trânsito e least privilege | Arquitetura/implementação futura |
| Secrets somente em secret manager; rotação e controle de acesso | Guardrail definido |
| Processo de vulnerabilidade, incident response e backups | Pendente M1+ |
| Subprocessadores e mudanças de SDK revisados antes de release | Processo a instituir |
| Conta de suporte para DSR e SLA interno | Pendente operação |

## Gates recomendados

### Antes do Alpha externo

- inventário de dados/SDKs;
- decisão documentada de faixa etária;
- ambientes sem ads/IAP reais para menores;
- termos de playtest e canal de suporte;
- DSR/exclusão testável em staging.

### Antes de publicar nas lojas

- parecer jurídico sobre LGPD, ECA Digital, idade e geografias;
- Política de Privacidade e Termos aprovados;
- Data Safety/App Privacy completos;
- account deletion in-app + web validado;
- classificação etária e configuração de audiência/ads aprovadas;
- DPAs, retenção, incident response e contato de privacidade operacionais.

## Q-SP-01 e Q-PV-02

- **Q-SP-01:** permanece pendente; contratar consultor com experiência em LGPD, jogos/apps, menores e lojas mobile antes de Alpha externo.
- **Q-PV-02:** permanece pendente; o consultor deve recomendar faixa etária, aferição de idade, consentimento/supervisão parental e alcance geográfico. O ECA Digital impede tratar isso como detalhe apenas de COPPA.

**Decisão humana em 2026-08-16:** este checklist e o procedimento de exclusão foram aprovados somente como protocolos preliminares. A aprovação não autoriza implementar mecanismo definitivo de idade, anúncios reais, IAP real, Alpha externo ou publicação.

## Fontes oficiais

- [LGPD — Lei 13.709/2018](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm)
- [ECA Digital — Lei 15.211/2025](https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2025/lei/l15211.htm)
- [ANPD — orientações de aferição de idade (2026)](https://www.gov.br/anpd/pt-br/assuntos/noticias/anpd-publica-orientacoes-preliminares-e-cronograma-para-afericao-de-idade-no-ambiente-digital)
- [ANPD — tratamento de dados de crianças e adolescentes](https://www.gov.br/anpd/pt-br/assuntos/noticias/anpd-divulga-enunciado-sobre-o-tratamento-de-dados-pessoais-de-criancas-e-adolescentes)
- [GDPR — Regulation (EU) 2016/679](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng)
- [FTC — COPPA FAQ](https://www.ftc.gov/business-guidance/resources/complying-coppa-frequently-asked-questions)
- [Apple App Review Guidelines](https://developer.apple.com/app-store/review/guidelines/)
- [Apple — account deletion](https://developer.apple.com/support/offering-account-deletion-in-your-app/)
- [Google Play — User Data policy](https://support.google.com/googleplay/android-developer/answer/10144311)
- [Google Play — target audience and children](https://support.google.com/googleplay/android-developer/answer/9867159)
