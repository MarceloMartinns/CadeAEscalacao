# Registro de Decisões (ADRs)

Este arquivo registra as decisões técnicas do projeto **Cadê a escalação?** no formato de *Architecture Decision Records* (ADR): o contexto, o que foi decidido, as alternativas e as consequências.

Também serve como material de estudo e como contexto para ferramentas de IA — um agente que lê este arquivo entende **por que** o código é como é.

**Status possíveis:** Proposta · Aceita · Substituída (por ADR-XXX) · Descartada

---

## ADR-001 — Arquitetura: monólito modular com Clean Architecture

**Status:** Aceita · **Data:** 2026-09-27

**Contexto**
Sistema pequeno (dezenas de usuários, poucos grupos), um desenvolvedor, e objetivo explícito de estudar arquitetura.

**Decisão**
Um único backend (monólito) dividido em camadas de Clean Architecture:

- `Domain` — entidades e regras (pontuação, montagem de times, rodízio de professor). Sem dependências externas.
- `Application` — casos de uso (confirmar presença, escalar times, registrar resultado).
- `Infrastructure` — EF Core, Supabase, serviços externos.
- `Server` — API HTTP.

Referências: `Application → Domain`, `Infrastructure → Application`, `Server → Application + Infrastructure`. As dependências sempre apontam para dentro.

Dentro de `Application`, organizar por funcionalidade (*vertical slices*: `Listas/`, `Escalacao/`, `Temporadas/`), não por tipo técnico.

**Alternativas**
- Microsserviços — descartado: complexidade operacional sem benefício nesse tamanho.
- Projeto único sem camadas — mais simples, mas perde o valor de estudo.

**Consequências**
- ✅ Regras de negócio testáveis sem banco.
- ✅ Troca de banco/ORM isolada na Infrastructure.
- ⚠️ Mais projetos e mapeamentos que o estritamente necessário — aceito pelo aprendizado.

---

## ADR-002 — Backend e frontend na mesma solution (template do Visual Studio)

**Status:** Aceita · **Data:** 2026-09-28

**Decisão**
Solution criada com o template **"React and ASP.NET Core" (TypeScript)** do Visual Studio 2026:

- `CadeAEscalacao.Server` — API ASP.NET Core.
- `cadeaescalacao.client` — React + Vite, como projeto `.esproj`.
- Os nomes do template foram mantidos (`.Server` em vez de `.Api`): renomear exigiria mexer em pastas, `.csproj` e referências sem ganho real.
- Projetos lado a lado na raiz (padrão do template). A organização em `src/` e `tests/` é feita com **pastas de solução**, que são só visuais.
- Solution no formato **`.slnx`** (XML, legível e fácil de revisar em diff).
- **Desenvolvimento:** F5 sobe API e Vite juntos. **Produção:** a API serve o build do React.

**Consequências**
- ✅ Um repositório, um deploy, um domínio — sem configurar CORS em produção.
- ⚠️ Front e back escalam juntos (irrelevante neste tamanho).

---

## ADR-003 — Supabase como PostgreSQL + autenticação

**Status:** Aceita · **Data:** 2026-09-27

**Decisão**
- **Banco:** acessado pela API via EF Core + Npgsql (connection string do Supabase, usando o *pooler*). O React **não** acessa o banco direto — toda regra passa pela API.
- **Auth:** Supabase Auth (login por e-mail/magic link ou Google). O React obtém o JWT e envia à API, que valida o token.
- **Migrations:** via EF Core (`dotnet ef migrations`).

**Alternativas**
- Usar o Supabase como backend completo (React → Supabase direto com RLS) — descartado: tiraria o C# do centro do projeto.

**Consequências**
- ⚠️ Plano gratuito pausa o projeto após 7 dias sem atividade. Como os jogos são semanais, o uso normal deve evitar isso; como garantia, um workflow agendado no GitHub Actions faz uma consulta a cada 3 dias.
- ⚠️ Limite de 500 MB — muito acima do necessário.

---

## ADR-004 — Testes

**Status:** Aceita · **Data:** 2026-09-27

**Decisão**

| Tipo | Ferramenta | Alvo |
|---|---|---|
| Unitário | NUnit | Domain e Application |
| Asserções | Shouldly | Leitura dos testes |
| Mocks | NSubstitute | Dependências da Application |
| Integração | `WebApplicationFactory` + Testcontainers (PostgreSQL) | API + banco real em container |
| Frontend | Vitest + React Testing Library | Componentes |
| E2E | Playwright | Fluxos principais (lista → escalação → resultado) |

Cada ferramenta entra no projeto quando for usada pela primeira vez.

**Alternativas**
- xUnit — padrão mais comum no ecossistema .NET; NUnit mantido por já ser conhecido.
- FluentAssertions — descartado: mudou para licença comercial a partir da v8.

**Consequências**
- Prioridade: regras de negócio (pontuação, montagem de times) com cobertura alta no Domain.

---

## ADR-005 — Docker e hospedagem

**Status:** Aceita · **Data:** 2026-09-27

**Decisão**
- **Docker:** sim. Um `Dockerfile` multi-stage (build do React + build do .NET → imagem `aspnet`).
- **App:** Render, plano gratuito (web service via Docker, sem cartão).
- **Banco:** Supabase, plano gratuito.
- **CI/CD:** GitHub Actions roda os testes a cada push; deploy automático no Render ao fazer merge na `main`.

**Alternativas**
- Azure App Service F1 — não exige Docker (publicação direta do Visual Studio), mas limita a 60 min de CPU/dia. Bom plano B.
- Google Cloud Run / Azure Container Apps — generosos, porém exigem cartão cadastrado.
- Koyeb — passou a exigir cartão em 2026.

**Consequências**
- ⚠️ O serviço gratuito dorme após ~15 min sem uso; o primeiro acesso leva cerca de 1 minuto. Aceitável para uso semanal.
- ✅ A mesma imagem roda local, no CI e em produção.

---

## ADR-006 — IA no desenvolvimento

**Status:** Proposta · **Data:** 2026-09-27

**Decisão**
- Manter um `CLAUDE.md` na raiz com convenções do projeto (arquitetura, comandos de build/teste, padrões de nome).
- Criar *skills* para tarefas repetitivas (ex.: "criar novo caso de uso com teste", "criar migration").
- Registrar aqui toda decisão relevante tomada com apoio de IA.
- Futuro: agente que sugere escalação equilibrada com base no histórico e nas posições.

---

## ADR-007 — Nome do projeto: "Cadê a escalação?"

**Status:** Aceita · **Data:** 2026-09-27

**Contexto**
Toda semana, no grupo do WhatsApp, alguém pergunta: *"Professor Fulano, cadê a escalação?"*.

**Decisão**
- Marca: **Cadê a escalação?**
- Código: `CadeAEscalacao` (namespaces, projetos e solution); `cadeaescalacao` / `cade-a-escalacao` em minúsculas (pasta do front, imagem Docker, repositório, URL).

**Alternativas**
- Resenha, Escalaí, Pelaí, Jogaí, TôDentro, Rachaí — descartadas: o nome tinha que vir da piada real do grupo.
- CadeEscalacao / CadeAEscala — mais curtos, mas perdem a frase original.

**Consequências**
- ✅ Quem é da pelada entende na hora.
- ⚠️ Identificador longo no código — aceitável.

---

## ADR-008 — Fluxo de trabalho no Git

**Status:** Aceita · **Data:** 2026-09-28

**Decisão**
- Repositório no GitHub: `cade-a-escalacao`.
- Duas branches permanentes:
  - **`main`** — versão estável; é o que vai para produção (deploy no Render a partir da etapa 10).
  - **`dev`** — integração do dia a dia.
- Cada etapa do roteiro nasce da `dev` numa branch curta (`etapa-03-testes`, `etapa-04-dominio`...) e volta para a `dev` por **Pull Request**, mesmo trabalhando sozinho.
- Quando a `dev` estiver estável, um PR `dev → main` publica a versão.
- Mensagens de commit no padrão **Conventional Commits**: `feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`, `ci:`.

**Alternativas**
- Commits direto na `main` — mais rápido, mas perde o histórico de PRs e o treino do fluxo usado em equipes.
- Só `main` + branches curtas (trunk-based) — mais simples; descartado para ter um ambiente de integração separado do que vai para produção.
- Git Flow completo (`release/*`, `hotfix/*`) — pesado demais para um projeto com um desenvolvedor.

**Consequências**
- ✅ Cada PR documenta uma etapa e, a partir da etapa 9, passa pelo CI antes do merge.
- ✅ A `main` só recebe código já integrado na `dev`.
- ⚠️ Um PR a mais (`dev → main`) por versão publicada.

---

## ADR-009 — Código em inglês

**Status:** Aceita · **Data:** 2026-09-29

**Contexto**
O código começou com nomes em português (`Pontuacao`, `Temporada`...). Inglês é o padrão do mercado, combina com as APIs do .NET e do React e facilita o uso de ferramentas e exemplos.

**Decisão**
- Todo o código em **inglês**: classes, métodos, propriedades, variáveis, testes, pastas de código, rotas da API e mensagens de exceção.
- Documentação (`README.md`, `docs/`) continua em português.
- Nomes de teste: `Method_Scenario_ExpectedResult` (ex.: `Create_ZeroDuration_Throws`).
- Glossário do domínio:

| Português (grupo) | Inglês (código) |
|---|---|
| Grupo / horário | Group |
| Presidente | President |
| Professor | Coach |
| Mensalista | Regular (`PlayerType.Regular`) |
| Suplente | Substitute (`PlayerType.Substitute`) |
| Dupla | Pair |
| Membro do grupo | GroupMember |
| Rodízio de professores | CoachRotation |
| Lista semanal | WeeklyList |
| Escalação | Lineup |
| Partida / jogo | Match |
| Temporada | Season |
| Pontuação | Scoring |
| Futebol society | SevenASide |
| Goleiro / zaga / meio / ataque | Goalkeeper / Defender / Midfielder / Forward |

**Consequências**
- ✅ Código consistente com o ecossistema .NET.
- ⚠️ O glossário precisa ser mantido quando surgirem termos novos.

---

## ADR-010 — Modelagem do domínio: agregados e IDs fortemente tipados

**Status:** Aceita · **Data:** 2026-09-29

**Contexto**
A etapa 4 cria as entidades. É preciso decidir onde fica cada regra, como as entidades se referenciam e qual tipo de ID usar antes de chegar ao banco (etapa 5).

**Decisão**
- Agregados (cada um salvo inteiro e com as próprias regras):
  - `Player`: a pessoa (nome).
  - `Group`: formato, dia, presidente, membros, duplas e rodízio de professores.
  - `WeeklyList`: inscrições e vagas da semana.
  - `Lineup`: escalação do professor.
  - `Match`: placar e resultado de cada jogador.
- Agregados se referenciam **por ID**, nunca por propriedade de navegação.
- Mensalista/suplente é do **vínculo com o grupo** (`GroupMember`), não do `Player`: a mesma pessoa pode ser mensalista no society e suplente no futsal.
- IDs fortemente tipados: `readonly record struct PlayerId(Guid Value)`, `GroupId`... gerados com `Guid.CreateVersion7()` (ordenáveis por data, bons para índice no Postgres).
- O domínio não acessa repositório. Dados de fora (ex.: presenças na temporada) chegam como parâmetro.

**Alternativas**
- `Guid` puro — mais simples, mas o compilador não impede trocar o ID de um jogador pelo de um grupo.
- `int` identity — o ID só existe depois de salvar e expõe a quantidade de registros na API.
- `PlayerType` no `Player` — não atende quem joga nos dois grupos com tipos diferentes.

**Consequências**
- ✅ Erros de ID trocado viram erro de compilação.
- ✅ Regras testáveis sem banco.
- ⚠️ Na etapa 5 cada ID precisa de um `ValueConverter` no EF Core.

---

## ADR-011 — Violação de regra de negócio com `DomainException`

**Status:** Aceita · **Data:** 2026-09-29

**Contexto**
O domínio precisa sinalizar dois tipos de erro: argumento inválido (nome vazio) e regra de negócio quebrada (jogador já está em uma dupla). A API vai responder de forma diferente a cada um.

**Decisão**
- Argumento inválido: família `ArgumentException` (como já fazem `Season` e `MatchFormat`).
- Regra de negócio: `DomainException` (`Domain/Common`), com mensagem em inglês.
- Na etapa 6 a API converte `DomainException` em **422 Unprocessable Entity** (Problem Details).

**Alternativas**
- Result pattern (`Result<T>`) — explícito, mas adiciona muito código em um domínio pequeno; pode ser reavaliado depois.
- Uma exceção por regra — granular demais por enquanto.

**Consequências**
- ✅ Testes simples: `Should.Throw<DomainException>`.
- ⚠️ Exceções não aparecem na assinatura do método; os testes documentam quando cada uma ocorre.

---

## Modelo

```markdown
## ADR-XXX — Título

**Status:** Proposta · **Data:** AAAA-MM-DD

**Contexto**
**Decisão**
**Alternativas**
**Consequências**
```
