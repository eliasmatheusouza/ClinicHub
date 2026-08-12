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
| 14 | Testes de fluxos críticos | ✅ Concluída | Testes AAA para cadastro/confirmação de e-mail e regras do agregado `User`; cobertura restaurada para 74,57% no Domain e 74,76% na Application. |
| 15 | Gate de cobertura | ✅ Concluída | Script de quality gate integrado à CI; falha abaixo de 70% nas camadas Domain e Application. |
| 16 | Relatórios e auditorias de CI | ✅ Concluída | Resultados TRX publicados, auditorias .NET/NPM validadas, CodeQL aprovado e Dependabot sem alertas abertos. |
| 17 | Análise estática e Quality Gate | 🟨 Em andamento | Laboratório SonarQube, scanner e workflow SonarCloud versionados; falta configurar organização/token e validar o gate remoto. |
| 18 | Governança de pull requests | 🟨 Em andamento | Proteção da `main`, revisão e checks de CI/CodeQL/DAST; incluir SonarCloud após concluir a Etapa 17. |
| 19 | Defesa da API | ✅ Concluída | Rate limiting nas rotas de autenticação, headers HTTP, HTTPS/HSTS em Production e validação de configuração segura. |
| 20 | Dados, auditoria e ownership | ✅ Concluída | Audit trail, policies, ownership, minimização/masking e plano de criptografia documentado e validado. |
| 21 | Hardening de deploy | ✅ Concluída | Imagens não-root, manifesto de produção isolado, secrets externos e DAST remoto com artefato revisado e sem alertas de risco. |
| 22 | Capacidade e performance | ⬜ Pendente | Definir SLOs, criar testes de carga e declarar capacidade com métricas reproduzíveis. |

## Próximas etapas priorizadas

Esta é a ordem de continuidade aprovada após a conclusão das etapas 16 e 21. Cada item só avança após sua evidência técnica ser registrada.

| Prioridade | Trabalho | Resultado esperado |
|---:|---|---|
| 1 | **Etapa 17 — SonarQube/SonarCloud e Quality Gate** | Analisar código novo para bugs, vulnerabilidades, duplicação e cobertura; falhar a CI quando o padrão não for atendido. |
| 2 | **Etapa 18 — Governança de Pull Requests** | Proteger `main`, exigir revisão e tornar CI, CodeQL, DAST e Quality Gate obrigatórios antes do merge. |
| 3 | **Etapa 22 — Capacidade e performance** | Criar cenários k6, definir SLOs e medir p95/p99, erros, throughput, banco, cache e filas antes de declarar capacidade simultânea. |
| 4 | **Resiliência de eventos** | Implementar outbox, retry limitado, DLQ e idempotência para que notificações não sejam perdidas silenciosamente. |
| 5 | **Produção operada** | Publicar imagens em registry, aplicar IaC, configurar cloud, backups/restores, alertas, SMTP real e testes end-to-end autenticados. |
| 6 | **Evolução funcional** | Completar portal do paciente, gestão de equipe, reenvio de confirmação e notificações reais. |

O detalhamento didático, exercícios e critérios de conclusão de cada frente estão no [plano de ensino completo](plano-de-ensino-completo.md).

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

## Trilha de qualidade e Platform Engineering

As etapas 14 a 18 aplicam ao ClinicHub os conceitos de AAA, FIRST, isolamento por mocks/stubs, cenários de borda, Shift-Left Testing, cobertura, SAST e quality gates. Elas não substituem as treze etapas originais: aprofundam a maturidade de engenharia do projeto.

| Etapa | Objetivo | Evidência de conclusão |
|---:|---|---|
| 14 | Cobrir os fluxos críticos novos e tornar a intenção dos testes explícita | Testes de sucesso, erro e borda para registro/confirmação; cobertura atualizada e meta de 70% restaurada. |
| 15 | Impedir regressão de cobertura | Job de CI falha abaixo do limiar acordado; relatório Cobertura fica disponível como artefato. |
| 16 | Dar feedback rápido sobre qualidade e riscos de dependência | Resultados de teste aparecem no PR; `dotnet`/NPM audit e análise de segurança executam na CI. |
| 17 | Analisar código novo com Quality Gate | Sonar configurado; novos bugs, vulnerabilidades, duplicação e cobertura insuficiente bloqueiam o job. |
| 18 | Tornar os checks obrigatórios antes do merge | `main` protegida, revisão e checks de backend/frontend/Docker/segurança obrigatórios. |

### Etapa 16 — Relatórios e auditorias de CI

**Confirmada em 11/08/2026.**

- A suíte .NET gera arquivos TRX; a ação `dorny/test-reporter` publica o resumo dos testes no check da execução.
- Criado job de auditoria que falha para dependências .NET vulneráveis e vulnerabilidades altas de dependências NPM de execução. O relatório NPM completo, incluindo dependências de desenvolvimento, fica disponível como artefato para revisão.
- Atualizados `Microsoft.NET.Test.Sdk`, xUnit, runner xUnit e Coverlet nos quatro projetos de teste. A auditoria posterior não encontrou vulnerabilidades transitivas no .NET, incluindo os projetos de teste.
- Adicionado workflow CodeQL para C# e JavaScript/TypeScript em push, PR, execução manual e agenda semanal.
- Adicionado Dependabot semanal para NuGet, NPM e GitHub Actions. Os alertas e as atualizações automáticas de segurança também foram habilitados no repositório.
- A execução remota final [CI #31452721365](https://github.com/eliasmatheusouza/ClinicHub/actions/runs/31452721365) aprovou backend, frontend, auditoria e imagens Docker; [CodeQL #31452721355](https://github.com/eliasmatheusouza/ClinicHub/actions/runs/31452721355) também foi aprovado.
- A revisão encontrou uma exposição real de e-mail/token de confirmação nos logs do modo didático. Ela foi removida, e a reanálise CodeQL ficou sem alertas abertos. Seis dependências transitivas de desenvolvimento alertadas pelo Dependabot foram atualizadas via `overrides` mínimos no NPM; a reindexação final ficou sem alertas Dependabot abertos.

### Etapa 17 — Análise estática e Quality Gate

**Em andamento em 11/08/2026.**

- Adicionados laboratório SonarQube Community Build com PostgreSQL, scanner .NET fixado em `11.2.1`, script local e workflow remoto condicional para SonarQube Cloud.
- A análise local completa foi aprovada pelo Quality Gate padrão em aproximadamente 90 segundos, com 49,1% de cobertura geral, 0 bugs e 0% de duplicação. Ela também revelou 44 code smells, 2 vulnerabilidades e 8 security hotspots legados, registrados para triagem em vez de serem ocultados.
- A integração gera Cobertura e OpenCover a partir do Coverlet. O SonarQube importa OpenCover; a CI continua consumindo Cobertura. Os relatórios de cada execução ficam isolados em `artifacts/sonarqube-tests/`.
- O scanner desta etapa limita-se à solução .NET para não analisar dependências transitivas do frontend. A análise e cobertura Angular ficam como evolução condicionada a um relatório LCOV estável.
- Para concluir a etapa falta configurar a organização/projeto e o secret `SONAR_TOKEN` no SonarQube Cloud, executar o workflow remoto e confirmar o bloqueio do job quando o Quality Gate de código novo reprovar.

### Etapa 18 — Governança de pull requests

**Iniciada em 11/08/2026.**

- A `main` passa a exigir pull request, uma aprovação, conversas resolvidas e histórico linear. Administradores continuam podendo agir em emergência, decisão consciente para um repositório pessoal de aprendizado.
- Os checks obrigatórios são Backend (.NET 8), Frontend (Angular), Dependency audit, Docker images, CodeQL para C# e JavaScript/TypeScript e OWASP ZAP baseline.
- O DAST baseline passou a executar também em pull requests e foi validado manualmente com sucesso antes de ser promovido a check obrigatório.
- O check SonarCloud **não** será exigido até a Etapa 17 receber organização, project key e `SONAR_TOKEN`; exigir um check ainda ignorado criaria bloqueios falsos.

## Trilha de segurança da aplicação

As etapas 19 a 21 aplicam os controles prioritários identificados na avaliação de segurança. Elas seguem a qualidade de CI porque novos controles precisam nascer com testes e análise automatizada.

| Etapa | Controles | Critério de conclusão |
|---:|---|---|
| 19 | Rate limiting em login/cadastro/confirmação/refresh; headers HTTP; HTTPS/HSTS em produção; validação de configuração | Rotas sensíveis recusam abuso, respostas possuem headers testados e a API não inicia em produção com configuração insegura. |
| 20 | Audit trail, policy-based authorization e ownership por recurso; minimização/masking de dados | Alterações sensíveis são rastreáveis e testes impedem acesso a dados de outros usuários. |
| 21 | Imagens não-root, portas internas privadas, secrets externos, deploy com TLS e DAST | Ambiente de demonstração possui checklist de hardening e scan dinâmico aprovado. |

### Etapa 19 — Defesa da API

**Confirmada em 11/08/2026.**

- Primeiro controle implementado: rate limiting por IP em login, cadastro, confirmação de e-mail e refresh token, com políticas e janelas configuráveis em `RateLimiting`.
- Login limita cinco tentativas por minuto, cadastro três tentativas por dez minutos, confirmação e refresh dez tentativas por minuto. O teste de integração reduz a política de login para duas tentativas e confirma retorno `429 Too Many Requests` na terceira.
- A política por IP deverá considerar cabeçalhos de proxy confiáveis quando houver deploy atrás de reverse proxy; não se deve confiar cegamente em cabeçalhos enviados pelo cliente.
- Adicionados headers `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` e CSP para rotas da API. Swagger é excluído da CSP restritiva em desenvolvimento.
- Em Production, HTTPS redirection e HSTS são habilitados. A inicialização é bloqueada se JWT, hosts, CORS, URL de confirmação ou modo de e-mail usarem valores inseguros; `appsettings.Production.json` não contém secrets e exige configuração externa.
- Validações executadas: formatação, build Release e quatro testes de integração aprovados, cobrindo liveness, headers, rate limiting e cenários aceitos/rejeitados da configuração Production.

### Etapa 20 — Dados, auditoria e ownership

**Iniciada em 11/08/2026.**

- Criada a tabela `AuditLogs`, com migration e índices por data, ator/data e CorrelationId. Mutações de recursos da API registram ator, papel, método, caminho da rota, status, correlação e momento UTC.
- O audit trail não persiste corpo, query string, tokens nem dados clínicos; rotas de autenticação são deliberadamente excluídas. A falha de auditoria é registrada e não altera a resposta de negócio no MVP.
- Controllers administrativos agora usam policies nomeadas, centralizando a relação entre permissões e roles no composition root.
- A associação única e opcional `Patient.UserId` foi implementada com migration. O portal autenticado usa apenas rotas `/me`, filtra pelo usuário extraído do JWT e não aceita identificadores de prontuários no cliente; a criação duplicada de perfil é recusada.
- Adicionados casos de uso e testes para criar e consultar o perfil próprio. Uma conta diferente recebe `patient.profile.not_found` em vez de dados de outro paciente. O fluxo administrativo de vincular um prontuário pré-existente continua pendente e exige validação explícita pela clínica.
- A listagem e o cache Redis de pacientes agora propagam DTOs minimizados: nascimento não é listado e e-mail/telefone são mascarados. O frontend busca o detalhe completo somente quando um operador autorizado abre o registro para edição.
- Definido o plano de criptografia com banco/backups protegidos, envelope encryption via KMS/Key Vault, rotação e tratamento especial para campos pesquisáveis; nenhuma chave é adicionada ao código ou a `appsettings`. Detalhes em `docs/protecao-de-dados.md` e ADR 0006.
- Validações de encerramento: build .NET Release, lint/build Angular, 50 testes, gate de cobertura e migration idempotente validados.

### Etapa 21 — Hardening de deploy

**Confirmada em 11/08/2026.**

- API, worker e frontend foram configurados para execução sem root. O frontend passa a escutar a porta 8080 interna, enquanto o Compose local mantém o acesso em `localhost:4200`.
- Criado `docker-compose.production.yml`, separado do ambiente didático: API/worker sem portas publicadas, rede interna privada, frontend/API apenas expostos às redes de serviço e restrições de capabilities/filesystem em modo somente leitura.
- O manifesto exige valores injetados pelo secret manager da plataforma; `.env.production.example` é propositalmente fictício e serve somente para validar a sintaxe. Não há segredo de produção versionado.
- Criado workflow DAST manual e semanal com OWASP ZAP baseline, relatório HTML/JSON como artefato e stack Docker isolada. O workflow prepara um diretório gravável exclusivo para o scanner e consulta `/health/ready`, evitando falso 404 no alvo inicial.
- Validação local em 11/08/2026: as três imagens foram construídas com sucesso; `docker image inspect` e `id` dentro dos contêineres confirmaram UID 10001 (`clinichub`) para API, worker e frontend. API readiness e frontend responderam HTTP 200; o worker também permaneceu ativo após o RabbitMQ estabilizar. O contexto de build da API foi reduzido de aproximadamente 150 MB para 3,7 MB com exclusão de artefatos e documentos não relacionados no `.dockerignore`.
- A primeira tentativa local de baixar a imagem do OWASP ZAP não concluiu após as camadas iniciais, apesar de espaço em disco disponível; por isso o workflow remoto foi usado como fonte de evidência.
- A execução remota final [DAST baseline #31451309129](https://github.com/eliasmatheusouza/ClinicHub/actions/runs/31451309129) foi aprovada, publicou o artefato `dast-zap-report` e não encontrou alertas alto, médio ou baixo. Restaram apenas observações informativas de cache em respostas sem conteúdo sensível. Um alerta baixo anterior de `Cross-Origin-Resource-Policy` foi corrigido no middleware e protegido por teste de integração.

## Capacidade e performance

A capacidade simultânea não será declarada sem medição. A arquitetura atual, os critérios de pontuação e o plano de carga estão documentados em [docs/capacidade-e-performance.md](capacidade-e-performance.md).

**Princípio de implementação:** o gate será progressivo. A cobertura total começa na meta histórica de 70% para Domain/Application; o objetivo de 80% será aplicado ao código novo quando a infraestrutura de medição por pull request estiver configurada. Isso evita testes artificiais apenas para elevar uma métrica global.

### Etapa 15 — Gate de cobertura

**Confirmada em 10/08/2026.**

- Criado `scripts/Verify-Coverage.ps1`, que localiza os relatórios Cobertura mais recentes de Domain e Application, informa a cobertura de linhas e encerra com código diferente de zero abaixo da meta.
- O job de backend da GitHub Actions passou a executar a suíte completa com coleta de cobertura e o script com a meta mínima de 70%.
- A regra cobre somente Domain/Application neste momento: são as camadas com regras de negócio e cobertura representativa. Infrastructure/API continuarão com seus testes, sem uma meta artificial até que a estratégia de testes de integração seja ampliada.
- Validação executada: 42 testes da solution aprovados; gate aprovado com 74,57% no Domain e 74,76% na Application. Também foi validado que a execução com limiar de 75% falha.

### Etapa 14 — Testes de fluxos críticos

**Confirmada em 10/08/2026.**

- Foco inicial: `RegisterAccountCommandHandler`, `ConfirmEmailCommandHandler`, validators associados e regras do agregado `User` para confirmação de e-mail.
- Os testes adotam o padrão Arrange–Act–Assert: preparar dependências e dados determinísticos, executar um único comportamento e afirmar tanto o resultado como as interações indispensáveis.
- Mocks são usados para repositórios, hash de senha, token, envio de e-mail e Unit of Work; `FixedClock` é um stub determinístico do relógio.
- Os cenários incluem sucesso, e-mail já cadastrado, token inválido, token expirado e token já utilizado.
- Foram adicionados 7 testes: 3 de domínio para consumo/expiração/reuso do token e 4 de Application para registro e confirmação de e-mail.
- Validações executadas: `dotnet format ClinicHub.sln --verify-no-changes --no-restore`, 18 testes de Domain e 22 de Application aprovados. A nova aferição de cobertura atingiu 74,57% de linhas no Domain e 74,76% na Application, acima da meta de 70%.

## Ecossistema de portfólio

O ClinicHub é o projeto-base de um ecossistema que incluirá DocMind (IA aplicada a documentos), DevPulse (monitoramento em tempo real) e NetForge (biblioteca NuGet extraída de necessidades reais). A sequência, os critérios de encerramento e as regras de qualidade estão em [docs/plano-ecossistema-portfolio.md](plano-ecossistema-portfolio.md).
