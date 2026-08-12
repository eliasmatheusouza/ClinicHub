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

## Etapa 22 em andamento: teste de carga com k6

O repositório agora possui o primeiro cenário executável em [performance/k6/patients-read.js](../performance/k6/patients-read.js). Ele simula uma recepção autenticada consultando a primeira página de pacientes — uma rota adequada para começar porque atravessa autenticação, API, cache Redis e banco, mas não cria registros artificiais, não altera dados clínicos e não sobrecarrega o rate limit de login.

O login ocorre apenas na preparação do teste; os usuários virtuais reutilizam o token de acesso. Isso mede a leitura de pacientes, e não a política de proteção do endpoint de autenticação.

### SLOs iniciais do laboratório

Estes limites são hipóteses de qualidade para o ambiente local, não promessas de produção. O k6 falha quando um deles não é atendido:

| Indicador | Limite inicial | Por que importa |
|---|---:|---|
| Erros HTTP | menos de 1% | Distingue lentidão de indisponibilidade. |
| Latência p95 geral | menor que 500 ms | 95% das requisições devem terminar dentro desse tempo. |
| Latência p99 geral | menor que 1 s | Expõe a cauda lenta, invisível em médias. |
| Busca de pacientes p95 | menor que 400 ms | Mantém uma meta específica para a rota medida. |
| Checks k6 | mais de 99% | Confirma status e formato básico da resposta, não apenas conexão HTTP. |

`p95` significa que apenas 5% das requisições foram mais lentas que o valor observado; `p99` permite enxergar os 1% mais lentos. Uma média baixa pode esconder uma experiência ruim para parte dos usuários, por isso não é usada como critério de aprovação.

### Executar de forma reproduzível

1. Suba a stack de desenvolvimento e espere a prontidão:

```powershell
Copy-Item .env.example .env
docker compose up -d --build
Invoke-WebRequest http://localhost:8082/health/ready
```

2. Execute o teste curto de segurança (*smoke*). As credenciais abaixo são exclusivamente as seeds de desenvolvimento presentes no `.env.example`; não use credenciais de produção em testes de carga.

```powershell
./scripts/Invoke-K6PatientsRead.ps1 `
  -Profile smoke `
  -UserEmail 'admin@clinichub.local' `
  -UserPassword 'Admin123!'
```

3. Se o smoke estiver verde, execute a linha de base. Ela aumenta gradualmente até 25 usuários virtuais, mantém esse pico por um minuto e dura dois minutos e trinta segundos.

```powershell
./scripts/Invoke-K6PatientsRead.ps1 `
  -Profile baseline `
  -UserEmail 'admin@clinichub.local' `
  -UserPassword 'Admin123!' `
  -CaptureResources
```

O script usa a imagem oficial `grafana/k6` no Docker; não instala k6 na máquina. Os resumos JSON são criados em `artifacts/performance/`, diretório ignorado pelo Git. Com `-CaptureResources`, ele também coleta uma amostra a cada dois segundos de CPU, memória, I/O e PIDs dos contêineres da API, SQL Server, Redis, RabbitMQ e worker, em um arquivo `*.jsonl` no mesmo diretório. O intervalo e os contêineres podem ser ajustados com `-ResourceSampleIntervalSeconds` e `-ResourceContainerNames`.

Em Docker Desktop no Windows, `host.docker.internal` permite que o contêiner do k6 alcance a API publicada em `localhost:8082`. Para outro alvo, informe `-BaseUrl 'http://host.docker.internal:porta'` ou a URL do ambiente autorizado.

### Como ler o resultado

1. Verifique se os thresholds ficaram verdes. Um teste vermelho é dado de diagnóstico, não falha a ser escondida.
2. Registre versão do commit, perfil, máquina/CPU/RAM, configuração Docker, duração, usuários virtuais, throughput, p95/p99 e taxa de erro.
3. Durante a carga, use `-CaptureResources` para preservar as amostras de recursos e observe também logs/latência no Seq, `/health/ready`, cache Redis, SQL Server e a fila RabbitMQ. Não atribua automaticamente uma latência à API sem observar dependências.
4. Repita o mesmo perfil ao menos três vezes. Use mediana ou intervalo dos resultados, pois um único experimento é sujeito a aquecimento de cache e ruído da máquina.
5. Só aumente a carga após o nível anterior cumprir os thresholds. Pare ao primeiro limite violado e investigue o gargalo antes de prosseguir.

### Evidência local: três repetições da linha de base

Em **11/08/2026 (BRT)**, o perfil `baseline` foi executado três vezes com a API pronta em Docker Compose e todos os thresholds aprovados. Cada experimento fez rampa de 0 a 25 VUs em 60 segundos, sustentou 25 VUs por 60 segundos e reduziu a carga nos 30 segundos finais.

| Repetição | Requisições | Throughput | Erros | p95 geral | p99 geral |
|---:|---:|---:|---:|---:|---:|
| 1 | 2.551 | 16,92 req/s | 0,00% | 4,49 ms | 6,01 ms |
| 2 | 2.552 | 16,93 req/s | 0,00% | 4,16 ms | 6,97 ms |
| 3 | 2.552 | 16,93 req/s | 0,00% | 4,74 ms | 8,82 ms |
| **Mediana** | **2.552** | **16,93 req/s** | **0,00%** | **4,49 ms** | **6,97 ms** |

Em todas as execuções, os checks do k6 foram 100% aprovados e a busca de pacientes também ficou abaixo do threshold específico de 400 ms no p95. A segunda e a terceira repetições usaram `-CaptureResources`, gerando 38 amostras por serviço em cada execução (a cada dois segundos). A primeira é válida para latência/throughput, mas não possui coleta contínua de recursos porque o recurso ainda não existia.

| Serviço | CPU mediana (rep. 2–3) | Pico de CPU observado | Pico de memória observado |
|---|---:|---:|---:|
| API | 4,87%–5,09% | 10,13% | 424,3 MiB |
| SQL Server | 1,08%–1,31% | 6,82% | 1.401,9 MiB |
| Redis | 0,56% | 3,16% | 9,2 MiB |
| Worker | 0,00% | 0,19% | 19,0 MiB |
| RabbitMQ | 0,22%–0,24% | 258,90% | 225,3 MiB |

O RabbitMQ apresentou três picos isolados de CPU, embora sua mediana tenha permanecido abaixo de 0,25%. Após a carga, os diagnósticos mostraram zero alarmes, fila pequena, duas conexões e `run_queue` igual a 1. Como o cenário de leitura não publica eventos, esses picos devem ser investigados em um cenário de notificações antes de qualquer otimização; eles não comprovam saturação sustentada nem devem ser ignorados.

O host de laboratório tinha AMD Ryzen 7 5700X (8 núcleos/16 processadores lógicos), 31,93 GiB de RAM e Docker Desktop limitado a 15,58 GiB. Os resumos k6 e as amostras de recursos são artefatos locais ignorados pelo Git; as tabelas acima são a evidência versionada.

Há uma capacidade **medida somente para este laboratório**: o cenário de listagem autenticada de pacientes sustentou 25 VUs por um minuto, nas três repetições, com p95 mediano de 4,49 ms e 0% de erros. Isso não é uma capacidade geral do produto: a máquina também mantinha contêineres não relacionados, não houve cache frio, escrita concorrente, proxy, réplicas ou rede de produção.

> Observação de reprodutibilidade: ao reutilizar um volume SQL Server local, a API pode iniciar antes de a recuperação do banco terminar. Antes de qualquer carga, aguarde `GET /health/ready` ficar saudável; se a API tiver terminado durante a recuperação, reinicie apenas a aplicação depois da prontidão do banco. Esse comportamento deve ser revalidado em ambiente limpo antes de tratá-lo como problema de inicialização.

### Próximos cenários mínimos

1. Login e renovação de sessão, com política de rate limiting considerada separadamente.
2. Listagem paginada e filtrada de pacientes, com cache frio e quente.
3. Criação, confirmação e reagendamento de consulta.
4. Registro de pagamento e consulta de relatório financeiro.
5. Carga mista, próxima ao comportamento real de uma recepção.

### Método de progressão e coleta

Para cada cenário, execute ao menos três repetições com a mesma massa, commit e configuração. Registre a mediana e o intervalo de p95/p99, throughput e erros. Rode variantes de cache frio e cache quente quando a rota usar Redis. Comece em 25 VUs, avance para 50 e 100 somente se o nível anterior cumprir os SLOs e pare no primeiro limite violado.

Use `-CaptureResources` no executor para gerar amostras contínuas. Cada linha do `*.jsonl` contém o momento UTC, contêiner, CPU, memória, I/O e PIDs; ela pode ser importada ou analisada depois sem depender de uma fotografia após o teste. Anote também tamanho da fila e quaisquer erros ou degradações nos logs/Seq.

### Métricas obrigatórias

- latência p50, p95 e p99;
- taxa de erros HTTP e de regras de negócio;
- throughput por endpoint;
- CPU e memória da API, worker e banco;
- tempo de consulta SQL, taxa de cache e tamanho de fila RabbitMQ;
- saturação de conexões e comportamento após falhas.

O protocolo didático completo, os cenários de escrita/mistura e o checklist de conclusão estão em [Próximas Evoluções](proximas-evolucoes.md#etapa-22--capacidade-e-performance).

### Critério de capacidade declarada

Uma capacidade só poderá ser documentada no formato:

> “Suporta **N usuários virtuais no cenário X**, com p95 menor que **Y ms**, taxa de erro menor que **Z%**, em ambiente com configuração **W**.”

Sem cenário, ambiente e métricas, “suporta N usuários” é apenas uma estimativa sem valor técnico.

## O que esta etapa ainda não prova

- O cenário atual é somente leitura autenticada; ele não mede criação de consulta, pagamento, escrita concorrente ou processamento assíncrono.
- Há somente uma linha de base de leitura versionada; ainda não há capacidade declarada até que o perfil seja repetido, os dados do ambiente sejam capturados durante a carga e os cenários restantes sejam executados.
- Docker Compose local não representa réplicas, proxy, banco gerenciado, limites de recursos ou rede de produção.
- O teste não deve rodar contra produção sem janela autorizada, massa de dados isolada, limites explícitos e plano de reversão.
