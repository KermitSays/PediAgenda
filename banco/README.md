# Banco de dados — PediAgenda

O projeto utiliza **MySQL 8**. Esta pasta contém a estrutura do banco e os dados de demonstração.

## Arquivos

| Arquivo | O que faz |
| --- | --- |
| `01_Schema.sql` | Cria as 12 tabelas, seus campos e os relacionamentos. |
| `02_popula_tabelas.sql` | Adiciona usuários, pacientes, médicos, horários e consultas para testar o sistema. |

## Como executar

1. Abra a conexão do banco no MySQL Workbench.
2. Em uma base vazia, execute primeiro `01_Schema.sql`.
3. Depois, execute `02_popula_tabelas.sql` para adicionar os dados de teste.

Se o banco `pediagenda` já estiver criado no Aiven, execute o schema a partir da linha `USE pediagenda;`.

Os horários de demonstração são criados para os próximos três dias úteis.

## Acessos para teste

A senha de todas as contas abaixo é **`senhaDemo123`**.

| Perfil | E-mail |
| --- | --- |
| Responsável | `maria@teste.local` |
| Outro responsável | `ana.oliveira@example.com` |
| Médico | `medico.teste@example.com` |
| Outro médico | `helena.duarte@example.com` |
| Recepção | `recepcao.teste@example.com` |

Também existe um responsável pré-cadastrado para testar a criação do acesso:

- **CPF:** `52998224725`
- **E-mail:** `precadastro@example.com`

Para concluir esse cadastro, informe esses dados, escolha uma senha e aceite os termos de uso.