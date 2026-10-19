# Validação da API

Base revisada: `master`, commit `1be6a35b30a3204a6cdd5ad32d64bae8dc382c0a` de `KermitSays/PediAgenda`.

## Resultado executado

- SDK .NET 9.0.319, runtime .NET/ASP.NET Core 9.0.21 e MySQL 8.0.46.
- API e console de login compilados sem erros ou avisos.
- Publicação Release da API concluída.
- **624 verificações HTTP aprovadas**, com requisições reais à API e persistência no MySQL local descartável.
- Cada uma das 25 combinações de método/caminho foi exercitada pelo menos cinco vezes. Os números abaixo incluem sucesso, validação, autenticação, autorização e conflito.
- Migração executada em estrutura atual, com colunas antigas e novamente após removê-las. IDs, nomes, CPF, e-mail, hashes/salts e estado ativo/inativo foram comparados antes/depois.

| Método | Rota | Verificações |
| --- | --- | ---: |
| GET | `/api/agenda` | 21 |
| GET | `/api/auth/eu` | 22 |
| POST | `/api/auth/login` | 46 |
| POST | `/api/cadastro/responsavel` | 29 |
| GET | `/api/consultas` | 15 |
| POST | `/api/consultas` | 39 |
| PATCH | `/api/consultas/{id}/cancelar` | 25 |
| PATCH | `/api/consultas/{id}/confirmar` | 25 |
| GET | `/api/consultas/{id}/prontuario` | 25 |
| POST | `/api/consultas/{id}/prontuario` | 60 |
| PATCH | `/api/consultas/{id}/realizar` | 35 |
| GET | `/api/horarios` | 22 |
| POST | `/api/horarios` | 30 |
| PATCH | `/api/horarios/{id}/bloquear` | 25 |
| PATCH | `/api/horarios/{id}/desbloquear` | 15 |
| GET | `/api/medico/pacientes` | 15 |
| GET | `/api/pacientes` | 15 |
| POST | `/api/pacientes` | 33 |
| GET | `/api/pacientes/{id}/prontuarios` | 20 |
| GET | `/api/relatorios` | 15 |
| POST | `/api/relatorios` | 20 |
| GET | `/api/relatorios/{id}` | 20 |
| GET | `/api/relatorios/{id}/exportar` | 20 |
| POST | `/api/responsaveis` | 16 |
| GET | `/api/saude` | 6 |

Também foram testados rota inexistente, método incorreto e JSON malformado. O roteiro reproduzível está em `testes/integracao_api.py`; seu relatório JSON não contém tokens nem senhas.

## Correções verificadas

- Remoção dos serviços de confirmação de e-mail e código de ativação, chamadas de métodos inexistentes e parâmetros incompatíveis do cadastro.
- Conclusão de pré-cadastro com CPF/e-mail da clínica, senha e termos, sem consumir código nem enviar mensagem externa.
- Pré-cadastro por recepção autenticada; conta permanece sem acesso até definir senha.
- Cadastro não sobrescreve senha existente, inclusive em conta inativa. O UPDATE condicional impede conclusão simultânea com duas senhas diferentes.
- Aceite dos termos registrado em UTC; os registros antigos não recebem datas inventadas.
- Autorização por perfil, isolamento de famílias e da agenda/prontuário/relatórios de cada médico; JWT vencido, adulterado, incompleto e perfil alterado são recusados.
- Contagem atômica de senhas incorretas e bloqueio temporário; token de conta desativada deixa de funcionar.
- Reservas concorrentes não duplicam horário nem colocam a mesma criança em agendas sobrepostas.
- Abertura concorrente de blocos e cadastro simultâneo do mesmo paciente não produzem duplicações.
- Médico inativo não recebe reserva por ID de horário; cancelamento devolve o bloco à busca.
- Novo prontuário persiste observações e marca a consulta como REALIZADA na mesma transação; duplicação, falta de permissão, consulta futura/cancelada e texto inválido são recusados.
- Horários da clínica usam America/Sao_Paulo; JWT/bloqueios e novos prontuários usam UTC.
- JSON estável para requisição inválida e falha de acesso ao banco (503), sem detalhes internos na resposta.
- Documentação e scripts alinhados; script antigo de recriação interrompe a execução e o schema novo não contém DROP DATABASE.

## Alcance

Os testes usam dados fictícios em uma instância local. Não foram aplicadas migrações no Aiven nem feita publicação no Render. A interface MAUI não foi alterada nem testada; a integração das telas com as novas rotas continua sendo trabalho do aplicativo. Recuperação de senha, reativação de contas e envio de lembretes não estão implementados nesta API.

Antes de publicar a API atualizada, aplique `banco/03_remover_verificacao_email.sql` no banco existente. O JWT de login continua necessário; somente a confirmação do cadastro foi removida.
