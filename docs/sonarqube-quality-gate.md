# SonarQube e Quality Gate

O ClinicHub usa duas formas complementares de análise:

- **SonarQube Community Build local:** laboratório reproduzível para entender regras, métricas e Quality Gates sem enviar código ou dados para fora da máquina.
- **SonarQube Cloud na CI:** integração opcional que publica o check do Quality Gate nos pull requests e pode bloquear o workflow quando o gate falha.

CodeQL continua responsável por SAST no GitHub; Sonar acrescenta code smells, duplicação, métricas de manutenção e um gate focado em código novo.

## 1. Subir o laboratório local

Com Docker Desktop em execução, na raiz do repositório:

```powershell
Copy-Item .env.sonarqube.example .env.sonarqube
docker compose --env-file .env.sonarqube -f docker-compose.sonarqube.yml up -d
```

Abra http://localhost:9000. O primeiro login é `admin` / `admin`; o SonarQube exigirá a troca da senha. Em **My Account > Security**, gere um token de análise para o usuário local e mantenha-o somente na sessão atual:

```powershell
$env:SONAR_TOKEN = '<token-gerado-no-sonarqube-local>'
./scripts/Invoke-SonarQubeAnalysis.ps1
```

O script restaura a versão fixada de `dotnet-sonarscanner`, executa build Release, testes com TRX e relatórios Cobertura/OpenCover, e aguarda o resultado do Quality Gate. O SonarQube lê o relatório OpenCover; Cobertura continua compatível com os relatórios já usados pela CI. Cada execução grava seus relatórios em `artifacts/sonarqube-tests/`, evitando reutilizar resultados antigos. Nesta primeira integração o scanner analisa somente a solução .NET, evitando percorrer dependências transitivas do frontend; cobertura e análise Angular entram quando houver relatório LCOV estável.

Para encerrar o laboratório, mantendo os dados locais:

```powershell
docker compose --env-file .env.sonarqube -f docker-compose.sonarqube.yml down
```

Não use `down -v` se quiser preservar histórico e configurações locais.

## 2. O que observar no painel

1. **Reliability:** bugs e rating de confiabilidade.
2. **Security:** vulnerabilidades e security hotspots; revise o contexto antes de marcar um hotspot como seguro.
3. **Maintainability:** code smells, dívida técnica e duplicação.
4. **Coverage:** a cobertura OpenCover dos testes .NET, gerada pelo coletor Coverlet. Ela complementa, mas não substitui, o gate atual de 70% em Domain/Application.
5. **New Code:** a área mais importante para evolução. A meta é não introduzir novos bugs/vulnerabilidades, manter duplicação baixa e exigir cobertura adequada no código alterado.

Para aprendizado, comece com o Quality Gate padrão e depois crie um gate próprio com estes critérios para **código novo**: zero bugs, zero vulnerabilidades, rating A de confiabilidade e segurança, cobertura mínima de 80% e duplicação menor que 3%. Não altere o gate global para "aprovar" dívida legada: trate essa dívida em backlog separado.

## 3. Linha de base local validada

Em 11/08/2026, a análise completa local terminou com o **Quality Gate padrão aprovado** em aproximadamente 90 segundos. O painel registrou 49,1% de cobertura geral, 51,1% de cobertura de linhas, 0 bugs e 0% de duplicação.

O mesmo diagnóstico revelou 44 code smells, 2 vulnerabilidades e 8 security hotspots legados. A aprovação do gate padrão não elimina esses itens: eles devem ser triados e corrigidos em backlog. O gate recomendado para código novo evita que essa dívida aumente enquanto ela é reduzida gradualmente.

## 4. Habilitar SonarQube Cloud no GitHub Actions

O workflow [sonar.yml](../.github/workflows/sonar.yml) está versionado e permanece **ignorado** até as configurações abaixo existirem. Isso evita uma falsa aprovação sem análise.

1. Crie uma organização e importe o repositório público no SonarQube Cloud usando a integração com GitHub.
2. Crie um token com permissão de executar análise.
3. No GitHub, cadastre o secret `SONAR_TOKEN`.
4. Cadastre as variáveis de repositório `SONAR_ORGANIZATION` e `SONAR_PROJECT_KEY` com os valores exibidos pelo SonarQube Cloud.
5. Faça um `workflow_dispatch` de **Sonar Quality Gate** e revise o primeiro painel.
6. Ajuste o Quality Gate para código novo e confirme que o job falha quando ele está vermelho.
7. Na etapa 18, torne esse check obrigatório na proteção da `main`.

O scanner usa `sonar.qualitygate.wait=true`; portanto, o job retorna erro quando o Quality Gate remoto reprova a análise. SonarQube Cloud também publica o status do gate como check no pull request.

Para configurar a conta gratuita, os tokens e as variáveis do GitHub passo a passo, consulte [configurar SonarQube Cloud gratuito](configurar-sonarcloud-gratuito.md).

## Limites e decisões conscientes

- O laboratório local serve para prática e não substitui o check remoto em PR.
- A cobertura e a análise importadas nesta primeira versão são da solução .NET. A cobertura e análise Angular podem ser adicionadas quando a suíte frontend gerar relatório LCOV estável.
- O token nunca é versionado: use variável de ambiente local ou GitHub Secret.
- O Docker local usa PostgreSQL persistente; volumes pertencem somente ao laboratório e não carregam dados clínicos.
