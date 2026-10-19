using Microsoft.Maui.Controls;

namespace PediAgenda.Controls;

// Voltar padrao das telas secundarias. Retornos de modais mantem seus proprios handlers.
public sealed class BotaoVoltar : Button
{
    public BotaoVoltar()
    {
        Style = (Style)Application.Current!.Resources["BotaoVoltarSimples"];
        Text = "Voltar";
        Clicked += async (_, _) => await Shell.Current.GoToAsync("..");
    }
}
