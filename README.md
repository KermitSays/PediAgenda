# 🚀 PediAgenda: Sistema de Agendamento e Atendimento Pediátrico

> Sistema digital voltado para o agendamento e gerenciamento de consultas pediátricas, alinhado ao Objetivo de Desenvolvimento Sustentável ODS-3 (Saúde e Bem-estar) da ONU.

## 👥 Equipe e Papéis
* **Talita Barbara Cardanha Lopes** - Product Owner
* **Sara Helena de Oliveira Nunes Ribeiro** - Scrum Master
* **Gustavo de Oliveira** - Dev Team
* **Joab Antonio de Souza** - Dev Team
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

## 🚀 Como Executar o Projeto

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
