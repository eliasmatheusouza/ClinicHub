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
