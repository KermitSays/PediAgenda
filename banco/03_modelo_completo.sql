-- Arquivo antigo substituído por 01_Schema.sql (banco vazio).
-- Para banco com dados, use 03_remover_verificacao_email.sql.
-- Interrompe a execução para não recriar uma estrutura incompatível com a API.
SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'Script antigo: use 01_Schema.sql ou 03_remover_verificacao_email.sql.';
