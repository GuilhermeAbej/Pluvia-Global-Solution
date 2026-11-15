using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Geo;

namespace Pluvia.Domain.Entities;

/// <summary>Resultado da triagem de imagem (objeto de valor). É só uma sugestão de prioridade.</summary>
public record AvaliacaoImagem(ClassificacaoImagem Classificacao, double Confianca, string Modelo);
