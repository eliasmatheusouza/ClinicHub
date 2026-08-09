# ClinicHub

Plataforma full stack de gestão de pacientes, consultas e financeiro para clínicas médicas. O projeto é um laboratório de arquitetura e práticas de engenharia: .NET 8, Angular standalone, Clean Architecture, mensageria, cache distribuído, observabilidade e CI/CD.

[![CI](https://github.com/eliasmatheusouza/ClinicHub/actions/workflows/ci.yml/badge.svg)](https://github.com/eliasmatheusouza/ClinicHub/actions/workflows/ci.yml)

## Arquitetura

```mermaid
flowchart LR
    Web["Angular 21"] -->|"JWT / REST"| Api["ASP.NET Core API"]
    Api --> App["Application\nCQRS + MediatR"]
    App --> Domain["Domain\nDDD"]
    App --> Infra["Infrastructure"]
    Infra --> Sql[("SQL Server")]
    Infra --> Redis[("Redis")]
    Infra --> MQ["RabbitMQ"]
    MQ --> Worker["Notifications Worker"]
    Api --> Seq["Seq"]
    Worker --> Seq
```

As camadas internas não dependem de HTTP, banco ou mensageria. Veja o desenho detalhado e os fluxos em [docs/arquitetura.md](docs/arquitetura.md).

## Stack

| Área | Tecnologias |
|---|---|
| Backend | .NET 8, ASP.NET Core, EF Core, Dapper, MediatR, FluentValidation |
| Dados e integração | SQL Server, Redis, RabbitMQ |
| Frontend | Angular 21 standalone, TypeScript, Angular Material |
| Observabilidade | Serilog, Seq, Correlation ID e health checks |
| Entrega | Docker Compose, GitHub Actions |

## Início rápido

Pré-requisitos: Docker Desktop com Linux containers e Docker Compose.

```powershell
Copy-Item .env.example .env
docker compose up -d --build
docker compose ps
```

| Serviço | Endereço |
|---|---|
| Aplicação Angular | http://localhost:4200 |
| API | http://localhost:8082 |
| Swagger / OpenAPI | http://localhost:8082/swagger |
| Seq | http://localhost:8081 |
| RabbitMQ Management | http://localhost:15672 |

O Compose aplica migrations e cria os usuários de desenvolvimento uma única vez.

| Perfil | E-mail | Senha |
|---|---|---|
| Admin | `admin@clinichub.local` | `Admin123!` |
| Doctor | `doctor@clinichub.local` | `Doctor123!` |

As credenciais são exclusivamente locais e podem ser alteradas no `.env`. Informações de portas, logs, health checks, reset de infraestrutura e SMTP estão em [docs/operacao-local.md](docs/operacao-local.md).

## Módulos

- **Autenticação:** JWT de curta duração, refresh token rotativo e autorização por role.
- **Cadastro público:** rota `/register`, confirmação por e-mail e ativação de conta `Patient` por token de uso único.
- **Pacientes:** CRUD, filtros, paginação e cache Redis versionado.
- **Agenda:** criar, confirmar, reagendar e cancelar; conflitos de horários são regras de domínio.
- **Notificações:** confirmação de consulta publica evento no RabbitMQ; worker consome a fila durável.
- **Financeiro:** pagamento por consulta confirmada e relatório de receita por período usando Dapper.

## Desenvolvimento sem Docker

Backend:

```powershell
dotnet restore ClinicHub.sln
dotnet run --project src/ClinicHub.API
```

Frontend:

```powershell
Set-Location frontend/clinichub-web
npm ci
npm start
```

Para executar localmente fora do Compose, ajuste as connection strings de `appsettings.Development.json` para serviços disponíveis. O padrão do projeto usa Redis em `localhost:6380` e API publicada em `localhost:8082`, evitando conflito com outros projetos locais.

## API e autenticação

Todos os contratos estão disponíveis no Swagger. Há exemplos de payload diretamente na interface para login, cadastro, pacientes, consultas e pagamentos.

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "admin@clinichub.local", "password": "Admin123!" }
```

Envie o `accessToken` recebido como `Authorization: Bearer {token}`. Para exemplos completos, roles e respostas de erro, consulte [docs/api-examples.md](docs/api-examples.md).

### Confirmação de e-mail

Novas contas públicas ficam inativas até a confirmação. No desenvolvimento, `EMAIL_DELIVERY_MODE=Log` escreve o link no log da API. Para SMTP real, altere para `Smtp` e configure as variáveis `EMAIL_*` presentes no `.env.example`. O token bruto não é persistido; somente seu hash SHA-256, por até 24 horas.

## Qualidade

```powershell
dotnet format ClinicHub.sln --verify-no-changes --no-restore
dotnet test ClinicHub.sln --configuration Release --no-restore --collect "XPlat Code Coverage"

Set-Location frontend/clinichub-web
npm run lint
npm run build
npm test -- --watch=false
```

A suíte atual contém 35 testes e a cobertura aferida para Domain/Application é superior a 70%.

## CI/CD

O workflow [CI](.github/workflows/ci.yml) é executado em todo push, pull request e disparo manual. Ele valida formatação, build Release, testes e cobertura .NET; análise TypeScript, build e testes Angular; além da especificação e imagens Docker. A primeira execução remota foi aprovada: [ver execução](https://github.com/eliasmatheusouza/ClinicHub/actions/runs/31289372213).

## Documentação

- [Plano de execução e evidências](docs/plano-de-execucao.md)
- [Arquitetura e fluxos](docs/arquitetura.md)
- [Modelo de domínio](docs/modelo-do-dominio.md)
- [Operação local](docs/operacao-local.md)
- [Guia da API](docs/api-examples.md)
- [Architecture Decision Records](docs/adr)

## Estrutura do repositório

```text
src/
  ClinicHub.Domain/                  # Agregados, VOs, eventos e contratos
  ClinicHub.Application/             # CQRS, handlers, DTOs e validações
  ClinicHub.Infrastructure/          # EF Core, Redis, RabbitMQ, Dapper e segurança
  ClinicHub.API/                     # REST, Swagger, middleware e health checks
  ClinicHub.Notifications.Worker/    # Consumidor RabbitMQ
frontend/clinichub-web/              # SPA Angular standalone
tests/                                # Testes de domínio, aplicação, infraestrutura e API
docs/                                 # Arquitetura, operação, API e ADRs
```
