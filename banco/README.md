# Banco MySQL 8 — PediAgenda

| Arquivo | Uso |
| --- | --- |
| `01_Schema.sql` | Cria banco e 12 tabelas em uma base NOVA e vazia; não executa DROP DATABASE |
| `02_popula_tabelas.sql` | Carga fictícia para uma base de testes vazia, criada pelo 01 |
| `03_remover_verificacao_email.sql` | Migração de um banco existente para o cadastro sem confirmação e sem código |
| `03_modelo_completo.sql` | Arquivo antigo desativado; interrompe a execução sem recriar o banco |
| `04_validacao_modelo_completo.sql` | Exercício manual de restrições em banco de teste vazio, sem a carga 02; alguns erros são esperados |

## Banco novo

Execute `01_Schema.sql`. Se a conta Aiven não puder criar bancos, crie `pediagenda` no painel e execute a partir de `USE pediagenda;`. Não execute a carga fictícia no banco compartilhado com dados reais.

A carga 02 mantém os IDs de demonstração e gera horários nos próximos três dias úteis. As contas completas dessa carga usam **senhaDemo123**:

| Perfil | E-mail |
| --- | --- |
| Responsável | `maria@teste.local` |
| Responsável de outra família | `ana.oliveira@example.com` |
| Médico | `medico.teste@example.com` |
| Outro médico | `helena.duarte@example.com` |
| Recepção | `recepcao.teste@example.com` |

Há também um pré-cadastro sem senha: CPF `52998224725`, e-mail `precadastro@example.com`. Complete-o pela API escolhendo uma senha e aceitando os termos.

## Banco existente

Execute somente `03_remover_verificacao_email.sql` antes de iniciar a nova API. O script:

- Permite credenciais nulas nos pré-cadastros.
- Adiciona `usuario.termos_aceitos_em`, sem inventar datas para registros antigos.
- Remove as colunas antigas de verificação de e-mail e código de ativação que existirem.
- Preserva nomes, CPF, e-mail, hashes de senha, estado ativo/inativo e atendimentos.
- Pode ser repetido; não recria tabelas nem apaga registros.

DDL no MySQL faz commit implícito; a migração não é uma transação com rollback integral. Contas antigas inativas continuam inativas: a clínica deve avaliar esses cadastros. Um pré-cadastro sem senha precisa ter o e-mail informado pela recepção antes de concluir o acesso. Não é necessário reaplicar a carga 02.

O e-mail continua sendo o identificador de login. O aceite dos termos é registrado em UTC no momento em que a senha é definida.
