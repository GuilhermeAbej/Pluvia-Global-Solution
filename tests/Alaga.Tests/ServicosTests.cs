using Alaga.Api.Dtos;
using Alaga.Api.Services;
using Alaga.Domain.Enums;
using Alaga.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Alaga.Tests;

public class SensorETestesDeAlertas
{
    [Fact]
    public async Task Leitura_isolada_acima_do_limite_nao_cria_ocorrencia()
    {
        using var a = new Ambiente();
        await a.Leituras(90);
        Assert.Empty(await a.Db.Ocorrencias.ToListAsync());
    }

    [Fact]
    public async Task Duas_leituras_seguidas_abrem_ocorrencia_ativa_com_fonte_historico_e_aviso()
    {
        using var a = new Ambiente();
        var ana = await a.NovoUsuario();
        await a.LocalPerto(ana);

        await a.Leituras(90, 95);

        var o = await a.Db.Ocorrencias.SingleAsync();
        Assert.Equal(StatusOcorrencia.Ativa, o.Status);
        Assert.Equal(a.FonteDoSensorId, o.FonteId);
        var historico = await a.Ocorrencias.HistoricoAsync(o.Id);
        Assert.Single(historico);
        Assert.Equal(StatusOcorrencia.Ativa, historico[0].Status);
        Assert.Single(a.Notificador.Enviados);
        Assert.Equal(TipoAlerta.NovoEvento, a.Notificador.Enviados[0].Tipo);
    }

    [Fact]
    public async Task Leituras_seguintes_nao_duplicam_ocorrencia_nem_aviso()
    {
        using var a = new Ambiente();
        var ana = await a.NovoUsuario();
        await a.LocalPerto(ana);

        await a.Leituras(90, 95, 99, 100);

        Assert.Equal(1, await a.Db.Ocorrencias.CountAsync());
        Assert.Single(a.Notificador.Enviados);
    }

    [Fact]
    public async Task Agua_abaixo_do_limite_encerra_registra_historico_e_avisa_sem_prometer_seguranca()
    {
        using var a = new Ambiente();
        var ana = await a.NovoUsuario();
        await a.LocalPerto(ana);

        await a.Leituras(90, 95, 10, 12);

        var o = await a.Db.Ocorrencias.SingleAsync();
        Assert.Equal(StatusOcorrencia.Encerrada, o.Status);
        Assert.Equal(2, (await a.Ocorrencias.HistoricoAsync(o.Id)).Count);
        Assert.Equal(new[] { TipoAlerta.NovoEvento, TipoAlerta.Encerramento },
            a.Notificador.Enviados.Select(x => x.Tipo).ToArray());
        Assert.Contains("não garante", a.Notificador.Enviados[1].Mensagem);
        Assert.Contains("Sensor ESP32-001", a.Notificador.Enviados[1].Mensagem);
    }

    [Fact]
    public async Task Leitura_intermediaria_mantem_a_ocorrencia_atualizada()
    {
        using var a = new Ambiente();
        await a.Leituras(90, 95);
        var antes = (await a.Db.Ocorrencias.AsNoTracking().SingleAsync()).AtualizadaEm;

        await a.Leituras(70);   // abaixo do limite, mas uma única leitura: não encerra

        var o = await a.Db.Ocorrencias.AsNoTracking().SingleAsync();
        Assert.Equal(StatusOcorrencia.Ativa, o.Status);
        Assert.True(o.AtualizadaEm > antes);
    }

    [Fact]
    public async Task Sensor_inexistente_e_sensor_com_defeito_sao_recusados()
    {
        using var a = new Ambiente();
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            a.Sensores.RegistrarLeituraAsync(new LeituraRequest("XX", 10)));

        a.Sensor.AlterarStatus(StatusSensor.Defeito);
        await a.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            a.Sensores.RegistrarLeituraAsync(new LeituraRequest("ESP32-001", 10)));
    }

    [Fact]
    public async Task Local_silenciado_ou_distante_nao_recebe_aviso()
    {
        using var a = new Ambiente();
        var silenciada = await a.NovoUsuario("Silenciada");
        var longe = await a.NovoUsuario("Longe");
        var local = await a.LocalPerto(silenciada);
        await a.Locais.SilenciarAsync(silenciada, local.Id, 60);
        await a.LocalPerto(longe, lat: -23.70, lng: -46.80);

        await a.Leituras(90, 95);

        Assert.Empty(a.Notificador.Enviados);
    }

    [Fact]
    public async Task Varios_locais_do_mesmo_usuario_geram_um_unico_aviso()
    {
        using var a = new Ambiente();
        var ana = await a.NovoUsuario();
        await a.LocalPerto(ana);
        await a.Locais.CriarAsync(ana, new CriarLocalRequest("Trabalho", -23.5651, -46.6501, 300));

        await a.Leituras(90, 95);

        Assert.Single(a.Notificador.Enviados);
    }

    [Fact]
    public async Task Veiculo_estacionado_perto_recebe_aviso_especifico()
    {
        using var a = new Ambiente();
        var ana = await a.NovoUsuario();
        var v = await a.Veiculos.CriarAsync(ana, new CriarVeiculoRequest("Meu carro", 300));
        await a.Veiculos.AtualizarPosicaoAsync(ana, v.Id, new PosicaoRequest(-23.5652, -46.6501));
        await a.LocalPerto(ana);   // também tem um local na região: vale o veículo

        await a.Leituras(90, 95);

        var aviso = Assert.Single(a.Notificador.Enviados);
        Assert.Contains("Meu carro", aviso.Mensagem);
        Assert.Contains("Não entre em área alagada", aviso.Mensagem);
    }

    [Fact]
    public async Task Veiculo_com_avisos_desligados_ou_sem_posicao_ou_longe_nao_recebe()
    {
        using var a = new Ambiente();
        var desligado = await a.NovoUsuario("Desligado");
        var semPosicao = await a.NovoUsuario("SemPosicao");
        var longe = await a.NovoUsuario("Longe");

        var v1 = await a.Veiculos.CriarAsync(desligado, new CriarVeiculoRequest("A"));
        await a.Veiculos.AtualizarPosicaoAsync(desligado, v1.Id, new PosicaoRequest(-23.5652, -46.6501));
        await a.Veiculos.DefinirAvisosAsync(desligado, v1.Id, false);
        await a.Veiculos.CriarAsync(semPosicao, new CriarVeiculoRequest("B"));
        var v3 = await a.Veiculos.CriarAsync(longe, new CriarVeiculoRequest("C", 300));
        await a.Veiculos.AtualizarPosicaoAsync(longe, v3.Id, new PosicaoRequest(-23.70, -46.80));

        await a.Leituras(90, 95);

        Assert.Empty(a.Notificador.Enviados);
    }

    [Fact]
    public async Task Aumento_de_risco_gera_aviso_de_mudanca_de_risco_uma_vez()
    {
        using var a = new Ambiente();
        var ana = await a.NovoUsuario();
        await a.LocalPerto(ana);                 // risco de partida: Indeterminado (sem histórico)

        await a.Leituras(90, 95, 10, 12);        // ocorrência 1
        a.Relogio.Avancar(TimeSpan.FromDays(40));
        await a.Leituras(90, 95);                // ocorrência 2: agora há 40 dias de histórico e 2 eventos

        var tipos = a.Notificador.Enviados.Select(x => x.Tipo).ToList();
        Assert.Equal(1, tipos.Count(t => t == TipoAlerta.MudancaDeRisco));
        var risco = a.Notificador.Enviados.Single(x => x.Tipo == TipoAlerta.MudancaDeRisco);
        Assert.Contains("Moderado", risco.Mensagem);
        Assert.Contains("estimativa", risco.Mensagem);
    }

    [Fact]
    public async Task Lista_de_alertas_traz_fonte_e_link_do_evento()
    {
        using var a = new Ambiente();
        var ana = await a.NovoUsuario();
        await a.LocalPerto(ana);
        await a.Leituras(90, 95);

        var alerta = Assert.Single(await a.Alertas.ListarDoUsuarioAsync(ana));

        Assert.Equal("Sensor ESP32-001", alerta.Fonte);
        Assert.Equal($"/api/ocorrencias/{alerta.OcorrenciaId}", alerta.Link);
    }
}

public class RelatosTests
{
    [Fact]
    public async Task Relato_nasce_em_analise_e_nao_aparece_no_mapa()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();

        var r = await a.Relatar(u);

        Assert.Equal(StatusRelato.EmAnalise, r.Status);
        Assert.Contains("em análise", r.Aviso);
        Assert.Empty(await a.Relatos.ListarPublicosAsync(-23.60, -46.70, 1000));
    }

    [Fact]
    public async Task Tres_pessoas_diferentes_no_mesmo_ponto_publicam_todos_os_relatos()
    {
        using var a = new Ambiente();
        var u1 = await a.NovoUsuario("A"); var u2 = await a.NovoUsuario("B"); var u3 = await a.NovoUsuario("C");

        await a.Relatar(u1);
        var r2 = await a.Relatar(u2, -23.6002, -46.7002);
        Assert.Equal(StatusRelato.EmAnalise, r2.Status);
        var r3 = await a.Relatar(u3, -23.6001, -46.7001);

        Assert.Equal(StatusRelato.Publicado, r3.Status);
        Assert.Equal(3, (await a.Relatos.ListarPublicosAsync(-23.60, -46.70, 1000)).Count);
        Assert.Empty(await a.Db.Ocorrencias.ToListAsync());   // relato nunca vira ocorrência oficial
    }

    [Fact]
    public async Task Relato_publicado_e_rotulado_como_comunitario_e_nao_oficial()
    {
        using var a = new Ambiente();
        var u1 = await a.NovoUsuario("A"); var u2 = await a.NovoUsuario("B"); var u3 = await a.NovoUsuario("C");
        await a.Relatar(u1); await a.Relatar(u2); await a.Relatar(u3);

        var publico = (await a.Relatos.ListarPublicosAsync(-23.60, -46.70, 1000))[0];

        Assert.Contains("relato comunitário", publico.Rotulo);
        Assert.Contains("não é registro oficial", publico.Rotulo);
    }

    [Fact]
    public async Task Mesma_pessoa_nao_relata_o_mesmo_ponto_duas_vezes()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();
        await a.Relatar(u);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => a.Relatar(u));
    }

    [Fact]
    public async Task Fonte_identificada_com_ocorrencia_ativa_perto_publica_o_relato()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();
        await a.Ocorrencias.RegistrarOficialAsync(new RegistrarOcorrenciaRequest(a.OficialId, -23.60, -46.70, "alagamento"));

        var r = await a.Relatar(u);

        Assert.Equal(StatusRelato.Publicado, r.Status);
    }

    [Fact]
    public async Task Ia_indicando_agua_marca_prioridade_mas_nao_publica()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();
        a.Analisador.Resultado = ClassificacaoImagem.IndicioDeAguaSobreAVia;

        var r = await a.Relatar(u, foto: Imagens.Png());

        Assert.Equal(PrioridadeRelato.Alta, r.Prioridade);
        Assert.Equal(StatusRelato.EmAnalise, r.Status);
    }

    [Fact]
    public async Task Ia_sem_indicio_claro_nao_descarta_o_relato()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();
        a.Analisador.Resultado = ClassificacaoImagem.ChuvaOuPisoMolhadoSemIndicioClaro;

        var r = await a.Relatar(u, foto: Imagens.Png());

        Assert.Equal(1, await a.Db.Relatos.CountAsync(x => x.Status == StatusRelato.EmAnalise));
        Assert.Equal(PrioridadeRelato.Normal, r.Prioridade);
    }

    [Fact]
    public async Task Foto_e_salva_sem_metadados_e_a_resposta_avisa()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();

        var r = await a.Relatar(u, foto: Imagens.Jpeg());

        Assert.True(r.MetadadosRemovidos);
        Assert.False(Imagens.Contem(a.Fotos.UltimoConteudo!, Imagens.SegredoGps));
    }

    [Fact]
    public async Task Arquivo_que_nao_e_imagem_e_recusado_e_nao_e_salvo()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();

        await Assert.ThrowsAsync<ImagemInvalidaException>(() => a.Relatar(u, foto: new byte[300]));

        Assert.Equal(0, a.Fotos.Salvas);
        Assert.Empty(await a.Db.Relatos.ToListAsync());
    }

    [Fact]
    public async Task Foto_grande_demais_e_recusada()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();
        a.Opcoes.FotoMaxBytes = 50;

        await Assert.ThrowsAsync<ImagemInvalidaException>(() => a.Relatar(u, foto: Imagens.Png()));
    }

    [Fact]
    public async Task Foto_sem_confirmar_que_e_atual_e_recusada_e_nao_deixa_arquivo_orfao()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => a.Relatar(u, foto: Imagens.Png(), fotoAtual: false));

        Assert.Equal(a.Fotos.Salvas, a.Fotos.Excluidas);
        Assert.Empty(await a.Db.Relatos.ToListAsync());
    }

    [Fact]
    public async Task Relato_com_horario_muito_antigo_e_recusado()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();
        var antigo = a.Relogio.GetUtcNow().UtcDateTime.AddHours(-20);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => a.Relatar(u, ocorridoEm: antigo));
    }

    [Fact]
    public async Task Limite_de_relatos_por_hora_e_aplicado_e_libera_depois()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();
        for (var i = 0; i < a.Opcoes.RelatosPorHora; i++)
            await a.Relatar(u, lat: -23.60 + i * 0.01);   // pontos distintos, para não cair na regra de duplicidade

        await Assert.ThrowsAsync<LimiteExcedidoException>(() => a.Relatar(u, lat: -23.90));

        a.Relogio.Avancar(TimeSpan.FromMinutes(61));
        await a.Relatar(u, lat: -23.90);
    }

    [Fact]
    public async Task Autor_acompanha_o_status_e_outro_usuario_nao_ve()
    {
        using var a = new Ambiente();
        var autor = await a.NovoUsuario("Autor");
        var outro = await a.NovoUsuario("Outro");
        var r = await a.Relatar(autor);

        var detalhe = await a.Relatos.ObterDoAutorAsync(autor, r.RelatoId);
        Assert.Equal("Relato em análise", detalhe.Rotulo);
        Assert.Single(await a.Relatos.ListarDoAutorAsync(autor));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => a.Relatos.ObterDoAutorAsync(outro, r.RelatoId));
    }

    [Fact]
    public async Task Autor_exclui_o_relato_e_a_foto_e_apagada()
    {
        using var a = new Ambiente();
        var autor = await a.NovoUsuario("Autor");
        var outro = await a.NovoUsuario("Outro");
        var r = await a.Relatar(autor, foto: Imagens.Png());

        await Assert.ThrowsAsync<AcessoNegadoException>(() => a.Relatos.ExcluirAsync(outro, r.RelatoId));
        await a.Relatos.ExcluirAsync(autor, r.RelatoId);

        Assert.Equal(1, a.Fotos.Excluidas);
        Assert.Empty(await a.Relatos.ListarDoAutorAsync(autor));
        var salvo = await a.Db.Relatos.AsNoTracking().SingleAsync();
        Assert.Equal(StatusRelato.Excluido, salvo.Status);
        Assert.Null(salvo.FotoNome);
    }

    [Fact]
    public async Task Foto_so_para_o_autor_ou_fonte_oficial()
    {
        using var a = new Ambiente();
        var autor = await a.NovoUsuario("Autor");
        var outro = await a.NovoUsuario("Outro");
        var r = await a.Relatar(autor, foto: Imagens.Png());

        await a.Relatos.ObterFotoAsync(r.RelatoId, autor, false);
        await a.Relatos.ObterFotoAsync(r.RelatoId, null, true);
        await Assert.ThrowsAsync<AcessoNegadoException>(() => a.Relatos.ObterFotoAsync(r.RelatoId, outro, false));
    }

    [Fact]
    public async Task Moderacao_publica_ou_rejeita_relato_em_analise()
    {
        using var a = new Ambiente();
        var u1 = await a.NovoUsuario("A"); var u2 = await a.NovoUsuario("B");
        var r1 = await a.Relatar(u1);
        var r2 = await a.Relatar(u2, lat: -23.80);

        Assert.Equal(StatusRelato.Publicado, (await a.Relatos.PublicarAsync(r1.RelatoId)).Status);
        Assert.Equal(StatusRelato.Rejeitado, (await a.Relatos.RejeitarAsync(r2.RelatoId)).Status);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => a.Relatos.PublicarAsync(r2.RelatoId));
    }

    [Fact]
    public async Task Analise_de_foto_avulsa_nao_cria_relato_e_avisa_que_e_triagem()
    {
        using var a = new Ambiente();

        var r = await a.Relatos.AnalisarFotoAsync(Imagens.Jpeg());

        Assert.Contains("não comprova", r.Aviso);
        Assert.True(r.MetadadosRemovidos);
        Assert.Empty(await a.Db.Relatos.ToListAsync());
        Assert.Equal(0, a.Fotos.Salvas);
    }

    [Fact]
    public async Task Relato_de_usuario_inexistente_lanca_nao_encontrado()
    {
        using var a = new Ambiente();
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => a.Relatar(999));
    }
}

public class OcorrenciasETestesDeRegiao
{
    [Fact]
    public async Task Fonte_oficial_registra_ocorrencia_e_repeticao_no_mesmo_ponto_e_recusada()
    {
        using var a = new Ambiente();
        var o = await a.Ocorrencias.RegistrarOficialAsync(new RegistrarOcorrenciaRequest(a.OficialId, -23.55, -46.63, "Av. teste"));

        Assert.Equal(StatusOcorrencia.Ativa, o.Status);
        Assert.Equal("CGE-SP (teste)", o.Fonte);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            a.Ocorrencias.RegistrarOficialAsync(new RegistrarOcorrenciaRequest(a.OficialId, -23.5501, -46.6301, null)));
    }

    [Fact]
    public async Task Chave_oficial_nao_fala_em_nome_de_sensor()
    {
        using var a = new Ambiente();
        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            a.Ocorrencias.RegistrarOficialAsync(new RegistrarOcorrenciaRequest(a.FonteDoSensorId, -23.55, -46.63, null)));
    }

    [Fact]
    public async Task So_a_mesma_fonte_encerra_e_o_sensor_nao_pode_ser_encerrado_por_oficial()
    {
        using var a = new Ambiente();
        var oficial = await a.Ocorrencias.RegistrarOficialAsync(new RegistrarOcorrenciaRequest(a.OficialId, -23.55, -46.63, null));

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => a.Ocorrencias.EncerrarOficialAsync(oficial.Id, a.OutraOficialId));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => a.Ocorrencias.EncerrarOficialAsync(oficial.Id, a.FonteDoSensorId));

        var encerrada = await a.Ocorrencias.EncerrarOficialAsync(oficial.Id, a.OficialId);
        Assert.Equal(StatusOcorrencia.Encerrada, encerrada.Status);
        Assert.Contains("precisa ser reavaliada", encerrada.Rotulo);

        // ocorrência aberta pelo sensor: nenhuma fonte oficial consegue encerrar
        await a.Leituras(90, 95);
        var doSensor = await a.Db.Ocorrencias.SingleAsync(o => o.FonteId == a.FonteDoSensorId);
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => a.Ocorrencias.EncerrarOficialAsync(doSensor.Id, a.OficialId));
    }

    [Fact]
    public async Task Encerramento_oficial_avisa_quem_recebeu_o_aviso_com_a_fonte_e_a_hora()
    {
        using var a = new Ambiente();
        var ana = await a.NovoUsuario();
        await a.LocalPerto(ana, -23.55, -46.63);
        var o = await a.Ocorrencias.RegistrarOficialAsync(new RegistrarOcorrenciaRequest(a.OficialId, -23.55, -46.63, null));

        await a.Ocorrencias.EncerrarOficialAsync(o.Id, a.OficialId);

        Assert.Equal(new[] { TipoAlerta.NovoEvento, TipoAlerta.Encerramento },
            a.Notificador.Enviados.Select(x => x.Tipo).ToArray());
        Assert.Contains("encerrada pela fonte CGE-SP (teste) às 09:00", a.Notificador.Enviados[1].Mensagem);
    }

    [Fact]
    public async Task Ocorrencia_inexistente_lanca_nao_encontrado_em_todas_as_consultas()
    {
        using var a = new Ambiente();
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => a.Ocorrencias.BuscarAsync(99));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => a.Ocorrencias.HistoricoAsync(99));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => a.Ocorrencias.EncerrarOficialAsync(99, a.OficialId));
    }

    [Fact]
    public async Task Filtros_de_status_fonte_e_data_e_ordenacao_por_distancia()
    {
        using var a = new Ambiente();
        var perto = await a.Ocorrencias.RegistrarOficialAsync(new RegistrarOcorrenciaRequest(a.OficialId, -23.5651, -46.6500, null));
        var longe = await a.Ocorrencias.RegistrarOficialAsync(new RegistrarOcorrenciaRequest(a.OutraOficialId, -23.5700, -46.6500, null));
        await a.Ocorrencias.EncerrarOficialAsync(longe.Id, a.OutraOficialId);

        FiltroOcorrencias F(StatusOcorrencia? s = null, int? f = null, DateTime? d = null) =>
            new(-23.5650, -46.6500, 2000, s, f, d, null);

        Assert.Equal(new[] { perto.Id, longe.Id }, (await a.Ocorrencias.ListarAsync(F())).Ocorrencias.Select(o => o.Id).ToArray());
        Assert.Equal(new[] { perto.Id }, (await a.Ocorrencias.ListarAsync(F(s: StatusOcorrencia.Ativa))).Ocorrencias.Select(o => o.Id).ToArray());
        Assert.Equal(new[] { longe.Id }, (await a.Ocorrencias.ListarAsync(F(s: StatusOcorrencia.Encerrada))).Ocorrencias.Select(o => o.Id).ToArray());
        Assert.Equal(new[] { longe.Id }, (await a.Ocorrencias.ListarAsync(F(f: a.OutraOficialId))).Ocorrencias.Select(o => o.Id).ToArray());
        Assert.Empty((await a.Ocorrencias.ListarAsync(F(d: a.Relogio.GetUtcNow().UtcDateTime.AddHours(1)))).Ocorrencias);
    }

    [Fact]
    public async Task Sem_nenhum_dado_a_regiao_e_sem_dados_recentes_e_nunca_segura()
    {
        using var a = new Ambiente();

        var r = await a.Ocorrencias.ListarAsync(new(-23.90, -46.90, 1000, StatusOcorrencia.Ativa, null, null, null));

        Assert.Equal("SemDadosRecentes", r.StatusDosDados);
        Assert.Contains("não significa que esteja segura", r.Aviso);
    }

    [Fact]
    public async Task Dados_de_sensor_recentes_deixam_a_regiao_atualizada_mas_sem_ponto_nao_e_seguro()
    {
        using var a = new Ambiente();
        await a.Leituras(10);   // sensor ativo, nenhuma ocorrência

        var r = await a.Ocorrencias.ListarAsync(new(-23.5650, -46.6500, 1000, StatusOcorrencia.Ativa, null, null, null));

        Assert.Equal("Atualizado", r.StatusDosDados);
        Assert.Empty(r.Ocorrencias);
        Assert.Contains("não significa que a via esteja segura", r.Aviso);
    }

    [Fact]
    public async Task Passada_a_janela_a_regiao_volta_a_sem_dados_recentes_e_a_ocorrencia_e_marcada()
    {
        using var a = new Ambiente();
        await a.Leituras(90, 95);
        var filtro = new FiltroOcorrencias(-23.5650, -46.6500, 1000, StatusOcorrencia.Ativa, null, null, null);
        Assert.Equal("Atualizado", (await a.Ocorrencias.ListarAsync(filtro)).StatusDosDados);

        a.Relogio.Avancar(TimeSpan.FromMinutes(a.Opcoes.JanelaDadosRecentesMinutos + 1));

        var r = await a.Ocorrencias.ListarAsync(filtro);
        Assert.Equal("SemDadosRecentes", r.StatusDosDados);
        Assert.True(r.Ocorrencias[0].SemAtualizacaoRecente);
        Assert.Contains("sem atualização recente", r.Ocorrencias[0].Rotulo);
    }
}

public class LocaisVeiculosEUsuarios
{
    [Fact]
    public async Task Local_de_outro_usuario_e_negado()
    {
        using var a = new Ambiente();
        var ana = await a.NovoUsuario("Ana"); var bia = await a.NovoUsuario("Bia");
        var local = await a.LocalPerto(ana);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => a.Locais.RiscoAsync(bia, local.Id));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => a.Locais.SilenciarAsync(bia, local.Id, 30));
    }

    [Fact]
    public async Task Risco_sem_historico_e_indeterminado_e_nao_seguro()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();
        var local = await a.LocalPerto(u);

        var r = await a.Locais.RiscoAsync(u, local.Id);

        Assert.Equal(RiscoEstimado.Indeterminado, r.Risco);
        Assert.Contains("NÃO significa", r.Observacao);
    }

    [Fact]
    public async Task Risco_com_historico_conta_as_ocorrencias_mesmo_encerradas()
    {
        using var a = new Ambiente();
        var u = await a.NovoUsuario();
        var local = await a.LocalPerto(u);
        await a.Leituras(90, 95, 10, 12);
        a.Relogio.Avancar(TimeSpan.FromDays(40));
        await a.Leituras(90, 95, 10, 12);

        var r = await a.Locais.RiscoAsync(u, local.Id);
        var consulta = await a.Risco.CalcularAsync(-23.5650, -46.6500, 500);

        Assert.Equal(2, r.OcorrenciasEm90Dias);
        Assert.Equal(RiscoEstimado.Moderado, r.Risco);
        Assert.Equal(RiscoEstimado.Moderado, consulta.Risco);   // consulta por ponto, sem cadastrar local
    }

    [Fact]
    public async Task Posicao_do_veiculo_so_para_o_dono_e_avisos_podem_ser_desligados()
    {
        using var a = new Ambiente();
        var dono = await a.NovoUsuario("Dono"); var outro = await a.NovoUsuario("Outro");
        var v = await a.Veiculos.CriarAsync(dono, new CriarVeiculoRequest("Carro"));
        await a.Veiculos.AtualizarPosicaoAsync(dono, v.Id, new PosicaoRequest(-23.5, -46.6));

        Assert.Equal(-23.5, (await a.Veiculos.ObterPosicaoAsync(dono, v.Id)).Lat);
        await Assert.ThrowsAsync<AcessoNegadoException>(() => a.Veiculos.ObterPosicaoAsync(outro, v.Id));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => a.Veiculos.DefinirAvisosAsync(outro, v.Id, false));
        Assert.False((await a.Veiculos.DefinirAvisosAsync(dono, v.Id, false)).AvisosAtivos);
    }

    [Fact]
    public async Task Cadastro_sem_consentimento_e_recusado()
    {
        using var a = new Ambiente();
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => a.Usuarios.CriarAsync(new("Ana", false)));
    }
}
