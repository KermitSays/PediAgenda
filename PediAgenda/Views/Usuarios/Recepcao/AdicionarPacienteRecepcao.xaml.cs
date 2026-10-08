namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class AdicionarPacienteRecepcao : ContentPage
{
    private ResponsavelRecepcaoItem?
        responsavelSelecionado;


    public AdicionarPacienteRecepcao()
    {
        InitializeComponent();


        DataNascimentoPicker.MaximumDate =
            DateTime.Today;


        DataNascimentoPicker.Date =
            DateTime.Today.AddYears(
                -5);
    }


    // =============================================
    // CPF ALTERADO
    // =============================================
    //
    // Aqui NÃO alteramos CpfEntry.Text.
    //
    // Isso evita o erro do Android causado por
    // modificar o texto enquanto o teclado ainda
    // está processando cursor e seleção.

    private void CpfEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (
            responsavelSelecionado ==
            null)
        {
            return;
        }


        string cpfDigitado =
            SomenteNumeros(
                e.NewTextValue
                ?? string.Empty);


        string cpfSelecionado =
            SomenteNumeros(
                responsavelSelecionado.Cpf);


        // Se o usuário modificar o CPF depois de
        // encontrar alguém, o responsável anterior
        // deixa de ser válido.

        if (
            cpfDigitado !=
            cpfSelecionado)
        {
            LimparResponsavelEncontrado();
        }
    }


    // =============================================
    // BUSCAR RESPONSÁVEL
    // =============================================

    private async void BuscarResponsavelButton_Clicked(
        object sender,
        EventArgs e)
    {
        string cpf =
            SomenteNumeros(
                CpfEntry.Text
                ?? string.Empty);


        if (cpf.Length != 11)
        {
            LimparResponsavelEncontrado();


            await DisplayAlertAsync(
                "CPF inválido",
                "Informe os 11 números do CPF do responsável.",
                "OK");


            return;
        }


        ResponsavelRecepcaoItem?
            responsavel =
                ResponsaveisRecepcaoDados
                    .BuscarPorCpf(
                        cpf);


        if (
            responsavel ==
            null)
        {
            LimparResponsavelEncontrado();


            await DisplayAlertAsync(
                "Responsável não localizado",
                "Responsável não localizado. Verifique o CPF informado.",
                "OK");


            return;
        }


        responsavelSelecionado =
            responsavel;


        ResponsavelNomeLabel.Text =
            responsavel.Nome;


        ResponsavelCpfLabel.Text =
            responsavel.CpfMascarado;


        ResponsavelTelefoneLabel.Text =
            responsavel.TelefoneMascarado;


        ResponsavelEncontradoBorder.IsVisible =
            true;
    }


    // =============================================
    // CADASTRAR PACIENTE
    // =============================================

    private async void CadastrarPacienteButton_Clicked(
        object sender,
        EventArgs e)
    {
        // =========================================
        // RESPONSÁVEL
        // =========================================

        if (
            responsavelSelecionado ==
            null)
        {
            await DisplayAlertAsync(
                "Responsável necessário",
                "Busque e selecione o responsável antes de cadastrar o paciente.",
                "OK");


            return;
        }


        // =========================================
        // GARANTE QUE O CPF NÃO FOI ALTERADO
        // =========================================

        string cpfAtual =
            SomenteNumeros(
                CpfEntry.Text
                ?? string.Empty);


        string cpfResponsavel =
            SomenteNumeros(
                responsavelSelecionado.Cpf);


        if (
            cpfAtual !=
            cpfResponsavel)
        {
            LimparResponsavelEncontrado();


            await DisplayAlertAsync(
                "Responsável necessário",
                "O CPF foi alterado. Busque novamente o responsável antes de cadastrar o paciente.",
                "OK");


            return;
        }


        // =========================================
        // NOME
        // =========================================

        string nome =
            NomePacienteEntry.Text?
                .Trim()
            ?? string.Empty;


        if (
            string.IsNullOrWhiteSpace(
                nome))
        {
            await DisplayAlertAsync(
                "Nome necessário",
                "Informe o nome completo do paciente.",
                "OK");


            return;
        }


        if (
            nome.Length <
            2)
        {
            await DisplayAlertAsync(
                "Nome inválido",
                "Informe um nome válido para o paciente.",
                "OK");


            return;
        }


        // =========================================
        // DATA DE NASCIMENTO
        // =========================================

        DateTime nascimento =
            DataNascimentoPicker.Date
            ?? DateTime.Today;


        if (
            nascimento.Date >
            DateTime.Today)
        {
            await DisplayAlertAsync(
                "Data inválida",
                "A data de nascimento não pode ser futura.",
                "OK");


            return;
        }


        // =========================================
        // EVITA DUPLICADO LOCAL
        // =========================================

        bool jaExiste =
            PacientesRecepcaoDados
                .Pacientes
                .Any(p =>
                    p.Nome.Equals(
                        nome,
                        StringComparison.OrdinalIgnoreCase)

                    &&

                    p.DataNascimento.Date ==
                        nascimento.Date

                    &&

                    p.Responsavel.Equals(
                        responsavelSelecionado.Nome,
                        StringComparison.OrdinalIgnoreCase));


        if (jaExiste)
        {
            await DisplayAlertAsync(
                "Paciente já cadastrado",

                "Já existe um paciente com este nome, " +
                "data de nascimento e responsável.",

                "OK");


            return;
        }


        // =========================================
        // CONFIRMAÇÃO
        // =========================================

        bool confirmar =
            await DisplayAlertAsync(
                "Confirmar cadastro",

                $"Cadastrar {nome} como paciente " +
                $"vinculado(a) a " +
                $"{responsavelSelecionado.Nome}?",

                "OK",
                "Cancelar");


        if (!confirmar)
            return;


        // =========================================
        // NOVO ID LOCAL
        // =========================================

        int novoId =
            PacientesRecepcaoDados
                .Pacientes.Count ==
            0

                ? 1

                : PacientesRecepcaoDados
                    .Pacientes
                    .Max(p =>
                        p.Id)
                    + 1;


        // =========================================
        // CADASTRO LOCAL
        // =========================================
        //
        // Continua sendo somente uma simulação
        // local.
        //
        // A integração futura deverá enviar estes
        // dados para a API e relacionar o paciente
        // ao id_responsavel correspondente.

        PacienteRecepcaoItem novoPaciente =
            new()
            {
                Id =
                    novoId,

                Nome =
                    nome,

                DataNascimento =
                    nascimento,

                Responsavel =
                    responsavelSelecionado.Nome,

                TelefoneResponsavel =
                    responsavelSelecionado.Telefone,

                EmailResponsavel =
                    string.Empty,

                Status =
                    "Ativo",

                Consultas =
                    new List<
                        ConsultaPacienteRecepcaoItem>()
            };


        PacientesRecepcaoDados
            .Pacientes
            .Add(
                novoPaciente);


        await DisplayAlertAsync(
            "Paciente cadastrado",

            $"{nome} foi cadastrado(a) e vinculado(a) " +
            $"a {responsavelSelecionado.Nome} com sucesso.",

            "OK");


        await Shell.Current.GoToAsync(
            "..");
    }


    // =============================================
    // LIMPA RESPONSÁVEL ENCONTRADO
    // =============================================

    private void LimparResponsavelEncontrado()
    {
        responsavelSelecionado =
            null;


        ResponsavelNomeLabel.Text =
            string.Empty;


        ResponsavelCpfLabel.Text =
            string.Empty;


        ResponsavelTelefoneLabel.Text =
            string.Empty;


        ResponsavelEncontradoBorder.IsVisible =
            false;
    }


    // =============================================
    // SOMENTE NÚMEROS
    // =============================================

    private static string SomenteNumeros(
        string texto)
    {
        return
            new string(
                texto
                    .Where(char.IsDigit)
                    .ToArray());
    }


    // =============================================
    // CANCELAR
    // =============================================

    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            "..");
    }
}