using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using PediAgenda.Dados;

namespace PediAgenda.Controls;

public sealed class BotaoLogoff : ImageButton
{
    private bool saindo;
    public BotaoLogoff()
    {
        Source = "logoff_icon.png";
        BackgroundColor = Colors.Transparent;
        BorderColor = Colors.Transparent;
        BorderWidth = 0;
        WidthRequest = 44;
        HeightRequest = 44;
        Padding = new Thickness(8);
        Aspect = Aspect.AspectFit;
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.Start;
        SemanticProperties.SetDescription(this, "Sair da conta");

        Clicked += AoClicar;
    }

    private async void AoClicar(object? sender, EventArgs e)
    {
        if (saindo || Shell.Current?.Window is not { } janela) return;
        saindo = true;
        try
        {
            SecureStorage.Default.Remove("pediagenda_token");
            SessaoUsuario.Limpar();
            // Uma Shell nova descarta a pilha autenticada e os dados das telas.
            janela.Page = new AppShell();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Sair da conta",
                "Não foi possível encerrar a sessão. Tente novamente.", "OK");
        }
        finally
        {
            saindo = false;
        }
    }
}

