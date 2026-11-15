using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Domain.Regras;

public static class PoliticaDeTriagem
{
    /// <summary>
    /// A IA só ajuda a ordenar a fila de análise. Nunca descarta nem rebaixa um relato,
    /// e nunca o publica: "não vi água na foto" não significa "não alaga ali".
    /// </summary>
    public static PrioridadeRelato Priorizar(AvaliacaoImagem? avaliacao) =>
        avaliacao?.Classificacao == ClassificacaoImagem.IndicioDeAguaSobreAVia
            ? PrioridadeRelato.Alta
            : PrioridadeRelato.Normal;
}
