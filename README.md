# PediAgenda: Sistema de Agendamento e Atendimento Pediátrico

> Sistema digital voltado para o agendamento e gerenciamento de consultas pediátricas, alinhado ao Objetivo de Desenvolvimento Sustentável ODS-3 (Saúde e Bem-estar) da ONU.

## 👥 Equipe e Papéis
* **Talita Barbara Cardanha Lopes** - Product Owner
* **Sara Helena de Oliveira Nunes Ribeiro** - Scrum Master
* **Lara Vitoria Araujo Moreira** - Dev Team

## 📋 Sobre o Projeto
O **PediAgenda** foi desenvolvido para modernizar e otimizar o fluxo de atendimentos em clínicas pediátricas. O sistema mitiga problemas comuns como filas e atrasos organizando a rotina através de perfis distintos:
* **Pacientes/Responsáveis:** Cadastro, login, busca de pediatras por especialidade e agendamento prático (particular ou convênio).
* **Profissionais (Médicos e Recepção):** Gerenciamento completo da agenda, prontuário eletrônico básico e geração de relatórios.

*Nota de Privacidade: O sistema foi estruturado seguindo os princípios da LGPD (Art. 14 da Lei nº 13.709/2018) para o tratamento seguro de dados de menores de idade.*

## 🛠️ Tecnologias e Ferramentas
* **Ambiente de Desenvolvimento:** .NET
* **Banco de Dados:** Relacional (SQL Server/PostgreSQL)
* **Metodologia Ágil:** Scrum (Organizado em 4 Sprints)

## 🗓️ Planejamento das Sprints
A evolução do projeto foi dividida em blocos de entrega de valor:
* **Sprint 1:** Infraestrutura Inicial, Módulo de Cadastro e Login *(Responsáveis: Joab, Sara, Talita)*.
* **Sprint 2:** Infraestrutura de Agendas e Módulo de Agendamento *(Responsáveis: Gustavo, Sara, Talita)*.
* **Sprint 3:** Gerenciamento de Agenda Médica e Mecanismo de Notificações *(Responsáveis: Sara, Lara, Gustavo)*.
* **Sprint 4:** Relatórios de Atendimento, Estabilização e Homologação *(Responsáveis: Sara, Lara, Equipe)*.

## Como Executar o Projeto

### Pré-requisitos
Antes de começar, você vai precisar ter instalado em sua máquina:
* SDK do [.NET](https://microsoft.com) (Versão utilizada no curso)
* Um SGBD compatível (Ex: SQL Server Management Studio ou PostgreSQL)
* Git

### Passo a Passo

```bash
# 1. Clonar o repositório do grupo
git clone https://github.com

# 2. Entrar na pasta raiz do projeto
cd pediagenda

# 3. Restaurar as dependências do .NET
dotnet restore

# 4. Atualizar o banco de dados (caso use Entity Framework Migrations)
dotnet ef database update

# 5. Rodar a aplicação
dotnet run
```

# Banco de Dados - PediAgenda

Scripts do banco de dados `pediagenda` (MySQL 8.0) hospedado em nuvem utilizando o **Aiven**.

## 📁 Estrutura dos Arquivos

| Arquivo | Descrição |
| :--- | :--- |
| `01_Schema.sql` | Criação do schema, 12 tabelas, chaves estrangeiras, índices e restrições do sistema. |
| `02_popula_tabelas.sql` | Popula as tabelas com dados iniciais e de teste (opcional). |

---

## ☁️ Como Configurar e Executar (Aiven + MySQL Workbench)

Como o banco está hospedado na nuvem pelo Aiven, usuários comuns geralmente não possuem permissão para executar comandos de criação de banco (`CREATE DATABASE`). Siga o passo a passo abaixo para realizar o deploy corretamente:

### Passo 1: Criar o banco de dados no Aiven
1. Acesse o painel do **Aiven** e selecione o seu serviço MySQL.
2. Vá até a aba **Databases**.
3. Clique em **Create database**, digite exatamente `pediagenda` e confirme.

### Passo 2: Conectar no MySQL Workbench
1. Abra o **MySQL Workbench** e crie uma nova conexão (`New Connection`).
2. Preencha os campos com o **Host**, **Port**, **Username** e **Password** fornecidos na aba *Overview / Quick Connect* do painel do Aiven.
3. *Dica:* Na aba **SSL**, certifique-se de configurar o arquivo de certificado CA se o Aiven exigir conexão segura.

### Passo 3: Executar o Schema (`01_Schema.sql`)
1. Abra o arquivo `01_Schema.sql` no MySQL Workbench.
2. **Atenção:** Como o banco já foi criado manualmente no painel do Aiven, comente ou remova as linhas de criação do banco (`DROP DATABASE` e `CREATE DATABASE`).
3. Garanta que o comando `USE pediagenda;` está ativo no início do script.
4. Clique no ícone do raio (**⚡**) para executar o script. 
5. Atualize o painel de Schemas no Workbench para confirmar que as **12 tabelas** foram criadas com sucesso.

### Passo 4: Popular o Banco (Opcional)
1. Abra o arquivo `02_popula_tabelas.sql`.
2. Certifique-se de que a conexão está apontando para o schema `pediagenda`.
3. Clique no raio (**⚡**) para rodar os dados de teste da equipe.


# PediAgenda â€” API

API que o app chama para autenticar, cadastrar e, daqui em diante, agendar.
.NET 9, banco MySQL `pediagenda`.

## Rodar

A senha do banco e a chave do token vÃªm de variÃ¡veis de ambiente, nunca do
cÃ³digo:

    setx PEDIAGENDA_CONEXAO "Server=localhost;Port=3306;Database=pediagenda;User ID=SEU_USUARIO;Password=SUA_SENHA;"

A chave do token Ã© aleatÃ³ria, cada um gera a sua (PowerShell):

    $b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); setx PEDIAGENDA_JWT_CHAVE ([Convert]::ToBase64String($b))

Feche e reabra o terminal, e:

    dotnet run --project api/PediAgenda.Api.csproj --urls http://localhost:5000

`GET /api/saude` responde `{"status":"ok"}` se subiu.


## Rotas expostas no código (20)

| Método | Rota | Perfil |
| --- | --- | --- |
| GET | `/api/saude` | Público |
| POST | `/api/auth/login` | Público |
| GET | `/api/auth/eu` | Logado |
| POST | `/api/cadastro/responsavel` | Público — conclui pré-cadastro |
| GET, POST | `/api/pacientes` | Responsável |
| GET | `/api/horarios` | Logado |
| POST, GET | `/api/consultas` | Responsável |
| PATCH | `/api/consultas/{id}/cancelar` | Responsável |
| POST | `/api/horarios` | Médico / recepção |
| GET | `/api/agenda` | Médico / recepção |
| PATCH | `/api/horarios/{id}/bloquear` | Médico / recepção |
| PATCH | `/api/horarios/{id}/desbloquear` | Médico / recepção |
| PATCH | `/api/consultas/{id}/confirmar` | Médico / recepção |
| PATCH | `/api/consultas/{id}/realizar` | Médico |
| POST, GET | `/api/relatorios` | Médico / recepção |
| GET | `/api/relatorios/{id}` | Médico / recepção |
| GET | `/api/relatorios/{id}/exportar` | Médico / recepção; CSV |


### Cadastro sem verificação de e-mail (API)

`POST /api/cadastro/responsavel` continua exigindo CPF e código de ativação
fornecido pela clínica, e-mail válido, senha e aceite dos termos. Ao concluir,
a conta é ativada imediatamente, o código da clínica é consumido e a API
retorna **HTTP 201**, sem enviar link por e-mail. O login já fica disponível.
O e-mail continua sendo o identificador de login, **não** foi removido.
Não existe mais `GET /api/cadastro/confirmar-email`.

Para bases já existentes, consulte `banco/03_remover_verificacao_email.sql`.
**Nunca execute `01_Schema.sql` no Aiven com dados reais:** ele começa com
`DROP DATABASE`. O script `02_popula_tabelas.sql` é legado, contém outras
incompatibilidades com o modelo físico e NÃO deve ser executado em produção.
