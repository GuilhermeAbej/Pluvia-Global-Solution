using Pluvia.Api.Data;
using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Regras;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Pluvia.Tests;

/// <summary>Imagens mínimas, mas estruturalmente válidas, com metadados para testar a remoção.</summary>
public static class Imagens
{
    public const string SegredoGps = "GPS=-23.5,-46.6";

    public static byte[] Png(bool comMetadados = true)
    {
        var o = new List<byte> { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        Chunk(o, "IHDR", new byte[13]);
        if (comMetadados) Chunk(o, "tEXt", System.Text.Encoding.ASCII.GetBytes(SegredoGps));
        Chunk(o, "IDAT", new byte[20]);
        Chunk(o, "IEND", Array.Empty<byte>());
        return o.ToArray();
    }

    public static byte[] Jpeg(bool comExif = true)
    {
        var o = new List<byte> { 0xFF, 0xD8 };
        Segmento(o, 0xE0, new byte[14]);
        if (comExif) Segmento(o, 0xE1, System.Text.Encoding.ASCII.GetBytes("Exif\0\0" + SegredoGps));
        Segmento(o, 0xDB, new byte[67]);
        Segmento(o, 0xDA, new byte[10]);
        o.AddRange(new byte[40]);   // dados da imagem
        o.AddRange(new byte[] { 0xFF, 0xD9 });
        return o.ToArray();
    }

    private static void Chunk(List<byte> o, string tipo, byte[] dados)
    {
        o.AddRange(new[] { (byte)(dados.Length >> 24), (byte)(dados.Length >> 16), (byte)(dados.Length >> 8), (byte)dados.Length });
        o.AddRange(System.Text.Encoding.ASCII.GetBytes(tipo));
        o.AddRange(dados);
        o.AddRange(new byte[4]);   // CRC (não verificado pelo sanitizador)
    }

    private static void Segmento(List<byte> o, byte marcador, byte[] dados)
    {
        var tam = dados.Length + 2;
        o.AddRange(new[] { (byte)0xFF, marcador, (byte)(tam >> 8), (byte)tam });
        o.AddRange(dados);
    }

    public static bool Contem(byte[] bytes, string texto) =>
        System.Text.Encoding.ASCII.GetString(bytes).Contains(texto);
}
