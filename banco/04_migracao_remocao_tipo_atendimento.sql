-- PediAgenda — retirada definitiva de consulta.tipo_atendimento.
-- ATENCAO: NAO EXECUTAR AGORA.
-- Execute somente DEPOIS de validar a API/app alterados e confirmar
-- que a versao hospedada (Render) e outros ambientes nao consultam
-- nem inserem na coluna tipo_atendimento.
-- Este ALTER TABLE faz COMMIT IMPLICITO: START TRANSACTION / ROLLBACK
-- NAO desfazem DROP COLUMN. Faca backup do Aiven antes de executar.
-- Nao modifica o status; CONFIRMADA permanece permitido.

USE pediagenda;

-- 1. Inspecione a coluna atual:
SHOW COLUMNS FROM consulta LIKE 'tipo_atendimento';

-- 2. Inspecione a quantidade de consultas e os status antes da alteracao:
SELECT status, COUNT(*) AS quantidade FROM consulta GROUP BY status;

-- 3. Execute SOMENTE depois da implantacao de todas as versoes corrigidas:
-- ALTER TABLE consulta DROP COLUMN tipo_atendimento;

-- 4. Para confirmar posteriormente, repita:
-- SHOW COLUMNS FROM consulta LIKE 'tipo_atendimento';
-- SELECT status, COUNT(*) AS quantidade FROM consulta GROUP BY status;
