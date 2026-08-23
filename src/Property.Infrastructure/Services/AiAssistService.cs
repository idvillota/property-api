using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;
using Property.Application.Features.AiAssist;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class AiAssistService : IAiAssistService
{
    private readonly ApplicationDbContext _db;
    private readonly AiAssistOptions _options;

    public AiAssistService(ApplicationDbContext db, IOptions<AiAssistOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<string> SuggestPqrsReplyAsync(Guid pqrsId, CancellationToken cancellationToken = default)
    {
        EnsureAvailable();

        var pqrs = await _db.PqrsCases.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pqrsId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("PQRS case was not found.");

        return BuildPqrsDraft(pqrs.Subject, pqrs.Description);
    }

    public Task<string> SuggestDunningTextAsync(
        SuggestDunningTextRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureAvailable();

        var unitCode = string.IsNullOrWhiteSpace(request.UnitCode) ? "su unidad" : request.UnitCode.Trim();
        var balance = request.Balance ?? 0m;
        var days = request.DaysOverdue ?? 0;

        var draft =
            $"Estimado propietario/residente de la unidad {unitCode}: " +
            $"le recordamos que registra un saldo pendiente de ${balance:N0} " +
            $"con {days} día(s) de mora. " +
            "Por favor regularice su situación o contáctenos para acordar un plan de pago.";

        return Task.FromResult(draft);
    }

    private void EnsureAvailable()
    {
        if (_options.Fail)
            throw new InvalidOperationException("AI assist provider is unavailable.");
    }

    private static string BuildPqrsDraft(string subject, string description)
    {
        var subjectLine = string.IsNullOrWhiteSpace(subject) ? "su solicitud" : subject.Trim();
        var bodySnippet = string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : description.Trim().Length > 200
                ? description.Trim()[..200] + "…"
                : description.Trim();

        var draft =
            $"Gracias por contactarnos respecto a \"{subjectLine}\". " +
            "Hemos revisado su caso y estamos trabajando en una solución. ";

        if (!string.IsNullOrWhiteSpace(bodySnippet))
            draft += $"Referente a su mensaje: \"{bodySnippet}\". ";

        draft += "Le mantendremos informado sobre los avances.";
        return draft;
    }
}
