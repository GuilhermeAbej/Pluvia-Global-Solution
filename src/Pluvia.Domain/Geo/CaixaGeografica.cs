using Pluvia.Domain.Exceptions;

namespace Pluvia.Domain.Geo;

public readonly record struct CaixaGeografica(double LatMin, double LatMax, double LngMin, double LngMax);
