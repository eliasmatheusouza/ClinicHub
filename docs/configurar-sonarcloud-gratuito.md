# Configurar SonarQube Cloud gratuito

Este guia registra e permite reproduzir a parte remota da Etapa 17 do ClinicHub. Ele usa o plano **Free** do SonarQube Cloud.

> Na configuração realizada em 12/08/2026 (UTC), o plano Free custava US$ 0, incluía linhas de código ilimitadas em projetos públicos e até 50 mil linhas em projetos privados, com até 5 membros. Planos e limites podem mudar: confirme-os sempre no painel antes de adotar para trabalho profissional.

## Resultado esperado

Ao final, o GitHub terá três configurações, todas já previstas no workflow [sonar.yml](../.github/workflows/sonar.yml):

| Tipo no GitHub | Nome | Valor |
|---|---|---|
| Secret | `SONAR_TOKEN` | Token pessoal criado no SonarQube Cloud. Nunca publique este valor. |
| Variável | `SONAR_ORGANIZATION` | Chave da organização mostrada no SonarQube Cloud. |
| Variável | `SONAR_PROJECT_KEY` | Chave do projeto ClinicHub mostrada no SonarQube Cloud. |

O workflow só começa a analisar quando as duas variáveis existirem. Assim, não há aprovação falsa enquanto a conta ainda não está configurada.

## Estado atual do ClinicHub

Esta é a fotografia da implementação em 12/08/2026 (UTC). Ela separa claramente o que já foi comprovado das credenciais que devem permanecer exclusivamente na sua conta externa.

| Área | Estado | Evidência ou decisão |
|---|---|---|
| Laboratório local | Concluído | SonarQube Community Build, PostgreSQL e `dotnet-sonarscanner` 11.2.1 versionados. |
| Análise local .NET | Concluída | Quality Gate padrão aprovado; 49,1% de cobertura geral, 0 bugs e 0% de duplicação. |
| Achados locais | Registrados | 44 code smells, 2 vulnerabilidades e 8 security hotspots legados aguardam triagem; o gate padrão não os apaga. |
| Workflow remoto | Concluído | [Execução #31553134108](https://github.com/eliasmatheusouza/ClinicHub/actions/runs/31553134108) aprovada: build, testes, cobertura, análise e Quality Gate. |
| `SONAR_TOKEN` | Concluído | Criado com expiração de 90 dias e guardado exclusivamente como Secret do GitHub; nunca é versionado. |
| `SONAR_ORGANIZATION` | Concluído | Variável de repositório com a chave `eliasmatheusouza`. |
| `SONAR_PROJECT_KEY` | Concluído | Variável de repositório com a chave `eliasmatheusouza_ClinicHub`. |
| Proteção da `main` | Ativa | PR, uma aprovação, conversas resolvidas, histórico linear e checks de CI, CodeQL, DAST e SonarCloud são obrigatórios. |
| Check SonarCloud obrigatório | Concluído | `SonarCloud quality gate` foi incluído na proteção da `main` depois da primeira análise aprovada. |

Os identificadores e a credencial não são arquivos do projeto: pertencem à sua conta, não devem ser inventados, compartilhados ou versionados. Apenas os nomes das variáveis e o procedimento ficam documentados.

## 1. Criar a conta e conectar o GitHub

1. Abra [SonarQube Cloud](https://sonarcloud.io/) e escolha entrar com **GitHub**.
2. Autorize o aplicativo oficial do SonarQube a acessar a conta `eliasmatheusouza`.
3. Selecione **Analyze new project**.
4. Instale ou configure o aplicativo do SonarQube Cloud para ter acesso ao repositório `ClinicHub`. Prefira selecionar somente esse repositório no início.
5. Crie ou selecione a organização vinculada à sua conta GitHub.
6. Escolha o plano **Free** e conclua a criação da organização.

Não crie uma organização manual desconectada do GitHub: a integração vinculada é o que permite a decoração de pull requests e associa o projeto ao repositório correto.

## 2. Importar o projeto ClinicHub

1. Em **Analyze new project**, escolha a organização recém-criada.
2. Marque o repositório `eliasmatheusouza/ClinicHub` e selecione **Set up**.
3. Aceite inicialmente a definição de código novo sugerida pelo assistente; depois ela poderá ser ajustada no projeto.
4. Crie o projeto.
5. Quando o assistente mostrar os valores de identificação, copie a **Organization key** e a **Project key**. Eles serão usados nas variáveis do GitHub, não em arquivos versionados.

## 3. Gerar um token de análise

1. No canto superior direito do SonarQube Cloud, abra **My Account > Security**.
2. Em Personal Access Tokens, informe um nome identificável, por exemplo `clinichub-github-actions`.
3. Gere o token e copie-o imediatamente. Ele só é exibido uma vez.
4. Cole-o diretamente no GitHub no próximo passo. Não o salve em `.env`, código, commit, captura de tela ou chat.

Para o plano Free, um Personal Access Token é o mecanismo normal. O token usado neste laboratório expira em 08/11/2026; antes dessa data, gere um novo e atualize apenas o Secret no GitHub. Tokens inativos também podem ser removidos pelo SonarQube Cloud.

## 4. Cadastrar secret e variáveis no GitHub

No repositório [ClinicHub](https://github.com/eliasmatheusouza/ClinicHub), abra **Settings > Secrets and variables > Actions**.

### Secret

1. Na aba **Secrets**, clique em **New repository secret**.
2. Nome: `SONAR_TOKEN`.
3. Valor: o token criado no passo anterior.
4. Salve.

### Variáveis não sigilosas

Na aba **Variables**, crie:

| Nome | Valor |
|---|---|
| `SONAR_ORGANIZATION` | A Organization key copiada no passo 2. |
| `SONAR_PROJECT_KEY` | A Project key copiada no passo 2. |

Como alternativa pelo terminal autenticado no GitHub CLI, use comandos interativos para o secret e comandos com os valores reais nas variáveis:

```powershell
gh secret set SONAR_TOKEN --repo eliasmatheusouza/ClinicHub
gh variable set SONAR_ORGANIZATION --repo eliasmatheusouza/ClinicHub --body '<organization-key>'
gh variable set SONAR_PROJECT_KEY --repo eliasmatheusouza/ClinicHub --body '<project-key>'
```

O primeiro comando pede o token sem exibi-lo. Nunca substitua o prompt por um token escrito no histórico do terminal.

## 5. Executar e validar a primeira análise

1. No GitHub, vá a **Actions > Sonar Quality Gate > Run workflow** e execute em `main`.
2. Confirme que o job **SonarCloud quality gate** deixou de ser ignorado.
3. Abra o link para o painel do SonarQube Cloud exibido no log.
4. Verifique cobertura, bugs, vulnerabilidades, hotspots e code smells.
5. Em **Quality Gates**, confira a regra atribuída ao projeto. No plano Free usado aqui, a regra padrão é a disponível; padrões customizados podem exigir outro plano.
6. Faça uma alteração de teste em uma branch, abra um pull request e confirme que o check aparece no PR. Corrija ou reverta a alteração de teste depois da validação. Para uma regra customizada futura, a recomendação é código novo com zero bugs/vulnerabilidades, cobertura mínima de 80% e duplicação menor que 3%.

## 6. Tornar o gate obrigatório

Somente após uma análise remota aprovada, no GitHub abra **Settings > Branches > main > Edit** e inclua o check exibido pelo GitHub, normalmente `SonarCloud quality gate`, em **Require status checks to pass before merging**.

A `main` já possui proteção para CI, CodeQL e DAST. O SonarCloud é acrescentado depois para evitar bloquear pull requests com um check ainda não configurado. Ao concluir este passo, atualize a Etapa 17 para concluída e a Etapa 18 também poderá ser concluída.

## Checklist de conclusão

Use esta lista quando for executar a configuração:

- [x] A conta/organização SonarQube Cloud foi criada no plano Free e o projeto foi importado.
- [x] O token foi criado, copiado uma única vez e salvo como `SONAR_TOKEN` no GitHub.
- [x] `SONAR_ORGANIZATION` e `SONAR_PROJECT_KEY` foram criadas como variáveis de repositório.
- [x] O workflow **Sonar Quality Gate** foi executado em `main` sem ser ignorado e foi aprovado.
- [x] O painel recebeu cobertura OpenCover e exibiu o resultado do Quality Gate.
- [x] `SonarCloud quality gate` foi incluído nos checks obrigatórios da proteção da `main`.
- [x] As Etapas 17 e 18 foram atualizadas para concluídas no plano de execução.
- [ ] Opcional para aprendizado: uma alteração controlada em pull request confirma visualmente a reprovação de um gate vermelho. Não a faça diretamente em `main`.

## Diagnóstico rápido

| Sintoma | Causa provável | Ação |
|---|---|---|
| Workflow ignorado | Uma ou ambas as variáveis não existem. | Confira `SONAR_ORGANIZATION` e `SONAR_PROJECT_KEY`. |
| Erro de autenticação | Secret ausente, expirado ou sem permissão. | Gere/atualize `SONAR_TOKEN`. |
| Projeto não encontrado | Chave de organização ou projeto incorreta. | Copie novamente os valores do painel do SonarQube Cloud. |
| Cobertura igual a zero | Relatório não foi localizado. | Revise o log; o workflow usa OpenCover em `artifacts/sonarqube-tests/`. |
| PR bloqueado | Gate remoto reprovou ou há outro check obrigatório pendente. | Abra o detalhe do check e corrija o motivo; não remova a proteção para contornar qualidade. |
