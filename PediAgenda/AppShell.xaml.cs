using PediAgenda.Views;
using PediAgenda.Views.Cadastro;

namespace PediAgenda;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        //ROTAS
        Routing.RegisterRoute(
            nameof(Views.Login.LoginResponsavel),
            typeof(Views.Login.LoginResponsavel));

        Routing.RegisterRoute(
            nameof(Views.Login.LoginFuncionario),
            typeof(Views.Login.LoginFuncionario));

        Routing.RegisterRoute(
            nameof(Views.Cadastro.CadastroCampo),
            typeof(Views.Cadastro.CadastroCampo));

        Routing.RegisterRoute(
            nameof(Views.Cadastro.CadastroConcluido),
            typeof(Views.Cadastro.CadastroConcluido));
    }
}