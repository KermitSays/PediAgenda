# Testes da API com MySQL real

Requisitos: SDK .NET 9, Python 3.9+ e MySQL 8 local com cliente `mysql` no PATH. No Windows, Python precisa disponibilizar os dados de fuso horário (`pip install tzdata`). Não são necessários workloads MAUI para compilar o backend.

O teste inicia e encerra sua própria API na porta 5099. Usa um MySQL que já deve estar em execução. **Apaga e recria o schema `pediagenda` dessa instância local. Use um MySQL descartável, separado do seu banco de trabalho.** O programa recusa hosts externos e exige a opção explícita abaixo.

PowerShell, na raiz:

```powershell
dotnet build api/PediAgenda.Api.csproj
$env:MYSQL_HOST = "127.0.0.1"
$env:MYSQL_PORT = "3306"
$env:MYSQL_USER = "root"
$env:MYSQL_PASSWORD = "SENHA_DO_MYSQL_LOCAL_DE_TESTE"
$env:PEDIAGENDA_TESTE_DESTRUTIVO = "SIM"
$env:PEDIAGENDA_TESTE_LOG = "$env:TEMP/pediagenda-api-testes.log"
$env:PEDIAGENDA_TESTE_RESULTADO = "$env:TEMP/pediagenda-resultados.json"
python testes/integracao_api.py
```

No Linux, use `export NOME=valor` para as variáveis. Se os executáveis estiverem fora do PATH, configure `MYSQL_BIN` e `DOTNET_BIN` com seus caminhos completos. A conta de preparação precisa poder criar o banco e a conta local `pediagenda_api_teste`; essa conta separada é usada pela API para testar também respostas a falhas de acesso ao banco.

O teste aplica schema e carga fictícia, verifica migração em estruturas atuais/legadas e sua repetição, testa cada uma das 25 combinações de método/caminho pelo menos cinco vezes e verifica respostas, dados persistidos e isolamento entre perfis/famílias/médicos. Testa também JWT inválido/vencido, conta inativa, termos, bloqueio de login e corridas de reservas, abertura de horários, pacientes e conclusão do cadastro.

O resultado JSON contém apenas rotas, status HTTP e cenários; não inclui tokens nem senhas. O workflow `API e MySQL` reproduz os testes no GitHub Actions usando um serviço MySQL descartável.
