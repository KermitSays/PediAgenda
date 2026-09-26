# PediAgenda — API

API que o app chama para autenticar, cadastrar e, daqui em diante, agendar.
.NET 9, banco MySQL `pediagenda`.

## Rodar

A senha do banco e a chave do token vêm de variáveis de ambiente, nunca do
código:

    setx PEDIAGENDA_CONEXAO "Server=localhost;Port=3306;Database=pediagenda;User ID=SEU_USUARIO;Password=SUA_SENHA;"

A chave do token é aleatória, cada um gera a sua (PowerShell):

    $b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); setx PEDIAGENDA_JWT_CHAVE ([Convert]::ToBase64String($b))

Feche e reabra o terminal, e:

    dotnet run --project api/PediAgenda.Api.csproj --urls http://localhost:5000

`GET /api/saude` responde `{"status":"ok"}` se subiu.

## POST /api/auth/login

Entra:

    { "email": "maria@exemplo.com", "senha": "senhaSegura123" }

Sai (200):

    {
      "id": 7,
      "nome": "Maria Silva",
      "email": "maria@exemplo.com",
      "perfil": "RESPONSAVEL",
      "token": "eyJhbGciOiJIUzI1NiIs...",
      "expiraEm": "2026-09-24T21:00:00Z"
    }

`perfil` é RESPONSAVEL, MEDICO ou RECEPCIONISTA — é por ele que o app escolhe a tela.

Erro (400, 401, 403 ou 423):

    { "codigo": "CREDENCIAIS_INVALIDAS", "mensagem": "E-mail ou senha inválidos." }

A tela pode exibir o `mensagem` direto.

## O token

O app guarda o `token` e manda em todas as chamadas que exigem login, no
cabeçalho `Authorization`:

    http.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", sessao.Token);

Ele vale 8 horas. Depois disso, ou se estiver ausente ou alterado, a API
responde 401 e o app deve voltar para a tela de login:

    { "codigo": "NAO_AUTENTICADO", "mensagem": "Sessão expirada ou inválida. Faça login novamente." }

Rota certa, mas perfil errado (por exemplo, responsável tentando abrir a agenda
do médico), responde 403 com `SEM_PERMISSAO`.

Para guardar o token no celular, use `SecureStorage` do MAUI, não
`Preferences`: ele fica criptografado pelo sistema.

## GET /api/auth/eu

Exige token. Devolve o dono dele:

    { "id": 7, "nome": "Maria Silva", "email": "maria@exemplo.com", "perfil": "RESPONSAVEL" }

Serve para o app conferir, ao abrir, se o token guardado ainda vale. Se vier
401, pede login.

## Pacientes

Os filhos do responsável logado. Exigem token de perfil RESPONSAVEL; médico e
recepção recebem 403. O responsável vem do token — não vai id na URL, então
ninguém consegue ver os filhos de outra família trocando um número.

### GET /api/pacientes

Sai (200), em ordem de nome, lista vazia se ainda não houver nenhum:

    [
      { "id": 2, "nome": "Ana Silva", "dataNascimento": "2022-07-01", "idade": 4 },
      { "id": 1, "nome": "Lucas Silva", "dataNascimento": "2019-03-10", "idade": 7 }
    ]

### POST /api/pacientes

Entra:

    { "nome": "Lucas Silva", "dataNascimento": "2019-03-10" }

A data vai no formato `AAAA-MM-DD` (no MAUI: `data.ToString("yyyy-MM-dd")`).

Sai (201) o paciente criado, no mesmo formato da lista.

Erros:

- 400 `DADOS_INVALIDOS`, com `campos`: nome vazio, data em outro formato, data
  no futuro, ou paciente com mais de 17 anos (a clínica é pediátrica)
- 409 `PACIENTE_JA_CADASTRADO`: mesmo nome e mesma data de nascimento para o
  mesmo responsável — protege contra toque duplo no botão de salvar

## GET /api/horarios

Horários livres de um dia, agrupados por médico. Exige token (qualquer perfil).

    GET /api/horarios?data=2026-09-25
    GET /api/horarios?data=2026-09-25&especialidade=Pediatria
    GET /api/horarios?data=2026-09-25&medico=3

`data` é obrigatória (`AAAA-MM-DD`); `especialidade` e `medico` são filtros
opcionais.

Sai (200) — só aparecem médicos com pelo menos um horário livre:

    {
      "data": "2026-09-25",
      "medicos": [
        {
          "id": 3,
          "nome": "Dr. Pedro Alves",
          "especialidade": "Pediatria",
          "horarios": [
            { "id": 41, "inicio": "08:00", "fim": "08:30" },
            { "id": 42, "inicio": "08:30", "fim": "09:00" }
          ]
        }
      ]
    }

Dia sem atendimento (fim de semana, por exemplo) volta `"medicos": []`. O `id`
do horário é o que a tela vai mandar para agendar.

Livre quer dizer: não bloqueado na agenda e sem consulta ativa. Consulta
cancelada devolve o horário para a lista. Se a data for hoje, só entram os
horários que ainda não começaram. A lista é calculada na hora, a partir do
banco (RNF03).

Data vazia, em outro formato ou no passado: 400 `DADOS_INVALIDOS`.

Para ter horários para testar, rode `banco/dados_de_teste.sql`.

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

Sai (201) igual ao login, com token — dá para ir direto para a tela inicial,
sem pedir login de novo.

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
