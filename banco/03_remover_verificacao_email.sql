-- Migração para banco EXISTENTE: não apaga usuários, senhas ou atendimentos.
-- Selecione pediagenda no Workbench e execute uma vez; pode repetir com segurança.
-- DDL no MySQL faz commit implícito. Execute antes de iniciar a API atualizada.
USE pediagenda;
SET @sql_safe_updates_anterior = @@SQL_SAFE_UPDATES;
SET SQL_SAFE_UPDATES = 0;

-- Contas pré-cadastradas não possuem senha; a recepção deve informar o e-mail.
ALTER TABLE usuario
    MODIFY email VARCHAR(160) NULL,
    MODIFY senha_hash VARBINARY(32) NULL,
    MODIFY senha_salt VARBINARY(16) NULL,
    MODIFY ativo BOOLEAN NOT NULL DEFAULT FALSE;

-- Registra somente aceites novos: não inventa datas para cadastros antigos.
SET @sql = IF(EXISTS(
    SELECT 1 FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'usuario'
      AND COLUMN_NAME = 'termos_aceitos_em'),
    'DO 0', 'ALTER TABLE usuario ADD COLUMN termos_aceitos_em DATETIME NULL');
PREPARE migracao FROM @sql;
EXECUTE migracao;
DEALLOCATE PREPARE migracao;

-- Remove somente as colunas antigas que realmente existirem nesta versão.
SELECT GROUP_CONCAT(CONCAT('DROP COLUMN `', COLUMN_NAME, '`') SEPARATOR ', ')
INTO @colunas
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'usuario'
  AND COLUMN_NAME IN ('email_verificado', 'email_verificado_em',
                      'token_verificacao_hash', 'token_verificacao_expira_em');
SET @sql = IF(@colunas IS NULL, 'DO 0', CONCAT('ALTER TABLE usuario ', @colunas));
PREPARE migracao FROM @sql;
EXECUTE migracao;
DEALLOCATE PREPARE migracao;

SELECT GROUP_CONCAT(CONCAT('DROP COLUMN `', COLUMN_NAME, '`') SEPARATOR ', ')
INTO @colunas
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'responsavel'
  AND COLUMN_NAME IN ('codigo_ativacao_hash', 'codigo_ativacao_expira_em', 'codigo_ativacao_usado_em');
SET @sql = IF(@colunas IS NULL, 'DO 0', CONCAT('ALTER TABLE responsavel ', @colunas));
PREPARE migracao FROM @sql;
EXECUTE migracao;
DEALLOCATE PREPARE migracao;

-- Mantém ativo/inativo e credenciais de todas as contas existentes.
-- A remoção da confirmação não reativa contas desativadas pela clínica.
SET SQL_SAFE_UPDATES = @sql_safe_updates_anterior;
SELECT 'Migração concluída; dados existentes preservados.' AS resultado;
