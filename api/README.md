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

A tela pode exibir o `mensagem` direto.

## POST /api/cadastro/responsavel

Cria a conta do responsável. Médico e recepcionista são criados pela clínica.

Entra:

    {
      "nome": "Maria Silva",
      "cpf": "12345678901",
      "email": "maria@exemplo.com",
      "telefone": "11999990000",
      "senha": "senhaSegura123",
      "aceiteTermos": true
    }

Todos obrigatórios. O CPF pode vir com ponto e traço. A senha segue a mesma
regra do login, mínimo de 8 caracteres.

Sai (201) igual ao login — dá para ir direto para a tela inicial, sem pedir
login de novo:

    { "id": 7, "nome": "Maria Silva", "email": "maria@exemplo.com", "perfil": "RESPONSAVEL" }

Campo inválido (400) vem com a lista, para a tela marcar cada um:

    {
      "codigo": "DADOS_INVALIDOS",
      "mensagem": "Verifique os campos destacados.",
      "campos": { "cpf": "O CPF deve ter 11 dígitos." }
    }

E-mail ou CPF repetido (409):

    { "codigo": "EMAIL_JA_CADASTRADO", "mensagem": "Já existe uma conta com esse e-mail." }

### Pendente

O `aceiteTermos` é exigido, mas **ainda não é gravado**: não existe coluna para
isso no banco. Para a LGPD, o que vale é poder provar depois que a pessoa
aceitou e quando. Falta decidir com o grupo onde registrar — a proposta é uma
coluna `termos_aceitos_em` em `usuario`, o que mexe no modelo e precisa do aval
de quem cuida da modelagem.
