# 🗺️ Roteiro

Plano de construção do **Cadê a escalação?**, dividido em etapas. Cada etapa:

- é desenvolvida numa branch própria (`etapa-XX-nome`), criada a partir da `dev`;
- entra na `dev` por Pull Request (ver ADR-008 em [`DECISOES.md`](DECISOES.md));
- tem um chat próprio no projeto "Futebol" do Claude.

**Legenda:** ✅ concluída · 🚧 em andamento · ⬜ não iniciada

| # | Etapa | Status |
|---|---|---|
| 1 | Estrutura da solution | ✅ |
| 2 | Git, GitHub e documentação | ✅ |
| 3 | Projeto de testes e primeiras regras de domínio | ✅ |
| 4 | Modelagem do domínio | ✅ |
| 5 | Banco de dados | ✅ |
| 6 | API | ✅ |
| 7 | Autenticação e papéis | 🚧 |
| 8 | Telas em React | ⬜ |
| 9 | CI com GitHub Actions | ⬜ |
| 10 | Docker e deploy | ⬜ |
| 11 | IA no desenvolvimento | ⬜ |
| 12 | Extras | ⬜ |

---

## 1. Estrutura da solution ✅
**Objetivo:** criar a solution com API e front-end juntos, separada em camadas.

- [x] Template "React and ASP.NET Core" (TypeScript) no Visual Studio 2026, .NET 10
- [x] Class libraries `Domain`, `Application` e `Infrastructure`
- [x] Referências entre camadas (dependências apontando para o Domain)
- [x] Pastas de solução `src`, `tests` e `docs`

**Estudo:** Clean Architecture, formato `.slnx`, template SPA do Visual Studio.

## 2. Git, GitHub e documentação ✅
**Objetivo:** versionar o projeto e registrar as decisões desde o início.

- [x] Repositório `cade-a-escalacao` no GitHub
- [x] Branches `main` e `dev`
- [x] `README.md` e `docs/DECISOES.md` (ADRs)

**Estudo:** ADRs, fluxo de branches com Pull Requests, Conventional Commits.

## 3. Projeto de testes e primeiras regras de domínio ✅
**Objetivo:** ter testes automatizados desde a primeira regra de negócio.

- [x] Projeto `CadeAEscalacao.Domain.Tests` (NUnit + Shouldly)
- [x] Regras: pontuação (3/1/0), formato do jogo (7+1, 5+1), temporada configurável, posições
- [x] Código renomeado para inglês (ADR-009)

**Estudo:** NUnit (`[Test]`, `[TestCase]`), Shouldly, nomenclatura de testes.

## 4. Modelagem do domínio ✅
**Objetivo:** levar as regras da pelada para o Domain, com testes.

> A primeira parte foi feita junto com a etapa 3 (branch `etapa-03-testes`). O restante é concluído na branch `etapa-05-banco-de-dados`, antes do mapeamento no banco.

- [x] Regra confirmada: quem escolhe o time após a escalação é a dupla do professor
- [x] `Group` — grupo/horário, com formato do jogo, dia, membros e presidente
- [x] `Player` — nome; o tipo (mensalista/suplente) fica em `GroupMember`, porque muda por grupo; as posições são informadas na lista semanal
- [x] `Pair` — duplas definidas pelo presidente
- [x] Rodízio de `Coach` (professor) — ordem definida pelo presidente
- [x] `WeeklyList` — confirmar, sair, entrar como suplente; limite de vagas; prioridade do mensalista
- [x] `Lineup` — escalação dos dois times, sem o professor e sem a dupla dele; escolha de time pela dupla
- [x] `Match` — resultado e pontos
- [x] Temporada do grupo e ranking (pontos e presenças)

**Estudo:** entidades, value objects, agregados, invariantes, Domain-Driven Design tático, TDD.

## 5. Banco de dados ✅
**Objetivo:** persistir o domínio no Supabase (decisões na ADR-012).

- [x] Criar projeto `cadeaescalacao-dev` no Supabase (plano gratuito, Session pooler)
- [x] Connection string via User Secrets do Server
- [x] Pacotes: Npgsql EF Core, `EFCore.NamingConventions`, `Microsoft.EntityFrameworkCore.Design`, `dotnet-ef`
- [x] `AppDbContext` e mapeamentos (`IEntityTypeConfiguration<T>`) no Infrastructure
- [x] Migration `InitialCreate` gerada, revisada e aplicada no banco de dev
- [ ] Testes de integração com Testcontainers (PostgreSQL) — adiados para a etapa 10 (Docker)

**Estudo:** EF Core, Npgsql, migrations, owned types, complex types, value converters, concorrência otimista.

## 6. API ✅
**Objetivo:** expor os casos de uso por HTTP.

- [x] Decidir: Controllers ou Minimal APIs → Minimal APIs (ADR-013)
- [x] Casos de uso na Application (`Lists/`, `Lineups/`, `Matches/`, `Rankings/`), mais cadastro mínimo (`Players/`, `Groups/`, `Seasons/`)
- [x] Endpoints: lista semanal, escalação, resultado, ranking (e cadastro mínimo)
- [x] Validação de entrada (`AddValidation`) e tratamento de erros (ProblemDetails: 400, 404, 409, 422)
- [x] Documentação OpenAPI (`/openapi/v1.json` em desenvolvimento)
- [x] Testes de integração com `WebApplicationFactory` e mocks com NSubstitute (`CadeAEscalacao.Server.Tests`)

**Estudo:** ASP.NET Core, injeção de dependência, vertical slices, testes de API.

## 7. Autenticação e papéis 🚧
**Objetivo:** cada pessoa entra com sua conta e só faz o que o papel permite (ADR-014 a ADR-017).

- [x] Decidir: Supabase Auth ou login na própria API → login na API com ASP.NET Core Identity, sem senha (ADR-014)
- [x] Login por código de 6 dígitos no e-mail (magic link) e sessão com JWT próprio + refresh token (ADR-015)
- [ ] Login com Google — adiado para junto da etapa 8 (ADR-014)
- [x] Validação do JWT na API (`AddJwtBearer`) e perfil do jogador em `/api/me`
- [x] Papéis: presidente, professor, jogador — calculados a partir do grupo e da escalação (ADR-016)
- [x] Regras de autorização (ex.: só o professor da vez publica a escalação) e quem age vem do token (ADR-017)
- [x] Testes de API com usuários autenticados (tokens de teste, login com e-mail falso)
- [x] Migration `AddIdentity` aplicada no banco de dev (`--context AuthDbContext`)

**Estudo:** ASP.NET Core Identity, JWT, refresh token, OAuth, autorização baseada em políticas, rate limiting.

## 8. Telas em React ⬜
**Objetivo:** interface usável no celular, que é onde o grupo vive.

- [ ] Login (código por e-mail, com link que já preenche o código)
- [ ] Login com Google (cliente OAuth no Google Cloud Console)
- [ ] Lista semanal (confirmar/sair/suplente)
- [ ] Tela de escalação do professor (filtro por posição)
- [ ] Escolha de time pela dupla
- [ ] Registro de resultado
- [ ] Ranking da temporada
- [ ] Testes com Vitest + React Testing Library

**Estudo:** React Router, gerenciamento de estado e chamadas à API, layout mobile-first.

## 9. CI com GitHub Actions ⬜
**Objetivo:** todo PR roda build e testes automaticamente.

- [ ] Workflow de build e testes (.NET e React)
- [ ] PR para `dev`/`main` só entra com o CI verde
- [ ] Workflow agendado para manter o Supabase gratuito ativo

**Estudo:** GitHub Actions, proteção de branch.

## 10. Docker e deploy ⬜
**Objetivo:** sistema no ar, de graça.

- [ ] `Dockerfile` multi-stage (React + .NET)
- [ ] Deploy no Render a partir da `main`
- [ ] Variáveis de ambiente (connection string, `Jwt:SigningKey`)
- [ ] Provedor de e-mail real para o código de login (Resend, Brevo ou SMTP)
- [ ] Testes E2E com Playwright nos fluxos principais

**Estudo:** Docker, imagens multi-stage, deploy contínuo, hospedagem gratuita.

## 11. IA no desenvolvimento ⬜
**Objetivo:** usar IA como parte do processo e do produto.

- [ ] `CLAUDE.md` com as convenções do projeto
- [ ] Skills para tarefas repetitivas (novo caso de uso com teste, nova migration)
- [ ] Agente que sugere escalação equilibrada a partir do histórico e das posições

**Estudo:** skills, agentes, uso de LLMs em funcionalidades do produto.

## 12. Extras ⬜
- [ ] Importar a planilha atual do Google Sheets (pontos e presenças)
- [ ] Compartilhar lista e escalação no WhatsApp
- [ ] Ideias futuras: avaliação de jogadores, apostas de brincadeira
