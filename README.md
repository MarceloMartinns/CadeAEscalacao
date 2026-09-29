# ⚽ Cadê a escalação?

> Sistema web para organizar peladas: lista de presença, escalação dos times, resultados e ranking da temporada.

Toda semana alguém pergunta no grupo: *"Professor Fulano, cadê a escalação?"*. O nome do projeto vem daí.

Hoje a organização acontece em uma lista no WhatsApp e em uma planilha do Google Sheets. O **Cadê a escalação?** centraliza tudo em um só lugar.

Além de resolver um problema real, este projeto é um **laboratório de estudos** de C#/.NET, React, testes, DevOps, arquitetura de software, IA aplicada ao desenvolvimento e hospedagem. As decisões técnicas e o motivo de cada uma estão em [`docs/DECISOES.md`](docs/DECISOES.md).

---

## 🧩 O problema

Cada grupo (horário) de futebol segue a mesma rotina:

1. O **presidente** (organizador) abre a lista da semana.
2. Os **jogadores** confirmam presença, saem da lista ou entram como suplentes, informando sua **posição** (goleiro, zaga, meio, ataque — podendo ser mais de uma).
3. No dia do jogo, o **professor** da vez escala os dois times — **sem se incluir e sem incluir sua dupla**.
4. Com a escalação pronta, a dupla escolhe em qual time vai jogar (mecanismo para o professor não montar um time mais forte para si).
5. O jogo acontece e o resultado vira pontos: **vitória 3, empate 1, derrota 0**.
6. Presenças e pontos somam no **ranking da temporada**.

## 👥 Papéis

| Papel | O que faz |
|---|---|
| **Presidente** | Cria o grupo e a temporada, define as duplas e a ordem dos professores, abre as listas |
| **Professor** | Mensalista da vez que monta a escalação dos dois times |
| **Mensalista** | Jogador fixo do grupo, com vaga garantida |
| **Suplente** | Jogador avulso, entra quando sobram vagas |

## 🏟️ Grupos atuais

| Grupo | Modalidade | Jogadores por time | Reservas por time |
|---|---|---|---|
| Terça | Futebol society (sintético) | 7 | 1 |
| Futsal | Futsal (salão) | 5 | 1 |

O formato é configurável por grupo, assim como a **duração da temporada** (padrão: 6 meses).

## 🗺️ Roteiro

O projeto é construído em 12 etapas, da estrutura da solution ao deploy e uso de IA. Detalhes e status de cada uma em [`docs/ROTEIRO.md`](docs/ROTEIRO.md).

## 🛠️ Stack

| Camada | Tecnologia |
|---|---|
| Backend | C# · ASP.NET Core (.NET 10 LTS) |
| Frontend | React · TypeScript · Vite |
| Banco / Auth | Supabase (PostgreSQL + Supabase Auth) |
| ORM | Entity Framework Core + Npgsql |
| Testes | NUnit · Shouldly · Vitest · Playwright |
| CI/CD | GitHub Actions |
| Container | Docker |
| Hospedagem | Render (app) · Supabase (banco) |

## 📁 Estrutura

```
CadeAEscalacao/
├── CadeAEscalacao.Domain/          # Regras de negócio puras (sem dependências)
├── CadeAEscalacao.Application/     # Casos de uso
├── CadeAEscalacao.Infrastructure/  # EF Core, Supabase, integrações
├── CadeAEscalacao.Server/          # API ASP.NET Core
├── cadeaescalacao.client/          # Front-end React (Vite)
├── CadeAEscalacao.Domain.Tests/    # Testes do domínio (NUnit + Shouldly)
├── docs/
│   ├── DECISOES.md                 # Registro de decisões (ADRs)
│   └── ROTEIRO.md                  # Etapas do projeto e status
├── README.md
└── CadeAEscalacao.slnx
```

## 🚀 Rodando localmente

**Pré-requisitos:** Visual Studio 2026 com a workload "ASP.NET e desenvolvimento Web" · .NET 10 SDK · Node.js 22 LTS

1. Abra `CadeAEscalacao.slnx` no Visual Studio.
2. Pressione **F5**. A API e o React sobem juntos.

## 📄 Licença

MIT
