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

- Banco novo e vazio: execute `banco/01_Schema.sql` (11 tabelas); a carga `02_popula_tabelas.sql` é opcional e EXCLUSIVA de bancos locais descartáveis, nunca no Aiven em uso.

No PowerShell, na raiz do repositório, configure a sessão atual:

```powershell
$env:PEDIAGENDA_CONEXAO = "Server=localhost;Port=3306;Database=pediagenda;User ID=SEU_USUARIO;Password=SUA_SENHA;"
$b = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
$env:PEDIAGENDA_JWT_CHAVE = [Convert]::ToBase64String($b)
dotnet run --project api/PediAgenda.Api.csproj --urls http://localhost:5000
```

`GET http://localhost:5000/api/saude` deve retornar `{"status":"ok"}`. Essa rota verifica a aplicação; os testes de integração verificam também o banco.

Evite salvar segredos em texto simples. Prefira variáveis de ambiente de sessão ou um gerenciador seguro de segredos. No Render, configure a conexão MySQL e a chave JWT como variáveis de ambiente. Para o cadastro com verificação de e-mail, configure também `PEDIAGENDA_EMAIL_API_KEY`, `PEDIAGENDA_EMAIL_REMETENTE`, `PEDIAGENDA_EMAIL_NOME` (opcional) e `RENDER_EXTERNAL_URL` (URL HTTPS pública do serviço). Não versione chaves ou senhas. **A confirmação de e-mail foi testada com API local; a configuração e o fluxo no Render devem ser validados separadamente.**

## Cadastro e sessão

Os dados do Responsável e de seus dependentes já são cadastrados pela clínica. Para habilitar a conta no aplicativo, o Responsável informa CPF/e-mail do pré-cadastro, define senha e aceita os termos em `POST /api/cadastro/responsavel`. A API envia pela Brevo um **link de verificação** e retorna **202** ao iniciar o processo. Após abrir o link de `GET /api/cadastro/confirmar-email?token=...`, o e-mail é confirmado, a conta é ativada e o login fica disponível. Não existe tela de digitar código de verificação. A rota técnica de pré-cadastro `POST /api/responsaveis` continua no backend legado, mas o cadastro pela Recepção no APP está fora do escopo final.

O JWT de **login** continua obrigatório nas rotas protegidas. Uma conta que já definiu senha não pode sobrescrevê-la pelo cadastro, inclusive quando está inativa. Reativação de contas pela clínica não é implementada por esta rota.

Horários de consulta usam `America/Sao_Paulo`, inclusive no Render; vencimento de JWT, bloqueios de login e novos prontuários usam UTC.


As telas têm seus próprios fluxos de integração. Executar a API local não altera automaticamente o endereço usado pelo aplicativo.

## Escopo final e migrações

O aplicativo tem os perfis Responsável, Médico e Recepção. O Responsável agenda, consulta suas consultas/histórico em **Minhas Consultas** e pode cancelar conforme as regras da aplicação. A Recepção não cadastra pacientes pelo app. Não há módulo de notificações ou reagendamento no aplicativo. O Médico consulta a agenda, registra atendimento/prontuário e exporta relatórios em Excel.

O status `CONFIRMADA` permanece previsto no banco, assim como `AGENDADA`, `CANCELADA` e `REALIZADA`. O campo `consulta.tipo_atendimento` foi retirado do código e do schema para novos bancos. Para o **Aiven já existente**, use os scripts de `banco/` somente depois de verificar a situação atual e o deploy da API. `ALTER TABLE` é DDL e não pode ser desfeito com `ROLLBACK`.

A API local e a publicação em Render são ambientes distintos: verificar a aplicação local **não** comprova que a versão online foi atualizada.
