# ADR 0004 — Dapper para leitura analítica de faturamento

- **Status:** Aceita
- **Data:** 2026-08-08

## Contexto

O relatório de receita agrega pagamentos por data e moeda. Não precisa materializar agregados de domínio nem executar mudanças de estado.

## Decisão

Manter escrita e regras no EF Core, e implementar `IRevenueReportReader` com Dapper para essa leitura analítica.

## Consequências

- A query é direta, projetada apenas nos dados necessários e não carrega o change tracker do EF Core.
- O contrato pertence à Application, preservando a infraestrutura como detalhe substituível.
- SQL explícito exige testes e manutenção cuidadosa quando o esquema evoluir.
