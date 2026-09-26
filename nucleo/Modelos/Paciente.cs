namespace PediAgenda.Nucleo.Modelos;

/// <summary>
/// A criança atendida. Espelha a tabela `paciente` de banco/03_modelo_completo.sql:
/// cada paciente pertence a um responsável, e um responsável pode ter vários.
/// </summary>
public class Paciente
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public DateOnly DataNascimento { get; set; }
    public int IdResponsavel { get; set; }

    public int IdadeEm(DateOnly data)
    {
        var idade = data.Year - DataNascimento.Year;
        if (data < DataNascimento.AddYears(idade))
            idade--;
        return idade;
    }
}
