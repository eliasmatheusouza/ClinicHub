# Proteção de dados e plano de criptografia

O ClinicHub trata dados pessoais e de saúde. Esta página separa controles já aplicados de decisões que precisam de infraestrutura de produção antes de serem implementadas.

## Controles já aplicados

| Controle | Aplicação |
|---|---|
| Minimização na listagem | `GET /api/patients` e o cache Redis retornam somente nome, e-mail mascarado e telefone mascarado. Data de nascimento não integra a lista. |
| Detalhe sob demanda | O dado completo é retornado apenas por `GET /api/patients/{id}` a uma policy administrativa autorizada, ou pelo portal `/api/patient-portal/me` ao próprio paciente. |
| Senhas e tokens | Senhas usam hash de Identity; refresh e confirmação de e-mail são persistidos apenas como SHA-256. |
| Trânsito | Production exige origens HTTPS e ativa redirecionamento HTTPS/HSTS. |
| Auditoria | Mutações de recursos registram ator, papel, rota, status e CorrelationId sem corpo de requisição ou dados clínicos. |

Masking reduz exposição acidental em telas, logs e cache; ele **não substitui** autorização nem criptografia. Usuários com autorização para abrir o detalhe ainda veem o dado necessário para seu trabalho.

## Plano de criptografia para produção

Não é seguro colocar uma chave AES fixa no `appsettings` ou inventar uma cifra própria. A implementação ocorrerá somente junto de um cofre de segredos/KMS e seguirá esta sequência:

1. Classificar dados: identificar campos clínicos, documentos, identificadores nacionais e anexos que exigem criptografia adicional além de TLS e disco gerenciado.
2. Usar banco gerenciado com criptografia em repouso e backups criptografados; limitar acesso ao banco por rede, identidade e menor privilégio.
3. Para campos de maior criticidade, usar envelope encryption: uma chave de dados AES-GCM por registro ou lote, protegida por uma chave mestra no KMS/Key Vault. A aplicação recebe permissão de desencriptar, não a chave mestra exportável.
4. Versionar o identificador de chave e o algoritmo junto do ciphertext para permitir rotação e recriptografia gradual, sem indisponibilidade.
5. Nunca cifrar diretamente campos que precisam de busca exata sem desenhar o índice correspondente. E-mail de login, por exemplo, exige normalização e índice único; uma busca protegida precisa de token de pesquisa derivado por chave separada ou serviço apropriado.
6. Definir retenção, exportação, correção, anonimização e exclusão com responsáveis e evidência de auditoria, conforme a base legal e os requisitos LGPD aplicáveis.

## Critérios antes de armazenar dado clínico real

- KMS/Key Vault configurado por ambiente, permissões mínimas e rotação testada.
- Backups, restauração e revogação de acesso exercitados.
- Threat model revisado para acesso interno, vazamento de banco, logs, anexos e integrações.
- Testes cobrem decrypt autorizado, negação sem permissão e rotação de chave.
- DPO/jurídico e segurança validam a base legal, retenção e resposta a incidentes.

Até esses critérios serem atendidos, o ClinicHub deve continuar como ambiente de aprendizado, com dados fictícios.
