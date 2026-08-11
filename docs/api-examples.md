# Guia da API e exemplos Swagger

Com a stack local em execução, abra o [Swagger](http://localhost:8082/swagger). Os endpoints de escrita exibem exemplos de payload preenchidos diretamente na interface.

## Autenticação

### Criar e confirmar uma conta

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "paciente@exemplo.com",
  "password": "SenhaSegura1",
  "confirmPassword": "SenhaSegura1"
}
```

O retorno é `202 Accepted`. Em desenvolvimento, consulte os logs da API para obter o link; em produção, configure SMTP no `.env`. Envie o token do link:

```http
POST /api/auth/confirm-email
Content-Type: application/json

{ "token": "token-recebido-no-link-de-confirmacao" }
```

### Login e refresh

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "admin@clinichub.local", "password": "Admin123!" }
```

Use o `accessToken` retornado em todos os endpoints protegidos:

```http
Authorization: Bearer {accessToken}
```

Para renovar a sessão, envie o `refreshToken` para `POST /api/auth/refresh`. A resposta invalida o refresh token anterior e entrega um novo par de tokens.

## Pacientes

### Portal do paciente

Somente uma conta com role `Patient` confirmada pode usar estas rotas. O token determina o perfil; não envie nem escolha um identificador de paciente.

```http
POST /api/patient-portal/me
Authorization: Bearer {token-do-paciente}
Content-Type: application/json

{
  "name": "Ana Souza",
  "birthDate": "1990-01-01",
  "phone": "11999999999"
}
```

Para consultar ou atualizar o próprio perfil, use `GET /api/patient-portal/me` ou `PUT /api/patient-portal/me` com o mesmo payload. O e-mail é sempre o da conta confirmada.

```http
POST /api/patients
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "Maria da Silva",
  "birthDate": "1990-05-20",
  "email": "maria.silva@exemplo.com",
  "phone": "+5511999999999"
}
```

Liste com `GET /api/patients?term=maria&page=1&pageSize=20`. `Admin` e `Receptionist` criam/alteram; somente `Admin` desativa; `Doctor` pode consultar.

## Agenda e financeiro

1. Recupere médicos com `GET /api/users/doctors`.
2. Agende via `POST /api/appointments`, usando IDs de paciente e médico, `startUtc` em UTC e `durationMinutes`.
3. Confirme em `POST /api/appointments/{id}/confirm`; esta ação publica `appointment.confirmed` no RabbitMQ.
4. Registre em `POST /api/payments` após a confirmação.
5. Consulte receita em `GET /api/financial/revenue?startDate=2026-08-01&endDate=2026-08-31`.

Consulte os exemplos de body no Swagger para cada operação. O relatório financeiro é exclusivo de `Admin`; o restante do fluxo de agenda requer `Admin` ou `Receptionist`.

## Respostas de erro

Erros esperados retornam `400`, `401`, `404` ou `409`, contendo uma coleção `errors` com `code` e `message`. Exceções inesperadas retornam `ProblemDetails` e incluem o `correlationId`, que também pode ser fornecido pelo cliente em `X-Correlation-ID`.
