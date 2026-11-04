# PediAgenda — registro dos testes finais

Este documento registra as **verificações manuais realizadas durante a preparação da entrega do TCC**, com o aplicativo .NET MAUI conectado à API hospedada no Render e ao MySQL no Aiven. O registro distingue fluxos concluídos de um teste que não foi finalizado integralmente.

Os resultados abaixo correspondem aos cenários observados pela equipe durante os testes; não são uma certificação de disponibilidade permanente, nem substituem os testes automatizados executados no GitHub Actions.

## Ambiente utilizado

- **Aplicativo:** PediAgenda, em emulador Android.
- **API:** ASP.NET Core, acessada pelo endpoint HTTPS do Render.
- **Persistência:** banco de dados MySQL hospedado no Aiven.
- **Verificação de e-mail:** serviço Brevo.
- **Conexão:** testes funcionais realizados com a API local desligada, para confirmar o uso da API hospedada.

## Verificações manuais

| Área | Cenário verificado | Resultado observado |
| --- | --- | --- |
| API hospedada | Resposta de `GET /api/saude`, sem API local em execução | Concluído; resposta de saúde da API obtida. |
| Responsável | Login e acesso a dependentes | Concluído. |
| Responsável | Agendamento e visualização da consulta na lista | Concluído. |
| Responsável | Cancelamento e atualização do histórico | Concluído. |
| Cadastro/Brevo | Habilitação da conta com CPF/e-mail pré-cadastrados e criação de senha | Concluído com conta temporária de teste. |
| Cadastro/Brevo | Recebimento do e-mail, confirmação pelo link e login após a ativação | Concluído no ambiente Render/Aiven/Brevo. |
| Cadastro | Tela de conclusão e botão de retorno ao login | Concluído. |
| Médico | Consulta de agenda, horários e consultas do dia | Concluído. |
| Médico | Bloqueio/liberação de horários | Concluído. |
| Médico | Leitura de prontuários existentes | Concluído. |
| Médico | Relatórios por período e geração de arquivo Excel (`.xlsx`) | Concluído, inclusive com registros fictícios de consultas realizadas. |
| Médico | Mensagem quando não há resultados para relatório | Concluído. |
| Recepção | Consulta de pacientes, médicos, agendas e consultas do dia | Concluído. |
| Recepção | Agendamento, confirmação de consulta, bloqueio de período e cancelamento decorrente do bloqueio | Concluído, com conferência do estado no banco. |
| Segurança de atendimento | Tentativa de finalizar uma consulta futura alterando data/hora do emulador | **Bloqueada pela API**, que conferiu o horário de início da consulta. |
| Novo prontuário | Gravar novas observações e concluir efetivamente um atendimento a partir do aplicativo | **Parcial:** a interface e a tentativa de envio foram verificadas, mas a gravação não ocorreu, pois a consulta ainda não havia começado segundo o horário do servidor. |

### Limite do teste de novo prontuário

A tela de registro permitiu inserir observações e solicitar a finalização. Contudo, a API retornou a regra **“A consulta ainda não começou”** e impediu que a consulta fosse marcada como `REALIZADA` e que um novo prontuário fosse inserido indevidamente.

Portanto, **não se deve afirmar que a gravação de um novo prontuário pelo aplicativo foi concluída nesse teste**. A consulta e a leitura de prontuários existentes foram verificadas; a validação da proteção de horário foi positiva. Um teste de conclusão efetiva exigiria uma consulta de teste já iniciada no horário oficial da clínica e ainda sem prontuário.

## Testes automatizados e limites

O repositório mantém testes automatizados em [`testes/`](../testes/) e um workflow em [`.github/workflows/medico.yml`](../.github/workflows/medico.yml), abrangendo API, integração e compilação Android. O resultado da **Pull Request final** deve ser conferido no GitHub Actions; a simples presença do workflow não significa aprovação automática.

O arquivo [`api/VALIDACAO.md`](../api/VALIDACAO.md) descreve uma **validação histórica anterior**, com ambiente e escopo diferentes. Seus totais não devem ser apresentados como resultado dos testes finais do aplicativo hospedado.

## Cuidados com os dados

Foram usadas contas e consultas **fictícias**. Contas temporárias criadas especificamente para verificar a confirmação via Brevo foram removidas após os testes. Não publique neste repositório CPF, e-mail pessoal, tokens de confirmação, credenciais do Aiven, chaves do Render/Brevo ou dumps de dados de pacientes.

Este registro pode ser atualizado apenas com novos resultados efetivamente observados e verificados.
