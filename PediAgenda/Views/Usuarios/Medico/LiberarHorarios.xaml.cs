
using System.Collections.ObjectModel;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(nameof(IdMedico), "IdMedico")]
public partial class LiberarHorarios : ContentPage
{
    public ObservableCollection<BloqueioExibicao> Bloqueios
    { get; set; } = new();

    private int idMedicoAgenda = 1;
    private string idMedico = "1";

    public string IdMedico
    {
        get => idMedico;

        set
        {
            idMedico = value;

            if (int.TryParse(value, out int id) && id > 0)
            {
                idMedicoAgenda = id;
            }

            if (BloqueiosCollectionView != null)
            {
                CarregarBloqueios();
            }
        }
    }

    public LiberarHorarios()
    {
        InitializeComponent();

        CarregarBloqueios();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        CarregarBloqueios();
    }

    // ============================================
    // CARREGAR BLOQUEIOS DO MÉDICO
    // ============================================

    private void CarregarBloqueios()
    {
        Bloqueios.Clear();

        foreach (BloqueioHorario bloqueio
                 in BloqueiosMedico.Bloqueios
                     .Where(b => b.IdMedico == idMedicoAgenda))
        {
            Bloqueios.Add(new BloqueioExibicao
            {
                Id = bloqueio.Id,

                PeriodoData =
                    bloqueio.DataInicial.Date ==
                    bloqueio.DataFinal.Date

                        ? bloqueio.DataInicial.ToString("dd/MM/yyyy")

                        : $"{bloqueio.DataInicial:dd/MM/yyyy} até " +
                          $"{bloqueio.DataFinal:dd/MM/yyyy}",

                PeriodoHorario =
                    $"{bloqueio.HorarioInicial:hh\\:mm} às " +
                    $"{bloqueio.HorarioFinal:hh\\:mm}"
            });
        }

        BloqueiosCollectionView.ItemsSource = Bloqueios;
    }

    // ============================================
    // LIBERAR PERÍODO BLOQUEADO
    // ============================================

    private async void LiberarPeriodoButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button ||
            button.BindingContext is not BloqueioExibicao item)
        {
            return;
        }

        // Encontrar o bloqueio selecionado
        BloqueioHorario? bloqueio =
            BloqueiosMedico.Bloqueios
                .FirstOrDefault(b => b.Id == item.Id);

        if (bloqueio == null)
        {
            await DisplayAlertAsync(
                "Atenção",
                "O período selecionado não foi encontrado.",
                "OK");

            CarregarBloqueios();
            return;
        }

        // ========================================
        // CONFIRMAÇÃO
        // ========================================

        bool confirmar = await DisplayAlertAsync(
            "Liberar período",

            $"Deseja liberar todo este período?\n\n" +
            $"Data: {item.PeriodoData}\n" +
            $"Horário: {item.PeriodoHorario}\n\n" +
            "As consultas canceladas pelo bloqueio " +
            "não serão restauradas.",

            "Liberar",
            "Cancelar");

        if (!confirmar)
        {
            return;
        }

        // ========================================
        // LIBERAÇÃO LOCAL
        // ========================================

        HorarioAgendamentoDados.LiberarBloqueio(
            bloqueio.Id);

        BloqueiosMedico.Bloqueios.Remove(
            bloqueio);

        // Atualizar a lista
        CarregarBloqueios();

        // ========================================
        // CONFIRMAÇÃO FINAL
        // ========================================

        await DisplayAlertAsync(
            "Período liberado",
            "O período foi liberado com sucesso.",
            "OK");
    }

    // ============================================
    // BOTÃO VOLTAR
    // ============================================

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // ============================================
    // MODELO DE EXIBIÇÃO
    // ============================================

    public class BloqueioExibicao
    {
        public Guid Id { get; set; }

        public string PeriodoData { get; set; } =
            string.Empty;

        public string PeriodoHorario { get; set; } =
            string.Empty;
    }
}
