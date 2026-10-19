-- =====================================================================
-- PediAgenda - 02_popula_tabelas.sql (compatível com 01_Schema.sql)
-- MySQL 8.0 - SOMENTE DADOS FICTÍCIOS / AMBIENTE DE TESTES
--
-- ATENÇÃO: execute APENAS em um schema pediagenda de TESTES, VAZIO,
-- previamente criado pelo modelo físico. Não execute em produção/Aiven
-- =====================================================================

SET NAMES utf8mb4;
USE pediagenda;
-- Senha de TODAS as contas completas desta carga fictícia: senhaDemo123
START TRANSACTION;

-- 1. Usuários: IDs originais preservados, colunas explícitas.
INSERT INTO usuario
  (id_usuario, nome, cpf, email, senha_hash, senha_salt,
   senha_iteracoes, ativo, tentativas_invalidas, bloqueado_ate)
VALUES
  (1, 'Maria de Teste', '11122233344', 'maria@teste.local', 0x41c21d45e7b4e90cd98529366dfb6257f8897391819e8052e8b357d9f16d7298, 0x7064c86f220ff828c0b8a40d35753009, 100000, TRUE, 0, NULL),
  (2, 'Pedro de Teste', '55566677788', 'pedro@teste.local', 0x9d0e82354dcebc37128aacc0728ded6f20f48fb10460626b0a4f1dbdc5882b77, 0x0639dd3700381ba93af35688cd150cfe, 100000, TRUE, 0, NULL),
  (4, 'Dra. Ana Teste', '99999999999', 'medico.teste@example.com', 0xcb9d1fa7dc779fef9b92b5097729ec62b070150c58f26e9985fa7647f6cd53ed, 0x43fa834313730dc736c6b6c6b2f06dd8, 100000, TRUE, 0, NULL),
  (5, 'Carla Recepção Teste', '88888888888', 'recepcao.teste@example.com', 0x665dc48c436fd8bdbade96432c4da37fc42476ca37aa1f51575a0840014db624, 0xa562a73e785826ae49d5c2922aff85c5, 100000, TRUE, 0, NULL),
  (6, 'Ana Oliveira', '90000000001', 'ana.oliveira@example.com', 0x4ae1dc522744ae0268bfff19dc60908c5696e3ed02605f8982f8e849173ef0e9, 0xe7382edadb17d00df76aadab3c960d29, 100000, TRUE, 0, NULL),
  (7, 'Bruno Souza', '90000000002', 'bruno.souza@example.com', 0x8195c32be8c7f88f5be37e1bbb82bb4956dfdcd55da2f7d9352eb135ad7083ff, 0x86a1338ed12fd1f0d18e75f8e97f4766, 100000, TRUE, 0, NULL),
  (8, 'Carla Santos', '90000000003', 'carla.santos@example.com', 0xc9909f02013cb74edb292888768bcb682b8f564a02ecf99ea0a57a521ce3e2cc, 0xa1787a274c056e57c57ca9832719faf5, 100000, TRUE, 0, NULL),
  (9, 'Diego Lima', '90000000004', 'diego.lima@example.com', 0x3f9e1716815e1f088fd64c4f00b16490b6865ced307a8c810c7fbc2ef2e349fb, 0xc324c3da40c4cdbd52034403b6f0d588, 100000, TRUE, 0, NULL),
  (10, 'Dra. Helena Duarte', '90000000005', 'helena.duarte@example.com', 0x34f64ef6c548cf99da51b32fb4196de99b6f6711e337594ff8e5f8e262a7c3a2, 0xc69ca28d9168cc5ef58d6c154a89ba37, 100000, TRUE, 0, NULL),
  (11, 'Dr. Rafael Mendes', '90000000006', 'rafael.mendes@example.com', 0x41b7b46b548e949d1ee33eb51a9349b9767ccbb5bdc5cd6b568095e8c5dcbb1d, 0x76e0a4c1a087a7249adc3cec61c1cf73, 100000, TRUE, 0, NULL),
  (12, 'Dra. Mariana Costa', '90000000007', 'mariana.costa@example.com', 0x1ed604d1ec02cfb069cde6a5eec94ee643886abfb5a02089f82e2f86e59b0266, 0xe446b982bad4f9c7452f46b9df9bfa53, 100000, TRUE, 0, NULL),
  (13, 'Dr. Gustavo Almeida', '90000000008', 'gustavo.almeida@example.com', 0xad28e330252b33ff6e58299c77cd311b07d544307bf5405e6f3f04fb30dc039c, 0x1b957e3e5aa74c0518b0cdeacbc90fa1, 100000, TRUE, 0, NULL),
  (14, 'Dra. Camila Rodrigues', '90000000009', 'camila.rodrigues@example.com', 0xce989a7aa6c2770e0f2e1af7e26cb56d50824ea570c368b850afb2191a1edb90, 0xc5974ff76c693600467d209d081a08ad, 100000, TRUE, 0, NULL),
  (15, 'Dr. Felipe Martins', '90000000010', 'felipe.martins@example.com', 0xbceccb5c735558b4dc5c479684bc6cdd0a9248419ee1c24df5428b51a947f0cf, 0x8c7a25a26705af132869e188c94d366b, 100000, TRUE, 0, NULL),
  (16, 'Dra. Beatriz Ferreira', '90000000011', 'beatriz.ferreira@example.com', 0x5b3c1960038ca0468db0fd6ff1b02ace0f6f32248365a32c13d723f4cc3137fe, 0xa6e8d85d4ba3beeae9f9b11461cd381d, 100000, TRUE, 0, NULL),
  (17, 'Dr. Lucas Rocha', '90000000012', 'lucas.rocha@example.com', 0x905b48666e2a2aaa76a5500a3a61b43a68b41a1f81c03d8e5ae54ac5a19cd6fe, 0xf8e7db505740a9bfe3a439468903ec32, 100000, TRUE, 0, NULL),
  (18, 'Julia Barbosa', '90000000013', 'julia.barbosa@example.com', 0x2f07e74759d731a542a78eba0971855cb880534c00f5c4ec541c01c2d7cafc99, 0xbc5c39fbbd09e98cf204b55fdbd04853, 100000, TRUE, 0, NULL),
  (19, 'Marcos Ribeiro', '90000000014', 'marcos.ribeiro@example.com', 0xe3a9206c22a5e6a1f614e2188f002292ca9c4404afe250f35ec4d992f20847a9, 0xae4d092dd3f9ed869f8543e79558318e, 100000, TRUE, 0, NULL);

-- Pré-cadastro com e-mail informado pela clínica e sem senha.
INSERT INTO usuario
  (id_usuario, nome, cpf, email, senha_hash, senha_salt, ativo)
VALUES
  (20, 'Responsável Pré-Cadastrado (Teste)', '52998224725', 'precadastro@example.com', NULL, NULL, FALSE);

-- 2. Responsáveis: chave estrangeira id_usuario incluída em TODOS os registros.
INSERT INTO responsavel (id_responsavel, telefone, id_usuario)
VALUES
  (1, '11999998888', 1),
  (2, '11000000001', 6),
  (3, '11000000002', 7),
  (4, '11000000003', 8),
  (5, '11000000004', 9);

INSERT INTO responsavel (id_responsavel, telefone, id_usuario)
VALUES (6, '11900000000', 20);

-- 3. Médicos (a especialidade é um atributo de médico neste schema).
INSERT INTO medico (id_medico, crm, especialidade, id_usuario)
VALUES
  (1, 'CRM-SP 999999', 'Pediatria', 2),
  (2, 'CRM-SP 000001', 'Pediatria', 4),
  (3, 'DEMO-SP-0001', 'Pediatria', 10),
  (4, 'DEMO-SP-0002', 'Pediatria', 11),
  (5, 'DEMO-SP-0003', 'Neuropediatria', 12),
  (6, 'DEMO-SP-0004', 'Neuropediatria', 13),
  (7, 'DEMO-SP-0005', 'Cardiologia pediátrica', 14),
  (8, 'DEMO-SP-0006', 'Cardiologia pediátrica', 15),
  (9, 'DEMO-SP-0007', 'Endocrinologia pediátrica', 16),
  (10,'DEMO-SP-0008', 'Endocrinologia pediátrica', 17);

-- 4. Recepcionistas.
INSERT INTO recepcionista (id_recepcionista, id_usuario)
VALUES (1, 5), (2, 18), (3, 19);

-- 5. Pacientes (os nove registros de origem foram preservados).
INSERT INTO paciente (id_paciente, nome, data_nascimento, id_responsavel)
VALUES
  (1, 'Lucas de Teste', '2025-03-10', 1),
  (2, 'Sofia Oliveira', '2020-05-12', 2),
  (3, 'Lucas Souza', '2019-08-23', 3),
  (4, 'Alice Souza', '2023-02-15', 3),
  (5, 'Gabriel Santos', '2016-11-09', 4),
  (6, 'Manuela Santos', '2020-04-18', 4),
  (7, 'Pedro Santos', '2024-06-02', 4),
  (8, 'Laura Lima', '2018-09-21', 5),
  (9, 'Miguel Lima', '2022-12-05', 5);

-- 6. Uma agenda para cada um dos dez médicos.
INSERT INTO agenda_medica (id_agenda, id_medico)
VALUES (1,1), (2,2), (3,3), (4,4), (5,5),
       (6,6), (7,7), (8,8), (9,9), (10,10);

-- 7. Datas dinâmicas: os PRÓXIMOS três dias úteis, não datas vencidas.
-- Isso permite testar GET /api/horarios e POST /api/consultas depois.
SET @dia1 := DATE_ADD(CURDATE(), INTERVAL
  CASE WEEKDAY(CURDATE()) WHEN 4 THEN 3 WHEN 5 THEN 2 ELSE 1 END DAY);
SET @dia2 := DATE_ADD(@dia1, INTERVAL IF(WEEKDAY(@dia1) = 4, 3, 1) DAY);
SET @dia3 := DATE_ADD(@dia2, INTERVAL IF(WEEKDAY(@dia2) = 4, 3, 1) DAY);

-- 10 agendas * 3 datas * 4 blocos = 120 horários.
INSERT INTO horario
  (data_horario, hora_inicio, hora_fim, disponivel, id_agenda)
SELECT d.dia, h.inicio, h.fim, TRUE, a.id_agenda
FROM agenda_medica AS a
CROSS JOIN (
  SELECT @dia1 AS dia UNION ALL
  SELECT @dia2 UNION ALL
  SELECT @dia3
) AS d
CROSS JOIN (
  SELECT '09:00:00' AS inicio, '09:30:00' AS fim UNION ALL
  SELECT '09:30:00', '10:00:00' UNION ALL
  SELECT '10:00:00', '10:30:00' UNION ALL
  SELECT '10:30:00', '11:00:00'
) AS h;

-- Horário histórico para exemplificar uma consulta já realizada.
INSERT INTO horario
  (data_horario, hora_inicio, hora_fim, disponivel, id_agenda)
VALUES
  (DATE_SUB(CURDATE(), INTERVAL 7 DAY), '14:00:00', '14:30:00', TRUE, 3);

-- 8. Consultas de exemplo. Buscar id_horario pela chave natural
-- evita depender da numeração gerada automaticamente nos 121 horários.
INSERT INTO consulta
  (id_consulta, status, tipo_atendimento, id_paciente, id_horario)
SELECT 1, 'AGENDADA', 'PARTICULAR', 2, h.id_horario
FROM horario h
WHERE h.id_agenda = 1 AND h.data_horario = @dia1 AND h.hora_inicio = '09:00:00';

INSERT INTO consulta
  (id_consulta, status, tipo_atendimento, id_paciente, id_horario)
SELECT 2, 'CONFIRMADA', 'CONVENIO', 3, h.id_horario
FROM horario h
WHERE h.id_agenda = 2 AND h.data_horario = @dia1 AND h.hora_inicio = '09:30:00';

INSERT INTO consulta
  (id_consulta, status, tipo_atendimento, id_paciente, id_horario)
SELECT 3, 'REALIZADA', 'PARTICULAR', 5, h.id_horario
FROM horario h
WHERE h.id_agenda = 3
  AND h.data_horario = DATE_SUB(CURDATE(), INTERVAL 7 DAY)
  AND h.hora_inicio = '14:00:00';

INSERT INTO consulta
  (id_consulta, status, tipo_atendimento, id_paciente, id_horario)
SELECT 4, 'CANCELADA', 'PARTICULAR', 8, h.id_horario
FROM horario h
WHERE h.id_agenda = 4 AND h.data_horario = @dia1 AND h.hora_inicio = '10:30:00';

-- 9. Prontuário fictício para a consulta realizada.
INSERT INTO prontuario (id_prontuario, observacoes, id_consulta)
VALUES (1, 'Registro fictício de atendimento para teste da API.', 3);

-- 10. Notificações de SISTEMA, sem disparar e-mails reais.
INSERT INTO notificacao (id_notificacao, canal, mensagem, data_envio, id_consulta)
VALUES
  (1, 'SISTEMA', 'Consulta agendada para teste.', NULL, 1),
  (2, 'SISTEMA', 'Consulta confirmada para teste.', NULL, 2),
  (3, 'SISTEMA', 'Consulta cancelada para teste.', NULL, 4);

-- 11. Relatórios ligados a médicos existentes.
INSERT INTO relatorio_atendimento
  (id_relatorio, inicio_periodo, fim_periodo, id_medico)
VALUES
  (1, DATE_SUB(CURDATE(), INTERVAL 30 DAY), CURDATE(), 3),
  (2, CURDATE(), DATE_ADD(CURDATE(), INTERVAL 30 DAY), 1);

-- 12. Histórico fictício de tentativas de login.
INSERT INTO tentativa_login
  (id_tentativa, email_informado, sucesso, motivo, ocorrido_em, id_usuario)
VALUES
  (1, 'teste-nao-existe@pediagenda.com', FALSE, 'email inexistente', '2026-10-04 04:33:35', NULL),
  (2, 'teste-nao-existe@pediagenda.com', FALSE, 'email inexistente', '2026-10-04 05:12:56', NULL),
  (3, 'recepcao.teste@example.com', TRUE, NULL, '2026-10-05 04:27:36', 5),
  (4, 'medico.teste@example.com', TRUE, NULL, '2026-10-07 06:33:35', 4),
  (5, 'medico.teste@example.com', TRUE, NULL, '2026-10-07 06:35:25', 4),
  (6, 'medico.teste@example.com', TRUE, NULL, '2026-10-09 00:48:33', 4),
  (7, 'recepcao.teste@example.com', TRUE, NULL, '2026-10-09 00:49:19', 5);

COMMIT;

-- Conferência: 12 tabelas / totais esperados logo após a carga.
SELECT 'usuario' AS tabela, COUNT(*) AS total FROM usuario
UNION ALL SELECT 'responsavel', COUNT(*) FROM responsavel
UNION ALL SELECT 'medico', COUNT(*) FROM medico
UNION ALL SELECT 'recepcionista', COUNT(*) FROM recepcionista
UNION ALL SELECT 'paciente', COUNT(*) FROM paciente
UNION ALL SELECT 'agenda_medica', COUNT(*) FROM agenda_medica
UNION ALL SELECT 'horario', COUNT(*) FROM horario
UNION ALL SELECT 'consulta', COUNT(*) FROM consulta
UNION ALL SELECT 'prontuario', COUNT(*) FROM prontuario
UNION ALL SELECT 'notificacao', COUNT(*) FROM notificacao
UNION ALL SELECT 'relatorio_atendimento', COUNT(*) FROM relatorio_atendimento
UNION ALL SELECT 'tentativa_login', COUNT(*) FROM tentativa_login;

-- Os horários disponíveis em dia útil para testar o Postman:
SELECT h.id_horario, h.data_horario, h.hora_inicio,
       m.id_medico, u.nome AS medico, m.especialidade
FROM horario h
JOIN agenda_medica a ON a.id_agenda = h.id_agenda
JOIN medico m ON m.id_medico = a.id_medico
JOIN usuario u ON u.id_usuario = m.id_usuario
WHERE h.data_horario >= CURDATE()
  AND h.disponivel = TRUE
  AND NOT EXISTS (
    SELECT 1 FROM consulta c
    WHERE c.id_horario = h.id_horario AND c.status <> 'CANCELADA'
  )
ORDER BY h.data_horario, h.hora_inicio, m.id_medico
LIMIT 20;

