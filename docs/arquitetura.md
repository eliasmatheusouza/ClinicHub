# Arquitetura do ClinicHub

O ClinicHub adota Clean Architecture. As dependências apontam para dentro: regras de domínio não dependem de banco, HTTP, mensageria ou interface.

```mermaid
flowchart LR
    Web["Angular 21\nSPA"] -->|"HTTPS / JWT"| Api["ClinicHub.API\nControllers, middleware e Swagger"]
    Api --> App["ClinicHub.Application\nCQRS, MediatR e FluentValidation"]
    App --> Domain["ClinicHub.Domain\nAggregates, VOs e eventos"]
    Api --> Infra["ClinicHub.Infrastructure\nAdapters e persistência"]
    App --> Infra
    Infra --> Sql[("SQL Server\nEF Core + Dapper")]
    Infra --> Redis[("Redis\nCache distribuído")]
    Infra --> Rabbit["RabbitMQ\nEventos de integração"]
    Rabbit --> Worker["Notifications.Worker\nConsumer"]
    Api --> Seq["Seq\nLogs estruturados"]
    Worker --> Seq
```

## Limites das camadas

| Camada | Responsabilidade | Não depende de |
|---|---|---|
| `Domain` | Regras, agregados, value objects, eventos e contratos | Frameworks e infraestrutura |
| `Application` | Casos de uso, CQRS, DTOs e validações | API, EF Core, Redis e RabbitMQ |
| `Infrastructure` | EF Core, Dapper, Redis, JWT, SMTP e RabbitMQ | API e Angular |
| `API` | HTTP, composição de dependências, autenticação e observabilidade | Regras de negócio concretas |
| `Notifications.Worker` | Consumo assíncrono da fila de notificações | Interface web |

## Fluxo de confirmação de consulta

```mermaid
sequenceDiagram
    participant UI as Angular
    participant API as API
    participant APP as Application
    participant DB as SQL Server
    participant MQ as RabbitMQ
    participant W as Notifications.Worker

    UI->>API: POST /api/appointments/{id}/confirm
    API->>APP: ConfirmAppointmentCommand
    APP->>DB: Persiste consulta confirmada
    APP->>MQ: appointment.confirmed
    MQ->>W: Evento durável
    W->>W: Simula notificação e registra log
    API-->>UI: 200 Consulta confirmada
```

## Segurança e acesso

- Access token JWT curto e refresh token rotativo, persistido somente como hash SHA-256.
- `Admin`, `Doctor` e `Receptionist` atendem aos casos de uso internos conforme as policies dos controllers.
- O cadastro público recebe `Patient`, só é ativado por confirmação de e-mail e não concede permissões administrativas.
- O token de confirmação de e-mail é aleatório, tem validade de 24 horas, é usado uma única vez e é persistido exclusivamente como hash.
- CORS permite a origem do frontend local (`http://localhost:4200`); em implantação, a origem deve ser explicitamente ajustada.
