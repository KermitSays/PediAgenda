-- Executar somente apos backup e verificacao da estrutura atual do Aiven.
-- Uma unica vez, no banco escolhido para homologacao; NAO aplicado automaticamente.
ALTER TABLE usuario
    ADD COLUMN email_verificado BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN email_verificado_em DATETIME NULL,
    ADD COLUMN token_verificacao_hash VARBINARY(32) NULL,
    ADD COLUMN token_verificacao_expira_em DATETIME NULL;

-- Preserva o acesso de contas JA ATIVAS antes da migracao.
-- Conferir os registros antes de executar, principalmente responsaveis de teste.
UPDATE usuario
SET email_verificado = TRUE,
    email_verificado_em = UTC_TIMESTAMP()
WHERE ativo = TRUE AND senha_hash IS NOT NULL AND senha_salt IS NOT NULL;
