# Operação local e troubleshooting

## Subir e parar a stack

```powershell
Copy-Item .env.example .env
docker compose up -d --build
docker compose ps
```

Para interromper sem apagar dados: `docker compose stop`. Para remover apenas os contêineres: `docker compose down`. Os volumes de SQL Server, Redis, RabbitMQ e Seq são persistentes; remova-os somente se quiser reiniciar os dados de desenvolvimento.

## Endereços locais

| Serviço | Endereço |
|---|---|
| Frontend | http://localhost:4200 |
| API / Swagger | http://localhost:8082 / http://localhost:8082/swagger |
| Seq | http://localhost:8081 |
| RabbitMQ Management | http://localhost:15672 |
| SQL Server | `localhost,1433` |
| Redis | `localhost:6380` |

As portas `8082` e `6380` foram escolhidas para coexistir com outros projetos locais. Dentro da rede Docker, os serviços usam `api:8080` e `redis:6379`.

## Diagnóstico

```powershell
docker compose logs --tail=100 api
docker compose logs --tail=100 notifications-worker
Invoke-WebRequest http://localhost:8082/health/ready
```

`/health/live` confirma que a API está no ar; `/health/ready` também verifica SQL Server, Redis e RabbitMQ. Para correlacionar requisições e logs, informe `X-Correlation-ID` no cliente.

## E-mail de confirmação

O padrão de desenvolvimento é `EMAIL_DELIVERY_MODE=Log`: o link é emitido nos logs da API. Para SMTP real, configure as variáveis `EMAIL_FROM`, `EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT`, `EMAIL_SMTP_USE_SSL`, `EMAIL_SMTP_USERNAME` e `EMAIL_SMTP_PASSWORD`, e altere o modo para `Smtp`.

## Configuração de Production

O `docker-compose.yml` e o `.env.example` são exclusivos para desenvolvimento. Em produção, não reutilize senhas, chaves JWT, seed de usuários ou o modo de e-mail `Log` presentes neles.

A API valida a configuração na inicialização quando `ASPNETCORE_ENVIRONMENT=Production` e não inicia se detectar:

- `Jwt:Key` previsível, com menos de 32 caracteres ou contendo valor de desenvolvimento;
- `AllowedHosts` vazio ou igual a `*`;
- origem CORS ou URL de confirmação de e-mail sem HTTPS;
- envio de e-mail diferente de SMTP.

Forneça os valores por secret manager/variáveis seguras, por exemplo:

```text
ASPNETCORE_ENVIRONMENT=Production
Jwt__Key=<segredo-aleatorio-com-32-ou-mais-caracteres>
Cors__AllowedOrigins__0=https://app.exemplo.com
AllowedHosts=api.exemplo.com
EmailConfirmation__FrontendUrl=https://app.exemplo.com
Email__DeliveryMode=Smtp
Email__Smtp__Host=smtp.exemplo.com
Email__Smtp__Username=<usuario-smtp>
Email__Smtp__Password=<segredo-smtp>
```

Em produção, a aplicação habilita HTTPS redirection e HSTS. Quando houver reverse proxy/TLS termination, defina claramente se essa responsabilidade ficará no proxy ou na API e configure somente proxies confiáveis antes de aceitar cabeçalhos encaminhados. Os endpoints de autenticação possuem rate limiting; os limites podem ser ajustados por `RateLimiting__<Politica>__PermitLimit` e `RateLimiting__<Politica>__WindowSeconds`.
