using Pluvia.Api.Data;
using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Regras;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Services;

public class SensorService : ISensorService
{
    private readonly PluviaDbContext _db;
    private readonly IDetectorDeAlagamento _detector;
    private readonly IGestorDeOcorrencias _gestor;
    private readonly TimeProvider _relogio;
    private readonly PluviaOptions _opt;

    public SensorService(PluviaDbContext db, IDetectorDeAlagamento detector, IGestorDeOcorrencias gestor,
        TimeProvider relogio, IOptions<PluviaOptions> opt)
    {
        _db = db; _detector = detector; _gestor = gestor; _relogio = relogio; _opt = opt.Value;
    }

    public async Task<LeituraResponse> RegistrarLeituraAsync(LeituraRequest req, CancellationToken ct = default)
    {
        var sensor = await _db.Sensores.FirstOrDefaultAsync(s => s.Codigo == req.Codigo, ct)
            ?? throw new RecursoNaoEncontradoException("Sensor", req.Codigo);
        if (!sensor.AceitaLeituras)
            throw new RegraDeNegocioException($"Sensor '{sensor.Codigo}' está {sensor.Status} e não aceita leituras.");

        var agora = _relogio.GetUtcNow().UtcDateTime;
        var leitura = new LeituraSensor(sensor.Id, req.NivelCm, agora);
        _db.Leituras.Add(leitura);
        await _db.SaveChangesAsync(ct);

        var recentes = await _db.Leituras.AsNoTracking()
            .Where(l => l.SensorId == sensor.Id)
            .OrderByDescending(l => l.RegistradaEm).ThenByDescending(l => l.Id)
            .Take(10)
            .Select(l => l.NivelCm)
            .ToListAsync(ct);

        if (_detector.Detectar(recentes, sensor.LimiteCm))
        {
            var (o, criada) = await _gestor.AbrirAsync(sensor.FonteId, sensor.Lat, sensor.Lng,
                $"Nível acima de {sensor.LimiteCm} cm no sensor {sensor.Codigo}.", ct);
            return new LeituraResponse(leitura.Id,
                criada ? "Alagamento ativo registrado pelo sensor." : "Ocorrência já ativa, atualizada.", o.Id);
        }

        var ativas = await _db.OcorrenciasPerto(sensor.Lat, sensor.Lng, _opt.RaioAgrupamentoMetros,
            StatusOcorrencia.Ativa, sensor.FonteId, ct: ct);

        if (ativas.Count > 0 && _detector.Normalizou(recentes, sensor.LimiteCm))
        {
            // Só a mesma fonte que abriu (este sensor) encerra, sempre sem prometer segurança.
            await _gestor.EncerrarAsync(ativas[0], sensor.FonteId, ct);
            return new LeituraResponse(leitura.Id, "Água abaixo do limite de forma sustentada. Ocorrência encerrada pela fonte.", ativas[0].Id);
        }

        // Leitura intermediária: o sensor segue vivo, então os dados da ocorrência continuam recentes.
        foreach (var a in ativas) a.RegistrarAtualizacao(agora);
        if (ativas.Count > 0) await _db.SaveChangesAsync(ct);

        return new LeituraResponse(leitura.Id, "Leitura registrada.", null);
    }
}
