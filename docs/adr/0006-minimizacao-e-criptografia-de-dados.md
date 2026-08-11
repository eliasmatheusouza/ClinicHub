# ADR 0006 — Minimização na leitura e criptografia orientada por KMS

- **Status:** Aceita
- **Data:** 2026-08-11

## Contexto

Listas operacionais e cache não precisam propagar todos os dados pessoais de um paciente. Ao mesmo tempo, criptografia de campo precisa de gestão de chaves, rotação e busca planejada; uma chave AES no código aumentaria o risco em vez de reduzi-lo.

## Decisão

Retornar um DTO mascarado nas listagens de pacientes, mantendo dados completos apenas no detalhe administrativo autorizado e no endpoint próprio do paciente. O cache Redis armazena o mesmo DTO minimizado.

Para dados clínicos futuros, adotar criptografia em repouso no serviço de dados e envelope encryption com KMS/Key Vault, versionamento e rotação de chaves. Não será implementada criptografia de campo antes de existir essa infraestrutura.

## Consequências

- E-mail, telefone e nascimento deixam de circular em listagens e no cache de listagem.
- A interface consulta o detalhe autorizado somente ao editar um paciente.
- Dados de saúde reais exigirão preparação de infraestrutura e governança antes de serem persistidos.
