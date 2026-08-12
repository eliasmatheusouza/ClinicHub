# Funcionalidades de Produto — ClinicHub como Plataforma Médica SaaS

Este é o catálogo de evolução funcional do ClinicHub. Ele descreve uma direção de produto para transformar o MVP administrativo em uma plataforma médica, sem confundir documento técnico com certificação clínica, jurídica ou regulatória.

> **Princípio essencial:** dados de saúde são sensíveis. Antes de disponibilizar qualquer funcionalidade a clientes reais, é necessária validação jurídica, regulatória, de segurança e de privacidade aplicável à operação, ao país, ao conselho profissional e ao tipo de atendimento. Este documento orienta aprendizado e descoberta de produto; não substitui essas avaliações.

## Visão do produto

O ClinicHub pode evoluir para uma plataforma SaaS multi-clínica que une agenda, atendimento, prontuário, documentos, comunicação e autoatendimento. O produto deve manter a clínica no controle de sua operação e o paciente no controle de seu acesso, sempre com rastreabilidade.

### Perfis principais

| Perfil | Necessidade central | Limite de acesso esperado |
|---|---|---|
| Administrador da clínica | Configurar equipe, unidades, regras e indicadores. | Apenas sua organização e funções administrativas autorizadas. |
| Recepcionista | Cadastrar pacientes, operar agenda e comunicar orientações. | Dados mínimos necessários para atendimento administrativo. |
| Profissional de saúde | Atender, registrar evolução e emitir documentos. | Pacientes sob vínculo assistencial e conforme policy da clínica. |
| Paciente | Agendar, acompanhar seus documentos e gerenciar horários próprios. | Exclusivamente os próprios dados e documentos liberados. |
| Worker de integração | Entregar comunicações e processar eventos. | Sem acesso interativo; permissões mínimas e auditáveis. |

## Regras transversais antes de novos módulos

Todo módulo deste catálogo deve ser desenvolvido com os mesmos fundamentos:

- **Tenant e ownership no servidor:** cliente, clínica, unidade, profissional e paciente nunca são separados apenas por filtros da SPA Angular.
- **Auditoria:** registrar quem visualizou, criou, alterou, baixou, compartilhou ou excluiu dados sensíveis, com data, contexto e correlação.
- **Privacidade por padrão:** coletar o mínimo, mascarar logs, definir retenção e não usar dados reais em desenvolvimento/testes.
- **Segurança de arquivos e integrações:** autorização antes do acesso, secrets fora do Git, validação de entrada, limites de taxa e tratamento idempotente de webhooks.
- **Resiliência:** ações síncronas críticas devem ter transações claras; integrações assíncronas dependem da Etapa 23 (outbox, retry, DLQ e idempotência).
- **Experiência acessível:** carregamento, erro, vazio, teclado, contraste e mensagens compreensíveis fazem parte da definição de pronto.

## 1. Prontuário Eletrônico do Paciente (PEP)

### Objetivo de negócio

Permitir que o profissional registre, consulte e evolua informações clínicas de modo estruturado e auditável durante o atendimento.

### Escopo inicial

- anamnese e antecedentes relevantes;
- evolução por atendimento, com autoria, data e vínculo à consulta;
- hipóteses e diagnósticos por catálogo de codificação versionado, como CID-10/CID-11 quando aplicável;
- alergias, problemas ativos, sinais vitais e plano de cuidado;
- prescrições, atestados e receitas geradas a partir de modelos aprovados;
- histórico cronológico que evidencie correções sem apagar indevidamente a informação anterior.

### Regras de domínio a decidir antes do código

1. Quem pode criar, corrigir, assinar, visualizar e compartilhar cada tipo de registro?
2. Um registro assinado pode ser alterado, adendado ou somente retificado? Como a versão anterior permanece auditável?
3. Quais campos são estruturados para relatório e quais são texto livre?
4. Como o sistema lida com consentimento, menor de idade, responsável legal e troca de profissional?
5. Qual catálogo clínico é adotado, qual versão e como ele será atualizado sem alterar registros históricos?

### Critério de conclusão do primeiro incremento

- [ ] Profissional autorizado registra e consulta evolução de uma consulta própria.
- [ ] Acesso horizontal indevido é bloqueado e testado.
- [ ] Alterações e leituras relevantes deixam trilha de auditoria.
- [ ] O histórico preserva autoria, horário e versão/retificação.
- [ ] Dados clínicos não aparecem em logs, erros de tela ou dados de teste.

## 2. Exames e documentos clínicos com upload seguro

### Objetivo de negócio

Permitir anexar e disponibilizar exames, laudos e imagens sem tornar a API um repositório de arquivos público ou expor documentos por URL previsível.

### Desenho recomendado

Usar armazenamento de objetos privado: AWS S3, Azure Blob Storage ou MinIO no laboratório local. O banco guarda somente metadados necessários — proprietário, tipo, tamanho, hash, status de varredura, retenção e chave opaca do objeto — e não o arquivo binário como padrão.

O acesso deve ocorrer por URL temporária assinada ou por streaming autorizado da API. Em ambos os casos, a aplicação verifica tenant, ownership e permissão antes de entregar o documento.

### Fluxo seguro de upload

```text
Usuário autorizado
  → pede permissão de upload
  → API valida contexto, tipo, tamanho e quota
  → objeto privado recebe chave opaca
  → antivírus/validador assíncrono inspeciona o arquivo
  → metadado muda para disponível ou rejeitado
  → download exige nova autorização e acesso temporário
```

### Controles mínimos

- allowlist de MIME type e extensão, limite de tamanho e bloqueio de arquivos executáveis;
- validação do conteúdo, e não apenas do `Content-Type` enviado pelo navegador;
- varredura antimalware antes de liberar download;
- criptografia em trânsito e em repouso, conforme capacidade do provedor;
- chaves opacas, bucket/container privado e URLs curtas, sem documentos em repositório ou logs;
- auditoria de upload, visualização, download, rejeição e exclusão;
- política de retenção, exclusão e restauração coerente com a clínica e legislação aplicável.

### Critério de conclusão do primeiro incremento

- [ ] Upload sintético de PDF/imagem validado e objeto permanece privado.
- [ ] Arquivo malformado, tipo proibido e usuário sem permissão são rejeitados em testes.
- [ ] Somente usuário autorizado obtém download temporário.
- [ ] Status de varredura impede acesso antes da aprovação.
- [ ] Exclusão segue retenção, auditoria e não deixa URLs permanentes válidas.

## 3. Grade dinâmica de disponibilidade médica (Slot Generator)

### Objetivo de negócio

Trocar horários cadastrados manualmente por regras de disponibilidade capazes de gerar slots livres de maneira consistente para agenda interna e portal do paciente.

### Capacidades do módulo

- configurar semana padrão por profissional/unidade: dias, turnos, duração de slot, intervalos e modalidade;
- cadastrar exceções: férias, feriados, bloqueios, encaixes e horários extraordinários;
- considerar duração do procedimento, sala/equipamento e fuso horário da clínica;
- gerar disponibilidade sob demanda para uma janela limitada, sem materializar indefinidamente todo o calendário;
- reservar temporariamente um slot durante a confirmação e impedir dupla reserva.

### Regras críticas

1. A fonte de verdade é a regra de disponibilidade mais consultas confirmadas e bloqueios; a SPA recebe apenas slots permitidos.
2. O servidor revalida o slot dentro da transação de agendamento. Dois usuários podem ver o mesmo horário, mas somente um pode confirmá-lo.
3. Todas as datas devem ser armazenadas e comparadas com estratégia explícita de UTC/fuso da clínica, incluindo mudanças de horário legal quando aplicáveis.
4. Cancelamento e reagendamento devolvem ou bloqueiam disponibilidade conforme política configurável.

### Critério de conclusão do primeiro incremento

- [ ] Agenda semanal com turnos e intervalo gera slots corretos na API.
- [ ] Férias, feriado e bloqueio substituem a regra padrão.
- [ ] Duas tentativas concorrentes não confirmam o mesmo slot.
- [ ] Frontend exibe disponibilidade da API e trata slot indisponível no momento da confirmação.
- [ ] Testes incluem fronteiras de dia, duração, intervalo e fuso horário.

## 4. Notificações reais por WhatsApp e e-mail transacional

### Objetivo de negócio

Reduzir faltas e tarefas manuais com lembretes, confirmação e reagendamento assistidos.

### Integrações possíveis

O worker pode possuir adaptadores para Meta WhatsApp Cloud API, Twilio e provedor de e-mail transacional. A escolha deve comparar custo, disponibilidade regional, templates, consentimento, suporte e requisitos contratuais; o domínio não deve depender diretamente de SDK de fornecedor.

### Fluxo de lembrete

```text
Consulta confirmada
  → evento salvo em outbox
  → worker publica e agenda lembrete (ex.: 24 h antes)
  → provedor entrega template aprovado
  → paciente escolhe confirmar ou iniciar reagendamento
  → webhook assinado retorna ao ClinicHub
  → API valida assinatura, idempotência e altera somente o estado permitido
  → auditoria registra entrega e ação
```

### Regras e cuidados

- obter e registrar consentimento e preferência de canal; disponibilizar opt-out quando aplicável;
- não incluir diagnóstico, prontuário ou informação clínica sensível no texto da notificação;
- manter templates versionados e mensagens parametrizadas, com fallback de canal quando permitido;
- validar assinatura do webhook, limitar taxa, idempotência e responder rapidamente; processamento pesado fica assíncrono;
- acompanhar entrega, falha, retry, DLQ e custo por mensagem;
- não iniciar esta integração antes de concluir a confiabilidade de eventos da Etapa 23.

### Critério de conclusão do primeiro incremento

- [ ] Adaptador fake permite testar o domínio sem chamar fornecedor real.
- [ ] Evento persiste e é entregue uma única vez mesmo com retry.
- [ ] Webhook inválido ou repetido não altera consulta indevidamente.
- [ ] Paciente consegue confirmar ou iniciar reagendamento conforme policy.
- [ ] Métricas, auditoria e opt-out são verificados por testes.

## 5. Portal de autoatendimento do paciente

### Objetivo de negócio

Permitir que o paciente resolva tarefas próprias sem depender da recepção e sem acesso às áreas administrativas.

### Escopo inicial

- histórico de consultas e próximos atendimentos próprios;
- confirmação, cancelamento e solicitação de reagendamento segundo a política da clínica;
- download de documentos explicitamente liberados, por acesso temporário autorizado;
- visualização de receitas e atestados gerados pelo profissional;
- atualização de dados de contato e preferências de comunicação;
- acompanhamento do status de solicitações, sem revelar informações de outros pacientes ou profissionais.

### Prescrições, atestados e assinatura

PDF é somente um formato de documento; assinatura digital possui requisitos legais, técnicos e de identidade que variam por jurisdição. A funcionalidade deve começar com geração de documento, autoria, versão e auditoria. Assinatura digital só deve ser habilitada após escolha de provedor e validação jurídica aplicável.

### Critério de conclusão do primeiro incremento

- [ ] Paciente visualiza apenas consultas, documentos e dados vinculados à própria conta.
- [ ] Cancelamento/reagendamento respeita antecedência, disponibilidade e política configurada.
- [ ] Documento é liberado pelo profissional e baixado por acesso temporário auditado.
- [ ] Expiração de sessão, erro de rede e ausência de dados possuem tratamento acessível no Angular.
- [ ] Teste E2E cobre login, consulta própria, tentativa de acesso indevido e ação permitida.

## Backlog comercial posterior

Estes itens devem ser descobertos com clínicas piloto; não devem ser iniciados todos de uma vez.

| Funcionalidade | Valor | Dependências principais |
|---|---|---|
| Multi-clínica, unidades e convites | Permite comercializar para mais de uma organização com isolamento. | Autorização por recurso, auditoria e tenant. |
| Gestão de equipe e credenciais | Organiza profissionais, especialidades, salas e permissões. | Convites, policies e agenda dinâmica. |
| Cobrança, faturas e conciliação | Reduz operação manual e integra o ciclo financeiro. | Regras financeiras, auditoria e integrações resilientes. |
| Teleatendimento | Amplia acesso e atendimento remoto. | Privacidade, consentimento, provedor especializado e observabilidade. |
| Relatórios operacionais | Apoia decisões de ocupação, faltas e receita. | Dados confiáveis, permissões e privacidade agregada. |
| Integrações clínicas e fiscais | Reduz retrabalho em ecossistemas existentes. | Contratos, versionamento, segurança e avaliação regulatória. |

## Sequência de entrega recomendada

1. Concluir base de capacidade, confiabilidade de eventos, observabilidade e autorização por recurso.
2. Entregar grade de disponibilidade e políticas de cancelamento/reagendamento.
3. Entregar portal do paciente com os próprios agendamentos, sem documentos clínicos inicialmente.
4. Integrar notificações reais com provider fake, outbox e webhooks seguros.
5. Adicionar PEP em incrementos pequenos e auditáveis.
6. Adicionar anexos clínicos privados com varredura e retenção.
7. Evoluir documentos e assinatura somente após avaliação jurídica e escolha de provedor.

Cada incremento deve produzir uma ADR quando introduzir uma decisão com alternativas relevantes, testes unitários/integração/E2E adequados e atualização dos diagramas/guia operacional.
