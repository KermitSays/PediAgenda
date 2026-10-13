
namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class AdicionarDependente : ContentPage
{
    private bool salvando = false;

    public AdicionarDependente()
    {
        InitializeComponent();

        DataNascimentoPicker.MaximumDate =
            DateTime.Today;

        DataNascimentoPicker.Date =
            DateTime.Today.AddYears(-5);
    }

    // CADASTRAR DEPENDENTE
    private async void SalvarButton_Clicked(
        object sender, EventArgs e)
    {
        if (salvando)
            return;

        MensagemErroLabel.IsVisible = false;

        string nome =
            NomeEntry.Text?.Trim() ?? "";


        DateTime nascimento =
            DataNascimentoPicker.Date ?? DateTime.Today;


        if (string.IsNullOrWhiteSpace(nome)
            || nome.Length < 3)
        {
            ExibirErro(
                "Informe o nome completo da criança.");
            return;
        }

        if (nascimento.Date > DateTime.Today)
        {
            ExibirErro(
                "A data de nascimento não pode ser futura.");
            return;
        }

        bool existe = ResponsavelDados.Pacientes.Any(
            p => p.Nome.Equals(
                    nome,
                    StringComparison.OrdinalIgnoreCase)
                && p.DataNascimento.Date == nascimento.Date);

        if (existe)
        {
            ExibirErro(
                "Este dependente já está cadastrado.");
            return;
        }

        bool confirmar = await DisplayAlertAsync(
            "Confirmar cadastro",
            $"Deseja cadastrar {nome} como dependente?",
            "Cadastrar",
            "Cancelar");

        if (!confirmar)
            return;

        salvando = true;
        SalvarButton.IsEnabled = false;

        try
        {
            // CADASTRO TEMPORÁRIO LOCAL
            // Será substituído pela integração com a API.

            int novoId =
                ResponsavelDados.Pacientes.Count == 0
                    ? 1
                    : ResponsavelDados.Pacientes.Max(
                        p => p.Id) + 1;

            var paciente = new PacienteResponsavelItem
            {
                Id = novoId,
                Nome = nome,
                DataNascimento = nascimento,
                Foto = "user.png"
            };

            ResponsavelDados.Pacientes.Add(paciente);

            await DisplayAlertAsync(
                "Cadastro realizado",
                "Dependente adicionado para teste. " +
                "Os dados ainda não foram salvos no banco.",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ExibirErro(
                "Não foi possível cadastrar o dependente.");
        }
        finally
        {
            salvando = false;
            SalvarButton.IsEnabled = true;
        }
    }

    private void ExibirErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }

    // VOLTAR
    private async void VoltarButton_Clicked(
        object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
