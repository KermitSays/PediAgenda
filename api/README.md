# PediAgenda — API de login

API que o app chama para autenticar. .NET 9, banco MySQL `pediagenda`.

## Rodar

A senha do banco vem de variável de ambiente, nunca do código:

    setx PEDIAGENDA_CONEXAO "Server=localhost;Port=3306;Database=pediagenda;User ID=SEU_USUARIO;Password=SUA_SENHA;"

    dotnet run --project api/PediAgenda.Api.csproj --urls http://localhost:5000

`GET /api/saude` responde `{"status":"ok"}` se subiu.

## POST /api/auth/login

Entra:

    { "email": "maria@exemplo.com", "senha": "senhaSegura123" }

Sai (200):

    { "id": 7, "nome": "Maria Silva", "email": "maria@exemplo.com", "perfil": "RESPONSAVEL" }

`perfil` é RESPONSAVEL, MEDICO ou RECEPCIONISTA — é por ele que o app escolhe a tela.

Erro (400, 401, 403 ou 423):

    { "codigo": "CREDENCIAIS_INVALIDAS", "mensagem": "E-mail ou senha inválidos." }

A tela pode exibir o `mensagem` direto. O cadastro vem depois.
