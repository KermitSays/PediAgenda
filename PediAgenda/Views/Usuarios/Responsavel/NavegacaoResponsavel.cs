namespace PediAgenda.Views.Usuarios.Responsavel;

public static class NavegacaoResponsavel
{
    // VOLTA AO MENU PRINCIPAL DO RESPONSÁVEL
    //
    // Primeiro limpa todo o fluxo anterior.
    // Depois abre novamente o menu.

    public static async Task IrParaMenuAsync()
    {
        await Shell.Current.Navigation
            .PopToRootAsync(false);


        await Shell.Current.GoToAsync(
            nameof(MenuResponsavel));
    }


    public static async Task IrParaMinhasConsultasAsync()
    {
        await Shell.Current.Navigation
            .PopToRootAsync(false);


        await Shell.Current.GoToAsync(
            nameof(MenuResponsavel));


        await Shell.Current.GoToAsync(
            nameof(MinhasConsultas));
    }
}