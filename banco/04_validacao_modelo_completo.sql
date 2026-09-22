-- =====================================================================
-- PediAgenda - Validacao do modelo completo (12 tabelas)
--
-- Pre-requisito: ter rodado o 03_modelo_completo.sql.
--
-- COMO RODAR NO WORKBENCH:
--   PARTE 1 -> pode rodar tudo de uma vez (raio). So consulta, nao altera nada.
--   PARTE 2 -> UMA INSTRUCAO POR VEZ: clique na linha e Ctrl+Enter, na ordem.
--              Varios testes DEVEM dar erro de proposito. O erro e o acerto.
--   PARTE 3 -> limpeza, tambem uma por vez e na ordem.
--
-- Codigos de erro esperados:
--   1062 = valor duplicado (UNIQUE)
--   1452 = chave estrangeira aponta para registro inexistente
--   1451 = tentativa de apagar registro que outro ainda usa (RESTRICT)
--   3819 = regra CHECK violada
--   1265 = valor fora da lista do ENUM
-- =====================================================================

USE pediagenda;

-- =====================================================================
-- PARTE 1 - CONFERENCIA (nao altera nada)
-- =====================================================================

-- 1.1 As doze tabelas existem?   Esperado: 12 linhas
SELECT TABLE_NAME AS tabela
FROM   information_schema.TABLES
WHERE  TABLE_SCHEMA = 'pediagenda'
ORDER  BY TABLE_NAME;

-- 1.2 Resumo das restricoes
--     Esperado: PRIMARY KEY 12 | FOREIGN KEY 12 | UNIQUE 9 | CHECK 2
SELECT CONSTRAINT_TYPE AS tipo, COUNT(*) AS quantidade
FROM   information_schema.TABLE_CONSTRAINTS
WHERE  TABLE_SCHEMA = 'pediagenda'
GROUP  BY CONSTRAINT_TYPE
ORDER  BY CONSTRAINT_TYPE;

-- 1.3 RELACIONAMENTOS: as 12 chaves estrangeiras e a regra de exclusao
SELECT rc.TABLE_NAME            AS tabela,
       k.COLUMN_NAME            AS coluna,
       rc.REFERENCED_TABLE_NAME AS aponta_para,
       rc.DELETE_RULE           AS ao_apagar
FROM   information_schema.REFERENTIAL_CONSTRAINTS rc
JOIN   information_schema.KEY_COLUMN_USAGE k
       ON  k.CONSTRAINT_SCHEMA = rc.CONSTRAINT_SCHEMA
       AND k.CONSTRAINT_NAME   = rc.CONSTRAINT_NAME
WHERE  rc.CONSTRAINT_SCHEMA = 'pediagenda'
ORDER  BY rc.TABLE_NAME;

-- 1.4 Modo estrito ligado? O teste T08 (ENUM) depende disso.
--     Esperado: o texto contem STRICT_TRANS_TABLES
SELECT @@sql_mode AS modo_sql;


-- =====================================================================
-- PARTE 2 - TESTES (uma instrucao por vez, na ordem)
-- =====================================================================

-- Workbench bloqueia DELETE sem chave no WHERE; desliga so nesta aba.
SET SQL_SAFE_UPDATES = 0;

-- ---------------------------------------------------------------------
-- Montagem do cenario: um responsavel com um filho e um medico com agenda
-- ---------------------------------------------------------------------

-- T01 -- DEVE FUNCIONAR: usuario da responsavel
INSERT INTO usuario (nome, cpf, email, senha_hash, senha_salt)
VALUES ('Maria de Teste', '11122233344', 'maria@teste.local',
        UNHEX(REPEAT('AB',32)), UNHEX(REPEAT('CD',16)));

-- T02 -- DEVE FUNCIONAR: Maria passa a ser responsavel (heranca)
INSERT INTO responsavel (telefone, id_usuario)
VALUES ('11999998888', (SELECT id_usuario FROM usuario WHERE email = 'maria@teste.local'));

-- T03 -- DEVE FUNCIONAR: o filho da Maria
INSERT INTO paciente (nome, data_nascimento, id_responsavel)
VALUES ('Lucas de Teste', '2025-03-10',
        (SELECT r.id_responsavel FROM responsavel r
         JOIN usuario u ON u.id_usuario = r.id_usuario
         WHERE u.email = 'maria@teste.local'));

-- T04 -- DEVE FUNCIONAR: usuario do medico
INSERT INTO usuario (nome, cpf, email, senha_hash, senha_salt)
VALUES ('Pedro de Teste', '55566677788', 'pedro@teste.local',
        UNHEX(REPEAT('AB',32)), UNHEX(REPEAT('CD',16)));

-- T05 -- DEVE FUNCIONAR: Pedro passa a ser medico
INSERT INTO medico (crm, id_usuario)
VALUES ('CRM-SP 999999', (SELECT id_usuario FROM usuario WHERE email = 'pedro@teste.local'));

-- T06 -- DEVE FUNCIONAR: agenda do medico
INSERT INTO agenda_medica (id_medico)
VALUES ((SELECT id_medico FROM medico WHERE crm = 'CRM-SP 999999'));

-- T07 -- DEVE FUNCIONAR: um horario das 09:00 as 09:30
INSERT INTO horario (data_horario, hora_inicio, hora_fim, id_agenda)
VALUES ('2026-10-05', '09:00', '09:30',
        (SELECT a.id_agenda FROM agenda_medica a
         JOIN medico m ON m.id_medico = a.id_medico
         WHERE m.crm = 'CRM-SP 999999'));

-- ---------------------------------------------------------------------
-- Regras que o banco precisa garantir
-- ---------------------------------------------------------------------

-- T08 -- DEVE FALHAR com 1062: e-mail ja cadastrado
INSERT INTO usuario (nome, cpf, email, senha_hash, senha_salt)
VALUES ('Outra Pessoa', '99988877766', 'maria@teste.local',
        UNHEX(REPEAT('AB',32)), UNHEX(REPEAT('CD',16)));

-- T09 -- DEVE FALHAR com 1062: o mesmo usuario virar responsavel duas vezes
INSERT INTO responsavel (telefone, id_usuario)
VALUES ('11977776666', (SELECT id_usuario FROM usuario WHERE email = 'maria@teste.local'));

-- T10 -- DEVE FALHAR com 1452: paciente de um responsavel que nao existe
INSERT INTO paciente (nome, data_nascimento, id_responsavel)
VALUES ('Crianca Orfa', '2024-01-01', 99999);

-- T11 -- DEVE FALHAR com 1062: segunda agenda para o mesmo medico
INSERT INTO agenda_medica (id_medico)
VALUES ((SELECT id_medico FROM medico WHERE crm = 'CRM-SP 999999'));

-- T12 -- DEVE FALHAR com 3819: horario que termina antes de comecar
INSERT INTO horario (data_horario, hora_inicio, hora_fim, id_agenda)
VALUES ('2026-10-05', '10:00', '09:00',
        (SELECT a.id_agenda FROM agenda_medica a
         JOIN medico m ON m.id_medico = a.id_medico WHERE m.crm = 'CRM-SP 999999'));

-- T13 -- DEVE FALHAR com 1062: o mesmo bloco de horario duas vezes
INSERT INTO horario (data_horario, hora_inicio, hora_fim, id_agenda)
VALUES ('2026-10-05', '09:00', '09:30',
        (SELECT a.id_agenda FROM agenda_medica a
         JOIN medico m ON m.id_medico = a.id_medico WHERE m.crm = 'CRM-SP 999999'));

-- T14 -- DEVE FALHAR com 1265: tipo de atendimento fora da lista
INSERT INTO consulta (tipo_atendimento, id_paciente, id_horario)
VALUES ('DINHEIRO',
        (SELECT id_paciente FROM paciente WHERE nome = 'Lucas de Teste'),
        (SELECT id_horario FROM horario WHERE data_horario = '2026-10-05' AND hora_inicio = '09:00'));

-- T15 -- DEVE FUNCIONAR: a consulta valida
INSERT INTO consulta (tipo_atendimento, id_paciente, id_horario)
VALUES ('CONVENIO',
        (SELECT id_paciente FROM paciente WHERE nome = 'Lucas de Teste'),
        (SELECT id_horario FROM horario WHERE data_horario = '2026-10-05' AND hora_inicio = '09:00'));

-- T16 -- DEVE FUNCIONAR: duas notificacoes para a mesma consulta (1:N)
INSERT INTO notificacao (canal, mensagem, id_consulta)
SELECT 'EMAIL', 'Consulta agendada para 05/10 as 09:00.', c.id_consulta
FROM consulta c JOIN paciente p ON p.id_paciente = c.id_paciente
WHERE p.nome = 'Lucas de Teste';

INSERT INTO notificacao (canal, mensagem, id_consulta)
SELECT 'SISTEMA', 'Lembrete: sua consulta e amanha.', c.id_consulta
FROM consulta c JOIN paciente p ON p.id_paciente = c.id_paciente
WHERE p.nome = 'Lucas de Teste';

-- T17 -- DEVE FUNCIONAR: o prontuario da consulta
INSERT INTO prontuario (observacoes, id_consulta)
SELECT 'Consulta de rotina. Crescimento adequado para a idade.', c.id_consulta
FROM consulta c JOIN paciente p ON p.id_paciente = c.id_paciente
WHERE p.nome = 'Lucas de Teste';

-- T18 -- DEVE FALHAR com 1062: segundo prontuario para a mesma consulta (1:0..1)
INSERT INTO prontuario (observacoes, id_consulta)
SELECT 'Registro duplicado.', c.id_consulta
FROM consulta c JOIN paciente p ON p.id_paciente = c.id_paciente
WHERE p.nome = 'Lucas de Teste';

-- T19 -- DEVE FALHAR com 3819: relatorio com periodo invertido
INSERT INTO relatorio_atendimento (inicio_periodo, fim_periodo, id_medico)
VALUES ('2026-10-31', '2026-10-01', (SELECT id_medico FROM medico WHERE crm = 'CRM-SP 999999'));

-- T20 -- DEVE FALHAR com 1451: apagar paciente que tem consulta no historico
DELETE FROM paciente WHERE nome = 'Lucas de Teste';

-- T21 -- CONFERENCIA: a consulta montada a partir de 6 tabelas
-- Data, hora e medico nao estao gravados na consulta: vem do horario,
-- que pertence a uma agenda, que pertence a um medico (3a Forma Normal).
-- Esperado: 1 linha com Lucas, Maria, Pedro, 05/10 09:00, CONVENIO, 2 notificacoes
SELECT p.nome            AS paciente,
       ur.nome           AS responsavel,
       um.nome           AS medico,
       h.data_horario    AS data,
       h.hora_inicio     AS hora,
       c.tipo_atendimento,
       c.status,
       (SELECT COUNT(*) FROM notificacao n WHERE n.id_consulta = c.id_consulta) AS notificacoes,
       (SELECT COUNT(*) FROM prontuario pr WHERE pr.id_consulta = c.id_consulta) AS prontuario
FROM   consulta c
JOIN   paciente      p  ON p.id_paciente     = c.id_paciente
JOIN   responsavel   r  ON r.id_responsavel  = p.id_responsavel
JOIN   usuario       ur ON ur.id_usuario     = r.id_usuario
JOIN   horario       h  ON h.id_horario      = c.id_horario
JOIN   agenda_medica a  ON a.id_agenda       = h.id_agenda
JOIN   medico        m  ON m.id_medico       = a.id_medico
JOIN   usuario       um ON um.id_usuario     = m.id_usuario;


-- =====================================================================
-- PARTE 3 - LIMPEZA (uma por vez, na ordem)
-- A ordem importa: RESTRICT obriga a apagar primeiro quem depende.
-- =====================================================================

-- L1: apaga o prontuario (ele trava a consulta)
DELETE FROM prontuario
WHERE  id_consulta IN (SELECT id_consulta FROM (
         SELECT c.id_consulta FROM consulta c
         JOIN paciente p ON p.id_paciente = c.id_paciente
         WHERE p.nome = 'Lucas de Teste') AS x);

-- L2: apaga a consulta. As 2 notificacoes devem sumir junto (CASCADE).
DELETE FROM consulta
WHERE  id_paciente = (SELECT id_paciente FROM paciente WHERE nome = 'Lucas de Teste');

-- L3: prova do CASCADE. Esperado: 0
SELECT COUNT(*) AS notificacoes_restantes FROM notificacao;

-- L4: agora o paciente pode sair (nao ha mais consulta)
DELETE FROM paciente WHERE nome = 'Lucas de Teste';

-- L5: apaga os dois usuarios. Em cascata somem: responsavel, medico,
--     agenda_medica e horario.
DELETE FROM usuario WHERE email IN ('maria@teste.local', 'pedro@teste.local');

-- L6: tudo zerado? Esperado: todas as colunas 0
SELECT (SELECT COUNT(*) FROM usuario)       AS usuarios,
       (SELECT COUNT(*) FROM responsavel)   AS responsaveis,
       (SELECT COUNT(*) FROM medico)        AS medicos,
       (SELECT COUNT(*) FROM agenda_medica) AS agendas,
       (SELECT COUNT(*) FROM horario)       AS horarios,
       (SELECT COUNT(*) FROM paciente)      AS pacientes,
       (SELECT COUNT(*) FROM consulta)      AS consultas;

-- L7: religa a protecao do Workbench
SET SQL_SAFE_UPDATES = 1;
