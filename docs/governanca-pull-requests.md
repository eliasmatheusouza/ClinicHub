# Governança de pull requests

Esta documentação explica a proteção aplicada à branch `main` do ClinicHub. O objetivo não é burocracia: é impedir que código não revisado ou não validado alcance a linha principal.

## Regras da `main`

- Mudanças chegam por pull request.
- Um usuário precisa aprovar a mudança; uma nova alteração dispensa aprovações antigas.
- Conversas de revisão precisam estar resolvidas.
- O histórico é linear, mantendo uma sequência de commits mais simples de auditar.
- Force push e exclusão da branch são bloqueados.
- Administradores podem contornar a regra somente em uma emergência. Em um repositório pessoal isso evita bloqueio operacional; em equipe, a opção recomendada é ativar também a aplicação para administradores.

## Checks obrigatórios iniciais

| Check | O que protege |
|---|---|
| `Backend (.NET 8)` | Formatação, build, testes e gate de cobertura do backend. |
| `Frontend (Angular)` | Análise estática, build e testes do frontend. |
| `Dependency audit` | Dependências NuGet e NPM vulneráveis. |
| `Docker images` | Especificação Compose e build das imagens. |
| `CodeQL (csharp)` | SAST do backend .NET. |
| `CodeQL (javascript-typescript)` | SAST do frontend. |
| `OWASP ZAP baseline` | DAST da API em stack isolada. |
| `SonarCloud quality gate` | Análise SonarCloud, cobertura importada e Quality Gate remoto. |

O DAST foi validado com sucesso em execução manual antes de ser incluído nos checks obrigatórios. Como ele também é publicado no workflow de PR, cada pull request para `main` precisa concluir o scan ZAP antes do merge.

## Ligação com SonarCloud

O repositório já possui `SONAR_TOKEN` como secret e as variáveis `SONAR_ORGANIZATION` e `SONAR_PROJECT_KEY`. A primeira execução remota aprovada foi [Sonar Quality Gate #31553134108](https://github.com/eliasmatheusouza/ClinicHub/actions/runs/31553134108), em 12/08/2026 (UTC).

`SonarCloud quality gate` agora é um dos checks obrigatórios da `main`. O workflow espera a resposta remota (`sonar.qualitygate.wait=true`); portanto, Quality Gate vermelho encerra o job com erro e impede o merge enquanto a proteção estiver ativa.

No plano Free, o projeto usa a regra padrão disponível no SonarCloud. A meta específica de 70% de cobertura para Domain/Application continua protegida pelo workflow de CI. Ao migrar para um plano que permita padrões de qualidade customizados, configure regras de código novo (zero bugs/vulnerabilidades, cobertura >= 80% e duplicação < 3%) antes de substituir o gate padrão.

O roteiro completo, incluindo os nomes das configurações no GitHub, a rotação do token e o checklist, está em [configurar SonarQube Cloud gratuito](configurar-sonarcloud-gratuito.md).

## Fluxo de trabalho

```text
branch de trabalho -> pull request -> revisão + checks aprovados -> merge linear na main
```

Não envie mudanças diretamente para `main` no fluxo normal. Crie uma branch, abra o PR, trate os comentários e use a página do GitHub para confirmar cada check antes do merge.

## Atualizações do Dependabot

Uma pull request do Dependabot atualiza uma dependência, mas **não é uma autorização para fazer merge automaticamente**. A política do ClinicHub é tratar atualizações por nível de risco e nunca mesclar várias atualizações incompatíveis de uma mesma família ao mesmo tempo.

| Tipo de atualização | Como tratar |
|---|---|
| Patch dentro da mesma versão principal | Candidata a aprovação rápida, depois de rebase e de todos os checks atuais verdes. Ainda revise o diff e as notas de segurança. |
| Minor ou major de biblioteca | Faça revisão do changelog e compatibilidade. Atualize em PR dedicado, valide testes e comportamento do módulo afetado. |
| Framework/runtime (EF Core, .NET, Angular, TypeScript) | Não aprove PRs isolados de famílias correlatas. Planeje uma atualização coordenada em uma branch própria, alinhe as versões e corrija incompatibilidades. |
| GitHub Actions | Trate como dependência da cadeia de entrega: atualize uma ação por vez, valide toda a CI e confira mudanças de permissões ou de comportamento. |

Antes de aprovar qualquer Dependabot PR, confirme:

1. a branch não está marcada como `BEHIND`;
2. os oito checks obrigatórios atuais estão verdes, incluindo `SonarCloud quality gate`;
3. o diff altera somente os manifestos/lockfiles esperados;
4. não existe outra PR concorrente que atualize o mesmo pacote ou uma família incompatível;
5. para uma atualização major, as notas de migração foram lidas e o fluxo afetado foi exercitado.

Para PRs criadas pelo Dependabot, mantenha `SONAR_TOKEN` também em **Dependabot secrets**. O bot não recebe o Actions secret comum, portanto esse segredo específico é necessário para que o check SonarCloud obrigatório seja executado.

Em 12/08/2026 (UTC), a configuração foi validada nas PRs [#21](https://github.com/eliasmatheusouza/ClinicHub/pull/21) e [#6](https://github.com/eliasmatheusouza/ClinicHub/pull/6): após rebase, CI, CodeQL, DAST e `SonarCloud quality gate` foram aprovados. Elas continuam exigindo a revisão humana configurada na proteção da `main`; validação automática não substitui aprovação consciente.

### Registro de manutenção: Dapper

A PR [#6](https://github.com/eliasmatheusouza/ClinicHub/pull/6) foi aprovada e mesclada por **rebase** em 12/08/2026 (UTC), atualizando Dapper de `2.1.35` para `2.1.79`. O processo confirma a política na prática: atualização isolada, branch atualizada contra a `main`, todos os checks obrigatórios verdes, revisão humana e histórico linear. A PR #21, usada na mesma validação, foi fechada sem merge e não alterou o código; uma atualização futura desse pacote deve nascer em uma nova PR e passar pela mesma sequência.

Se uma PR estiver vermelha, não aprove para "testar depois": leia o log, corrija em uma branch de manutenção ou feche-a e substitua-a por uma atualização coordenada. Assim o Dependabot reduz trabalho repetitivo sem transferir a responsabilidade técnica para o robô.
