using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace PediAgenda.Controls;

// Componente visual provisorio. A sessao NAO e encerrada nesta etapa.
public sealed class BotaoLogoff : ImageButton
{
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
        // Evita simular um logout que ainda nao limpa a sessao.
        if (Shell.Current is not null)
        {
            await Shell.Current.DisplayAlert(
                "Sair da conta",
                "O encerramento de sessao sera ativado quando a autenticacao estiver integrada.",
                "Entendi");
        }
    }
}
