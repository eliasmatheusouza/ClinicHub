# Hardening de deploy

Este guia define o modelo de implantação segura do ClinicHub. Ele é separado do `docker-compose.yml` local, que continua expondo portas apenas para estudo e diagnóstico.

## Imagens e privilégios

API, worker e frontend executam como o usuário `clinichub` de UID 10001; as imagens finais não usam root. O frontend Nginx escuta a porta não privilegiada 8080. No manifesto de produção, os contêineres usam filesystem somente leitura, `no-new-privileges`, remoção de capabilities e volumes temporários mínimos para cache/logs.

Validação após publicar uma imagem:

```powershell
docker image inspect <imagem> --format '{{.Config.User}}'
```

O resultado esperado é `clinichub`, nunca vazio ou `root`.

## Rede e TLS

`docker-compose.production.yml` não publica portas da API, worker, banco, Redis ou RabbitMQ. A rede `internal` é marcada como interna; a rede `edge` é externa e deve ser compartilhada somente com um reverse proxy/ingress confiável.

O proxy é responsável por publicar hosts HTTPS, terminar TLS, redirecionar HTTP para HTTPS e encaminhar à API apenas pela rede de borda. Não exponha SQL Server, Redis, RabbitMQ management, Seq ou a porta interna da API na internet.

## Secrets externos

O arquivo [`.env.production.example`](../.env.production.example) tem valores fictícios exclusivamente para validar o Compose. Em produção, não copie esse arquivo: injete as variáveis pelo secret manager da plataforma (AWS Secrets Manager/SSM, Azure Key Vault, GitHub Environment secrets ou equivalente) no momento do deploy.

Os valores mínimos são `JWT_KEY`, credenciais/connection strings de SQL, Redis e RabbitMQ e senha SMTP. Eles não podem ser adicionados ao Git, a imagens Docker, logs, artefatos de CI ou ao frontend. A API já recusa configurações Production inseguras na inicialização.

```powershell
docker compose --env-file .env.production.example -f docker-compose.production.yml config --quiet
```

## DAST

O workflow manual/semanal [DAST baseline](../.github/workflows/dast.yml) sobe a stack isolada e executa o OWASP ZAP contra `/health/ready` pela rede Docker. Ele prepara um diretório gravável exclusivo e publica o relatório HTML/JSON como artefato `dast-zap-report`. A opção `-I` permite revisar alertas de severidade baixa no relatório, mas falhas de scan interrompem o job.

A primeira linha de base remota foi aprovada em [DAST baseline #31451309129](https://github.com/eliasmatheusouza/ClinicHub/actions/runs/31451309129), sem alertas alto, médio ou baixo. As observações informativas sobre cache em respostas sem dados sensíveis foram registradas e não representam aceitação de risco clínico.

Antes de promover uma versão, revise alertas novos do ZAP, mantenha evidência da execução e corrija ou aceite formalmente cada risco. Para ambientes públicos, complemente o baseline com scan autenticado e uma política explícita de severidade que bloqueie releases.
