# ADR 0001 — Clean Architecture e CQRS com MediatR

- **Status:** Aceita
- **Data:** 2026-08-08

## Contexto

O ClinicHub concentra regras de agenda, autenticação, pacientes e faturamento. Essas regras precisam continuar testáveis e independentes da forma de entrega HTTP e dos fornecedores de infraestrutura.

## Decisão

Adotar Clean Architecture com as camadas `Domain`, `Application`, `Infrastructure` e `API`. A Application separa intenções de escrita em Commands e leituras em Queries, ambos despachados pelo MediatR. Validators FluentValidation são executados no pipeline antes dos handlers.

## Consequências

- Regras e handlers podem ser testados sem web server ou banco externo.
- Dependências apontam para dentro; infraestrutura implementa contratos definidos pelas camadas internas.
- Há mais arquivos e tipos para casos de uso pequenos, um custo aceito para manter evolução previsível e regras explícitas.
