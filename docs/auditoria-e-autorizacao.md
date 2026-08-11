# Auditoria e autorização

Esta documentação explica os controles iniciados na etapa 20. Eles melhoram a rastreabilidade das mutações administrativas sem registrar conteúdo clínico desnecessário.

## Audit trail

Toda requisição `POST`, `PUT`, `PATCH` ou `DELETE` em `/api`, exceto as rotas de autenticação em `/api/auth`, gera um registro em `AuditLogs` após a resposta da aplicação. O registro contém:

| Campo | Finalidade |
|---|---|
| `ActorUserId` | Usuário autenticado que executou a ação, quando disponível. |
| `ActorRole` | Papel efetivo do usuário no momento da requisição. |
| `Action` | Verbo HTTP da alteração. |
| `ResourcePath` | Somente o caminho da rota, sem query string ou corpo. |
| `StatusCode` | Resultado HTTP da operação. |
| `CorrelationId` | Chave para correlacionar o evento com logs Serilog/Seq. |
| `OccurredAtUtc` | Momento UTC de persistência. |

Corpos de requisição, query strings, headers, tokens, e-mail, telefone e dados clínicos **não** são incluídos. Rotas de login, refresh e confirmação também são excluídas para evitar a duplicação de informações sensíveis de autenticação.

O middleware não interrompe uma operação de negócio se o banco de auditoria estiver indisponível: ele registra a falha no Serilog e preserva a resposta original. Isto é uma escolha explícita de disponibilidade para o MVP. Em uma versão regulada, a evolução recomendada é uma trilha imutável, com retenção definida, acesso restrito, alerta de falha e estratégia transacional/outbox conforme o requisito de compliance.

## Políticas de autorização

As permissões de API deixam de depender de strings de roles espalhadas pelos controllers. As políticas estão em `src/ClinicHub.API/Authorization/AuthorizationPolicies.cs` e são registradas no composition root.

| Política | Roles atuais | Recurso |
|---|---|---|
| `patients.read` | Admin, Doctor, Receptionist | Consultar pacientes |
| `patients.write` | Admin, Receptionist | Criar e editar pacientes |
| `patients.deactivate` | Admin | Desativar paciente |
| `appointments.manage` | Admin, Receptionist | Agenda |
| `payments.manage` | Admin, Receptionist | Pagamentos |
| `financial.read` | Admin | Relatório financeiro |
| `doctors.read` | Admin, Receptionist | Lista de médicos |

Uma policy nomeada permite trocar a regra em um único lugar, evoluir para requirements próprios e tornar as permissões verificáveis em testes.

## Ownership do paciente

`Patient.UserId` é um vínculo opcional e único para `User`. Ele permite manter prontuários administrativos sem conta, mas impede que uma conta `Patient` tenha mais de um perfil ou consulte o perfil de outra pessoa.

O portal usa a policy `patient-portal.access` e expõe somente rotas `/me`:

| Rota | Comportamento |
|---|---|
| `GET /api/patient-portal/me` | Busca o perfil apenas pelo identificador presente no token. |
| `POST /api/patient-portal/me` | Cria o primeiro perfil e o vincula à conta autenticada. |
| `PUT /api/patient-portal/me` | Atualiza nome, nascimento e telefone do perfil vinculado. |

Nenhuma dessas rotas recebe um `patientId` do cliente. A consulta é filtrada na Application/Infrastructure pelo `UserId` autenticado, e os testes cobrem a tentativa de uma conta diferente obter um perfil que não lhe pertence. O e-mail do perfil vem da conta confirmada e não pode ser substituído pelo portal.

Quando já existir um prontuário administrativo com o mesmo e-mail, o portal recusa a autocriação. A associação desse caso deve ser feita em fluxo administrativo verificado (convite/validação pela clínica), nunca simplesmente por um e-mail informado no cliente.

Ainda pendentes nesta etapa: DTOs específicos para mascarar campos em novos casos de uso, política de retenção/criptografia para dados sensíveis e uma interface Angular para o portal.
