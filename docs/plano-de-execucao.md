# Plano de Execução — ClinicHub

Este documento acompanha a implementação incremental do ClinicHub. Cada etapa só será marcada como concluída após a respectiva validação técnica.

**Legenda:** `⬜ Pendente` · `🟨 Em andamento` · `✅ Concluída`

| # | Etapa | Status | Entregáveis e critério de confirmação |
|---:|---|---|---|
| 1 | Setup do projeto | ✅ Concluída | Solution .NET 8 estruturada, referências entre camadas, Docker Compose e build/configuração validados. |
| 2 | Camada Domain | ✅ Concluída | Agregados, entidades, value objects, contratos e domain events modelados e validados por compilação. Os testes serão implementados e aferidos na etapa 10. |
| 3 | Camada Infrastructure | ✅ Concluída | EF Core, contexto, mapeamentos, migration inicial, repositórios e Unit of Work implementados e validados. |
| 4 | Camada Application | ✅ Concluída | Casos de uso CQRS com MediatR, DTOs e validações FluentValidation implementados e validados por compilação. |
| 5 | Camada API e observabilidade | ✅ Concluída | Controllers, middleware global, CorrelationId, Serilog, Swagger e health checks configurados e validados por build/Compose. |
| 6 | Autenticação e autorização | ✅ Concluída | JWT, refresh token rotativo, cadastro público com confirmação de e-mail, roles e autorização por claims implementados e validados. |
| 7 | Módulo Pacientes | ✅ Concluída | CRUD, filtros, paginação, cache Redis e invalidação por versão implementados e validados por build/Compose. |
| 8 | Módulo Agendamentos | ✅ Concluída | Criação, reagendamento, cancelamento, conflito de horários, eventos e publicação RabbitMQ implementados e validados por build/Compose. |
| 9 | Módulo Financeiro | ✅ Concluída | Registro de pagamentos e relatório por período com consulta Dapper implementados e validados por build/Compose. |
| 10 | Testes automatizados | ✅ Concluída | Testes unitários e de integração implementados; cobertura aferida em 70,0% no Domain e 72,9% na Application. |
| 11 | Frontend Angular | ✅ Concluída | SPA standalone integrada à API: autenticação, CRUD de pacientes, operações de agenda, pagamentos, relatório e layout responsivo validados. |
| 12 | CI/CD | ✅ Concluída | Pipeline GitHub Actions validada local e remotamente: build, testes, análise estática, cobertura e imagens Docker. |
| 13 | Documentação final | ✅ Concluída | README, diagramas Mermaid, guia operacional/API, ADRs e exemplos Swagger implementados e validados. |

## Registro de confirmações

### Etapa 1 — Setup do projeto

**Confirmada em 08/08/2026.**

- Criados os projetos `Domain`, `Application`, `Infrastructure`, `API` e `Notifications.Worker`, todos direcionados a .NET 8.
- Configuradas referências no sentido da Clean Architecture, sem dependência das camadas internas em relação à API.
- Criado `docker-compose.yml` com API, worker, SQL Server, Redis, RabbitMQ e Seq.
- Criados Dockerfiles da API e do worker, `.env.example`, `.dockerignore` e `.gitignore`.
- Verificações executadas: `dotnet build ClinicHub.sln --no-restore` e `docker compose --env-file .env.example config --quiet`, ambas concluídas com sucesso.

### Etapa 2 — Camada Domain

**Confirmada em 08/08/2026.**

- Criadas as abstrações `Entity`, `AggregateRoot`, `IDomainEvent`, `DomainResult` e `DomainNotification` para manter regras de domínio independentes de tecnologia e evitar exceptions como fluxo de regra de negócio.
- Modelados os agregados `Patient`, `Appointment` e `Payment`, com seus ciclos de vida e invariantes iniciais.
- Modelados os value objects `PersonName`, `EmailAddress`, `PhoneNumber`, `AppointmentSlot` e `Money`.
- Criado o evento `AppointmentConfirmedDomainEvent`, registrado pelo agregado no momento da confirmação.
- Definidos os contratos de repositórios e `IUnitOfWork`, que serão implementados pela Infrastructure na próxima etapa.
- Documentado o desenho em `docs/modelo-do-dominio.md`.
- Verificação executada: `dotnet build ClinicHub.sln --no-restore`, concluída com sucesso, sem avisos ou erros.

### Etapa 3 — Camada Infrastructure

**Confirmada em 08/08/2026.**

- Adicionado o `ClinicHubDbContext` com SQL Server e mapeamentos EF Core para os agregados e seus value objects.
- Implementados `PatientRepository`, `AppointmentRepository`, `PaymentRepository` e `UnitOfWork` para os contratos definidos no domínio.
- A verificação de conflito de agenda consulta consultas não canceladas com sobreposição de intervalos no banco de dados.
- Criada a migration inicial em `src/ClinicHub.Infrastructure/Persistence/Migrations` com tabelas, tipos, índices e restrições iniciais.
- Criada extensão `AddInfrastructure` para o registro futuro das dependências no composition root da API e do worker.
- Verificações executadas: `dotnet build ClinicHub.sln --no-restore` e geração idempotente de migration via `dotnet ef migrations script --idempotent`, ambas concluídas com sucesso.

### Etapa 4 — Camada Application

**Confirmada em 08/08/2026.**

- Adicionados contratos `ICommand` e `IQuery`, resultados estruturados de aplicação e um pipeline de validação MediatR.
- Configurados MediatR e FluentValidation no método `AddApplication`.
- Implementados os primeiros casos de uso: `CreatePatientCommand` e `GetPatientByIdQuery`, com handlers, DTOs e validators.
- A criação de paciente converte notificações do domínio em erros de aplicação, verifica e-mail duplicado, persiste pelo repositório e confirma pela Unit of Work.
- Adicionada a abstração `IClock`, implementada pela Infrastructure, para tornar regras temporais determinísticas em testes e independentes do relógio do sistema.
- Verificação executada: `dotnet build ClinicHub.sln --no-restore`, concluída com sucesso, sem avisos ou erros.

### Etapa 5 — Camada API e observabilidade

**Confirmada em 08/08/2026.**

- Configurada a API como composition root, conectando `AddApplication` e `AddInfrastructure`.
- Expostos os endpoints iniciais de criação e consulta de pacientes, com respostas de erro estruturadas.
- Implementado middleware de `X-Correlation-ID`; o identificador é devolvido ao cliente e enriquecido nos logs.
- Configurado Serilog para logs estruturados no console e Seq, incluindo log automático de requisições.
- Implementado tratamento global de exceções com `ProblemDetails` e registro do CorrelationId.
- Configurados Swagger em desenvolvimento e health checks: `/health/live` (liveness) e `/health/ready` (SQL Server, Redis e RabbitMQ).
- Verificações executadas: `dotnet build ClinicHub.sln --no-restore` e `docker compose --env-file .env.example config --quiet`, ambas concluídas com sucesso, sem avisos ou erros.
- A validação HTTP em processo foi tentada, mas o ambiente bloqueou a criação do processo auxiliar antes de sua execução; nenhum processo persistiu.

### Etapa 6 — Autenticação e autorização

**Confirmada em 08/08/2026.**

- Modelados e persistidos `User` e `RefreshToken`; refresh tokens são armazenados exclusivamente como hashes SHA-256.
- Implementados `POST /api/auth/login` e `POST /api/auth/refresh`, com rotação de refresh token a cada renovação.
- Configurado JWT com emissor, audiência, assinatura simétrica, expiração curta e claims de usuário/e-mail/role.
- Restringidos endpoints de pacientes a usuários autenticados; criação exige `Admin` ou `Receptionist`.
- Configurado Swagger para receber token Bearer.
- Adicionada inicialização exclusiva de desenvolvimento: quando habilitada pelo Docker Compose, aplica migrations e cria um Admin a partir do `.env` apenas se ainda não existir.
- Criada a migration `AddAuthentication` para tabelas de usuários e refresh tokens.
- Verificações executadas: `dotnet build ClinicHub.sln --no-restore`, `docker compose --env-file .env.example config --quiet` e geração de script idempotente de migrations, todas concluídas com sucesso e sem avisos.
- Evolução confirmada em 08/08/2026: adicionados cadastro público e confirmação de e-mail. Novas contas recebem o role `Patient`, permanecem inativas até a ativação e não recebem permissões administrativas.
- O token de confirmação é criptograficamente aleatório, armazenado apenas como hash SHA-256, tem validade de 24 horas, é invalidado após uso e é protegido por índice único no banco. A migration `AddEmailConfirmation` inclui os campos e o índice necessários.
- Expostos `POST /api/auth/register` e `POST /api/auth/confirm-email`; em desenvolvimento o link é registrado nos logs, e em produção o envio é habilitado pelas variáveis SMTP documentadas no `.env.example`.
- Validação ponta a ponta: registro retornou `202`, login antes da confirmação retornou `401`, confirmação do token foi aceita e o login posterior retornou JWT válido. Build .NET, 35 testes automatizados, build/teste Angular e Compose também foram concluídos com sucesso.

### Etapa 7 — Módulo Pacientes

**Confirmada em 08/08/2026.**

- Completado o CRUD de pacientes: criação, consulta individual, listagem, atualização e desativação lógica.
- Implementados filtros por nome/e-mail e paginação com limite de 100 itens por página.
- A busca de listagens usa Redis com TTL de cinco minutos e chave contendo uma versão de cache.
- Criação, atualização e desativação incrementam a versão de cache somente após confirmar a Unit of Work, evitando retorno de listas desatualizadas.
- A indisponibilidade de Redis é tratada como cache miss/no-op, com log estruturado; as consultas continuam no SQL Server.
- Aplicadas permissões: `Admin`/`Receptionist` para criar e alterar; `Admin` para desativar; todos os roles autenticados para consulta.
- Verificações executadas: `dotnet build ClinicHub.sln --no-restore` e `docker compose --env-file .env.example config --quiet`, ambas concluídas com sucesso, sem avisos ou erros.

### Etapa 8 — Módulo Agendamentos e notificações

**Confirmada em 08/08/2026.**

- Implementados comandos e endpoints para agendar, confirmar, reagendar e cancelar consultas.
- A criação e o reagendamento validam paciente ativo, usuário com role `Doctor`, horários UTC futuros, duração e sobreposição de intervalos de consultas não canceladas.
- A confirmação registra `AppointmentConfirmedDomainEvent` no agregado somente após a transição de estado ser válida.
- Após a Unit of Work confirmar a consulta, o dispatcher da Infrastructure encapsula o evento em uma notificação MediatR e o handler publica `AppointmentConfirmedIntegrationEvent` no RabbitMQ.
- Implementado o worker separado que consome a fila durável `clinichub.notifications.appointment-confirmed`, faz ack manual e simula o envio da notificação com log Serilog estruturado.
- O seed de desenvolvimento cria também um médico, permitindo exercitar o fluxo de agendamento no Docker Compose.
- Verificações executadas: `dotnet build ClinicHub.sln --no-restore` e `docker compose --env-file .env.example config --quiet`, ambas concluídas com sucesso, sem avisos ou erros.

### Etapa 9 — Módulo Financeiro

**Confirmada em 08/08/2026.**

- Implementado o registro de pagamento vinculado a uma consulta confirmada, com value object `Money`, método de pagamento e data UTC.
- Impedida a duplicidade de pagamento por consulta pela regra de aplicação e pelo índice único já existente no banco.
- Exposto `POST /api/payments` para `Admin` e `Receptionist`.
- Implementado `IRevenueReportReader` na Application e `DapperRevenueReportReader` na Infrastructure, mantendo a leitura analítica fora dos agregados EF Core.
- O relatório filtra por período e agrupa receita bruta e quantidade de pagamentos por dia e moeda; é exposto em `GET /api/financial/revenue` apenas para `Admin`.
- Verificações executadas: `dotnet build ClinicHub.sln --no-restore`, `docker compose --env-file .env.example config --quiet` e geração de script idempotente de migrations, todas concluídas com sucesso, sem avisos ou erros.

### Etapa 10 — Testes automatizados

- Criados projetos de testes para `Domain`, `Application`, `Infrastructure` e integração da API.
- Adicionados testes de regras de agendamento, evento de confirmação, handler de criação de paciente, validator de agendamento, repositório EF Core InMemory e endpoint de liveness com CorrelationId via `WebApplicationFactory`.
- Ampliada a suíte para 35 testes de sucesso: 15 em Domain, 18 em Application, 1 em Infrastructure e 1 de integração da API.
- A coleta de cobertura foi habilitada com `coverlet.collector`. A aferição final dos conjuntos dedicados atingiu 70,0% no Domain e 72,9% na Application, superando a meta mínima de 70%.

### Etapa 11 — Frontend Angular

**Confirmada em 08/08/2026.**

- Criado o projeto Angular 21 standalone em `frontend/clinichub-web`, separado da solution .NET e usando Angular Material.
- Definida estrutura por responsabilidade: `core` (auth/HTTP/modelos), `layout` (shell) e `features` (auth, dashboard, pacientes, agendamentos e financeiro).
- Implementados login reativo, persistência de tokens, interceptor JWT com renovação automática após 401, guard de autenticação e rotas lazy.
- Implementado shell responsivo com sidebar, toolbar e visibilidade do financeiro conforme role.
- Adicionado Dockerfile multi-stage/Nginx para o frontend, serviço `frontend` no Compose e política CORS da API para `http://localhost:4200`.
- Integradas as telas de pacientes, agendamentos e financeiro à API real. Pacientes permitem listar, filtrar, paginar, criar, editar e desativar; agenda permite carregar pacientes e médicos, agendar, confirmar, reagendar e cancelar; financeiro registra pagamentos e consulta receita por período.
- Adicionado `GET /api/users/doctors`, protegido para `Admin` e `Receptionist`, para que a tela de agenda selecione médicos ativos sem acoplamento a dados fixos.
- Implementados serviços HTTP por feature, modelos tipados, mensagens de êxito/erro e formulários reativos com validação no cliente.
- Adicionadas as rotas públicas lazy de cadastro e confirmação de e-mail, com formulários reativos, feedback de estado e link de criação de conta na tela de login.
- Verificações executadas: `npm run build` (sem avisos), `npm test -- --watch=false` (2 testes aprovados), `dotnet build ClinicHub.sln --no-restore` e `docker compose --env-file .env.example config --quiet`, todas concluídas com sucesso.
- Validação ponta a ponta executada no Compose: frontend (`HTTP 200`), readiness da API (`HTTP 200`), preflight CORS para `http://localhost:4200` (`HTTP 204`), login e rotação de refresh token, listagem de médicos, criação/listagem de paciente, agendamento/confirmação, pagamento e relatório de receita. O worker também registrou o consumo do evento de confirmação no RabbitMQ.
- Para coexistir com outros projetos locais, a API é publicada em `localhost:8082` e o Redis em `localhost:6380`; a comunicação entre contêineres permanece em `api:8080` e `redis:6379`.

### Etapa 12 — CI/CD

**Confirmada em 08/08/2026.**

- Criado o workflow `.github/workflows/ci.yml`, acionado por `push`, pull request e disparo manual.
- O job de backend executa restore, `dotnet format --verify-no-changes`, build Release, testes e coleta/publicação do artefato de cobertura.
- O job de frontend usa Node 22.20.0, `npm ci`, análise TypeScript (`npm run lint`), build e testes Angular.
- O job Docker valida a especificação do Compose e constrói as imagens da API, worker e frontend após os jobs de código.
- Validação local concluída com sucesso usando os mesmos comandos: formatação .NET, build Release, 35 testes com cobertura, análise/build/teste Angular, `docker compose ... config --quiet` e `docker compose ... build`.
- Repositório público criado em `https://github.com/eliasmatheusouza/ClinicHub`. A primeira execução remota do workflow foi concluída com sucesso: backend, frontend e build das imagens Docker aprovados. Evidência: `https://github.com/eliasmatheusouza/ClinicHub/actions/runs/31289372213`.

### Etapa 13 — Documentação final

**Confirmada em 09/08/2026.**

- README reestruturado como porta de entrada: arquitetura Mermaid, stack, execução local, credenciais de desenvolvimento, módulos, qualidade, CI/CD e estrutura do repositório.
- Criados `docs/arquitetura.md`, `docs/operacao-local.md` e `docs/api-examples.md`, cobrindo limites das camadas, fluxos assíncronos, segurança, troubleshooting, endereços locais, SMTP e exemplos de consumo da API.
- Atualizado o modelo de domínio com `User`, `RefreshToken`, confirmação de e-mail, regras financeiras e diagrama entidade-relacionamento Mermaid.
- Registradas cinco ADRs: Clean Architecture/CQRS, Redis distribuído, RabbitMQ assíncrono, Dapper para relatórios e confirmação de e-mail por token hash.
- Swagger/OpenAPI recebeu exemplos de payload para login, cadastro, confirmação de e-mail, refresh, pacientes, agenda e pagamentos; validado em execução em `http://localhost:8082/swagger/v1/swagger.json`.
- Verificações executadas: build e formatação .NET, 35 testes automatizados, análise/build/teste Angular e verificação dos artefatos documentais, todas concluídas com sucesso.
- Adicionado o `docs/guia-de-estudo.md`, que explica o projeto a partir do código, com ordem de leitura, diagramas, fluxos, experiências práticas e exercícios de evolução.
- Adicionado o `docs/avaliacao-de-maturidade.md`, distinguindo a base atual de MVP profissional das lacunas de segurança, resiliência, operação e DDD estratégico necessárias para produção.

## Regra de atualização

Ao fim de cada etapa, este documento receberá:

1. a troca de status para `✅ Concluída`;
2. a data de confirmação;
3. os artefatos implementados;
4. os comandos, testes ou evidências usados na validação.

## Próximas evoluções pós-MVP

As treze etapas originais estão concluídas. Os itens abaixo não alteram esse histórico; representam a continuidade recomendada, em ordem de prioridade.

| Prioridade | Evolução | Objetivo |
|---:|---|---|
| 1 | Qualidade e segurança | Restaurar a cobertura mínima de 70%, testar os fluxos novos de autenticação, atualizar dependências vulneráveis, adicionar rate limiting, HTTPS e secrets de produção. |
| 2 | Resiliência assíncrona | Implementar retry de conexão, DLQ e monitoramento do worker RabbitMQ; disponibilizar reenvio de confirmação de e-mail. |
| 3 | Deploy público | Externalizar a configuração da URL da API, publicar imagens em registry e implantar frontend, API e infraestrutura. |
| 4 | Gestão de equipe | Criar convites e administração de médicos e recepcionistas com roles e auditoria. |
| 5 | Portal do paciente | Expor somente os dados do paciente autenticado e permitir consultar, cancelar e reagendar consultas próprias. |
| 6 | Notificações e produto | Integrar e-mail/SMS/WhatsApp reais, indicadores operacionais, disponibilidade médica e dashboard com dados reais. |

## Ecossistema de portfólio

O ClinicHub é o projeto-base de um ecossistema que incluirá DocMind (IA aplicada a documentos), DevPulse (monitoramento em tempo real) e NetForge (biblioteca NuGet extraída de necessidades reais). A sequência, os critérios de encerramento e as regras de qualidade estão em [docs/plano-ecossistema-portfolio.md](plano-ecossistema-portfolio.md).
