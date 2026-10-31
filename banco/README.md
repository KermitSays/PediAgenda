# Banco de dados — PediAgenda (MySQL 8)

A estrutura final destinada a um **banco novo e vazio** contém 11 tabelas: `usuario`, `responsavel`, `medico`, `recepcionista`, `paciente`, `agenda_medica`, `horario`, `consulta`, `prontuario`, `relatorio_atendimento` e `tentativa_login`. Não há tabela `notificacao`, nem coluna `consulta.tipo_atendimento`. O status `CONFIRMADA` continua disponível.

## Scripts

| Arquivo | Objetivo |
| --- | --- |
| `01_Schema.sql` | Cria o schema em um banco NOVO e vazio; **não** é uma migração do Aiven atual. |
| `02_popula_tabelas.sql` | Dados fictícios e credenciais para um banco de testes isolado; **não executar no Aiven em uso**. |
| `03_migracao_confirmacao_brevo.sql` | Adiciona os quatro campos de verificação de e-mail a bancos antigos; **já executado no Aiven em 30/10/2026**, não repetir. |
| `04_migracao_remocao_tipo_atendimento.sql` | Roteiro de conferência; `DROP COLUMN` fica comentado até a API do Render e todas as integrações estarem atualizadas. |

## Verificação de e-mail

A tabela `usuario` contém `email_verificado`, `email_verificado_em`, `token_verificacao_hash` e `token_verificacao_expira_em` para a ativação por link enviado via Brevo. Não existe campo de código de ativação para digitação no aplicativo.

Em um banco novo, a carga fictícia marca como verificados os usuários de demonstração que já têm credenciais, preservando o responsável de pré-cadastro como inativo/não verificado. Os dados fictícios **não** refletem necessariamente a situação atual do Aiven.

## Segurança e atualização

Não execute `01_Schema.sql` ou `02_popula_tabelas.sql` sobre o Aiven em utilização. Faça backup e compare a estrutura com as versões implantadas antes de quaisquer alterações. `ALTER TABLE` implica confirmação automática e **não é revertido por `ROLLBACK`**. Não versionar dump de produção, dados de usuários ou segredos de conexão.

As operações de cancelamento da aplicação preservam o histórico da consulta por atualização de status (exclusão lógica).
