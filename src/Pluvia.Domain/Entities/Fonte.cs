using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;

namespace Pluvia.Domain.Entities;

/// <summary>Quem registra uma ocorrência. Todo registro "confirmado" tem uma fonte identificada.</summary>
public class Fonte
{
    private Fonte() { Nome = string.Empty; }

    public Fonte(string nome, TipoFonte tipo)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DominioException("Nome da fonte é obrigatório.");
        Nome = nome.Trim();
        Tipo = tipo;
    }

    public int Id { get; private set; }
    public string Nome { get; private set; }
    public TipoFonte Tipo { get; private set; }
}
