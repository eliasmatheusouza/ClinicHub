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

## Limite atual: ownership do paciente

Uma conta com role `Patient` ainda não possui vínculo persistido com a entidade `Patient`. Logo, não há endpoint de portal do paciente e não se deve adicionar um `[Authorize(Roles = "Patient")]` a rotas existentes: isso concederia acesso amplo a prontuários de terceiros.

Para implementar ownership corretamente, a próxima fatia deve:

1. modelar uma associação explícita e única entre `User` e `Patient`;
2. criar um requirement `PatientOwnerRequirement` que compare o usuário autenticado com o recurso solicitado;
3. filtrar consultas na Application/Infrastructure pelo identificador vinculado, e não apenas no controller;
4. expor DTOs mínimos próprios para o portal, com masking quando aplicável;
5. cobrir sucesso, tentativa de acesso cruzado e ausência de vínculo com testes de integração.

Até lá, dados de pacientes permanecem acessíveis somente às políticas administrativas acima.
