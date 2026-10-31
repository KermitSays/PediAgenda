using ClosedXML.Excel;
using PediAgenda.Dados;
using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;
using PediAgenda.Nucleo.Servicos;
using PediAgenda.Views.Usuarios.Medico;
using Xunit;

namespace PediAgenda.Testes;

public class LoginEExcelTests
{
    private sealed class RelogioTeste : TimeProvider
    {
        public DateTimeOffset Agora = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    [Theory]
    [InlineData("x")]
    [InlineData("")]
    [InlineData("senhaErrada123")]
    public void Cinco_senhas_invalidas_bloqueiam_e_expiram(string senha)
    {
        var repo = new UsuarioRepositorioMemoria();
        repo.Inserir("Médico", "12345678909", "medico@teste.local", "senhaDemo123", Perfil.Medico);
        var relogio = new RelogioTeste();
        var servico = new AutenticacaoService(repo, relogio);
        for (int i = 0; i < 4; i++)
            Assert.Equal(MotivoFalha.CredenciaisInvalidas, servico.Autenticar("medico@teste.local", senha).Motivo);
        Assert.Equal(MotivoFalha.ContaBloqueada, servico.Autenticar("medico@teste.local", senha).Motivo);
        Assert.Equal(MotivoFalha.ContaBloqueada, servico.Autenticar("medico@teste.local", "senhaDemo123").Motivo);
        relogio.Agora += TimeSpan.FromMinutes(16);
        Assert.True(servico.Autenticar("medico@teste.local", "senhaDemo123").Sucesso);
        Assert.Equal(0, repo.BuscarPorEmail("medico@teste.local")!.TentativasInvalidas);
    }

    [Fact]
    public void Sucesso_interrompe_sequencia_de_falhas()
    {
        var repo = new UsuarioRepositorioMemoria();
        var servico = new AutenticacaoService(repo);
        for (int i = 0; i < 4; i++) servico.Autenticar("joab@pediagenda.local", "x");
        Assert.True(servico.Autenticar("joab@pediagenda.local", "senhaForte123").Sucesso);
        Assert.Equal(MotivoFalha.CredenciaisInvalidas, servico.Autenticar("joab@pediagenda.local", "x").Motivo);
        Assert.Equal(1, repo.BuscarPorEmail("joab@pediagenda.local")!.TentativasInvalidas);
    }

    [Fact]
    public void Excel_reaberto_contem_periodo_medico_e_somente_realizadas()
    {
        var inicio = new DateOnly(2026, 10, 1);
        var fim = new DateOnly(2026, 10, 31);
        var linhas = new List<LinhaRelatorioMedicoApi>
        {
            new(inicio.AddDays(2), "15:30", "=Paciente", 5, "REALIZADA"),
            new(inicio, "10:00", "Agendada", 4, "AGENDADA"),
            new(inicio, "11:00", "Cancelada", 4, "CANCELADA"),
            new(inicio, "12:00", "Confirmada", 4, "CONFIRMADA")
        };
        var documento = RelatorioMedicoDocumento.Criar(new(1, new(2, "Dra. Teste", "Pediatria"), inicio, fim, linhas), inicio, fim);
        linhas.Clear(); // O documento preserva o resultado mesmo que a fonte mude.
        using var stream = ExportadorExcelMedico.Gerar(documento);
        using var excel = new XLWorkbook(stream);
        var folha = excel.Worksheet("Atendimentos");
        Assert.Equal("01/10/2026 até 31/10/2026", folha.Cell("B4").GetString());
        Assert.Equal("Dra. Teste", folha.Cell("B3").GetString());
        Assert.Equal(1, folha.Cell("B6").GetValue<int>());
        Assert.Equal("=Paciente", folha.Cell("A9").GetString());
        Assert.False(folha.Cell("A9").HasFormula);
        Assert.Equal(inicio.AddDays(2).ToDateTime(TimeOnly.MinValue), folha.Cell("B9").GetDateTime());
        Assert.True(folha.Cell("A10").IsEmpty());
    }

    [Fact]
    public void Relatorio_recusa_periodo_divergente()
    {
        var hoje = new DateOnly(2026, 10, 28);
        var resposta = new RelatorioMedicoResposta(1, new(1, "Médico", "Pediatria"), hoje, hoje, []);
        Assert.Throws<InvalidOperationException>(() => RelatorioMedicoDocumento.Criar(resposta, hoje.AddDays(-1), hoje));
    }

    [Fact]
    public void Sair_limpa_identificacao_e_invalida_respostas_da_sessao_anterior()
    {
        SessaoUsuario.Iniciar(8, "Médico", "medico@teste.local", "MEDICO");
        long versao = SessaoUsuario.Versao;
        SessaoUsuario.Limpar();
        Assert.False(SessaoUsuario.EstaLogado);
        Assert.Equal(0, SessaoUsuario.IdUsuario);
        Assert.Empty(SessaoUsuario.Nome);
        Assert.Empty(SessaoUsuario.Email);
        Assert.Empty(SessaoUsuario.Perfil);
        Assert.NotEqual(versao, SessaoUsuario.Versao);
        SessaoUsuario.Iniciar(8, "Médico", "medico@teste.local", "MEDICO");
        Assert.NotEqual(versao, SessaoUsuario.Versao);
        SessaoUsuario.Limpar();
    }
}
