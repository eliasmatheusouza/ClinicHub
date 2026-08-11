# Capacidade e Performance — Estado Atual do ClinicHub

> **Resposta curta:** o ClinicHub ainda não possui uma capacidade simultânea declarada. A estrutura atual é de desenvolvimento local e não foi submetida a teste de carga controlado.

## Por que não declarar um número agora

“Usuários simultâneos” não é uma propriedade fixa do código. Ela depende do cenário executado, hardware, banco, configuração de containers, cache, filas, latência de rede e metas de resposta.

O Compose atual executa API, frontend, SQL Server, Redis, RabbitMQ, worker e Seq em uma única máquina, sem limites de CPU/memória, réplicas ou autoscaling. Portanto, ele é excelente para desenvolvimento e integração, mas não representa um ambiente de produção nem permite prometer uma quantidade de acessos.

## Características que favorecem escala futura

| Componente | Contribuição | Limite atual |
|---|---|---|
| API com JWT | Permite réplicas sem sessão de servidor | Ainda não há réplicas, proxy ou deploy de produção. |
| Redis | Reduz leituras repetidas de listagens | Cache não substitui dimensionamento de banco. |
| RabbitMQ + worker | Retira notificações do caminho síncrono | Sem retry, DLQ e outbox por enquanto. |
| SQL Server | Persiste transações e regras críticas | Provável gargalo inicial; não houve tuning nem teste de concorrência. |
| Gate de CI | Reduz regressões de qualidade | Não mede throughput, latência ou consumo. |

## Pontuação de maturidade atual

As notas abaixo são uma avaliação arquitetural para aprendizado e não uma certificação de produção.

| Dimensão | Nota | Motivo resumido |
|---|---:|---|
| Arquitetura e domínio | 8,5/10 | Clean Architecture, DDD tático e CQRS pragmático. |
| Backend e integrações | 8/10 | API, Redis, RabbitMQ, worker, Dapper e health checks. |
| Qualidade e CI | 8/10 | Testes, cobertura mínima, relatórios, auditoria e CodeQL em implantação. |
| Frontend | 7,5/10 | Angular integrado, formulários e rotas; falta reforço de sessão e guards por role. |
| Documentação e aprendizado | 9/10 | Guias, ADRs, planos e fluxos bem registrados. |
| Segurança de produção | 5,5/10 | Base de autenticação existe; rate limiting iniciou, mas faltam headers, HTTPS, secrets, auditoria e policies. |
| Resiliência e operação | 5,5/10 | Logs e health checks existem; faltam outbox, DLQ, retry e alertas. |
| Deploy e escalabilidade | 4,5/10 | Compose local validado; ainda sem topologia/observabilidade de produção. |

**Nota como projeto de portfólio e aprendizado: 8/10.**  
**Nota como sistema pronto para dados médicos em produção: 5,5/10.**

## Etapa futura de teste de carga

Antes de declarar capacidade, executar uma etapa dedicada com k6 (ou ferramenta equivalente).

### Cenários mínimos

1. Login e renovação de sessão, com política de rate limiting considerada separadamente.
2. Listagem paginada e filtrada de pacientes, com cache frio e quente.
3. Criação, confirmação e reagendamento de consulta.
4. Registro de pagamento e consulta de relatório financeiro.
5. Carga mista, próxima ao comportamento real de uma recepção.

### Níveis iniciais de experimento

Executar em ambiente semelhante ao que se deseja avaliar, aumentando gradualmente usuários virtuais: 20, 50, 100 e além. Cada nível só deve avançar após avaliar os resultados anteriores.

### Métricas obrigatórias

- latência p50, p95 e p99;
- taxa de erros HTTP e de regras de negócio;
- throughput por endpoint;
- CPU e memória da API, worker e banco;
- tempo de consulta SQL, taxa de cache e tamanho de fila RabbitMQ;
- saturação de conexões e comportamento após falhas.

### Critério de capacidade declarada

Uma capacidade só poderá ser documentada no formato:

> “Suporta **N usuários virtuais no cenário X**, com p95 menor que **Y ms**, taxa de erro menor que **Z%**, em ambiente com configuração **W**.”

Sem cenário, ambiente e métricas, “suporta N usuários” é apenas uma estimativa sem valor técnico.
