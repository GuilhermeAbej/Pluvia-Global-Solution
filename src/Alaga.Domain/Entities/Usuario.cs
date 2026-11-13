using Alaga.Domain.Exceptions;

namespace Alaga.Domain.Entities;

public class Usuario
{
    private Usuario() { Nome = string.Empty; }

    public Usuario(string nome, bool consentimentoLgpd, DateTime agoraUtc)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DominioException("Nome é obrigatório.");
        if (!consentimentoLgpd) throw new RegraDeNegocioException("É necessário aceitar o tratamento de dados (LGPD).");
        Nome = nome.Trim();
        CriadoEm = agoraUtc;
    }

    public int Id { get; private set; }
    public string Nome { get; private set; }
    public DateTime CriadoEm { get; private set; }
}
