namespace Alaga.Domain.Enums;

public enum TipoFonte { Oficial = 1, Sensor = 2 }

/// <summary>Só existem ocorrências registradas por uma fonte identificada (oficial ou sensor).</summary>
public enum StatusOcorrencia { Ativa = 1, Encerrada = 2 }

public enum StatusRelato { EmAnalise = 1, Publicado = 2, Rejeitado = 3, Excluido = 4 }

public enum TipoEvento { AguaSobreAVia = 1, ViaBloqueada = 2, VeiculoAtingido = 3, Outro = 9 }

public enum StatusSensor { Ativo = 1, Inativo = 2, Defeito = 3 }

public enum TipoAlerta { NovoEvento = 1, Encerramento = 2, MudancaDeRisco = 3 }

public enum PrioridadeRelato { Normal = 1, Alta = 2 }

/// <summary>Categorias sugeridas pela triagem de imagem (seção 6 do documento). Nunca é decisão final.</summary>
public enum ClassificacaoImagem
{
    Inconclusiva = 0,
    ChuvaOuPisoMolhadoSemIndicioClaro = 1,
    IndicioDeAguaSobreAVia = 2
}

/// <summary>"Indeterminado" existe de propósito: falta de dado nunca vira "seguro".</summary>
public enum RiscoEstimado { Indeterminado = 0, Baixo = 1, Moderado = 2, Alto = 3 }
