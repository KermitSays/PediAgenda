# PediAgenda — passagem do back-end

Documento de passagem da API e do banco de dados, feito por Joab Antonio de
Souza ao sair do grupo. Serve para quem for continuar o desenvolvimento e para
quem for apresentar essa parte na defesa.

---

## 1. Resumo

- **O que existe:** o banco `pediagenda` (12 tabelas) e uma API em ASP.NET
  Core (.NET 9) com 20 rotas. A API cobre todos os requisitos da Declaração
  de Visão, **menos o RF06** (e-mail de confirmação) e o **RNF04**
  (atualização automática da agenda) — ver a seção 6.
- **Onde está:** no repositório do grupo, nas pastas `banco/` (scripts),
  `nucleo/` (regras), `api/` (as rotas) e `login/` (console de demonstração).
- **Documentação de cada rota:** `api/README.md` — o que entra, o que sai,
  cada erro possível.
- **Estado no GitHub:** ver a seção 5.

---

## 2. Como rodar do zero

Faça uma vez, na ordem:

1. **MySQL 8.0 ligado**, na porta 3306.
2. **Criar o banco:** no Workbench, abrir e rodar `banco/03_modelo_completo.sql`.
   Atenção: ele apaga e recria o banco `pediagenda`.
3. **Dados de teste:** rodar `banco/dados_de_teste.sql`. Cria dois médicos,
   uma recepcionista e horários nos próximos dias úteis. Pode rodar de novo
   sempre que quiser horários a partir de amanhã.
4. **Usuário do banco para a API** (no Workbench, trocando a senha):

       CREATE USER 'pediagenda_app'@'localhost' IDENTIFIED BY 'senha_escolhida';
       GRANT SELECT, INSERT, UPDATE, DELETE ON pediagenda.* TO 'pediagenda_app'@'localhost';

5. **As duas variáveis de ambiente** (PowerShell), depois fechar e reabrir o
   terminal:

       setx PEDIAGENDA_CONEXAO "Server=localhost;Port=3306;Database=pediagenda;User ID=pediagenda_app;Password=senha_escolhida;"
       $b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); setx PEDIAGENDA_JWT_CHAVE ([Convert]::ToBase64String($b))

6. **Subir a API:**

       dotnet run --project api/PediAgenda.Api.csproj --urls http://localhost:5000

7. **Conferir:** `http://localhost:5000/api/saude` responde `{"status":"ok"}`.

Contas para testar (senhas só de teste):

| Perfil | E-mail | Senha |
|---|---|---|
| Médico | `pedro.alves@teste.pediagenda.local` | `senhaMedico123` |
| Médica | `carla.mendes@teste.pediagenda.local` | `senhaMedico123` |
| Recepção | `julia.rocha@teste.pediagenda.local` | `senhaRecepcao123` |
| Responsável | criar pela rota de cadastro | — |

Se a API não subir, ela mesma diz qual variável está faltando.

---

## 3. Mapa das rotas

Todas começam com `http://localhost:5000`. "Logado" = precisa do token do
login no cabeçalho `Authorization: Bearer ...`.

| Rota | Quem pode | Requisito |
|---|---|---|
| `GET /api/saude` | qualquer um | — |
| `POST /api/auth/login` | qualquer um | RF03, RF04, RNF01, RNF02 |
| `GET /api/auth/eu` | logado | — |
| `POST /api/cadastro/responsavel` | qualquer um | RF01, RF02, RNF01 |
| `GET /api/pacientes` | responsável | — |
| `POST /api/pacientes` | responsável | — |
| `GET /api/horarios` | logado | RF05, RNF03 |
| `POST /api/consultas` | responsável | RF05 |
| `GET /api/consultas` | responsável | — |
| `PATCH /api/consultas/{id}/cancelar` | responsável | RF07 |
| `POST /api/horarios` | médico, recepção | — |
| `GET /api/agenda` | médico, recepção | RF08 |
| `PATCH /api/horarios/{id}/bloquear` | médico, recepção | RF09 |
| `PATCH /api/horarios/{id}/desbloquear` | médico, recepção | RF09 |
| `PATCH /api/consultas/{id}/confirmar` | médico, recepção | — |
| `PATCH /api/consultas/{id}/realizar` | só médico | — |
| `POST /api/relatorios` | médico, recepção | RF10, RNF05 |
| `GET /api/relatorios` | médico, recepção | RF10 |
| `GET /api/relatorios/{id}` | médico, recepção | RF10 |
| `GET /api/relatorios/{id}/exportar` | médico, recepção | RF11 |

**Regra geral de acesso:** o responsável só vê os próprios filhos e consultas;
o médico só mexe na própria agenda; a recepção mexe em todas. Quem é quem vem
do token — nenhuma rota recebe "id do usuário" pela chamada.

O fluxo principal do app, em ordem: cadastro → login → cadastrar filho →
`GET /api/horarios` → `POST /api/consultas` → `GET /api/consultas`.

---

## 4. Receita para criar uma rota nova

Todas as rotas seguem o mesmo padrão. Exemplo real: a de pacientes.

1. **Modelo** (`nucleo/Modelos/`): a classe que representa o dado.
   Ex.: `Paciente.cs`.
2. **Repositório** (`nucleo/Repositorios/`): uma interface com o que se
   precisa do banco, e a implementação MySQL com o SQL.
   Ex.: `IPacienteRepositorio.cs` e `PacienteRepositorioMySql.cs`.
   **Sempre SQL com parâmetros** (`@nome`), nunca montando texto — é o que
   impede SQL injection.
3. **Serviço** (`nucleo/Servicos/`): as regras. Valida a entrada, decide, e
   devolve um resultado dizendo se deu certo e, se não, por quê.
   Ex.: `PacienteService.cs`.
4. **Rota** (`api/Program.cs`): registrar o repositório e o serviço em
   `builder.Services`, e criar o `Map...`. A rota só traduz o resultado do
   serviço para HTTP (200, 400, 404, 409...).
   Ex.: o bloco `var pacientes = app.MapGroup("/api/pacientes")`.
5. **Documentar** no `api/README.md`.
6. **Testar contra o banco** antes de subir: o caso certo, cada erro, e o
   acesso de um perfil que não deveria entrar.

Para saber quem está chamando, dentro da rota: `IdUsuario(usuario)` dá o id
do usuário logado, e `AtorDe(usuario)` dá id e perfil.

---

## 5. Estado do GitHub

Situação em 24/09/2026. O trabalho foi entregue em PRs encadeados, cada um em
cima do anterior. **É preciso aceitar na ordem**:

| Ordem | Conteúdo | Branch | Situação |
|---|---|---|---|
| — | login, cadastro, scripts do banco | `feature/api-login` | PR #1, aceito |
| 1 | token (JWT) | `feature/token-jwt` | PR #2, aberto |
| 2 | pacientes | `feature/pacientes` | PR #3, aberto |
| 3 | horários livres e dados de teste | `feature/horarios` | PR #4, aberto |
| 4 | agendamento e cancelamento | `feature/consultas` | no GitHub, sem PR ainda |
| 5 | rotas da clínica | `feature/clinica` | no GitHub, sem PR ainda |
| 6 | relatórios | `feature/relatorios` | no GitHub, sem PR ainda |

As três últimas já estão no GitHub; o PR delas fica para depois do #2, para
não empilhar revisão.

**Quando um PR é aceito,** o seguinte passa a apontar para o `master`: no PR,
botão **Edit** ao lado do título → trocar a base para `master`. O conteúdo não
muda.

**Para abrir o PR de uma das três últimas** (qualquer um do grupo pode): no
GitHub, aba **Pull requests** → **New pull request** → em *compare*, escolher a
branch; em *base*, a branch da linha de cima na tabela — ou `master`, se aquela
já tiver sido aceita.

---

## 6. O que ficou pendente

| Pendência | De quem | Situação |
|---|---|---|
| **RF06** — e-mail de confirmação | Gustavo (Mecanismo de Notificação) | A tabela `notificacao` já tem `data_envio`, que aceita nulo. Sugestão: a API grava uma notificação com `data_envio` nulo a cada agendamento, e o serviço de e-mail envia as pendentes e preenche a data. |
| **RNF04** — atualização automática da agenda | quem fizer a tela | A API calcula a agenda na hora. O jeito mais simples é a tela chamar `GET /api/agenda` de novo a cada 30 ou 60 segundos. |
| **Aceite dos termos** — gravar data e hora | Talita (modelagem) | Pronto na máquina do Joab (coluna `termos_aceitos_em` em `usuario`, script `05` e a gravação no cadastro), ainda não enviado. Se a Talita aprovar, é só pedir que ele envia. |
| **Campo de telefone** na tela de cadastro | telas | A API exige o telefone (a coluna é obrigatória no banco). |
| **Exportar em PDF** | — | Hoje só CSV, que abre no Excel. |
| **Recepção agendar e cancelar** por telefone | — | Hoje só o responsável agenda e cancela. |
| **Prazo mínimo para cancelar** | — | Hoje dá até a consulta começar. É uma linha, se a clínica quiser outra regra. |
| **Validar dígitos do CPF** | — | Hoje confere só se tem 11 números. |
| **README principal** fala em SQL Server/PostgreSQL | — | O projeto usa MySQL. |

---

## 7. Atenção: o celular não enxerga `localhost`

Vai ser o primeiro erro quando as telas chamarem a API.

- **Emulador Android:** usar `http://10.0.2.2:5000` no lugar de
  `http://localhost:5000`. Para o emulador, `10.0.2.2` é o computador.
- **Celular de verdade:** usar o IP do computador na rede (`ipconfig`, linha
  IPv4), subir a API com `--urls http://0.0.0.0:5000` e permitir no firewall
  do Windows quando ele perguntar.
- **HTTP sem "s":** o Android bloqueia HTTP comum por padrão. Para testar,
  liberar tráfego em texto claro no app (`usesCleartextTraffic`). Em produção,
  o certo seria HTTPS.

---

## 8. Decisões e os porquês

Para a defesa, e para ninguém "consertar" o que é de propósito:

- **Senha:** o banco guarda só o hash PBKDF2-SHA256 com 100.000 iterações e
  salt por usuário. A senha nunca é gravada nem devolvida.
- **Mensagem de login genérica:** "E-mail ou senha inválidos" vale para os dois
  casos, para ninguém descobrir quem é paciente da clínica testando e-mails
  (LGPD, dados de saúde de crianças).
- **Bloqueio:** 5 tentativas erradas travam a conta por 15 minutos (RNF02), e
  toda tentativa fica registrada em `tentativa_login`.
- **Token (JWT):** assinado com uma chave que só a API tem; alterar o perfil
  dentro dele invalida a assinatura. Vale 8 horas.
- **Nenhuma rota recebe o id do usuário:** ele vem do token. Por isso uma
  família não consegue ver os filhos de outra trocando um número na chamada.
- **Dois pedidos no mesmo horário:** a reserva trava o horário no banco
  (`SELECT ... FOR UPDATE`). Testado com dez pedidos simultâneos: um consegue,
  nove recebem 409.
- **SQL escrito à mão, sem Entity Framework:** o banco foi modelado antes, com
  cada restrição pensada; com EF, as tabelas passariam a nascer do código.
- **Consulta sem data, hora e médico:** vêm do horário reservado (3ª Forma
  Normal, justificado na seção 3.2 do relatório).
- **CSV:** nomes que começam com `=` ganham um apóstrofo, para o Excel não
  executar como fórmula um texto digitado pela família.

---

## 9. Contato

Fico à disposição para **tirar dúvidas** sobre o banco e a API pelo WhatsApp.
Não sigo desenvolvendo: a partir daqui, as mudanças ficam com o grupo — este
documento e o `api/README.md` foram feitos para isso.

Joab Antonio de Souza
