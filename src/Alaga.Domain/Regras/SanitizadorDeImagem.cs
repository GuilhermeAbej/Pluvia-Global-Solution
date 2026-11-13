using Alaga.Domain.Exceptions;

namespace Alaga.Domain.Regras;

public record ImagemSanitizada(byte[] Bytes, string Extensao, bool MetadadosRemovidos);

/// <summary>
/// Valida a imagem pelos bytes (não pela extensão) e remove metadados (EXIF com GPS, XMP, comentários).
/// Aceita JPEG e PNG. Não desfoca placas nem rostos: por isso a foto só é vista pelo autor e pela fonte oficial.
/// </summary>
public static class SanitizadorDeImagem
{
    public const int TamanhoMinimoBytes = 64;

    public static ImagemSanitizada Sanitizar(byte[] b)
    {
        if (b.Length < TamanhoMinimoBytes)
            throw new ImagemInvalidaException("Imagem pequena demais para ser analisada.");

        if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return Jpeg(b);
        if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return Png(b);
        throw new ImagemInvalidaException("O arquivo não é uma imagem JPEG ou PNG válida.");
    }

    private static ImagemSanitizada Jpeg(byte[] b)
    {
        var saida = new MemoryStream(b.Length);
        saida.WriteByte(0xFF); saida.WriteByte(0xD8);
        var i = 2;
        var removeu = false;

        while (i < b.Length)
        {
            if (b[i] != 0xFF) throw Malformada();
            while (i + 1 < b.Length && b[i + 1] == 0xFF) i++;   // bytes de preenchimento
            if (i + 1 >= b.Length) throw Malformada();
            var marcador = b[i + 1];

            if (marcador == 0xDA)                               // início dos dados da imagem: copia o resto
            {
                saida.Write(b, i, b.Length - i);
                return new ImagemSanitizada(saida.ToArray(), ".jpg", removeu);
            }
            if (marcador == 0xD9) { saida.WriteByte(0xFF); saida.WriteByte(0xD9); i += 2; continue; }
            if (marcador is >= 0xD0 and <= 0xD7 or 0x01) { saida.Write(b, i, 2); i += 2; continue; }

            if (i + 3 >= b.Length) throw Malformada();
            var tamanho = (b[i + 2] << 8) | b[i + 3];
            if (tamanho < 2 || i + 2 + tamanho > b.Length) throw Malformada();

            // E1 = EXIF/XMP, ED = Photoshop, FE = comentário
            if (marcador is 0xE1 or 0xED or 0xFE) removeu = true;
            else saida.Write(b, i, 2 + tamanho);
            i += 2 + tamanho;
        }
        throw Malformada();
    }

    private static ImagemSanitizada Png(byte[] b)
    {
        var saida = new MemoryStream(b.Length);
        saida.Write(b, 0, 8);
        var i = 8;
        var removeu = false;
        var viuIhdr = false;

        while (i + 12 <= b.Length)
        {
            var tamanho = (b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3];
            if (tamanho < 0 || (long)i + 12 + tamanho > b.Length) throw Malformada();
            var tipo = System.Text.Encoding.ASCII.GetString(b, i + 4, 4);
            if (i == 8 && tipo != "IHDR") throw Malformada();
            if (tipo == "IHDR") viuIhdr = true;

            if (tipo is "eXIf" or "tEXt" or "iTXt" or "zTXt" or "tIME") removeu = true;
            else saida.Write(b, i, 12 + tamanho);

            i += 12 + tamanho;
            if (tipo == "IEND")
                return viuIhdr ? new ImagemSanitizada(saida.ToArray(), ".png", removeu) : throw Malformada();
        }
        throw Malformada();
    }

    private static ImagemInvalidaException Malformada() => new("A imagem está corrompida ou incompleta.");
}
