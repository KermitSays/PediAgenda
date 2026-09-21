using PediAgenda.Views;
using PediAgenda.Views.Cadastro;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        //ROTAS LOGIN E CADASTRO
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

        //ROTAS RESPONSÁVEL
        Routing.RegisterRoute(
            nameof(Views.Usuarios.Responsavel.MenuResponsavel),
            typeof(Views.Usuarios.Responsavel.MenuResponsavel));

        Routing.RegisterRoute(
            nameof(Views.Usuarios.Responsavel.AgendarConsulta),
            typeof(Views.Usuarios.Responsavel.AgendarConsulta));

        Routing.RegisterRoute(
            nameof(Views.Usuarios.Responsavel.AgendarMedico),
            typeof(Views.Usuarios.Responsavel.AgendarMedico));

        Routing.RegisterRoute(
            nameof(Views.Usuarios.Responsavel.AgendarDataHorario),
            typeof(Views.Usuarios.Responsavel.AgendarDataHorario));

        Routing.RegisterRoute(
            nameof(Views.Usuarios.Responsavel.AgendarTipoAtendimento),
            typeof(Views.Usuarios.Responsavel.AgendarTipoAtendimento));

        Routing.RegisterRoute(
            nameof(Views.Usuarios.Responsavel.AgendarConfirmacao),
            typeof(Views.Usuarios.Responsavel.AgendarConfirmacao));

        Routing.RegisterRoute(
            nameof(Views.Usuarios.Responsavel.AgendamentoConcluido),
            typeof(Views.Usuarios.Responsavel.AgendamentoConcluido));


    }
}