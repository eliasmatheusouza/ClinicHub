# Decisões Arquiteturais do ClinicHub — O Porquê de Cada Escolha

Este documento responde à pergunta mais importante para quem estuda o projeto: **por que o ClinicHub usa esta escolha e não outra?** Ele é um catálogo de decisões estruturais atuais. Não significa que toda tecnologia seja universalmente superior; cada escolha é adequada ao contexto didático e ao escopo atual do ClinicHub.

Para decisões que mudam o rumo do sistema, há ADRs curtos em [docs/adr](adr). Este guia explica o panorama; o ADR preserva contexto e consequência de uma decisão específica.

## Como ler as decisões

Em cada item, considere quatro perguntas:

1. Qual problema a escolha resolve aqui?
2. Por que a alternativa não foi a primeira opção neste contexto?
3. Qual custo ou limitação estamos aceitando?
4. Em que situação a decisão deve ser revista?

## Mapa rápido

| Área | Escolha atual | Alternativas consideradas |
|---|---|---|
| Arquitetura | Monólito modular com Clean Architecture | Microservices, aplicação em camadas sem fronteiras claras |
| Domínio | DDD tático e CQRS pragmático | CRUD anêmico, CQRS completo com segregação física |
| Backend | .NET 8 + ASP.NET Core Controllers | Node.js, Java, Minimal API |
| Persistência de escrita | SQL Server + EF Core | PostgreSQL, MySQL, Dapper para tudo |
| Leitura analítica | Dapper | EF Core para todas as queries |
| Cache | Redis distribuído | Cache em memória, sem cache |
| Eventos | RabbitMQ + Worker separado | Chamada síncrona, background task dentro da API |
| Frontend | Angular standalone + Material | React, Vue, Angular baseado em NgModules |
| Autenticação | JWT curto + refresh rotativo | Cookie de sessão, JWT longo sem renovação |
| Operação local | Docker Compose | Instalação manual, Kubernetes local |
| Qualidade | xUnit, Moq, Coverlet, CI e análise estática | Teste manual como única evidência |

## Arquitetura e organização do código

### Por que um monólito modular, e não microservices?

O ClinicHub possui módulos distintos, mas ainda compartilha domínio, equipe, dados e ciclo de entrega. Um monólito modular permite aprender fronteiras de negócio sem pagar desde o início o custo operacional de rede, descoberta de serviços, observabilidade distribuída, deploys independentes e consistência eventual entre vários bancos.

Microservices fazem sentido quando há fronteiras de domínio estáveis, equipes autônomas, necessidades de escala/deploy muito diferentes ou uma razão operacional concreta. Dividir cedo apenas transforma chamadas de método em falhas de rede.

**Custo aceito:** os módulos ainda são entregues juntos. A evolução correta é reforçar contratos, eventos confiáveis e observabilidade antes de considerar uma extração.

### Por que Clean Architecture?

As camadas `Domain`, `Application`, `Infrastructure` e `API` fazem as dependências apontarem para as regras de negócio. Assim, a regra de conflito de agenda não precisa conhecer HTTP, SQL Server ou RabbitMQ, e pode ser testada sem essas tecnologias.

Uma aplicação em camadas simples pode ser suficiente para CRUD pequeno. Aqui, há autenticação, agenda, pagamentos, cache e eventos; separar contratos e detalhes reduz o acoplamento à medida que o projeto cresce.

**Custo aceito:** mais projetos, interfaces e arquivos. Para um endpoint trivial, isso parece mais lento; o ganho aparece na manutenção e nos testes.

### Por que DDD tático, e não um modelo puramente anêmico?

Agregados, value objects e invariantes colocam regras como “não confirmar uma consulta cancelada” perto dos dados que elas protegem. Isso evita que controllers e serviços genéricos virem o único lugar onde a regra existe.

DDD não é usar nomes sofisticados em todo lugar. CRUD simples, DTOs e consultas de relatório continuam simples. O projeto usa DDD onde há comportamento e invariantes, não como cerimônia universal.

### Por que CQRS pragmático com MediatR?

Commands representam intenção de alterar estado; queries representam leitura. O MediatR encaminha cada intenção a um handler e permite aplicar comportamentos transversais, como validação, sem poluir controllers.

Não há bancos separados nem event sourcing: essa seria uma forma mais complexa de CQRS, desnecessária para o MVP. Sem MediatR, controllers poderiam chamar serviços diretamente; a escolha atual favorece casos de uso explícitos e testes isolados.

Veja o [ADR 0001](adr/0001-clean-architecture-e-cqrs.md).

### Por que FluentValidation, e não validação manual ou somente Data Annotations?

Os validators ficam próximos aos commands e queries, suportam regras compostas, mensagens consistentes e testes sem HTTP. Um pipeline MediatR os executa antes do handler.

Data Annotations são úteis para regras simples de DTO e UI, mas ficam limitadas quando a regra envolve coleções, datas, dependências entre campos ou contexto de caso de uso. FluentValidation não substitui invariantes do domínio: valida entrada; o domínio protege estado válido.

### Por que Repository e Unit of Work em vez de chamar `DbContext` em todo handler?

Os contratos da Application expressam o que o caso de uso precisa e permitem testar o fluxo sem EF Core. A Infrastructure decide como persistir. O `Unit of Work` explicita o momento do commit e evita gravar parcialmente antes de todas as regras passarem.

Para uma aplicação pequena, injetar `DbContext` diretamente é mais curto. O custo é tornar Application dependente da tecnologia de persistência e misturar consulta/persistência com regras. O repositório não deve virar uma cópia genérica de todos os métodos do ORM; ele precisa representar necessidades de domínio.

## Backend e contratos HTTP

### Por que .NET 8 e ASP.NET Core?

O .NET 8 oferece runtime LTS, bom ecossistema para APIs, DI nativa, testes, EF Core, health checks e integração madura com ferramentas de segurança. ASP.NET Core permite controllers, middleware, OpenAPI e execução eficiente em contêiner.

Node.js ou Java poderiam atender ao mesmo produto. Esta escolha favorece o objetivo de aprender um ecossistema .NET moderno de ponta a ponta, não uma superioridade absoluta de linguagem.

### Por que Controllers REST, e não Minimal API ou GraphQL?

Controllers mantêm endpoints, filtros, autorização e contratos organizados à medida que os módulos aumentam. Minimal API seria excelente para serviço muito pequeno ou protótipo, mas reduziria a estrutura didática das convenções MVC neste projeto.

GraphQL seria útil quando clientes precisassem compor muitos formatos de leitura. O ClinicHub atual possui operações de negócio claras; REST é mais simples de documentar no Swagger, proteger, cachear e testar.

### Por que Swagger/OpenAPI?

OpenAPI transforma a API em contrato navegável: acelera integração do Angular, testes manuais e consumo futuro por parceiros. Não substitui testes de integração, mas reduz ambiguidade de payloads, códigos de resposta e autenticação.

## Dados e persistência

### Por que SQL Server, e não PostgreSQL ou MySQL?

SQL Server combina transações relacionais, integridade, ferramentas conhecidas no ecossistema .NET e uma experiência clara para estudar EF Core e Dapper. Agenda, pagamento e usuário precisam de consistência transacional e consultas relacionais previsíveis.

PostgreSQL seria uma alternativa técnica excelente e, muitas vezes, mais econômica em cloud; MySQL também atenderia vários casos. A escolha não é um requisito de produto. Ela deve ser revista conforme custo, competência da equipe, serviços gerenciados disponíveis, requisitos de busca e estratégia de cloud.

### Por que EF Core para escrita e persistência de domínio?

EF Core reduz código repetitivo em mapeamento, rastreia mudanças, trabalha com migrations e permite persistir agregados e relações de forma produtiva. Ele é adequado para commands e para regras que alteram o estado do domínio.

Usar SQL manual para tudo daria controle máximo, mas aumentaria o custo de cada alteração no modelo. EF Core não dispensa entender SQL, índices e transações; ele só abstrai tarefas repetitivas.

### Por que Dapper para o relatório financeiro, e não EF Core?

O relatório de receita é uma leitura analítica: agrega dados por período e moeda, não carrega agregados nem altera estado. Dapper permite escrever uma projeção SQL direta, trazendo apenas as colunas necessárias e sem change tracker.

EF Core também poderia executar essa consulta. Dapper foi escolhido para demonstrar uma abordagem híbrida: **EF Core para persistência e escrita; Dapper para uma leitura especializada que ganha clareza com SQL explícito**. Isso não significa que Dapper seja mais rápido em toda consulta nem que EF Core seja inadequado para relatórios.

**Custo aceito:** SQL explícito precisa de teste e revisão quando o esquema evolui. Veja o [ADR 0004](adr/0004-dapper-para-relatorios-financeiros.md).

### Por que migrations automáticas no desenvolvimento?

Migrations versionam a evolução do esquema junto com o código e permitem que um novo ambiente local seja iniciado sem passos manuais. No Compose de desenvolvimento, a API pode aplicar migrations para reduzir fricção didática.

Em produção, migrations devem ser controladas pelo processo de deploy, com backup, janela, revisão e plano de rollback. Auto-migrate não é uma política adequada para banco crítico em produção.

### Por que não usar event sourcing agora?

Event sourcing preservaria cada mudança como evento imutável e reconstruiria estado a partir deles. É poderoso para auditoria e domínios específicos, mas exige versionamento de eventos, projeções, replay, consistência eventual e operação mais complexa.

O ClinicHub já obtém consistência relacional com SQL Server e auditoria aplicada. A próxima evolução é outbox e idempotência, não event sourcing. Essa decisão pode mudar se houver necessidade real de reconstrução temporal do domínio.

## Cache e mensageria

### Por que Redis, e não cache em memória?

Redis torna o cache compartilhável entre réplicas futuras da API, enquanto cache em memória fica preso a uma única instância e pode devolver resultados inconsistentes em escala horizontal. Ele é usado nas listagens paginadas e filtradas de pacientes, onde leituras se repetem.

Redis é uma otimização, não fonte de verdade: se estiver indisponível, a API registra o problema e busca dados no SQL Server. Sem cache, o sistema continuaria correto, mas pode aumentar carga no banco.

**Custo aceito:** invalidação de cache é difícil; por isso há TTL e versão de chave após commit. Veja o [ADR 0002](adr/0002-redis-para-cache-distribuido.md).

### Por que RabbitMQ, e não enviar a notificação dentro da API?

Confirmar uma consulta precisa responder ao usuário sem esperar um e-mail ou WhatsApp externo. RabbitMQ coloca a notificação em fila durável e um worker a processa fora da requisição HTTP, reduzindo acoplamento e tempo de resposta.

Uma chamada síncrona é mais simples, mas falhas do fornecedor tornam a confirmação lenta ou indisponível. Uma `BackgroundService` dentro da própria API reduz a infraestrutura, porém compete com a API e dificulta escalar/processar falhas separadamente.

**Limitação atual:** RabbitMQ sozinho não garante que uma alteração persistida e uma mensagem publicada nunca se desencontrem. A Etapa 23 adicionará outbox, retry, DLQ e idempotência. Veja o [ADR 0003](adr/0003-rabbitmq-para-notificacoes-assincronas.md).

### Por que um Worker Service separado?

O worker possui ciclo de vida, logs e escala próprios. Ele pode continuar processando fila mesmo quando a API não recebe tráfego, e falhas de notificação não devem reiniciar ou degradar a API.

O custo é operar outro processo e observar fila, retries e saúde. Isso é deliberado para ensinar integração assíncrona; a resiliência completa ainda está planejada.

## Frontend

### Por que Angular standalone, e não React ou Vue?

Angular oferece framework completo: roteamento, DI, formulários reativos, HTTP, testes e convenções consistentes. Para um sistema administrativo com muitos formulários e telas, essa padronização reduz decisões repetidas e facilita onboarding. O modo standalone reduz a dependência de `NgModule` e deixa imports mais locais.

React e Vue são excelentes alternativas. React oferece flexibilidade e grande ecossistema; Vue tem curva inicial amigável. A escolha é Angular porque o projeto busca uma SPA estruturada no ecossistema TypeScript, não porque um framework seja objetivamente melhor.

### Por que Angular Material, e não construir todos os componentes do zero?

Material fornece componentes acessíveis, consistentes e produtivos para tabelas, formulários, diálogos e feedback. Isso permite concentrar estudo em fluxo de negócio e integração, em vez de reconstruir controles básicos.

O custo é uma identidade visual menos exclusiva e atenção necessária ao bundle. Um design system próprio passa a valer quando o produto tiver necessidades de marca, acessibilidade e comportamento que Material não atenda bem.

### Por que SPA consumindo API REST, e não renderização no servidor?

Uma SPA separa frontend e backend, permite aprender contratos HTTP, JWT e implantação independente. O domínio atual é um sistema autenticado de operação interna, no qual SEO não é prioridade.

SSR seria interessante para portal público com descoberta orgânica, primeira renderização rápida ou páginas institucionais. Não foi necessário para o escopo atual.

## Segurança

### Por que JWT de curta duração com refresh token rotativo, e não sessão/cookie ou JWT longo?

JWT curto permite que a API permaneça sem sessão de servidor e seja replicada mais facilmente. O refresh token rotativo reduz o tempo de exposição do access token e permite revogação/rotação por sessão. O token de refresh é persistido somente como hash.

Cookies de sessão seriam uma opção sólida, especialmente para uma aplicação web no mesmo domínio, mas exigiriam estratégia de proteção CSRF e armazenamento de sessão. JWT longo é simples, porém aumenta impacto de vazamento e reduz controle de revogação.

### Por que confirmação de e-mail com token armazenado como hash?

O token bruto é uma credencial temporária. Persistir apenas SHA-256 reduz o impacto caso a base seja exposta: o banco não contém o link utilizável. O token expira e tem uso único.

Não basta confirmar e-mail para resolver toda identidade do usuário; é uma barreira inicial contra cadastro incorreto. Reenvio, rate limiting e auditoria continuam necessários.

### Por que autorização no backend, e não somente guards no Angular?

O navegador é controlado pelo usuário. Guard de rota melhora experiência e evita apresentar ações sem sentido, mas não protege dados. A decisão real de acesso ocorre na API por roles, claims, policies e ownership.

A evolução planejada é autorização por recurso/tenant, pois role global não resolve todos os cenários clínicos. Veja [Auditoria, autorização e ownership](auditoria-e-autorizacao.md).

### Por que rate limiting, headers de segurança e HTTPS/HSTS?

São controles de borda com boa relação entre custo e proteção: rate limiting reduz abuso de login; headers diminuem vetores comuns do navegador; HTTPS protege tráfego; HSTS evita downgrade depois que HTTPS é confiável.

Eles não substituem validação, autorização, secrets e monitoramento. HSTS deve ser habilitado com cuidado em produção, pois um domínio configurado incorretamente pode ficar inacessível no navegador por um período.

## Observabilidade e operação

### Por que Serilog, Seq e Correlation ID, e não somente `Console.WriteLine`?

Logs estruturados permitem filtrar por propriedades como usuário, rota, erro e correlação. Seq facilita consulta local; o `CorrelationId` conecta logs de uma requisição através de middleware e serviços.

Console ainda é útil e recebe os logs em contêiner. O limite é que logs, sozinhos, não fornecem métricas, traces distribuídos ou alertas. A Etapa 24 adicionará OpenTelemetry e dashboards.

### Por que health checks de liveness/readiness?

Liveness diz se a aplicação está viva; readiness indica se dependências necessárias, como SQL Server, Redis e RabbitMQ, estão prontas para atender. Isso evita enviar tráfego para uma API iniciada, porém sem banco disponível.

Health check não corrige dependência falha nem substitui alertas. Ele é um contrato para Docker, orquestrador e diagnóstico operacional.

### Por que Docker Compose, e não instalação manual ou Kubernetes?

Compose descreve API, frontend, banco, cache, fila, worker e logs em uma configuração versionada. Quem clona o projeto consegue reproduzir a topologia local sem instalar cada servidor manualmente.

Kubernetes resolveria necessidades de produção como réplicas e autoscaling, mas adicionaria curva operacional alta e não é necessário para o laboratório local. Compose não é um substituto de produção; há manifesto isolado e um plano de cloud/IaC para evolução.

### Por que k6 para carga, e não JMeter ou teste manual?

k6 permite cenários versionados em JavaScript, thresholds automatizados e execução em contêiner. Isso combina com CI e com uma SPA TypeScript. O teste manual não mede concorrência de forma repetível.

JMeter é uma alternativa madura, especialmente em organizações que já usam sua interface e plugins. A escolha k6 privilegia cenários como código e leitura simples em pull request. Veja [Capacidade e performance](capacidade-e-performance.md).

## Testes e qualidade

### Por que xUnit, Moq e Coverlet?

xUnit é o framework de testes .NET usado nas suites de Domain, Application, Infrastructure e integração. Moq isola contratos da Application em testes unitários; Coverlet coleta cobertura. Isso permite testar regras e handlers rapidamente sem exigir banco ou broker para cada caso.

Mocks não devem reproduzir a implementação: eles verificam colaboração e cenários de falha. Testes de integração com `WebApplicationFactory` complementam mocks ao validar HTTP, DI e pipeline real.

### Por que GitHub Actions, e não executar validações só na máquina local?

A CI executa formatação, build, testes, cobertura, frontend, auditoria e imagens Docker em ambiente independente. Isso reduz “funciona na minha máquina” e dá evidência para revisão de pull request.

GitLab CI, Azure DevOps ou Jenkins também poderiam atender. GitHub Actions foi escolhido porque o repositório é público no GitHub e a integração com PR, secrets, CodeQL e Dependabot é direta.

### Por que SonarCloud, CodeQL, Dependabot e OWASP ZAP juntos?

As ferramentas se complementam:

- **SonarCloud** aponta qualidade, duplicação, vulnerabilidades e cobertura no código analisado.
- **CodeQL** procura padrões de segurança por análise semântica de C# e TypeScript/JavaScript.
- **Dependabot** monitora dependências e propõe atualizações.
- **OWASP ZAP baseline** faz uma varredura dinâmica básica contra a aplicação em execução.

Nenhuma delas prova segurança completa. Falsos positivos, limites de cobertura e vulnerabilidades de design ainda exigem revisão humana, testes e modelagem de ameaça.

### Por que branch protection e revisão obrigatória?

Checks automatizados detectam classes de erro; revisão humana avalia intenção, domínio, risco e clareza. A proteção da `main` exige ambos para criar um hábito de engenharia semelhante ao de equipes reais.

Em emergência, administradores podem precisar de um processo de exceção documentado. Bypass deve ser exceção auditável, não rotina.

## Como decidir a próxima tecnologia

Antes de adicionar uma biblioteca ou serviço, registre no ADR ou na pull request:

1. problema concreto e usuários afetados;
2. alternativas realmente avaliadas;
3. custo operacional, segurança, privacidade e lock-in;
4. impacto em testes, observabilidade e deploy;
5. critério que fará a equipe revisar ou reverter a escolha.

Esse processo evita adotar tecnologias apenas por tendência. A arquitetura do ClinicHub deve permanecer simples o suficiente para ser ensinada, mas sólida o bastante para expor os problemas reais de um sistema clínico.
