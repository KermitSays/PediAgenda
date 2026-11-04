# PediAgenda

**Sistema de agendamento e acompanhamento de consultas pediátricas** desenvolvido como Trabalho de Conclusão de Curso (TCC) de Desenvolvimento de Sistemas.

O PediAgenda é um aplicativo com três perfis — **Responsável**, **Médico** e **Recepção** — integrado a uma API ASP.NET Core e a um banco de dados MySQL. O projeto organiza a agenda de uma clínica pediátrica fictícia, com regras de acesso específicas para cada perfil.

**Equipe:** Lara Vitoria Araujo Moreira, Sara Helena de Oliveira Nunes Ribeiro e Talita Barbara Cardanha Lopes.

## Funcionalidades do aplicativo

| Perfil | Funcionalidades |
| --- | --- |
| **Responsável** | Habilitação da conta com CPF e e-mail previamente registrados na clínica; criação de senha e confirmação por link enviado pela Brevo; login; consulta de dependentes; agendamento, cancelamento, próximas consultas e histórico em **Minhas Consultas**. |
| **Médico** | Agenda médica e consultas do dia; consulta de pacientes e prontuários; bloqueio e liberação de horários; registro de atendimento com observações; relatórios por período e exportação para Excel (`.xlsx`). |
| **Recepção** | Consulta de pacientes e médicos; visualização de agendas e consultas do dia; agendamento e gestão de consultas; confirmação e cancelamento; bloqueio e liberação de horários. |

Os cadastros de responsáveis e pacientes são considerados previamente realizados pela clínica. A interface final da Recepção **não inclui** o cadastro desses registros. Não foram incluídos módulos de notificações, reagendamento ou recuperação de senha.

O histórico de consultas do Responsável fica em **Minhas Consultas**. Os calendários de agendamento evitam sábados e domingos, sem tratamento específico de feriados.

## Arquitetura e tecnologias

```text
Aplicativo Android (.NET MAUI / C# / XAML)
                  |
             HTTPS + JWT
                  |
        API ASP.NET Core (.NET 9)
             hospedada no Render
              /         \
      MySQL no Aiven    Brevo (verificação de e-mail)
```

- **Aplicativo:** .NET MAUI, .NET 10, C# e XAML.
- **Back-end:** ASP.NET Core Web API (.NET 9), com regras de negócio na camada `nucleo/`.
- **Banco de dados:** MySQL 8, com instância hospedada no Aiven.
- **Hospedagem da API:** Render, com variáveis de ambiente para configuração.
- **Confirmação de e-mail:** Brevo, por link de ativação.
- **Autenticação:** JWT; senhas protegidas por hash PBKDF2-SHA256 com salt individual.
- **Relatórios Excel no aplicativo:** ClosedXML.

**Endpoint público de verificação da API:** [https://pediagenda-api.onrender.com/api/saude](https://pediagenda-api.onrender.com/api/saude). Essa rota verifica a resposta da aplicação; não substitui testes de banco, autenticação ou interface. A instância hospedada pode demorar mais na primeira requisição após um período de inatividade.

## Estrutura do repositório

| Caminho | Finalidade |
| --- | --- |
| [`PediAgenda/`](PediAgenda/) | Aplicativo e telas .NET MAUI. |
| [`api/`](api/) | API HTTP e serviços de integração. Consulte o [guia da API](api/README.md). |
| [`nucleo/`](nucleo/) | Regras de negócio, segurança e repositórios MySQL. |
| [`banco/`](banco/) | Modelo físico para banco novo, dados fictícios e scripts de migração. Consulte o [guia do banco](banco/README.md). |
| [`testes/`](testes/) | Testes automatizados HTTP, banco e recursos do aplicativo. |
| [`docs/`](docs/) | Validação final e documentação histórica. Consulte o [índice](docs/README.md). |
| [`login/`](login/) | Demonstração técnica histórica de autenticação em console; não é a tela de login do aplicativo. |

O [diagrama lógico do banco de dados](banco/Diagramas_Banco_de_Dados_Lógico.png) está disponível na pasta `banco/`.

## Como executar o aplicativo

**Requisitos:** Visual Studio com suporte a .NET MAUI, SDK .NET 10, workload Android e emulador ou dispositivo Android compatível.

1. Clone o repositório e abra `PediAgenda.slnx` no Visual Studio.
2. Selecione o projeto `PediAgenda` como projeto de inicialização.
3. Selecione um emulador Android e compile/execute o aplicativo.
4. O aplicativo está configurado para utilizar a API HTTPS do Render. **Não é necessário iniciar uma API local apenas para testar essa configuração.**

O aplicativo precisa de conectividade com a API e de contas autorizadas. **Credenciais reais, tokens, senhas e dados pessoais não são fornecidos neste repositório.** Em caso de indisponibilidade do serviço hospedado, as funcionalidades dependentes de rede podem não operar.

### Executar a API em ambiente local (opcional)

Para desenvolver o back-end localmente, são necessários o **SDK .NET 9** e um **MySQL 8 de testes**. O arquivo `banco/01_Schema.sql` cria a estrutura em um banco novo e vazio. A carga `banco/02_popula_tabelas.sql` é exclusivamente de demonstração e **não deve ser executada sobre o Aiven em utilização**.

No PowerShell, dentro da raiz do repositório, configure as variáveis **apenas na sessão de desenvolvimento** (substitua os exemplos por valores do seu ambiente):

```powershell
$env:PEDIAGENDA_CONEXAO = "Server=localhost;Port=3306;Database=pediagenda;User ID=SEU_USUARIO;Password=SUA_SENHA;"
$b = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
$env:PEDIAGENDA_JWT_CHAVE = [Convert]::ToBase64String($b)
dotnet run --project api/PediAgenda.Api.csproj --urls http://localhost:5000
```

Nesse modo, `http://localhost:5000/api/saude` é o endpoint local. **Iniciar a API local não redireciona automaticamente o aplicativo:** os endereços usados no cliente foram configurados para o Render. Para testar alterações do back-end localmente, é necessário configurar o cliente para o ambiente desejado.

Para a funcionalidade de confirmação de e-mail, a API precisa também das variáveis `PEDIAGENDA_EMAIL_API_KEY`, `PEDIAGENDA_EMAIL_REMETENTE`, `PEDIAGENDA_EMAIL_NOME` (opcional) e de uma URL pública de retorno para o link de ativação. Consulte o [guia da API](api/README.md). **Nunca inclua chaves, strings de conexão ou senhas no Git.**

## Banco de dados e regras de negócio

O modelo para um banco novo contém **11 tabelas** e preserva os estados de consulta `AGENDADA`, `CONFIRMADA`, `CANCELADA` e `REALIZADA`.

- O cancelamento atualiza o status para `CANCELADA`, preservando o histórico (exclusão lógica).
- A Recepção não tem acesso às observações clínicas dos prontuários.
- O registro de prontuário é restrito ao médico autorizado, respeita o horário de início da consulta e evita registros duplicados.
- A API usa o horário da clínica (`America/Sao_Paulo`) para regras de agendamento; tokens e registros técnicos utilizam UTC conforme a operação.
- O escopo final não inclui a tabela `notificacao` nem a coluna `consulta.tipo_atendimento` em um **banco criado do zero**.

**Atenção:** o SQL versionado para banco novo não prova, por si só, que uma estrutura de banco já hospedada tenha recebido todas as migrações. Os scripts de `banco/` **não devem ser aplicados indiscriminadamente no Aiven**. Alterações estruturais exigem backup e análise de compatibilidade com todos os serviços ativos.

## Validação e documentação

- [Registro dos testes finais do aplicativo e serviços](docs/VALIDACAO_FINAL.md), incluindo a limitação encontrada no teste de novo prontuário.
- [Validação histórica da API](api/VALIDACAO.md), que documenta uma etapa anterior do projeto e **não** representa o estado final.
- [Documentação de conclusão das integrações do Médico](docs/MEDICO_CONCLUSAO.md), referente à fase em que foi produzida.
- [Índice da documentação e arquivos históricos](docs/README.md).

Este é um projeto acadêmico desenvolvido para uma **clínica fictícia**. As contas e consultas de demonstração devem ser tratadas como dados de teste; nenhum documento disponibilizado aqui deve incluir dados reais de pacientes.
