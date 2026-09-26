# Banco de dados

Scripts do banco `pediagenda` (MySQL 8.0), na ordem em que se roda.

| Arquivo | O que faz |
|---|---|
| `03_modelo_completo.sql` | Cria o banco inteiro: 12 tabelas, chaves, índices e restrições |
| `04_validacao_modelo_completo.sql` | 21 testes que conferem se as regras do modelo pegam |
| `dados_de_teste.sql` | Dois médicos, uma recepcionista e horários nos próximos dias úteis |

**Atenção: o `03` apaga o banco `pediagenda` e recria tudo do zero.** Só rode
enquanto houver apenas dados de teste.

No MySQL Workbench: abra a conexão primeiro e, de dentro dela, *File → Open SQL
Script*, depois o raio ⚡. Ao final devem aparecer 12 tabelas.

O `04` é opcional: ele cria um cenário de teste, tenta violar cada regra (CPF
repetido, horário duplicado na mesma agenda, consulta sem horário, prontuário
em consulta que já tem um) e no fim apaga o que criou. As mensagens de erro
que aparecem são o resultado esperado — é o banco recusando o que deve recusar.

## Dados de teste

O `dados_de_teste.sql` cria contas para testar o que não se cadastra pela
tela (médico e recepção) e horários para agendar. Pode rodar quantas vezes
quiser: ele apaga só os dados dele e recria, sempre a partir de amanhã.

| Conta | E-mail | Senha |
|---|---|---|
| Dr. Pedro Alves, Pediatria (manhãs) | `pedro.alves@teste.pediagenda.local` | `senhaMedico123` |
| Dra. Carla Mendes, Pneumologia Pediátrica (tardes) | `carla.mendes@teste.pediagenda.local` | `senhaMedico123` |
| Julia Rocha, recepcionista | `julia.rocha@teste.pediagenda.local` | `senhaRecepcao123` |

**Senhas só para teste.** No primeiro dia útil, o Dr. Pedro tem 10:00 e 10:30
bloqueados, para testar horário indisponível.

## Perfis de usuário

Não existe tabela de perfis. O perfil de cada usuário vem da especialização que
aponta para ele: `responsavel`, `medico` ou `recepcionista`. Um usuário sem
nenhuma das três é tratado pelo login como conta inativa.

## Senhas

A tabela `usuario` não guarda senha em texto puro: guarda `senha_hash`,
`senha_salt` e `senha_iteracoes` (PBKDF2-SHA256). Quem grava isso é a API; pelo
SQL não dá para criar usuário com senha utilizável.
