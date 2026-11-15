using Pluvia.Domain.Exceptions;

namespace Pluvia.Domain.Regras;

public record ImagemSanitizada(byte[] Bytes, string Extensao, bool MetadadosRemovidos);
