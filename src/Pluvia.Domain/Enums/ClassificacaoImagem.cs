namespace Pluvia.Domain.Enums;

/// <summary>Categorias sugeridas pela triagem de imagem (seção 6 do documento). Nunca é decisão final.</summary>
public enum ClassificacaoImagem
{
    Inconclusiva = 0,
    ChuvaOuPisoMolhadoSemIndicioClaro = 1,
    IndicioDeAguaSobreAVia = 2
}
