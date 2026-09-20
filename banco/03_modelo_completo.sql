-- =====================================================================
-- PediAgenda - Modelo fisico completo (Agenda 4 - Desenvolvimento)
--
-- Responsavel pela modelagem: Joab Antonio de Souza
-- Base: Diagrama de Classes do grupo (Talita, PTCC agenda 05)
-- SGBD: MySQL 8.0
--
-- ATENCAO: este script SUBSTITUI a estrutura criada pelo
-- 01_estrutura_usuarios.sql. Ele apaga o schema pediagenda e recria
-- tudo do zero, seguindo o diagrama de classes. So rode se o banco
-- tiver apenas dados de teste.
--
-- Ajustes em relacao ao diagrama de classes (justificados no relatorio):
--   1. senha -> senha_hash, senha_salt, senha_iteracoes      (RNF01)
--   2. login removido: o login e feito pelo e-mail            (RF03)
--   3. tentativas_invalidas e bloqueado_ate em usuario        (RNF02)
--   4. nova tabela tentativa_login (auditoria de acesso)      (RNF02)
--   5. consulta sem data/horario/id_medico: sao obtidos pelo
--      horario reservado (3a Forma Normal, evita redundancia)
--   6. ServicoNotificacao nao vira tabela: e classe de servico,
--      sem dados proprios para armazenar
-- =====================================================================

DROP DATABASE IF EXISTS pediagenda;

CREATE DATABASE pediagenda
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

USE pediagenda;

-- ---------------------------------------------------------------------
-- USUARIO: classe base da generalizacao (dados de autenticacao)
-- ---------------------------------------------------------------------
CREATE TABLE usuario (
    id_usuario           INT UNSIGNED     NOT NULL AUTO_INCREMENT,
    nome                 VARCHAR(120)     NOT NULL,
    cpf                  CHAR(11)         NOT NULL,
    email                VARCHAR(160)     NOT NULL,
    senha_hash           VARBINARY(32)    NOT NULL,
    senha_salt           VARBINARY(16)    NOT NULL,
    senha_iteracoes      INT UNSIGNED     NOT NULL DEFAULT 100000,
    ativo                BOOLEAN          NOT NULL DEFAULT TRUE,
    tentativas_invalidas TINYINT UNSIGNED NOT NULL DEFAULT 0,
    bloqueado_ate        DATETIME         NULL,
    criado_em            DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_usuario       PRIMARY KEY (id_usuario),
    CONSTRAINT uq_usuario_cpf   UNIQUE (cpf),
    CONSTRAINT uq_usuario_email UNIQUE (email)
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Especializacoes de USUARIO (1:1). O UNIQUE em id_usuario garante que
-- um usuario seja no maximo um responsavel, um medico, um recepcionista.
-- ---------------------------------------------------------------------
CREATE TABLE responsavel (
    id_responsavel INT UNSIGNED NOT NULL AUTO_INCREMENT,
    telefone       VARCHAR(20)  NOT NULL,
    id_usuario     INT UNSIGNED NOT NULL,
    CONSTRAINT pk_responsavel         PRIMARY KEY (id_responsavel),
    CONSTRAINT uq_responsavel_usuario UNIQUE (id_usuario),
    CONSTRAINT fk_responsavel_usuario FOREIGN KEY (id_usuario)
        REFERENCES usuario (id_usuario) ON DELETE CASCADE
) ENGINE = InnoDB;

CREATE TABLE medico (
    id_medico     INT UNSIGNED NOT NULL AUTO_INCREMENT,
    crm           VARCHAR(20)  NOT NULL,
    especialidade VARCHAR(60)  NOT NULL DEFAULT 'Pediatria',
    id_usuario    INT UNSIGNED NOT NULL,
    CONSTRAINT pk_medico         PRIMARY KEY (id_medico),
    CONSTRAINT uq_medico_crm     UNIQUE (crm),
    CONSTRAINT uq_medico_usuario UNIQUE (id_usuario),
    CONSTRAINT fk_medico_usuario FOREIGN KEY (id_usuario)
        REFERENCES usuario (id_usuario) ON DELETE CASCADE
) ENGINE = InnoDB;

CREATE TABLE recepcionista (
    id_recepcionista INT UNSIGNED NOT NULL AUTO_INCREMENT,
    id_usuario       INT UNSIGNED NOT NULL,
    CONSTRAINT pk_recepcionista         PRIMARY KEY (id_recepcionista),
    CONSTRAINT uq_recepcionista_usuario UNIQUE (id_usuario),
    CONSTRAINT fk_recepcionista_usuario FOREIGN KEY (id_usuario)
        REFERENCES usuario (id_usuario) ON DELETE CASCADE
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- PACIENTE: a crianca atendida. N pacientes por responsavel.
-- ---------------------------------------------------------------------
CREATE TABLE paciente (
    id_paciente     INT UNSIGNED NOT NULL AUTO_INCREMENT,
    nome            VARCHAR(120) NOT NULL,
    data_nascimento DATE         NOT NULL,
    id_responsavel  INT UNSIGNED NOT NULL,
    CONSTRAINT pk_paciente             PRIMARY KEY (id_paciente),
    CONSTRAINT fk_paciente_responsavel FOREIGN KEY (id_responsavel)
        REFERENCES responsavel (id_responsavel) ON DELETE RESTRICT
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- AGENDA_MEDICA: cada medico possui uma agenda (1:1).
-- ---------------------------------------------------------------------
CREATE TABLE agenda_medica (
    id_agenda INT UNSIGNED NOT NULL AUTO_INCREMENT,
    id_medico INT UNSIGNED NOT NULL,
    CONSTRAINT pk_agenda_medica PRIMARY KEY (id_agenda),
    CONSTRAINT uq_agenda_medico UNIQUE (id_medico),
    CONSTRAINT fk_agenda_medico FOREIGN KEY (id_medico)
        REFERENCES medico (id_medico) ON DELETE CASCADE
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- HORARIO: os blocos de atendimento da agenda. O UNIQUE impede criar o
-- mesmo bloco duas vezes na mesma agenda.
-- ---------------------------------------------------------------------
CREATE TABLE horario (
    id_horario   INT UNSIGNED NOT NULL AUTO_INCREMENT,
    data_horario DATE         NOT NULL,
    hora_inicio  TIME         NOT NULL,
    hora_fim     TIME         NOT NULL,
    disponivel   BOOLEAN      NOT NULL DEFAULT TRUE,
    id_agenda    INT UNSIGNED NOT NULL,
    CONSTRAINT pk_horario        PRIMARY KEY (id_horario),
    CONSTRAINT uq_horario_bloco  UNIQUE (id_agenda, data_horario, hora_inicio),
    CONSTRAINT ck_horario_ordem  CHECK (hora_fim > hora_inicio),
    CONSTRAINT fk_horario_agenda FOREIGN KEY (id_agenda)
        REFERENCES agenda_medica (id_agenda) ON DELETE CASCADE
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- CONSULTA: liga paciente a um horario. Data, hora e medico sao
-- obtidos pelo horario (horario -> agenda_medica -> medico).
-- ---------------------------------------------------------------------
CREATE TABLE consulta (
    id_consulta      INT UNSIGNED NOT NULL AUTO_INCREMENT,
    status           ENUM('AGENDADA','CONFIRMADA','CANCELADA','REALIZADA')
                                  NOT NULL DEFAULT 'AGENDADA',
    tipo_atendimento ENUM('CONVENIO','PARTICULAR') NOT NULL,
    id_paciente      INT UNSIGNED NOT NULL,
    id_horario       INT UNSIGNED NOT NULL,
    criada_em        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_consulta          PRIMARY KEY (id_consulta),
    CONSTRAINT fk_consulta_paciente FOREIGN KEY (id_paciente)
        REFERENCES paciente (id_paciente) ON DELETE RESTRICT,
    CONSTRAINT fk_consulta_horario  FOREIGN KEY (id_horario)
        REFERENCES horario (id_horario) ON DELETE RESTRICT
) ENGINE = InnoDB;

CREATE INDEX ix_consulta_horario ON consulta (id_horario, status);

-- ---------------------------------------------------------------------
-- PRONTUARIO: cada consulta gera no maximo um registro (1:0..1).
-- ---------------------------------------------------------------------
CREATE TABLE prontuario (
    id_prontuario INT UNSIGNED NOT NULL AUTO_INCREMENT,
    observacoes   TEXT         NOT NULL,
    data_registro DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    id_consulta   INT UNSIGNED NOT NULL,
    CONSTRAINT pk_prontuario          PRIMARY KEY (id_prontuario),
    CONSTRAINT uq_prontuario_consulta UNIQUE (id_consulta),
    CONSTRAINT fk_prontuario_consulta FOREIGN KEY (id_consulta)
        REFERENCES consulta (id_consulta) ON DELETE RESTRICT
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- NOTIFICACAO: confirmacoes e lembretes disparados por consulta (1:N).
-- ---------------------------------------------------------------------
CREATE TABLE notificacao (
    id_notificacao INT UNSIGNED NOT NULL AUTO_INCREMENT,
    canal          ENUM('EMAIL','SISTEMA') NOT NULL,
    mensagem       VARCHAR(500) NOT NULL,
    data_envio     DATETIME     NULL,
    id_consulta    INT UNSIGNED NOT NULL,
    CONSTRAINT pk_notificacao          PRIMARY KEY (id_notificacao),
    CONSTRAINT fk_notificacao_consulta FOREIGN KEY (id_consulta)
        REFERENCES consulta (id_consulta) ON DELETE CASCADE
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- RELATORIO_ATENDIMENTO: relatorios emitidos por medico e periodo.
-- ---------------------------------------------------------------------
CREATE TABLE relatorio_atendimento (
    id_relatorio   INT UNSIGNED NOT NULL AUTO_INCREMENT,
    inicio_periodo DATE         NOT NULL,
    fim_periodo    DATE         NOT NULL,
    gerado_em      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    id_medico      INT UNSIGNED NOT NULL,
    CONSTRAINT pk_relatorio        PRIMARY KEY (id_relatorio),
    CONSTRAINT ck_relatorio_periodo CHECK (fim_periodo >= inicio_periodo),
    CONSTRAINT fk_relatorio_medico FOREIGN KEY (id_medico)
        REFERENCES medico (id_medico) ON DELETE CASCADE
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- TENTATIVA_LOGIN: auditoria das tentativas de acesso (RNF02).
-- ---------------------------------------------------------------------
CREATE TABLE tentativa_login (
    id_tentativa    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    email_informado VARCHAR(160)    NOT NULL,
    sucesso         BOOLEAN         NOT NULL,
    motivo          VARCHAR(40)     NULL,
    ocorrido_em     DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    id_usuario      INT UNSIGNED    NULL,
    CONSTRAINT pk_tentativa_login   PRIMARY KEY (id_tentativa),
    CONSTRAINT fk_tentativa_usuario FOREIGN KEY (id_usuario)
        REFERENCES usuario (id_usuario) ON DELETE SET NULL
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Conferencia: devem aparecer 12 tabelas
-- ---------------------------------------------------------------------
SHOW TABLES;
