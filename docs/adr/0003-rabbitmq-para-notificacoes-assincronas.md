# ADR 0003 — RabbitMQ para notificações de consultas confirmadas

- **Status:** Aceita
- **Data:** 2026-08-08

## Contexto

Confirmar uma consulta não deve depender da latência ou disponibilidade de um provedor de e-mail. A operação principal precisa persistir a transição de estado e responder ao usuário com segurança.

## Decisão

Após confirmar e persistir a consulta, publicar `appointment.confirmed` no RabbitMQ. Um Worker Service separado consome uma fila durável e simula o envio de notificação em log estruturado.

## Consequências

- A confirmação HTTP não fica acoplada à notificação.
- O consumidor pode ser escalado e evoluído para provedores reais sem alterar o agregado.
- Mensageria introduz observabilidade e tratamento de falhas adicionais; por isso há health check, fila durável e ack manual.
