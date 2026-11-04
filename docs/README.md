# Documentação do PediAgenda

A documentação foi dividida entre o **estado final da entrega** e os **registros de fases anteriores**. Essa distinção evita confundir decisões antigas com as funcionalidades atualmente apresentadas.

## Entrega e verificação final

- [README principal](../README.md): apresentação, tecnologias, arquitetura, funcionalidades e execução do PediAgenda.
- [Validação final](VALIDACAO_FINAL.md): testes funcionais realizados com Render, Aiven e Brevo, incluindo uma limitação de teste registrada explicitamente.
- [Guia da API](../api/README.md): rotas, autenticação, regras e serviços.
- [Banco de dados](../banco/README.md): SQL para banco novo, carga fictícia e migrações.

## Registros históricos preservados

- [PASSAGEM.md](PASSAGEM.md): passagem do back-end em fase anterior, com escopo e procedimentos que **não devem ser usados para reconstruir o ambiente final**.
- [MEDICO_CONCLUSAO.md](MEDICO_CONCLUSAO.md): registro técnico de conclusão da integração do Médico em uma versão anterior.
- [Validação histórica da API](../api/VALIDACAO.md): resultados de outra fase do desenvolvimento, não equivalentes aos testes finais do app hospedado.

**Importante:** não execute scripts antigos nem copie credenciais de documentos históricos para o Aiven em uso. Consulte os arquivos atuais antes de realizar qualquer modificação.
