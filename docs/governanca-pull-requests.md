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

O DAST foi validado com sucesso em execução manual antes de ser incluído nos checks obrigatórios. Como ele também é publicado no workflow de PR, cada pull request para `main` precisa concluir o scan ZAP antes do merge.

## Ligação com SonarCloud

O workflow `Sonar Quality Gate` existe, mas fica ignorado até o repositório receber `SONAR_TOKEN`, `SONAR_ORGANIZATION` e `SONAR_PROJECT_KEY`. Não o torne obrigatório antes de validar a Etapa 17: um check ignorado não prova qualidade e pode bloquear merges indevidamente.

Depois da primeira análise remota aprovada:

1. Ajuste o Quality Gate para código novo.
2. Provoque uma falha controlada e confirme que o workflow reprova.
3. Adicione `SonarCloud quality gate` aos checks obrigatórios.
4. Revise se a política de administradores deve passar a ser aplicada sem exceção.

## Fluxo de trabalho

```text
branch de trabalho -> pull request -> revisão + checks aprovados -> merge linear na main
```

Não envie mudanças diretamente para `main` no fluxo normal. Crie uma branch, abra o PR, trate os comentários e use a página do GitHub para confirmar cada check antes do merge.
