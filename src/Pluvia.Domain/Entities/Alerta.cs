using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Geo;

namespace Pluvia.Domain.Entities;

public class Alerta
{
    private Alerta() { Mensagem = string.Empty; }

    public Alerta(int usuarioId, int ocorrenciaId, TipoAlerta tipo, string mensagem, DateTime agoraUtc)
    {
        UsuarioId = usuarioId;
        OcorrenciaId = ocorrenciaId;
        Tipo = tipo;
        Mensagem = mensagem;
        CriadoEm = agoraUtc;
    }

    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public int OcorrenciaId { get; private set; }
    public TipoAlerta Tipo { get; private set; }
    public string Mensagem { get; private set; }
    public DateTime CriadoEm { get; private set; }
}
