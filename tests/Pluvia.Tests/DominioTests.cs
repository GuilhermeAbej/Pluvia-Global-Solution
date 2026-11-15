using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Geo;
using Pluvia.Domain.Regras;
using Xunit;

namespace Pluvia.Tests;

public class DominioTests
{
    private static readonly DateTime Agora = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    private static Relato NovoRelato(DateTime? ocorridoEm = null, string? foto = null, bool fotoAtual = true) =>
        new(1, -23.5, -46.6, "agua", TipoEvento.AguaSobreAVia, ocorridoEm ?? Agora, foto, fotoAtual, Agora);

    // ----- Geo -----
    [Fact]
    public void Haversine_distancia_conhecida()
    {
        var d = GeoCalculator.DistanciaMetros(-23.5650, -46.6500, -23.5660, -46.6500);
        Assert.InRange(d, 105, 117);   // 0,001 grau de latitude vale cerca de 111 m
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public void Coordenada_invalida_e_recusada(double lat, double lng) =>
        Assert.Throws<DominioException>(() => GeoCalculator.Validar(lat, lng));

    [Fact]
    public void Caixa_contem_pontos_dentro_do_raio()
    {
        var c = GeoCalculator.Caixa(-23.5650, -46.6500, 500);
        Assert.InRange(-23.5655, c.LatMin, c.LatMax);
        Assert.InRange(-46.6505, c.LngMin, c.LngMax);
    }

    // ----- Detector -----
    [Fact]
    public void Detector_exige_leituras_seguidas_acima_do_limite()
    {
        var d = new DetectorPorLimiteConfirmado(2);
        Assert.False(d.Detectar(new[] { 90 }, 80));
        Assert.False(d.Detectar(new[] { 90, 10 }, 80));
        Assert.True(d.Detectar(new[] { 90, 85 }, 80));
    }

    [Fact]
    public void Detector_normaliza_so_com_leituras_seguidas_abaixo()
    {
        var d = new DetectorPorLimiteConfirmado(2);
        Assert.False(d.Normalizou(new[] { 10, 90 }, 80));
        Assert.True(d.Normalizou(new[] { 10, 20 }, 80));
    }

    // ----- Ocorrência -----
    [Fact]
    public void Ocorrencia_so_e_encerrada_pela_mesma_fonte()
    {
        var o = new Ocorrencia(fonteId: 1, -23.5, -46.6, null, Agora);
        Assert.Throws<RegraDeNegocioException>(() => o.Encerrar(fonteId: 2, Agora));
        Assert.True(o.Ativa);

        o.Encerrar(fonteId: 1, Agora.AddHours(1));
        Assert.Equal(StatusOcorrencia.Encerrada, o.Status);
        Assert.Throws<RegraDeNegocioException>(() => o.Encerrar(1, Agora));
        Assert.Throws<RegraDeNegocioException>(() => o.RegistrarAtualizacao(Agora));
    }

    [Fact]
    public void Ocorrencia_ativa_sem_atualizacao_e_marcada_como_desatualizada()
    {
        var o = new Ocorrencia(1, -23.5, -46.6, null, Agora);
        Assert.False(o.SemAtualizacaoRecente(Agora.AddMinutes(30), TimeSpan.FromMinutes(60)));
        Assert.True(o.SemAtualizacaoRecente(Agora.AddMinutes(61), TimeSpan.FromMinutes(60)));
        o.RegistrarAtualizacao(Agora.AddMinutes(61));
        Assert.False(o.SemAtualizacaoRecente(Agora.AddMinutes(62), TimeSpan.FromMinutes(60)));
    }

    // ----- Relato -----
    [Fact]
    public void Relato_nasce_em_analise() => Assert.Equal(StatusRelato.EmAnalise, NovoRelato().Status);

    [Fact]
    public void Relato_com_foto_exige_confirmacao_de_foto_atual() =>
        Assert.Throws<RegraDeNegocioException>(() => NovoRelato(foto: "x.png", fotoAtual: false));

    [Fact]
    public void Relato_no_futuro_ou_muito_antigo_e_recusado()
    {
        Assert.Throws<RegraDeNegocioException>(() => NovoRelato(Agora.AddHours(1)));
        Assert.Throws<RegraDeNegocioException>(() => NovoRelato(Agora.AddHours(-13)));
        NovoRelato(Agora.AddHours(-11));
    }

    [Fact]
    public void Relato_so_muda_de_status_a_partir_de_em_analise()
    {
        var r = NovoRelato();
        r.Publicar(Agora);
        Assert.Equal(StatusRelato.Publicado, r.Status);
        Assert.Throws<RegraDeNegocioException>(() => r.Publicar(Agora));
        Assert.Throws<RegraDeNegocioException>(() => r.Rejeitar());

        var outro = NovoRelato();
        outro.Rejeitar();
        Assert.Equal(StatusRelato.Rejeitado, outro.Status);
    }

    [Fact]
    public void Relato_excluido_perde_texto_e_foto()
    {
        var r = NovoRelato(foto: "x.png");
        r.Excluir();
        Assert.Equal(StatusRelato.Excluido, r.Status);
        Assert.Null(r.FotoNome);
        Assert.Equal(string.Empty, r.Descricao);
        Assert.Throws<RegraDeNegocioException>(() => r.Excluir());
    }

    // ----- Triagem e validação: a IA nunca publica nem descarta -----
    [Theory]
    [InlineData(ClassificacaoImagem.IndicioDeAguaSobreAVia, PrioridadeRelato.Alta)]
    [InlineData(ClassificacaoImagem.ChuvaOuPisoMolhadoSemIndicioClaro, PrioridadeRelato.Normal)]
    [InlineData(ClassificacaoImagem.Inconclusiva, PrioridadeRelato.Normal)]
    public void Triagem_so_define_prioridade(ClassificacaoImagem c, PrioridadeRelato esperado) =>
        Assert.Equal(esperado, PoliticaDeTriagem.Priorizar(new AvaliacaoImagem(c, 0.9, "t")));

    [Fact]
    public void Sem_foto_a_prioridade_e_normal() =>
        Assert.Equal(PrioridadeRelato.Normal, PoliticaDeTriagem.Priorizar(null));

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(2, false, false)]
    [InlineData(3, false, true)]
    [InlineData(1, true, true)]
    public void Validacao_exige_pessoas_diferentes_ou_fonte_ativa(int pessoas, bool fonteAtiva, bool esperado) =>
        Assert.Equal(esperado, PoliticaDeValidacao.DevePublicar(pessoas, fonteAtiva, 3));

    // ----- Estimador -----
    [Theory]
    [InlineData(0, 10, RiscoEstimado.Indeterminado)]   // sem histórico nunca vira "baixo"
    [InlineData(5, 10, RiscoEstimado.Indeterminado)]
    [InlineData(0, 60, RiscoEstimado.Baixo)]
    [InlineData(2, 60, RiscoEstimado.Moderado)]
    [InlineData(3, 60, RiscoEstimado.Alto)]
    public void Estimador_por_historico(int ocorrencias, int dias, RiscoEstimado esperado) =>
        Assert.Equal(esperado, new EstimadorPorHistorico().Estimar(ocorrencias, dias));

    // ----- Mensagens -----
    [Fact]
    public void Encerramento_traz_fonte_e_hora_e_nao_garante_seguranca()
    {
        var m = MensagemDeAlerta.Encerramento("Casa", "CGE-SP", Agora);   // 12:00 UTC = 09:00 em Brasília
        Assert.Contains("encerrada pela fonte CGE-SP às 09:00", m);
        Assert.Contains("não garante", m);
    }

    [Fact]
    public void Aviso_de_veiculo_nunca_manda_entrar_na_agua()
    {
        var m = MensagemDeAlerta.NovoEventoVeiculo("Carro", "CGE-SP", Agora);
        Assert.Contains("Não entre em área alagada", m);
    }

    [Fact]
    public void Aviso_de_risco_diz_que_e_estimativa()
    {
        var m = MensagemDeAlerta.MudancaDeRisco("Casa", RiscoEstimado.Alto);
        Assert.Contains("estimativa", m);
        Assert.Contains("não confirma água na pista", m);
    }

    // ----- Sanitizador de imagem -----
    [Fact]
    public void Png_perde_metadados_e_continua_valido()
    {
        var r = SanitizadorDeImagem.Sanitizar(Imagens.Png());
        Assert.True(r.MetadadosRemovidos);
        Assert.Equal(".png", r.Extensao);
        Assert.False(Imagens.Contem(r.Bytes, Imagens.SegredoGps));
        Assert.Contains("IEND", System.Text.Encoding.ASCII.GetString(r.Bytes));
    }

    [Fact]
    public void Jpeg_perde_exif_e_mantem_dados_da_imagem()
    {
        var original = Imagens.Jpeg();
        var r = SanitizadorDeImagem.Sanitizar(original);
        Assert.True(r.MetadadosRemovidos);
        Assert.Equal(".jpg", r.Extensao);
        Assert.False(Imagens.Contem(r.Bytes, Imagens.SegredoGps));
        Assert.True(r.Bytes.Length < original.Length);
        Assert.Equal(new byte[] { 0xFF, 0xD9 }, r.Bytes[^2..]);
    }

    [Fact]
    public void Imagem_sem_metadados_passa_inalterada()
    {
        var original = Imagens.Png(comMetadados: false);
        var r = SanitizadorDeImagem.Sanitizar(original);
        Assert.False(r.MetadadosRemovidos);
        Assert.Equal(original, r.Bytes);
    }

    [Fact]
    public void Sanitizador_recusa_texto_arquivo_pequeno_e_imagem_truncada()
    {
        Assert.Throws<ImagemInvalidaException>(() => SanitizadorDeImagem.Sanitizar(new byte[200]));
        Assert.Throws<ImagemInvalidaException>(() => SanitizadorDeImagem.Sanitizar(Imagens.Png()[..20]));
        var truncada = Imagens.Png()[..70];
        Assert.Throws<ImagemInvalidaException>(() => SanitizadorDeImagem.Sanitizar(truncada));
        var jpegCortado = Imagens.Jpeg()[..30];
        Assert.Throws<ImagemInvalidaException>(() => SanitizadorDeImagem.Sanitizar(jpegCortado));
    }

    // ----- Outras entidades -----
    [Fact]
    public void Usuario_exige_consentimento() =>
        Assert.Throws<RegraDeNegocioException>(() => new Usuario("Ana", false, Agora));

    [Theory]
    [InlineData(50)]
    [InlineData(5000)]
    public void Area_e_veiculo_recusam_raio_fora_do_intervalo(int raio)
    {
        Assert.Throws<DominioException>(() => new AreaMonitorada(1, "Casa", -23.5, -46.6, raio));
        Assert.Throws<DominioException>(() => new Veiculo(1, "Carro", raio));
    }

    [Fact]
    public void Silencio_tem_limite_de_24_horas()
    {
        var a = new AreaMonitorada(1, "Casa", -23.5, -46.6, 300);
        Assert.Throws<RegraDeNegocioException>(() => a.Silenciar(TimeSpan.FromHours(25), Agora));
        a.Silenciar(TimeSpan.FromMinutes(30), Agora);
        Assert.True(a.EstaSilenciada(Agora.AddMinutes(10)));
        Assert.False(a.EstaSilenciada(Agora.AddMinutes(31)));
    }
}
