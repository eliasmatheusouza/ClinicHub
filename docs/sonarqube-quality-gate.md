# SonarQube e Quality Gate

O ClinicHub usa duas formas complementares de análise:

- **SonarQube Community Build local:** laboratório reproduzível para entender regras, métricas e Quality Gates sem enviar código ou dados para fora da máquina.
- **SonarQube Cloud na CI:** integração opcional que publica o check do Quality Gate nos pull requests e pode bloquear o workflow quando o gate falha.

CodeQL continua responsável por SAST no GitHub; Sonar acrescenta code smells, duplicação, métricas de manutenção e um gate focado em código novo.

## O que o Sonar faz — e o que ele não faz

Sonar é uma ferramenta de **análise estática**: ela lê o código e os relatórios gerados pela esteira sem precisar executar o sistema como um usuário. Suas regras procuram padrões que costumam causar defeitos, vulnerabilidades ou dificultar a manutenção. No ClinicHub, a análise é feita depois de compilar e testar o backend; o scanner envia os resultados e a cobertura para o SonarCloud, que calcula o resultado do Quality Gate.

| Recurso | Pergunta que ajuda a responder | Exemplo prático no ClinicHub |
|---|---|---|
| Bugs / Reliability | Há código com grande chance de se comportar errado? | Um valor pode ser nulo, uma condição é sempre verdadeira ou uma exceção não é tratada. |
| Vulnerabilities / Security | Uma regra conhecida de segurança foi violada? | Entrada pode chegar a uma consulta, log ou resposta HTTP de forma insegura. |
| Security hotspots | Este trecho merece revisão humana por ser sensível? | Uso de criptografia, autenticação ou configuração de cabeçalhos; não é automaticamente uma falha. |
| Code smells / Maintainability | O código ficará difícil de entender, alterar ou testar? | Propriedade sem uso, método excessivamente complexo ou acoplamento desnecessário. |
| Duplicação | Estamos repetindo lógica que pode divergir no futuro? | Dois fluxos com o mesmo bloco de regra de negócio. |
| Cobertura | Os testes executaram as linhas e ramos alterados? | O Coverlet gera OpenCover; o SonarCloud importa esse relatório, não inventa testes. |

O Sonar **não substitui** testes unitários, integração ou ponta a ponta; ele não prova que uma regra de negócio está correta. Também não substitui CodeQL, Dependabot ou OWASP ZAP: eles são controles complementares para SAST, dependências e teste dinâmico. Um Quality Gate verde significa que as regras configuradas foram atendidas; não significa que o software está livre de qualquer defeito ou risco.

### Como interpretar um achado

1. Abra o achado e leia a regra e o trecho apontado.
2. Verifique o contexto do domínio e a exposição real. Hotspots exigem essa investigação antes de serem marcados como seguros.
3. Corrija o código e escreva ou ajuste um teste quando o risco representar comportamento verificável.
4. Se for falso positivo ou dívida aceita conscientemente, registre a justificativa no SonarCloud e, quando necessário, abra item de backlog. Nunca silencie a regra apenas para deixar o painel verde.

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

Para aprendizado, comece com o Quality Gate padrão. Quando o plano permitir padrões customizados, crie um gate para **código novo** com estes critérios: zero bugs, zero vulnerabilidades, rating A de confiabilidade e segurança, cobertura mínima de 80% e duplicação menor que 3%. Não altere o gate global para "aprovar" dívida legada: trate essa dívida em backlog separado.

## 3. Linha de base local validada

Em 11/08/2026, a análise completa local terminou com o **Quality Gate padrão aprovado** em aproximadamente 90 segundos. O painel registrou 49,1% de cobertura geral, 51,1% de cobertura de linhas, 0 bugs e 0% de duplicação.

O mesmo diagnóstico revelou 44 code smells, 2 vulnerabilidades e 8 security hotspots legados. A aprovação do gate padrão não elimina esses itens: eles devem ser triados e corrigidos em backlog. O gate recomendado para código novo evita que essa dívida aumente enquanto ela é reduzida gradualmente.

## 4. Habilitar SonarQube Cloud no GitHub Actions

O workflow [sonar.yml](../.github/workflows/sonar.yml) está versionado. Antes da configuração, ele permanece **ignorado** até as variáveis existirem, evitando uma falsa aprovação sem análise. Depois da configuração, a primeira execução remota [#31553134108](https://github.com/eliasmatheusouza/ClinicHub/actions/runs/31553134108) foi aprovada em 1m59s.

1. Crie uma organização e importe o repositório no SonarQube Cloud usando a integração com GitHub.
2. Crie um token com permissão de executar análise e guarde-o como `SONAR_TOKEN` no GitHub.
3. Cadastre as variáveis de repositório `SONAR_ORGANIZATION` e `SONAR_PROJECT_KEY` com os valores exibidos pelo SonarQube Cloud.
4. Faça um `workflow_dispatch` de **Sonar Quality Gate** e revise o painel.
5. Torne `SonarCloud quality gate` obrigatório na proteção da `main` somente depois de uma análise aprovada.

No ClinicHub, os cinco passos já foram concluídos. O plano Free mantém o Quality Gate padrão; a CI complementa-o com o gate próprio de 70% de cobertura em Domain/Application.

O scanner usa `sonar.qualitygate.wait=true`; portanto, o job retorna erro quando o Quality Gate remoto reprova a análise. SonarQube Cloud também publica o status do gate como check no pull request.

Para configurar a conta gratuita, os tokens e as variáveis do GitHub passo a passo, consulte [configurar SonarQube Cloud gratuito](configurar-sonarcloud-gratuito.md).

O documento de configuração também mantém a fotografia do que já foi validado localmente e do que ainda depende do SonarQube Cloud.

## Limites e decisões conscientes

- O laboratório local serve para prática e não substitui o check remoto em PR.
- A cobertura e a análise importadas nesta primeira versão são da solução .NET. A cobertura e análise Angular podem ser adicionadas quando a suíte frontend gerar relatório LCOV estável.
- O token nunca é versionado: use variável de ambiente local ou GitHub Secret.
- O Docker local usa PostgreSQL persistente; volumes pertencem somente ao laboratório e não carregam dados clínicos.
