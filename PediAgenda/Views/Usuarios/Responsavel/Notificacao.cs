namespace PediAgenda.Views.Usuarios.Responsavel;

public class Notificacao
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Titulo { get; set; } = string.Empty;

    public string Mensagem { get; set; } = string.Empty;

    public DateTime DataHora { get; set; }

    public bool Lida { get; set; }

    public string DataHoraFormatada =>
        DataHora.ToString("dd/MM/yyyy 'às' HH:mm");
}