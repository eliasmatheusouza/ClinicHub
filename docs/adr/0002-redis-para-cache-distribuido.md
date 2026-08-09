# ADR 0002 — Redis para cache distribuído de pacientes

- **Status:** Aceita
- **Data:** 2026-08-08

## Contexto

Listagens de pacientes são consultadas com frequência e podem ser servidas por múltiplas instâncias da API. Um cache em memória seria isolado por processo e perderia consistência entre réplicas.

## Decisão

Usar Redis como cache distribuído das listagens filtradas e paginadas de pacientes. As entradas têm TTL de cinco minutos e uma versão de cache é incrementada apenas depois do commit de criação, alteração ou desativação.

## Consequências

- Todas as instâncias compartilham o mesmo cache.
- A versão na chave evita invalidar conjuntos por busca ampla de chaves.
- Redis é uma otimização: indisponibilidade gera cache miss e a API continua lendo do SQL Server.
