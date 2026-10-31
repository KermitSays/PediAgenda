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

## Cadastro com verificação de e-mail (Brevo)

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

CPF e e-mail devem corresponder ao pré-cadastro. Senha de 8 a 1.024 caracteres. Em caso de envio bem-sucedido do e-mail, retorna **202** com `mensagem` e `usuario: {id, nome, email}`. A conta permanece inativa até o link ser confirmado; **não gera sessão**. Uma conta com senha já definida retorna 409 e mantém sua senha, inclusive se estiver inativa. O aceite é gravado em `usuario.termos_aceitos_em`.

A API envia um link via Brevo. A confirmação ocorre em `GET /api/cadastro/confirmar-email?token=...`; após a confirmação, a conta fica ativa e o login pode ser realizado. **Não há tela nem campo para digitar código de verificação.** Se o serviço de envio falhar, a API responde 502 e o responsável pode tentar o cadastro novamente.

Variáveis do serviço: `PEDIAGENDA_EMAIL_API_KEY`, `PEDIAGENDA_EMAIL_REMETENTE`, `PEDIAGENDA_EMAIL_NOME` (opcional) e `RENDER_EXTERNAL_URL` (endereço público da API para o link). Nunca publicar chaves de API no repositório. O cadastro foi validado localmente; a implantação no Render requer configuração e teste próprios. O JWT de login permanece necessário para autenticação.

## Rotas principais (a lista também inclui módulos de Recepção em arquivos de rotas separados)

| Método | Rota | Perfil / comportamento |
| --- | --- | --- |
| GET | `/api/saude` | Público; aplicação em execução |
| POST | `/api/auth/login` | Público; autenticar |
| GET | `/api/auth/eu` | Autenticado; dados do usuário |
| POST | `/api/cadastro/responsavel` | Público; iniciar ativação por e-mail (202) |
| GET | `/api/cadastro/confirmar-email?token=...` | Público; confirmar e-mail pelo link |
| POST | `/api/responsaveis` | Rota técnica mantida no backend; pré-cadastro não faz parte das telas finais da Recepção |
| GET | `/api/pacientes` | Responsável; seus dependentes |
| POST | `/api/pacientes` | Rota técnica legada; cadastro de dependente não faz parte do aplicativo final |
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

`POST /api/consultas`: `{"idPaciente":1,"idHorario":123}`. O paciente precisa pertencer ao responsável autenticado. A mesma criança não pode ser reservada em agendas sobrepostas; um bloco não recebe duas consultas ativas. Cancelar mantém o histórico e devolve o bloco à lista de horários livres.

## Agenda e relatórios

`POST /api/horarios`: `{"idMedico":2,"data":"2026-12-01","inicio":"08:00","fim":"12:00","duracao":30}`. Durações permitidas: 15, 20, 30, 40, 45 e 60 minutos. Retorna `criados` e `ignorados`; não cria sobreposições. Datas/horas usam o fuso `America/Sao_Paulo`.

`GET /api/agenda?data=2026-12-01&medico=2`: a recepção pode consultar todas as agendas ou filtrar um médico. O médico sempre acessa a própria agenda, mesmo que informe outro ID.

`POST /api/relatorios`: `{"idMedico":2,"inicio":"2026-12-01","fim":"2026-12-31"}`. Período máximo de 366 dias. O médico sempre gera o próprio relatório; a recepção precisa escolher o médico. O relatório registra o período e recalcula os dados ao ser reaberto. Exportação CSV protege textos que possam ser interpretados como fórmulas.

## Prontuário

`POST /api/consultas/123/prontuario`: `{"observacoes":"Avaliação e orientações do atendimento."}`. A consulta deve ser do médico autenticado, já ter começado e não estar cancelada. Salva um único registro e muda o status para `REALIZADA` na mesma transação. Aceita texto não vazio de até 65.535 bytes UTF-8.

Retorna 201 com `id`, `idConsulta`, `idPaciente`, `observacoes` e `dataRegistro` em UTC. Um segundo registro retorna 409, sem sobrescrever o primeiro. A rota de realização sem prontuário continua disponível; é possível registrar depois o prontuário dessa consulta realizada. O histórico só inclui registros das consultas do médico autenticado; recepção e responsáveis não acessam essas rotas.

## Limites desta entrega

Não implementa envio de lembretes ou notificações no aplicativo. O modelo novo do banco não inclui a tabela `notificacao` nem o campo `consulta.tipo_atendimento`. O aplicativo .NET MAUI integra os três perfis e exporta relatórios médicos em Excel; as rotas de exportação CSV da API são um recurso distinto. A migração de coluna no Aiven e a implantação da API no Render devem ser coordenadas.

[Validação histórica da API](VALIDACAO.md) · [Reproduzir testes](../testes/README.md)
