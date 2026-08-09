# ADR 0005 — Ativação de conta por confirmação de e-mail

- **Status:** Aceita
- **Data:** 2026-08-08

## Contexto

O cadastro público precisa comprovar posse do e-mail sem conceder acessos internos antes da ativação.

## Decisão

Criar contas públicas como `Patient`, inicialmente inativas. Gerar token criptograficamente aleatório, persistir apenas seu hash SHA-256 com expiração de 24 horas e ativar a conta após confirmação de uso único. Em desenvolvimento, registrar o link nos logs; em produção, enviar por SMTP configurável.

## Consequências

- Vazamento do banco não revela tokens de confirmação utilizáveis.
- A conta não pode obter JWT antes da confirmação.
- A entrega de e-mail permanece desacoplada da regra por uma abstração de sender, porém SMTP precisa de configuração operacional em produção.
