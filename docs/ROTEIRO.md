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
| 4 | Modelagem do domínio | ⬜ |
| 5 | Banco de dados | ⬜ |
| 6 | API | ⬜ |
| 7 | Autenticação e papéis | ⬜ |
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

## 4. Modelagem do domínio ⬜
**Objetivo:** levar as regras da pelada para o Domain, com testes.

- [ ] Confirmar a regra: quem escolhe o time após a escalação (dupla do professor ou do presidente)
- [ ] `Group` — grupo/horário, com formato do jogo e temporada atual
- [ ] `Player` — nome, posições, tipo (mensalista/suplente)
- [ ] `Pair` — duplas definidas pelo presidente
- [ ] Rodízio de `Coach` (professor) — ordem definida pelo presidente
- [ ] `WeeklyList` — confirmar, sair, entrar como suplente; limite de vagas; prioridade do mensalista
- [ ] `Lineup` — escalação dos dois times, sem o professor e sem a dupla dele; escolha de time pela dupla
- [ ] `Match` — resultado e pontos
- [ ] Ranking da temporada (pontos e presenças)

**Estudo:** entidades, value objects, agregados, invariantes, Domain-Driven Design tático, TDD.

## 5. Banco de dados ⬜
**Objetivo:** persistir o domínio no Supabase.

- [ ] Criar projeto no Supabase (plano gratuito)
- [ ] `DbContext` e mapeamentos (`IEntityTypeConfiguration<T>`) no Infrastructure
- [ ] Migrations com EF Core
- [ ] Connection string via `dotnet user-secrets`
- [ ] Testes de integração com Testcontainers (PostgreSQL)

**Estudo:** EF Core, Npgsql, migrations, repositórios, Docker Desktop para os testes.

## 6. API ⬜
**Objetivo:** expor os casos de uso por HTTP.

- [ ] Decidir: Controllers ou Minimal APIs (novo ADR)
- [ ] Casos de uso na Application (`Lists/`, `Lineups/`, `Matches/`, `Rankings/`)
- [ ] Endpoints: lista semanal, escalação, resultado, ranking
- [ ] Validação de entrada e tratamento de erros (ProblemDetails)
- [ ] Documentação OpenAPI
- [ ] Testes de integração com `WebApplicationFactory` e mocks com NSubstitute

**Estudo:** ASP.NET Core, injeção de dependência, vertical slices, testes de API.

## 7. Autenticação e papéis ⬜
**Objetivo:** cada pessoa entra com sua conta e só faz o que o papel permite.

- [ ] Supabase Auth (e-mail/magic link e Google)
- [ ] Validação do JWT na API
- [ ] Papéis: presidente, professor, jogador
- [ ] Regras de autorização (ex.: só o professor da vez publica a escalação)

**Estudo:** JWT, OAuth, autorização baseada em políticas.

## 8. Telas em React ⬜
**Objetivo:** interface usável no celular, que é onde o grupo vive.

- [ ] Login
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
- [ ] Variáveis de ambiente (connection string, chaves do Supabase)
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
