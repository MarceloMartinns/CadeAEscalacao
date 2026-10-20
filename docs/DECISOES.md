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

**Status:** Aceita, com a parte de autenticação substituída pela ADR-014 · **Data:** 2026-09-27

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

## ADR-012 — Banco de dados: Supabase, EF Core e mapeamento dos agregados

**Status:** Aceita · **Data:** 2026-10-15

**Contexto**
A etapa 5 persiste o domínio (ADR-010) no PostgreSQL do Supabase (ADR-003). O Docker ainda não está instalado, então não há Postgres local. Os agregados têm IDs fortemente tipados, coleções privadas e value objects, e o domínio não deve depender do EF Core.

**Decisão**
- **Banco de desenvolvimento no Supabase**, projeto `cadeaescalacao-dev` (região São Paulo). O de produção é criado na etapa 10; o plano gratuito permite 2 projetos ativos.
- **Conexão pelo Session pooler** (porta 5432, usuário `postgres.<ref>`). A conexão direta só funciona por IPv6 no plano gratuito, e o Transaction pooler (6543) não suporta prepared statements.
- **Schema `public`**, com "Automatically expose new tables" desmarcado: as tabelas não ficam acessíveis pela Data API do Supabase. O React nunca acessa o banco direto.
- **Connection string em User Secrets** do `CadeAEscalacao.Server`, chave `ConnectionStrings:Default`, no formato Npgsql (`Host=...;Port=5432;Database=postgres;Username=postgres.<ref>;Password=...;SSL Mode=Require`). Nunca no `appsettings.json`.
- **EF Core 10 + Npgsql**, nomes em `snake_case` (`EFCore.NamingConventions`). Mapeamento em `Infrastructure/Persistence`, um `IEntityTypeConfiguration<T>` por agregado:
  - IDs fortemente tipados → `uuid`, com um `ValueConverter` por tipo registrado em `ConfigureConventions`. As chaves usam `ValueGeneratedNever` (o domínio gera Guid v7).
  - `GroupMember`, `Pair` e `WeeklyListEntry` → *owned collections* em tabelas filhas (`group_members`, `group_pairs`, `weekly_list_entries`), com chave composta (agregado + jogador).
  - `MatchFormat` → *complex type* (colunas `players_per_team` e `bench_players_per_team` em `groups`).
  - Listas ordenadas de jogadores (rodízio de professores, jogadores e times de `Lineup`/`Match`) → arrays `uuid[]`, com converter e `ValueComparer`.
  - Chaves estrangeiras entre agregados no banco (sem propriedades de navegação no domínio), todas com `Restrict`.
  - Índices únicos: uma lista por grupo e data (`weekly_lists`), uma escalação por lista, uma partida por escalação.
- **Concorrência otimista com uma coluna `version`** (propriedade de sombra) em cada raiz de agregado. O `AppDbContext` incrementa a versão sempre que o agregado ou uma das suas tabelas filhas muda. Assim, duas inscrições simultâneas na mesma lista não passam ambas: a segunda recebe `DbUpdateConcurrencyException`.
- **Ajustes mínimos no Domain:** construtor privado sem parâmetros em `Group` (o EF não passa complex type no construtor) e as listas de IDs de `Group`, `Lineup` e `Match` deixam de ser `readonly` (o EF as atribui ao carregar).
- **Testes de integração com Testcontainers adiados** até a instalação do Docker (etapa 10). Até lá, as migrations são validadas aplicando no banco de dev.

**Alternativas**
- Postgres local via Docker — mais isolado, mas o Docker ainda não está instalado.
- Schema próprio (`app`) — descartado: com a exposição automática desligada, o `public` já não aparece na Data API.
- Coluna `xmin` do Postgres como token de concorrência — não detecta mudanças só nas tabelas filhas (uma nova inscrição insere em `weekly_list_entries` sem alterar a linha de `weekly_lists`).
- Coleções em JSON (`jsonb`) — menos tabelas, mas sem chave estrangeira para `players` e com consultas mais difíceis.
- Tabela para o rodízio com coluna de posição — mais normalizada, mas o array já guarda a ordem e o rodízio só é lido inteiro.

**Consequências**
- ✅ O domínio continua sem referência ao EF Core; o mapeamento fica todo no Infrastructure.
- ✅ Integridade garantida no banco (chaves estrangeiras e índices únicos) além das regras do domínio.
- ⚠️ O projeto de dev pausa após 7 dias sem uso; reativar pelo painel do Supabase.
- ⚠️ A Application precisa tratar `DbUpdateConcurrencyException` (tentar de novo ou devolver 409) na etapa 6.
- ⚠️ Arrays `uuid[]` não têm chave estrangeira; a consistência dos IDs nos times depende do domínio.

---

## ADR-013 — API com Minimal APIs organizadas por funcionalidade

**Status:** Aceita · **Data:** 2026-10-15

**Contexto**
A etapa 6 expõe os casos de uso por HTTP. O template do Visual Studio veio com Controllers (`AddControllers`, `WeatherForecastController`), que é o estilo que o desenvolvedor sempre usou. O projeto tem objetivo de estudo e a `Application` é organizada por funcionalidade (*vertical slices*, ADR-001). Nesta etapa são poucos endpoints: lista semanal, escalação, resultado e ranking.

**Decisão**
- Endpoints com **Minimal APIs**.
- Convenção de organização, para não acumular tudo no `Program.cs`: uma pasta por funcionalidade no `Server` (`Endpoints/Lists`, `Endpoints/Lineups`, `Endpoints/Matches`, `Endpoints/Rankings`), cada uma com um método de extensão `MapXxxEndpoints()` que cria um `MapGroup` (ex.: `/api/lists`) e registra as rotas dela. O `Program.cs` só chama esses métodos.
- Respostas com **`TypedResults`** (`Results<Ok<T>, NotFound>`), que já descrevem os códigos de resposta no documento OpenAPI.
- Validação de entrada com a validação nativa do .NET 10 (`AddValidation()`, DataAnnotations nos DTOs de requisição), que devolve 400 com ProblemDetails.
- Erros convertidos em ProblemDetails por um `IExceptionHandler` global: `DomainException` → 422 (ADR-011) e `DbUpdateConcurrencyException` → 409 (ADR-012).

**Alternativas**
- Controllers — mais familiares para o desenvolvedor e ainda comuns em projetos de empresa, com estrutura imposta pelo framework (`[ApiController]`, atributos de rota, validação automática). Descartado: mais cerimônia (`[ProducesResponseType]` em cada ação) e menos alinhado aos vertical slices e ao objetivo de estudar a abordagem que a Microsoft recomenda para projetos novos.

**Consequências**
- ✅ Cada funcionalidade fica num lugar só, com pouco código repetitivo.
- ✅ OpenAPI correto sem anotações extras, graças aos `TypedResults`.
- ⚠️ O framework não impõe estrutura: a convenção acima precisa ser seguida à mão.
- ⚠️ A validação embutida só existe a partir do .NET 10; exemplos antigos da internet usam filtros ou FluentValidation.
- ⚠️ Curva de aprendizado para quem vem de Controllers.

---

## ADR-014 — Login feito pela própria API, com ASP.NET Core Identity e sem senha

**Status:** Aceita · **Data:** 2026-10-15

**Contexto**
A ADR-003 previa o Supabase Auth: o React faria o login no Supabase e a API só validaria o token. Na etapa 7 ficou claro que isso prende o projeto ao Supabase: trocar de banco ou de hospedagem obrigaria a trocar também o login e a religar a conta de cada pessoa. A pelada tem poucas dezenas de pessoas, que entram pelo celular, e o objetivo de estudo inclui autenticação.

**Decisão**
- A **API faz o login**, com **ASP.NET Core Identity** (`AddIdentityCore`). O Supabase passa a ser só o banco (ADR-003, parte de banco, continua valendo).
- **Sem senha:** o login é por **magic link no e-mail**. O e-mail traz um **código de 6 dígitos** e, a partir da etapa 8 (quando existir a tela de login), um link que já preenche o código. O código vale para quem abre o e-mail em outro app ou aparelho. Ele é gerado pelo provedor de token de e-mail do Identity e vale poucos minutos; depois de 5 códigos errados a conta fica bloqueada por um tempo (*lockout*).
- **Login com Google** fica para depois (junto da etapa 8), porque exige um cliente OAuth no Google Cloud Console e termina com um redirecionamento para a tela de login. Quando entrar, usa a tabela `user_logins` do Identity.
- **Tabelas do Identity num `AuthDbContext` separado** (`IdentityUserContext<AppUser, Guid>`, sem as tabelas de papéis, ver ADR-016), no mesmo banco e no Infrastructure: `users`, `user_logins`, `user_tokens`, `user_claims` e `refresh_tokens` (ADR-015). O `AppDbContext` continua só com o domínio.
- **Ligação com o domínio:** `users.player_id` (opcional, único, com chave estrangeira para `players`, escrita à mão na migration). O domínio não conhece o usuário.
- **Cadastro:** o primeiro código válido cria o usuário, já com o e-mail confirmado. Depois, `POST /api/me` com o nome cria o `Player` e o liga ao usuário. O presidente adiciona ao grupo quem já se cadastrou; convite para quem ainda não tem conta fica para depois.
- **E-mail** atrás de uma abstração `IEmailSender`. Em desenvolvimento, a implementação só escreve o e-mail no console da API. O provedor real (Resend, Brevo ou SMTP) entra na etapa 10.
- **Proteção:** limite de requisições (`AddRateLimiter`) nos endpoints `/api/auth`, e o pedido de código sempre responde **202**, sem revelar se o e-mail já existe.

**Alternativas**
- Supabase Auth (ADR-003) — menos código e sem e-mail para enviar, mas prende o login ao Supabase.
- Provedor externo trocável por OpenID Connect (Auth0, Keycloak, Entra...) — a API só valida o token, mas o login continua fora do projeto, e trocar de provedor muda o `sub` de todo mundo.
- Login com senha — exige guardar senha, "esqueci a senha" e regras de força de senha; o magic link resolve o mesmo problema com menos risco.
- Identity no próprio `AppDbContext` — um contexto só, mas mistura autenticação com o domínio (e com a coluna `version`) e dificulta trocar só o banco de usuários nos testes.

**Consequências**
- ✅ Login independente do banco e da hospedagem: só depende do EF Core e de um serviço de e-mail.
- ✅ Nenhuma senha guardada.
- ⚠️ A segurança do login passa a ser responsabilidade do projeto (códigos, bloqueio, limite de requisições).
- ⚠️ Duas pastas de migrations (`dotnet ef ... --context AuthDbContext`).
- ⚠️ Criar o `Player` e ligar ao usuário envolve os dois contextos, sem transação única: primeiro o `Player` é salvo, depois o usuário é ligado a ele (409 se já houver jogador ligado). Se a segunda parte falhar, sobra um `Player` sem conta e sem grupo, que não causa problema.
- ⚠️ Em produção é preciso um serviço de e-mail (etapa 10).

---

## ADR-015 — Sessão com JWT próprio e refresh token

**Status:** Aceita · **Data:** 2026-10-15

**Contexto**
Depois do login (ADR-014), o React precisa provar a cada requisição quem é a pessoa. O mesmo mecanismo precisa funcionar no teste manual pelo `.http` e nos testes automatizados.

**Decisão**
- **Access token:** JWT emitido pela API, válido por **30 minutos**, com `sub` (id do usuário) e `player_id` (quando já há cadastro). Validado com `AddJwtBearer` (emissor, audiência, validade e assinatura).
- **Assinatura HMAC (HS256)** com chave de 256 bits em User Secrets (`Jwt:SigningKey`), e em variável de ambiente na produção. Só a API emite e valida, então não é preciso chave pública.
- **Refresh token:** valor aleatório opaco, válido por **30 dias**, trocado a cada uso (*rotation*). A tabela `refresh_tokens` guarda só o **hash**. Se um refresh token já usado aparecer de novo, todos os tokens daquele login são revogados.
- Endpoints: `POST /api/auth/email/start`, `POST /api/auth/email/verify` (devolve `accessToken`, `expiresIn` e `refreshToken`), `POST /api/auth/refresh` e `POST /api/auth/logout`. O `POST /api/me` devolve um access token novo, já com `player_id` (o refresh token continua o mesmo; os próximos access tokens também trazem o `player_id`, porque ele é lido do banco a cada refresh).
- Onde o React guarda os tokens é decidido na etapa 8. Dá para mover o refresh token para um cookie HttpOnly sem mudar o resto.

**Alternativas**
- Cookie de autenticação do Identity — o mais seguro para uma SPA na mesma origem (o JavaScript não lê o cookie), mas exige proteção contra CSRF e complica o teste pelo `.http`. Pode ser reavaliado na etapa 8.
- `MapIdentityApi` — pronto, mas os tokens não são JWT e o login dele exige senha.
- Assinatura assimétrica (ECDSA/RSA, com JWKS) — útil quando outro serviço valida o token; aqui só a API valida.
- Só access token, sem refresh — mais simples, mas obriga a fazer login de novo a cada expiração.

**Consequências**
- ✅ Requisições sem consulta ao banco para saber quem é a pessoa (`player_id` vem no token).
- ✅ Testes geram tokens reais com a chave de teste.
- ⚠️ Um access token roubado vale até expirar (30 min); o logout só revoga o refresh token.
- ⚠️ A chave de assinatura é um segredo: nunca no `appsettings.json` nem no Git.

---

## ADR-016 — Papéis vêm do grupo, e a autorização fica nos casos de uso

**Status:** Aceita · **Data:** 2026-10-15

**Contexto**
O roteiro prevê três papéis: presidente, professor e jogador. Eles não são da pessoa, e sim do grupo e da semana: quem é presidente no society pode ser só jogador no futsal, e o professor da vez muda toda semana pelo rodízio.

**Decisão**
- **Nenhum papel no token nem nas tabelas do Identity.** O papel é calculado a partir dos dados do domínio:
  - **presidente** = `Group.PresidentId`;
  - **professor da vez** = `Lineup.CoachId`;
  - **dupla do professor** = `Lineup.PartnerId`;
  - **jogador** = membro do grupo (`GroupMember`).
- **No Server, por políticas:** a política padrão exige usuário autenticado em todo `/api` (sem token ou token inválido → **401**); a política `RegisteredPlayer` exige `player_id` no token (todos os endpoints, menos `/api/auth` e `/api/me`).
- **Nos casos de uso, as regras de papel.** Eles já carregam o grupo e a escalação, então verificam o papel e lançam `ForbiddenException` (Application/Common), que a API converte em **403** com ProblemDetails.
- Permissões:

| Ação | Quem |
|---|---|
| Criar grupo | qualquer jogador cadastrado (vira presidente) |
| Ver grupo, lista, escalação, partida e ranking | membros do grupo |
| Adicionar membro, criar dupla, mudar rodízio, criar temporada | presidente |
| Abrir, fechar e cancelar lista; iniciar escalação; escolher a dupla do dia | presidente |
| Confirmar presença, mudar posições, sair da lista | o próprio membro |
| Montar os times (publicar a escalação) | professor da vez |
| Escolher o time | dupla do professor |
| Registrar o resultado | professor da vez ou presidente |
| Corrigir o placar | presidente |

**Alternativas**
- Papéis no token ou `IdentityRole` — não representam papel por grupo nem o professor que muda toda semana.
- Tudo com `IAuthorizationHandler` baseado em recurso no Server — mais próximo do ASP.NET, mas carregaria cada agregado duas vezes e espalharia regra de negócio fora da Application.
- Regras de papel no Domain (ex.: `lineup.AssignTeams(actorId, ...)`) — possível, mas mudaria várias assinaturas e testes do domínio sem ganho agora.

**Consequências**
- ✅ Regras de permissão testáveis sem HTTP, junto com o caso de uso.
- ✅ A mesma pessoa tem papéis diferentes em grupos diferentes, sem nada extra.
- ⚠️ Cada caso de uso novo precisa lembrar de verificar o papel; os testes de API cobrem os 403.

---

## ADR-017 — Quem faz a ação vem do token, não da rota nem do corpo

**Status:** Aceita · **Data:** 2026-10-15

**Contexto**
Até a etapa 6, quem agia era identificado por um `playerId` na rota ou no corpo (ex.: `SignUpRequest.PlayerId`, `CreateGroupRequest.PresidentId`). Com login, isso permitiria agir em nome de outra pessoa.

**Decisão**
- A Application recebe quem age por uma abstração `ICurrentUser` (em `Abstractions/`), implementada no Server a partir do `player_id` do token.
- O `playerId` **sai quando representa quem age**:
  - `POST /api/lists/{id}/entries` confirma a pessoa logada (sem `playerId` no corpo);
  - `PUT` e `DELETE /api/lists/{id}/entries/{playerId}` viram `/api/lists/{id}/entries/me`;
  - `CreateGroupRequest.PresidentId` sai: quem cria o grupo vira o presidente;
  - `POST /api/players` sai: o cadastro é feito por `POST /api/me`.
- O `playerId` **fica quando representa o alvo da ação**: adicionar membro, criar dupla, rodízio, dupla do dia e montagem dos times.

**Alternativas**
- Manter o `playerId` no corpo e conferir se é igual ao do token — redundante e fácil de esquecer.
- Passar quem age como parâmetro em cada `HandleAsync` — explícito, mas repete o mesmo parâmetro em quase todos os casos de uso.

**Consequências**
- ✅ Não dá para agir em nome de outra pessoa.
- ⚠️ As rotas mudam; o `.http` e os testes de API são atualizados.

---

## ADR-018 — Front-end: React Router, TanStack Query, Mantine e pastas por funcionalidade

**Status:** Aceita · **Data:** 2026-10-19

**Contexto**
A etapa 8 transforma o template do Visual Studio (ADR-002) nas telas da pelada. O grupo usa o app no celular, o desenvolvedor está aprendendo React e o objetivo de estudo inclui roteamento, chamadas à API e layout mobile-first. As telas são, na maior parte, listas, formulários curtos e botões de ação.

**Decisão**
- **Roteamento:** React Router (v8) no modo biblioteca (`createBrowserRouter`), com as rotas `/login`, `/perfil`, `/grupos`, `/grupos/novo`, `/grupos/:groupId`, `/grupos/:groupId/membros`, `/listas/:listId`, `/escalacoes/:lineupId`, `/partidas/:matchId` e `/temporadas/:seasonId/ranking`. A API já devolve o `index.html` para qualquer rota desconhecida (`MapFallbackToFile`).
- **Estado do servidor:** TanStack Query (cache, carregamento, erros e invalidação depois de cada ação). Não há biblioteca de estado global: o que vem da API fica no TanStack Query, e o resto é estado local dos componentes.
- **Componentes e CSS:** Mantine, que já traz os componentes que as telas usam (`PinInput` para o código, `Chip` para as posições, `SegmentedControl`, `AppShell`, notificações) e funciona bem no celular.
- **Pastas por funcionalidade** (como os *vertical slices* da ADR-001):
  - `src/app` — rotas, providers e layout;
  - `src/api` — cliente HTTP, tipos gerados (ADR-019) e sessão (ADR-020);
  - `src/features/{auth,groups,lists,lineups,matches,rankings}` — telas e hooks de cada funcionalidade;
  - `src/components` — componentes compartilhados.
- **Testes** com Vitest, React Testing Library, `user-event` e jsdom (ADR-004), ao lado do arquivo testado (`*.test.tsx`). A API é simulada com **MSW** (Mock Service Worker), que intercepta as requisições HTTP: o teste exercita o mesmo cliente que a tela usa.
- **Idioma:** textos da tela em português, código em inglês (ADR-009). A tradução dos valores da API (ex.: `Goalkeeper` → "Goleiro") fica num único arquivo. Datas no formato brasileiro.

**Alternativas**
- TanStack Router — rotas com tipos mais fortes, mas menos difundido; React Router é o mais usado e o previsto no roteiro.
- `fetch` + `useEffect` sem biblioteca — menos dependências, mas cada tela repetiria carregamento, erro e recarga dos dados.
- Tailwind CSS + shadcn/ui — mais flexível e muito usado, mas exige aprender Tailwind junto com React; pode ser reavaliado depois.
- Mocks do cliente HTTP com `vi.mock` — mais simples, mas não testa o cliente de verdade.

**Consequências**
- ✅ Cada funcionalidade fica numa pasta só, do hook à tela e ao teste.
- ✅ Componentes prontos para celular, sem escrever CSS do zero.
- ⚠️ Mais dependências no `package.json` (React Router, TanStack Query, Mantine, MSW).
- ⚠️ O visual fica com a cara do Mantine; personalizar além do tema dá trabalho.

---

## ADR-019 — Tipos e cliente da API gerados a partir do OpenAPI

**Status:** Aceita · **Data:** 2026-10-19

**Contexto**
O React precisa dos formatos de requisição e resposta da API. A API já descreve tudo no documento OpenAPI (ADR-013), com os enums como texto. Tipos escritos à mão ficariam desatualizados a cada mudança na API.

**Decisão**
- O `dotnet build` do Server grava o documento OpenAPI num arquivo, com o pacote `Microsoft.Extensions.ApiDescription.Server`. Assim não é preciso subir a API para gerar os tipos.
- O comando `npm run gen:api` usa o **`openapi-typescript`** para gerar `src/api/schema.d.ts` a partir desse arquivo. O arquivo gerado vai para o Git, para o `npm run build` funcionar sem o .NET.
- As chamadas usam o **`openapi-fetch`**, que confere a rota, os parâmetros e o corpo pelos tipos gerados. A sessão (ADR-020) entra na função `fetch` do cliente (`src/api/client.ts`), que põe o token e renova a sessão no 401.
- Quando a API mudar: `dotnet build` → `npm run gen:api` → o `tsc` aponta o que as telas precisam ajustar.

**Alternativas**
- Tipos escritos à mão — nenhuma ferramenta a mais, mas ficam desatualizados sem aviso.
- Gerador de cliente completo (NSwag, Orval, Kiota) — gera muito código e, no caso do Orval, os hooks do TanStack Query; é mais difícil de entender ao estudar.

**Consequências**
- ✅ Mudança na API vira erro de compilação no front, não erro em produção.
- ⚠️ É preciso lembrar de rodar `npm run gen:api` depois de mudar a API (a CI da etapa 9 pode conferir isso).
- ⚠️ Os tipos refletem o OpenAPI: se um endpoint não declarar uma resposta, o tipo também não terá. A posição (`Position`) é um enum de flags e aparece só como texto; o front define os valores dela à mão, em `src/app/labels.ts`.
- ⚠️ Para os números virem como `number` (e não `number | string`), a API passou a aceitar números só como números (`JsonNumberHandling.Strict`).
- ⚠️ O `openapi-typescript` 7 declara compatibilidade só com o TypeScript 5. O `package.json` tem um `overrides` que o faz usar o TypeScript 6 do projeto; rever quando sair uma versão nova.
- ⚠️ Durante o build, o `Program.cs` roda só para gravar o documento, sem User Secrets: ele usa valores provisórios para a connection string e a chave quando quem roda é a ferramenta `GetDocument.Insider`.

---

## ADR-020 — Sessão no navegador: access token em memória e refresh token em cookie HttpOnly

**Status:** Aceita · **Data:** 2026-10-19 · Completa a ADR-015

**Contexto**
A ADR-015 deixou para a etapa 8 onde o React guarda os tokens. Um token no `localStorage` pode ser lido por qualquer script que rode na página (XSS). O app e a API ficam na mesma origem (ADR-002).

**Decisão**
- **Access token só em memória** (uma variável do módulo de sessão). Ao fechar ou recarregar a página, ele se perde.
- **Refresh token em cookie** `HttpOnly`, `Secure`, `SameSite=Strict`, com `Path=/api/auth`: o JavaScript não lê o cookie, e o navegador só o envia para os endpoints de login.
- O React envia `"useCookie": true` em `email/verify` e `refresh`: a API grava o refresh token no cookie e devolve `refreshToken: null` no corpo, para que o JavaScript nunca o veja. Sem esse campo, tudo funciona como antes (token no corpo), o que mantém o `.http` e os testes.
- `refresh` e `logout` leem o token do corpo **ou**, se o corpo não trouxer, do cookie. O `logout` e um `refresh` recusado apagam o cookie.
- **Ao abrir o app**, o React chama `POST /api/auth/refresh`: se o cookie for válido, a sessão volta sem novo login.
- **Renovação no 401:** a função `fetch` do cliente (ADR-019) chama `/api/auth/refresh` e repete a requisição uma vez. Se várias requisições falharem juntas, todas esperam o **mesmo** refresh (o refresh token é rotativo, e usá-lo duas vezes revogaria a sessão, ADR-015). Se o refresh falhar, a sessão é limpa e a tela vai para `/login`.

**Alternativas**
- Os dois tokens no `localStorage` — mais simples, mas um XSS leva a sessão de 30 dias.
- Cookie de autenticação do Identity em vez de JWT — o mais seguro, mas exige proteção contra CSRF em todos os endpoints e mudaria a ADR-015.
- Access token no `sessionStorage` — sobrevive ao recarregar, mas tem o mesmo risco de XSS; o refresh na abertura já resolve.

**Consequências**
- ✅ Um script malicioso só consegue o access token, que vale no máximo 30 minutos.
- ✅ Sem CSRF: o cookie só vai para `/api/auth`, só na mesma origem (`SameSite=Strict`), e esses endpoints não fazem nada em nome da pessoa além de renovar ou encerrar a sessão.
- ⚠️ Cada abertura do app faz um refresh antes da primeira tela.
- ⚠️ O cookie `Secure` exige HTTPS; em desenvolvimento o Vite já serve em HTTPS (`https://localhost:49409`).

---

## ADR-021 — Endpoints de consulta para as telas

**Status:** Aceita · **Data:** 2026-10-19

**Contexto**
Até a etapa 7 a API só tinha leitura por id, e as respostas traziam apenas ids de jogadores. Com isso, o React não tem como descobrir os grupos da pessoa, a lista da semana, a escalação e a temporada, nem mostrar nomes. Para adicionar um membro, o presidente precisaria saber o id do jogador.

**Decisão**
- `GET /api/groups` — os grupos de que a pessoa logada é membro.
- `GroupResponse` passa a trazer o **nome** de cada membro. As outras telas traduzem id → nome com o grupo, que o TanStack Query guarda em cache (ADR-018), em vez de repetir nomes em todas as respostas.
- `GET /api/groups/{id}/lists` — as 20 listas mais recentes do grupo (cerca de cinco meses de jogos semanais), da mais recente para a mais antiga, cada uma com o `lineupId`, o status da escalação e o `matchId` quando já existirem. É o ponto de partida da tela do grupo. Paginação fica para quando fizer falta.
- `GET /api/groups/{id}/seasons` — as temporadas do grupo, para chegar ao ranking.
- **Exceção: o ranking traz o `groupId` e o nome de cada jogador.** Quem saiu do grupo deixa de estar entre os membros, mas continua no ranking da temporada; sem o nome na resposta, ele apareceria sem nome.
- `POST /api/groups/{id}/members/by-email` — o presidente adiciona por e-mail quem já se cadastrou (`POST /api/me`). A busca do e-mail fica atrás de uma abstração na Application, implementada no Infrastructure com o Identity (ADR-014). E-mail sem cadastro → 404 (só o presidente chega até aqui). Convite para quem ainda não tem conta continua para depois.
- Todas as regras de visibilidade continuam as da ADR-016: só membros veem o grupo, as listas e as temporadas.
- As telas do presidente cobrem o mínimo para o fluxo no celular: criar grupo, membros, duplas, temporada, abrir/fechar/cancelar a lista e iniciar a escalação. Reordenar o rodízio e corrigir o placar continuam só pela API (`.http`).

**Alternativas**
- Nomes em todas as respostas (lista, escalação, partida, ranking) — menos trabalho no front, mas mais consultas e respostas maiores na API.
- Um endpoint "painel" com tudo da semana — menos requisições, mas acopla a API ao desenho de uma tela.
- Buscar jogador por nome — nomes se repetem; o e-mail é único.

**Consequências**
- ✅ As telas navegam só com o que a API devolve.
- ⚠️ Adicionar por e-mail revela ao presidente se aquele e-mail tem conta; aceito, porque só o presidente do grupo chega a esse endpoint.

---

## ADR-022 — Link de login no e-mail

**Status:** Aceita · **Data:** 2026-10-19

**Contexto**
A ADR-014 previu que, a partir da etapa 8, o e-mail traria um link que já preenche o código.

**Decisão**
- O e-mail traz o código de 6 dígitos **e** um link `{App:PublicUrl}/login#email=…&code=…`.
- O código vai no **fragmento** (`#`): o navegador não envia o fragmento ao servidor, então ele não aparece em logs de servidor ou proxy. A tela de login lê o fragmento, preenche os campos, envia o código e limpa o endereço.
- `App:PublicUrl` é configuração: em desenvolvimento, `https://localhost:49409` (Vite); em produção, o endereço do app (etapa 10).
- O código digitado continua valendo para quem abre o e-mail em outro aparelho.

**Alternativas**
- Código na query string (`?code=`) — mais comum, mas vai para os logs.
- Link com token próprio, diferente do código — mais um tipo de token para guardar e validar, sem ganho.

**Consequências**
- ✅ Um toque no e-mail faz o login no mesmo celular.
- ⚠️ O link vale só enquanto o código vale (poucos minutos) e conta como tentativa no bloqueio da ADR-014.

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
