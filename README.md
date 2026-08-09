# ClinicHub

Plataforma de gestão de agendamentos e financeiro para clínicas médicas, construída como projeto de estudo de engenharia de software.

O acompanhamento das etapas e suas validações está em [docs/plano-de-execucao.md](docs/plano-de-execucao.md).

## Estrutura inicial

```text
src/
  ClinicHub.Domain/                  # Núcleo do domínio
  ClinicHub.Application/             # Casos de uso e contratos
  ClinicHub.Infrastructure/          # Persistência e integrações
  ClinicHub.API/                     # API HTTP
  ClinicHub.Notifications.Worker/    # Consumidor assíncrono
tests/                               # Projetos de testes (próxima etapa)
docs/adr/                            # Decisões arquiteturais
```

## Executar a infraestrutura local

1. Copie `.env.example` para `.env`.
2. Execute `docker compose up --build`.
3. A API estará em `http://localhost:8082/health/live` e o RabbitMQ em `http://localhost:15672`.

O cliente Angular está em `frontend/clinichub-web`. Para rodá-lo isoladamente, execute `npm start` nessa pasta e acesse `http://localhost:4200`. O Docker Compose também sobe o frontend no mesmo endereço.

## Banco de dados e migrations

Após iniciar o SQL Server, aplique a migration inicial com:

```powershell
dotnet ef database update --project src/ClinicHub.Infrastructure --startup-project src/ClinicHub.API --context ClinicHubDbContext
```

## Observabilidade

- Swagger (desenvolvimento): `http://localhost:8082/swagger`.
- Liveness: `GET /health/live` — confirma que o processo está em execução.
- Readiness: `GET /health/ready` — verifica SQL Server, Redis e RabbitMQ.
- Envie `X-Correlation-ID` para correlacionar logs e respostas; caso ausente, a API gera um identificador.
- Logs estruturados são emitidos no console e enviados ao Seq em `http://localhost:8081` quando a infraestrutura estiver ativa.

## Acesso de desenvolvimento

Ao iniciar pelo Docker Compose, as migrations são aplicadas e usuários Admin e Doctor de desenvolvimento são criados uma única vez. As credenciais vêm das variáveis `SEED_ADMIN_*` e `SEED_DOCTOR_*` no `.env` (os valores de exemplo são apenas para ambiente local). Autentique-se em `POST /api/auth/login` e use o access token como `Bearer` no Swagger.

## Cadastro e confirmação de e-mail

A tela `http://localhost:4200/register` cria uma conta com o perfil `Patient`, inicialmente inativa. A senha precisa ter pelo menos oito caracteres, incluindo letra maiúscula, minúscula e número. A ativação acontece pelo link enviado por e-mail e, até isso ocorrer, o login é recusado.

- `POST /api/auth/register` — recebe `email`, `password` e `confirmPassword` e retorna `202 Accepted`.
- `POST /api/auth/confirm-email` — recebe o token presente no link e ativa a conta.

Em desenvolvimento, `EMAIL_DELIVERY_MODE=Log` registra o link de confirmação no log da API. Para envio real, configure no `.env` `EMAIL_DELIVERY_MODE=Smtp`, `EMAIL_FROM`, `EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT`, `EMAIL_SMTP_USE_SSL`, `EMAIL_SMTP_USERNAME` e `EMAIL_SMTP_PASSWORD`. O token bruto não é persistido: somente seu hash SHA-256 é armazenado, com validade de 24 horas.

## Pacientes

Todos os endpoints exigem JWT. `Admin` e `Receptionist` podem criar/alterar; somente `Admin` pode desativar; `Doctor` pode consultar.

- `POST /api/patients` — cria um paciente.
- `GET /api/patients?term=&page=1&pageSize=20` — lista pacientes ativos, filtrando por nome ou e-mail.
- `GET /api/patients/{id}` — consulta um paciente.
- `PUT /api/patients/{id}` — altera dados cadastrais.
- `DELETE /api/patients/{id}` — desativa logicamente o paciente.

As listagens são armazenadas no Redis por cinco minutos e invalidadas por versão após qualquer alteração de paciente. Se Redis estiver indisponível, a consulta continua diretamente no SQL Server.

## Agendamentos e notificações

`Admin` e `Receptionist` podem agendar, confirmar, reagendar e cancelar consultas.

- `POST /api/appointments` — agenda para um paciente e médico existentes.
- `POST /api/appointments/{id}/confirm` — confirma e publica `appointment.confirmed`.
- `PUT /api/appointments/{id}/schedule` — reagenda, reaplicando a regra de conflito.
- `POST /api/appointments/{id}/cancel` — cancela com motivo obrigatório.

A confirmação só é aceita para horários futuros. O intervalo do médico é validado contra consultas não canceladas. Após a confirmação ser persistida, um evento de domínio percorre o MediatR, é publicado no exchange RabbitMQ `clinichub.appointments` e consumido pelo worker, que registra a simulação da notificação em log estruturado.

## Financeiro

- `POST /api/payments` — registra um pagamento para uma consulta confirmada. Aceita `AppointmentId`, `Amount`, `Currency` e `Method` (enum de `PaymentMethod`).
- `GET /api/financial/revenue?startDate=2026-08-01&endDate=2026-08-31` — relatório de faturamento, exclusivo de `Admin`.

O registro de pagamento impede duplicidade por consulta. O relatório é uma query Dapper otimizada, agregada por dia e moeda.

## Testes

```powershell
dotnet test ClinicHub.sln --no-restore
dotnet test ClinicHub.sln --no-restore --collect "XPlat Code Coverage"
```

Há projetos separados para testes de Domain, Application, Infrastructure e integração da API. A cobertura de Domain/Application é aferida durante a etapa de testes e só será confirmada ao atingir a meta de 70%.

## CI/CD

O workflow [`.github/workflows/ci.yml`](.github/workflows/ci.yml) executa em todo `push`, `pull request` e disparo manual. Ele possui três jobs:

- **Backend:** restauração, formatação/análise estática, build Release, testes e artefato de cobertura.
- **Frontend:** instalação determinística (`npm ci`), análise TypeScript, build e testes Angular.
- **Docker:** validação do Compose e construção das imagens da API, worker e frontend.

A primeira execução remota ocorrerá automaticamente quando o repositório for enviado ao GitHub.
