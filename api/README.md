# PediAgenda — API HTTP

.NET 9 + MySQL 8. Configuração e comando de execução: [README principal](../README.md).

## Sessão e erros

Login público:

```http
POST /api/auth/login
Content-Type: application/json

{"email":"medico.teste@example.com","senha":"senhaDemo123"}
```

Resposta 200: `id`, `nome`, `email`, `perfil`, `token` e `expiraEm`. Os perfis são `RESPONSAVEL`, `MEDICO` e `RECEPCIONISTA`. Nas demais chamadas envie `Authorization: Bearer SEU_TOKEN`. O JWT dura 8 horas e precisa corresponder a uma conta ativa e ao perfil atual no banco.

Falhas têm `codigo` e `mensagem`; erros de validação incluem `campos`. Exemplos: 400 dados inválidos, 401 sessão ausente/vencida, 403 perfil sem acesso, 404 registro não encontrado, 409 conflito, 423 login bloqueado e 503 falha de acesso ao banco. JSON malformado e métodos incorretos também têm resposta JSON. Cinco senhas incorretas que atendam à política de formato bloqueiam a conta por 15 minutos. O contador é atômico, inclusive para chamadas simultâneas.

## Cadastro sem confirmação

Apenas a recepção autenticada pode pré-cadastrar um responsável:

```http
POST /api/responsaveis

{"nome":"Maria Silva","cpf":"12345678901","email":"maria@example.com","telefone":"11912345678"}
```

Retorna 201 com `id` do usuário, nome, e-mail e `ativo: false`. CPF/e-mail duplicados retornam 409. Não recebe senha.

O responsável conclui o acesso publicamente:

```http
POST /api/cadastro/responsavel

{"cpf":"12345678901","email":"maria@example.com","senha":"senhaSegura123","aceiteTermos":true}
```

CPF e e-mail devem corresponder ao pré-cadastro. Senha de 8 a 1.024 caracteres. Retorna 201 com `mensagem` e `usuario: {id, nome, email}`. Não gera sessão: faça login depois. Uma conta com senha já definida retorna 409 e mantém sua senha, inclusive se estiver inativa. O aceite é gravado em `usuario.termos_aceitos_em`.

Não há envio de e-mail, rota de confirmação, código de ativação, dependência Brevo ou variáveis de serviço de e-mail. O token JWT de login continua necessário para autenticação.

## Rotas (25 combinações de método e caminho)

| Método | Rota | Perfil / comportamento |
| --- | --- | --- |
| GET | `/api/saude` | Público; aplicação em execução |
| POST | `/api/auth/login` | Público; autenticar |
| GET | `/api/auth/eu` | Autenticado; dados do usuário |
| POST | `/api/cadastro/responsavel` | Público; concluir pré-cadastro |
| POST | `/api/responsaveis` | Recepção; pré-cadastrar |
| GET | `/api/pacientes` | Responsável; seus dependentes |
| POST | `/api/pacientes` | Responsável; cadastrar dependente |
| GET | `/api/horarios` | Autenticado; horários disponíveis |
| POST | `/api/consultas` | Responsável; agendar |
| GET | `/api/consultas` | Responsável; suas consultas |
| PATCH | `/api/consultas/{id}/cancelar` | Responsável; cancelar antes do início |
| POST | `/api/horarios` | Médico / recepção; abrir blocos |
| GET | `/api/agenda` | Médico / recepção; agenda do dia |
| PATCH | `/api/horarios/{id}/bloquear` | Médico / recepção; bloquear se não tiver consulta |
| PATCH | `/api/horarios/{id}/desbloquear` | Médico / recepção; liberar |
| PATCH | `/api/consultas/{id}/confirmar` | Médico / recepção; confirmar AGENDADA antes do início |
| PATCH | `/api/consultas/{id}/realizar` | Médico; concluir consulta que já começou |
| POST | `/api/relatorios` | Médico / recepção; gerar por médico/período |
| GET | `/api/relatorios` | Médico / recepção; histórico |
| GET | `/api/relatorios/{id}` | Médico / recepção; abrir |
| GET | `/api/relatorios/{id}/exportar` | Médico / recepção; exportar CSV |
| GET | `/api/medico/pacientes` | Médico; pacientes com consultas não canceladas em sua agenda |
| GET | `/api/pacientes/{id}/prontuarios` | Médico; histórico dos próprios atendimentos desse paciente |
| GET | `/api/consultas/{id}/prontuario` | Médico; prontuário da própria consulta |
| POST | `/api/consultas/{id}/prontuario` | Médico; registrar e concluir o atendimento atomicamente |

## Pacientes e agendamento

`POST /api/pacientes`: `{"nome":"Ana","dataNascimento":"2021-03-14"}`. Aceita pacientes de 0 a 17 anos; recusa datas futuras e duplicação do mesmo nome/nascimento na família, inclusive sob concorrência.

`GET /api/horarios?data=2026-12-01&medico=2&especialidade=Pediatria`: `data` é obrigatória; os outros filtros são opcionais. Horários bloqueados, ocupados, passados ou de médicos inativos não aparecem.

`POST /api/consultas`: `{"idPaciente":1,"idHorario":123,"tipoAtendimento":"PARTICULAR"}`. Os tipos são `PARTICULAR` e `CONVENIO`. O paciente precisa pertencer ao responsável autenticado. A mesma criança não pode ser reservada em agendas sobrepostas; um bloco não recebe duas consultas ativas. Cancelar mantém o histórico e devolve o bloco à lista de horários livres.

## Agenda e relatórios

`POST /api/horarios`: `{"idMedico":2,"data":"2026-12-01","inicio":"08:00","fim":"12:00","duracao":30}`. Durações permitidas: 15, 20, 30, 40, 45 e 60 minutos. Retorna `criados` e `ignorados`; não cria sobreposições. Datas/horas usam o fuso `America/Sao_Paulo`.

`GET /api/agenda?data=2026-12-01&medico=2`: a recepção pode consultar todas as agendas ou filtrar um médico. O médico sempre acessa a própria agenda, mesmo que informe outro ID.

`POST /api/relatorios`: `{"idMedico":2,"inicio":"2026-12-01","fim":"2026-12-31"}`. Período máximo de 366 dias. O médico sempre gera o próprio relatório; a recepção precisa escolher o médico. O relatório registra o período e recalcula os dados ao ser reaberto. Exportação CSV protege textos que possam ser interpretados como fórmulas.

## Prontuário

`POST /api/consultas/123/prontuario`: `{"observacoes":"Avaliação e orientações do atendimento."}`. A consulta deve ser do médico autenticado, já ter começado e não estar cancelada. Salva um único registro e muda o status para `REALIZADA` na mesma transação. Aceita texto não vazio de até 65.535 bytes UTF-8.

Retorna 201 com `id`, `idConsulta`, `idPaciente`, `observacoes` e `dataRegistro` em UTC. Um segundo registro retorna 409, sem sobrescrever o primeiro. A rota de realização sem prontuário continua disponível; é possível registrar depois o prontuário dessa consulta realizada. O histórico só inclui registros das consultas do médico autenticado; recepção e responsáveis não acessam essas rotas.

## Limites desta entrega

Não implementa recuperação de senha, reativação de contas ou envio de lembretes. A tabela de notificações permanece compatível com o modelo, mas não há serviço externo de envio. As telas MAUI não foram modificadas; as novas rotas precisam ser chamadas pelos respectivos fluxos do app.

[Validação executada](VALIDACAO.md) · [Reproduzir testes](../testes/README.md)
