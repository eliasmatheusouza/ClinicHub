# Próximas Evoluções — Roteiro Arquitetural do ClinicHub

Este documento transforma a avaliação arquitetural do ClinicHub em um roteiro de aprendizado. Ele não é uma promessa de prazo: cada etapa só deve ser marcada como concluída quando sua evidência técnica estiver registrada no [plano de execução](plano-de-execucao.md).

## Como usar este roteiro

1. Trabalhe em uma etapa por vez, em uma branch e pull request próprios.
2. Comece pelo problema que a etapa resolve; tecnologia é consequência, não ponto de partida.
3. Defina testes, métricas e critério de aceite antes de implementar.
4. Registre decisão, evidência de teste e limitação restante.
5. Marque a etapa como concluída apenas quando o critério de conclusão for atendido.

## Ordem recomendada

| Etapa | Estado | Evolução | Por que agora | Evidência para concluir |
|---:|---|---|---|---|
| 22 | 🟨 Em andamento | Capacidade e performance | A aplicação precisa de medidas antes de qualquer afirmação sobre acessos simultâneos. | Três execuções por cenário, coleta de recursos e declaração de capacidade contextualizada. |
| 23 | ⬜ Planejada | Confiabilidade de eventos | Notificações não podem ser perdidas se banco e broker falharem em momentos diferentes. | Outbox, publicação confiável, retry limitado, DLQ, idempotência e testes de falha. |
| 24 | ⬜ Planejada | Observabilidade operacional | Logs isolados não explicam uma jornada completa nem permitem alertar antes de um incidente. | Traces, métricas, dashboard e alertas de latência, erro e fila. |
| 25 | ⬜ Planejada | Testes end-to-end | API e frontend podem estar corretos isoladamente e falhar na jornada do usuário. | Suite Playwright cobrindo fluxos críticos autenticados na CI. |
| 26 | ⬜ Planejada | Autorização por recurso | Papel global não é suficiente para proteger dados clínicos entre clínicas, pacientes e profissionais. | Policies e testes de ownership/escopo para cada recurso sensível. |
| 27 | ⬜ Planejada | Privacidade e LGPD aplicada | Dados de saúde exigem ciclo de vida, minimização e evidência de tratamento responsável. | Exportação, retenção, anonimização sob regra, classificação de dados e auditoria. |
| 28 | ⬜ Planejada | Cloud e infraestrutura como código | Docker local não reproduz rede, secrets, backup, limites nem deploy de produção. | Ambiente didático em cloud criado por IaC, com orçamento, secrets e plano de destruição. |
| 29 | ⬜ Planejada | Robustez do frontend | A qualidade percebida depende de sessão, acessibilidade, feedback e tolerância a falhas no Angular. | Guards, tratamento global de erro, estados de UI e testes de componentes. |
| 30 | ⬜ Planejada | Arquitetura ensinável | Decisões corretas sem contexto são difíceis de manter e de ensinar à comunidade. | Diagramas C4 atualizados e ADRs para decisões relevantes. |
| 31 | ⬜ Planejada | Evolução funcional | Recursos de produto dão contexto real para segurança, domínio e operação. | Portal do paciente, equipe, reenvio de confirmação e notificações reais entregues por incrementos. |

## Etapa 22 — Capacidade e performance

O objetivo não é alcançar um número grande de usuários virtuais. É descobrir, de modo reproduzível, em qual cenário o sistema atende seus SLOs e qual componente limita a evolução. A referência completa e o primeiro baseline estão em [capacidade-e-performance.md](capacidade-e-performance.md).

### Protocolo para uma medição confiável

1. **Preparar um ambiente controlado.** Use massa de dados sintética, anote commit, CPU/RAM, limite do Docker, configuração dos serviços e demais processos em execução. Não compare números de máquinas diferentes como se fossem equivalentes.
2. **Esperar a prontidão.** Confirme `GET /health/ready` antes de iniciar o gerador de carga. Uma aplicação iniciando ou um banco recuperando volume invalida a medição.
3. **Definir o cenário e os SLOs.** Registre rota, verbo, volume de dados, autenticação, tempo de reflexão e limites de p95, p99 e erros. Não use apenas média de latência.
4. **Executar smoke e baseline.** O smoke valida o próprio teste. O baseline deve ser repetido pelo menos três vezes; use mediana e intervalo, nunca um único resultado.
5. **Separar cache frio e quente.** Rode uma vez sem cache preenchido e outra com cache aquecido. Ambos representam experiências válidas, mas respondem perguntas diferentes.
6. **Coletar telemetria durante a carga.** Em outra janela, acompanhe `docker stats`, logs e latência no Seq, saúde, CPU/memória da API e banco, Redis e fila RabbitMQ. Uma foto depois do teste não representa o pico.
7. **Aumentar um nível de cada vez.** Depois de 25 VUs estáveis, teste 50 e 100 VUs, interrompendo a progressão no primeiro SLO violado. Investigue o gargalo antes de elevar novamente.
8. **Registrar e comunicar com precisão.** A conclusão deve informar ambiente, cenário, pico de VUs, duração, throughput, p95/p99, erro, versão e limitações. Isso é uma capacidade medida; qualquer número sem esse contexto é especulação.

### Cenários de carga por prioridade

| Prioridade | Cenário | O que mede | Cuidados |
|---:|---|---|---|
| 1 | Listagem paginada de pacientes | Leitura autenticada, Redis, API e SQL Server. | Executar cache frio e quente; não declarar capacidade só com ele. |
| 2 | Login e renovação de sessão | Autenticação, hash de senha e rate limiting. | Usar contas sintéticas; avaliar rate limiting separadamente. |
| 3 | Criar, confirmar e reagendar consulta | Escrita concorrente, regras de agenda e transações. | Dados isolados e limpeza planejada após a execução. |
| 4 | Registrar pagamento e consultar relatório | Consistência financeira e leitura analítica. | Nunca usar valores ou dados reais. |
| 5 | Carga mista de recepção | Relação realista entre leituras, escritas e ações administrativas. | Definir a distribuição de tráfego e o tempo de reflexão. |
| 6 | Notificações sob falha | Fila, worker, retries e DLQ após a Etapa 23. | Induzir falhas somente em ambiente autorizado e reversível. |

### Critério de conclusão da Etapa 22

- [ ] Três repetições do cenário de leitura para cada nível testado, com resultados comparados.
- [ ] Cenários de escrita e carga mista executados com massa sintética.
- [ ] CPU, memória, banco, cache e fila coletados durante a carga.
- [ ] Gargalos e decisões de otimização registrados em ADR quando relevantes.
- [ ] Capacidade declarada apenas no formato “N VUs no cenário X, em ambiente W, com p95 Y e erro Z”.
- [ ] Testes executados somente em ambiente autorizado, nunca contra produção sem janela e plano de reversão.

## Etapa 23 — Confiabilidade de eventos

### Problema

Persistir uma mudança no SQL Server e publicar uma mensagem no RabbitMQ são duas operações independentes. Se uma delas falhar, a notificação pode ser perdida ou duplicada.

### O que implementar

- **Outbox Pattern:** gravar o evento na mesma transação da alteração de domínio; um processo separado o publica depois.
- **Retry limitado:** reprocessar falhas transitórias com atraso e limite explícito.
- **Dead Letter Queue (DLQ):** encaminhar mensagens que excederam o retry para inspeção e reprocessamento controlado.
- **Idempotência:** registrar ou deduzir que uma mensagem já foi consumida para que a repetição não envie duas notificações nem altere o estado duas vezes.

### Critério de conclusão

- [ ] Teste que interrompe o broker após a transação e comprova recuperação posterior.
- [ ] Teste de mesma mensagem recebida duas vezes sem efeito duplicado.
- [ ] DLQ observável e procedimento documentado de reprocessamento.
- [ ] Métricas de mensagens pendentes, retry e falha definitiva.

## Etapa 24 — Observabilidade operacional

Implementar OpenTelemetry para correlacionar uma requisição do Angular até API, SQL Server, Redis e RabbitMQ. Expor métricas de taxa de erro, duração, fila, retries e saúde; usar Prometheus/Grafana ou serviço equivalente para dashboards e alertas.

**Critério de conclusão:** uma solicitação pode ser acompanhada por um `traceId`; há dashboard de latência/erros/fila e alerta de teste acionado e documentado.

## Etapa 25 — Testes end-to-end

Criar uma suíte Playwright para as jornadas: criar conta, confirmar e-mail em ambiente de teste, login, criar paciente, agendar, consultar agenda e validar acesso negado. Executar contra stack isolada na CI, com credenciais e dados sintéticos.

**Critério de conclusão:** fluxo crítico verde na CI, vídeos/traces disponíveis como artefato em falha e nenhum teste depende de dados manuais externos.

## Etapa 26 — Autorização por recurso

Evoluir de papéis globais para policies de recurso e escopo. Exemplos: recepcionista só administra a própria clínica; profissional acessa apenas agenda e pacientes autorizados; paciente consulta somente seus próprios dados. Toda regra precisa de teste positivo e negativo.

**Critério de conclusão:** matriz de permissões documentada, policies implementadas, consultas filtradas por ownership no servidor e testes que impeçam acesso horizontal indevido.

## Etapa 27 — Privacidade e LGPD aplicada

Mapear dados pessoais e sensíveis, finalidade, base legal, retenção, responsáveis e locais de armazenamento. Implementar exportação quando aplicável, anonimização ou exclusão sob regras de retenção, masking em logs e revisão de acesso a auditoria.

**Critério de conclusão:** inventário de dados e política de retenção versionados; logs sem dados sensíveis; fluxos de exportação/anonimização testados com dados sintéticos. Revisão jurídica continua necessária para uso real.

## Etapa 28 — Cloud e infraestrutura como código

Criar um ambiente de aprendizado separado da produção, com Terraform ou Bicep. A infraestrutura deve cobrir rede, registry, compute, banco, cache/fila quando aplicável, secrets, logs, orçamento e uma forma segura de destruir os recursos.

**Critério de conclusão:** `plan` revisável, deploy repetível, secrets fora do Git, orçamento/alerta configurado, backup/restore testado e `destroy` documentado. Custos e limites gratuitos devem ser validados no provedor antes de criar recursos.

## Etapa 29 — Robustez do frontend

Adicionar guards e autorização visual sem confiar nela como proteção final, interceptor de erro/autenticação, estados de carregamento e vazio, feedback acessível, tratamento de expiração de sessão e testes de componentes. Aplicar WCAG como referência prática para contraste, foco de teclado, rótulos e mensagens de erro.

**Critério de conclusão:** rotas sensíveis protegidas no cliente e servidor, telas críticas navegáveis por teclado, estados de erro/carregamento cobertos por teste e sem segredos no bundle.

## Etapa 30 — Arquitetura ensinável

Manter diagramas C4 de contexto, contêineres e componentes; incluir fluxos de autenticação, agendamento e notificação. Criar ADRs curtos para decisões que tenham alternativas relevantes, como Dapper versus EF Core, JWT, RabbitMQ, Redis, Outbox e cache.

**Critério de conclusão:** diagramas refletem o código atual, ADRs explicam contexto/decisão/consequências e o README aponta para o material.

## Etapa 31 — Evolução funcional

Entregar por incrementos: gestão segura de equipe e convites, portal do paciente, cancelamento/reagendamento, reenvio de confirmação e notificações por provedor real. Cada recurso novo deve passar por domínio, autorização, privacidade, teste e operação antes de ser considerado concluído.

**Critério de conclusão:** cada incremento possui caso de uso, política de acesso, testes e documentação de operação; integrações externas possuem sandbox, timeout e tratamento de falha.
