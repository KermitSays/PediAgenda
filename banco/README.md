# Banco de dados

Scripts do banco `pediagenda` (MySQL 8.0), na ordem em que se roda.

| Arquivo | O que faz |
|---|---|
| `03_modelo_completo.sql` | Cria o banco inteiro: 12 tabelas, chaves, índices e restrições |
| `04_validacao_modelo_completo.sql` | 21 testes que conferem se as regras do modelo pegam |

**Atenção: o `03` apaga o banco `pediagenda` e recria tudo do zero.** Só rode
enquanto houver apenas dados de teste.

No MySQL Workbench: abra a conexão primeiro e, de dentro dela, *File → Open SQL
Script*, depois o raio ⚡. Ao final devem aparecer 12 tabelas.

O `04` é opcional: ele cria um cenário de teste, tenta violar cada regra (CPF
repetido, horário duplicado na mesma agenda, consulta sem horário, prontuário
em consulta que já tem um) e no fim apaga o que criou. As mensagens de erro
que aparecem são o resultado esperado — é o banco recusando o que deve recusar.

## Perfis de usuário

Não existe tabela de perfis. O perfil de cada usuário vem da especialização que
aponta para ele: `responsavel`, `medico` ou `recepcionista`. Um usuário sem
nenhuma das três é tratado pelo login como conta inativa.

## Senhas

A tabela `usuario` não guarda senha em texto puro: guarda `senha_hash`,
`senha_salt` e `senha_iteracoes` (PBKDF2-SHA256). Quem grava isso é a API; pelo
SQL não dá para criar usuário com senha utilizável.
