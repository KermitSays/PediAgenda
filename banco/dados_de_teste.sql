-- =====================================================================
-- PediAgenda - Dados de teste: medicos, recepcionista e horarios
-- =====================================================================
-- Rode depois do 03_modelo_completo.sql, quando quiser ter o que agendar.
-- Pode rodar quantas vezes quiser: ele apaga os dados de teste anteriores
-- (so os dele, reconhecidos pelo dominio @teste.pediagenda.local) e cria
-- de novo, com horarios sempre a partir de amanha.
--
-- Contas criadas (SENHAS SO PARA TESTE - nunca use em producao):
--
--   Dr. Pedro Alves     pedro.alves@teste.pediagenda.local    senhaMedico123
--                       Pediatria, manhas das 08:00 as 12:00
--   Dra. Carla Mendes   carla.mendes@teste.pediagenda.local   senhaMedico123
--                       Pneumologia Pediatrica, tardes das 13:00 as 17:00
--   Julia Rocha         julia.rocha@teste.pediagenda.local    senhaRecepcao123
--                       recepcionista
--
-- As senhas estao em hash PBKDF2-SHA256, como no cadastro real: o banco
-- nunca ve a senha em texto. Os dois medicos tem a mesma senha e hashes
-- diferentes - e o salt de cada um.
--
-- Horarios: blocos de 30 minutos nos proximos dias uteis (10 dias corridos
-- a partir de amanha, sem sabado e domingo). No primeiro dia util, o
-- Dr. Pedro tem 10:00 e 10:30 bloqueados, para testar horario indisponivel.
-- =====================================================================

USE pediagenda;

-- O Workbench recusa DELETE com JOIN no modo seguro.
SET SQL_SAFE_UPDATES = 0;

-- ---------------------------------------------------------------------
-- 1. Apaga os dados de teste anteriores, na ordem das chaves estrangeiras
-- ---------------------------------------------------------------------
DELETE pr
FROM   prontuario pr
JOIN   consulta      c ON c.id_consulta = pr.id_consulta
JOIN   horario       h ON h.id_horario  = c.id_horario
JOIN   agenda_medica a ON a.id_agenda   = h.id_agenda
JOIN   medico        m ON m.id_medico   = a.id_medico
JOIN   usuario       u ON u.id_usuario  = m.id_usuario
WHERE  u.email LIKE '%@teste.pediagenda.local';

DELETE c
FROM   consulta      c
JOIN   horario       h ON h.id_horario = c.id_horario
JOIN   agenda_medica a ON a.id_agenda  = h.id_agenda
JOIN   medico        m ON m.id_medico  = a.id_medico
JOIN   usuario       u ON u.id_usuario = m.id_usuario
WHERE  u.email LIKE '%@teste.pediagenda.local';

-- Apagar o usuario apaga em cascata: medico -> agenda_medica -> horario,
-- e recepcionista.
DELETE FROM usuario WHERE email LIKE '%@teste.pediagenda.local';

-- ---------------------------------------------------------------------
-- 2. Medicos e recepcionista
-- ---------------------------------------------------------------------
INSERT INTO usuario (nome, cpf, email, senha_hash, senha_salt, senha_iteracoes)
VALUES ('Dr. Pedro Alves', '90000000001', 'pedro.alves@teste.pediagenda.local',
        UNHEX('7E81D6259EF0EEF7E71B62F7D7D18D4407D1B40C37995735B70D1AF30F0A6FF5'),
        UNHEX('4C0D147A4136809059F6234D31FC981E'), 100000);
SET @usuario_pedro = LAST_INSERT_ID();
INSERT INTO medico (crm, especialidade, id_usuario)
VALUES ('CRM-SP 123456', 'Pediatria', @usuario_pedro);
SET @medico_pedro = LAST_INSERT_ID();
INSERT INTO agenda_medica (id_medico) VALUES (@medico_pedro);
SET @agenda_pedro = LAST_INSERT_ID();

INSERT INTO usuario (nome, cpf, email, senha_hash, senha_salt, senha_iteracoes)
VALUES ('Dra. Carla Mendes', '90000000002', 'carla.mendes@teste.pediagenda.local',
        UNHEX('EE56A5397627A30F40F7CC482342A187CB92E18A81D94C25C729BD9FAF5B80E0'),
        UNHEX('F67FA5A99AC8648CBE7E8D405E094E0F'), 100000);
SET @usuario_carla = LAST_INSERT_ID();
INSERT INTO medico (crm, especialidade, id_usuario)
VALUES ('CRM-SP 654321', 'Pneumologia Pediatrica', @usuario_carla);
SET @medico_carla = LAST_INSERT_ID();
INSERT INTO agenda_medica (id_medico) VALUES (@medico_carla);
SET @agenda_carla = LAST_INSERT_ID();

INSERT INTO usuario (nome, cpf, email, senha_hash, senha_salt, senha_iteracoes)
VALUES ('Julia Rocha', '90000000003', 'julia.rocha@teste.pediagenda.local',
        UNHEX('735C914193D56F83FDDBBE94A0A6394BC87C436D721671C83AC58E87419A3C2A'),
        UNHEX('51DC6C07FFB8436660C896612F68B9AE'), 100000);
INSERT INTO recepcionista (id_usuario) VALUES (LAST_INSERT_ID());

-- ---------------------------------------------------------------------
-- 3. Horarios: dias uteis dos proximos 10 dias, blocos de 30 minutos
-- ---------------------------------------------------------------------
INSERT INTO horario (data_horario, hora_inicio, hora_fim, id_agenda)
SELECT dias.dia, blocos.inicio, ADDTIME(blocos.inicio, '00:30:00'), @agenda_pedro
FROM  (SELECT CURDATE() + INTERVAL n DAY AS dia
       FROM (SELECT 1 n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
             UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7
             UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10) numeros) dias
CROSS JOIN
      (SELECT TIME '08:00:00' AS inicio UNION ALL SELECT TIME '08:30:00'
       UNION ALL SELECT TIME '09:00:00' UNION ALL SELECT TIME '09:30:00'
       UNION ALL SELECT TIME '10:00:00' UNION ALL SELECT TIME '10:30:00'
       UNION ALL SELECT TIME '11:00:00' UNION ALL SELECT TIME '11:30:00') blocos
WHERE DAYOFWEEK(dias.dia) NOT IN (1, 7);        -- 1 = domingo, 7 = sabado

INSERT INTO horario (data_horario, hora_inicio, hora_fim, id_agenda)
SELECT dias.dia, blocos.inicio, ADDTIME(blocos.inicio, '00:30:00'), @agenda_carla
FROM  (SELECT CURDATE() + INTERVAL n DAY AS dia
       FROM (SELECT 1 n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
             UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7
             UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10) numeros) dias
CROSS JOIN
      (SELECT TIME '13:00:00' AS inicio UNION ALL SELECT TIME '13:30:00'
       UNION ALL SELECT TIME '14:00:00' UNION ALL SELECT TIME '14:30:00'
       UNION ALL SELECT TIME '15:00:00' UNION ALL SELECT TIME '15:30:00'
       UNION ALL SELECT TIME '16:00:00' UNION ALL SELECT TIME '16:30:00') blocos
WHERE DAYOFWEEK(dias.dia) NOT IN (1, 7);

-- Dois horarios bloqueados, para testar o filtro de disponibilidade.
UPDATE horario
SET    disponivel = FALSE
WHERE  id_agenda = @agenda_pedro
  AND  data_horario = (SELECT primeiro FROM (SELECT MIN(data_horario) AS primeiro
                                             FROM horario WHERE id_agenda = @agenda_pedro) x)
  AND  hora_inicio IN ('10:00:00', '10:30:00');

SET SQL_SAFE_UPDATES = 1;

-- ---------------------------------------------------------------------
-- 4. Conferencia
-- ---------------------------------------------------------------------
SELECT   u.nome                                  AS medico,
         m.especialidade,
         COUNT(*)                                AS horarios,
         SUM(h.disponivel)                       AS disponiveis,
         MIN(h.data_horario)                     AS primeiro_dia,
         MAX(h.data_horario)                     AS ultimo_dia
FROM     medico m
JOIN     usuario       u ON u.id_usuario = m.id_usuario
JOIN     agenda_medica a ON a.id_medico  = m.id_medico
JOIN     horario       h ON h.id_agenda  = a.id_agenda
WHERE    u.email LIKE '%@teste.pediagenda.local'
GROUP BY u.nome, m.especialidade
ORDER BY u.nome;
