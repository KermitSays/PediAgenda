# PediAgenda

Sistema de agendamento e atendimento pediátrico desenvolvido por Talita Barbara Cardanha Lopes, Sara Helena de Oliveira Nunes Ribeiro e Lara Vitoria Araujo Moreira.

## Projetos

| Pasta | Função |
| --- | --- |
| `PediAgenda/` | Aplicativo .NET MAUI; usa .NET 10 e os workloads da plataforma |
| `api/` | API HTTP em .NET 9 com autenticação JWT |
| `nucleo/` | Regras de negócio e repositórios MySQL, compartilhados pela API |
| `banco/` | Schema MySQL 8, carga fictícia e migração de bancos existentes |
| `login/` | Demonstração em console do login |
| `testes/` | Testes HTTP com API e MySQL locais descartáveis |

## Executar a API

É necessário o SDK .NET 9 e um MySQL 8 acessível. O backend usa MySqlConnector diretamente; não usa Entity Framework nem `dotnet ef`.

- Banco novo e vazio: execute `banco/01_Schema.sql`; a carga `02_popula_tabelas.sql` é opcional e exclusiva de testes.
- Banco existente: execute `banco/03_remover_verificacao_email.sql` antes de iniciar esta versão da API. A migração preserva usuários, credenciais, contas inativas e atendimentos.

No PowerShell, na raiz do repositório, configure a sessão atual:

```powershell
$env:PEDIAGENDA_CONEXAO = "Server=localhost;Port=3306;Database=pediagenda;User ID=SEU_USUARIO;Password=SUA_SENHA;"
$b = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
$env:PEDIAGENDA_JWT_CHAVE = [Convert]::ToBase64String($b)
dotnet run --project api/PediAgenda.Api.csproj --urls http://localhost:5000
```

`GET http://localhost:5000/api/saude` deve retornar `{"status":"ok"}`. Essa rota verifica a aplicação; os testes de integração verificam também o banco.

Para guardar as variáveis entre sessões, use `setx` e reabra o terminal e o Visual Studio. No Render, configure as mesmas variáveis no serviço; use o host, porta, credenciais e opções SSL fornecidos pelo Aiven, com `Database=pediagenda`.

## Cadastro e sessão

A recepção pré-cadastra nome, CPF, e-mail e telefone em `POST /api/responsaveis`. O responsável conclui seu acesso em `POST /api/cadastro/responsavel`, informando o CPF e e-mail cadastrados pela clínica, sua senha e o aceite dos termos. O cadastro não envia e-mail nem exige código de ativação. A API retorna 201; o próximo passo é fazer login.

O JWT de **login** continua obrigatório nas rotas protegidas. Uma conta que já definiu senha não pode sobrescrevê-la pelo cadastro, inclusive quando está inativa. Recuperação de senha e reativação de contas pela clínica não são implementadas por esta rota.

Horários de consulta usam `America/Sao_Paulo`, inclusive no Render; vencimento de JWT, bloqueios de login e novos prontuários usam UTC.

## Referências

- [Rotas e exemplos da API](api/README.md)
- [Scripts do banco e migração](banco/README.md)
- [Como executar os testes](testes/README.md)
- [Resultado da revisão e validação](api/VALIDACAO.md)

As telas têm seus próprios fluxos de integração. Executar a API local não altera automaticamente o endereço usado pelo aplicativo.
