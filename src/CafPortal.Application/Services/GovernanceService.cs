using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class GovernanceService(IApplicationDbContext db, ICurrentUser currentUser) : IGovernanceService
{
    private readonly IApplicationDbContext _db = db;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<NominationGovernanceDto?> GetForNominationAsync(int nominationId, CancellationToken ct = default)
    {
        var nom = await _db.Nominations.AsNoTracking()
            .Where(n => n.Id == nominationId)
            .Select(n => new { n.Id, n.AccountName, Tpid = n.Account != null ? n.Account.Tpid : null })
            .FirstOrDefaultAsync(ct);
        if (nom is null) return null;

        var gates = await _db.GateDefinitions.AsNoTracking()
            .Where(g => g.Active)
            .Include(g => g.Items.Where(i => i.Active))
            .OrderBy(g => g.Order)
            .ToListAsync(ct);

        var state = await _db.NominationGateItems.AsNoTracking()
            .Where(x => x.NominationId == nominationId)
            .ToDictionaryAsync(x => x.GateItemDefinitionId, ct);

        return Build(nom.Id, nom.AccountName, nom.Tpid, gates, state);
    }

    public async Task<NominationGovernanceDto?> UpdateItemAsync(int nominationId, int itemDefId, UpdateGateItemRequest req, CancellationToken ct = default)
    {
        var nomExists = await _db.Nominations.AnyAsync(n => n.Id == nominationId, ct);
        var itemDef = await _db.GateItemDefinitions.FirstOrDefaultAsync(i => i.Id == itemDefId, ct);
        if (!nomExists || itemDef is null) return null;

        if (!Enum.TryParse<GateItemStatus>(req.Status, ignoreCase: true, out var status))
            status = GateItemStatus.Pending;

        var row = await _db.NominationGateItems
            .FirstOrDefaultAsync(x => x.NominationId == nominationId && x.GateItemDefinitionId == itemDefId, ct);
        if (row is null)
        {
            row = new NominationGateItem { NominationId = nominationId, GateItemDefinitionId = itemDefId };
            _db.NominationGateItems.Add(row);
        }

        row.Status = status;
        row.Owner = req.Owner;
        row.Ref = req.Ref;
        row.Notes = req.Notes;
        row.CompletedUtc = status == GateItemStatus.Done ? (row.CompletedUtc ?? DateTime.UtcNow) : null;
        row.UpdatedBy = _currentUser.Name;
        row.UpdatedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetForNominationAsync(nominationId, ct);
    }

    private static NominationGovernanceDto Build(int nominationId, string? account, string? tpid,
        List<GateDefinition> gates, IReadOnlyDictionary<int, NominationGateItem> state)
    {
        static string StatusName(GateItemStatus s) => s switch
        {
            GateItemStatus.Done => "Done",
            GateItemStatus.NotApplicable => "NA",
            _ => "Pending",
        };

        var gateDtos = new List<GateDto>();
        double weightedSum = 0, totalWeight = 0;
        string? currentGateKey = null;

        foreach (var g in gates.OrderBy(g => g.Order))
        {
            var items = g.Items.OrderBy(i => i.Order).Select(i =>
            {
                state.TryGetValue(i.Id, out var s);
                return new GateItemDto(
                    i.Id, i.Key, i.Label, i.Kind.ToString(), i.SubStage, i.ResponsibleRole, i.Mandatory, i.Order,
                    StatusName(s?.Status ?? GateItemStatus.Pending), s?.Owner, s?.CompletedUtc, s?.Ref, s?.Notes,
                    s?.UpdatedBy, s?.UpdatedUtc);
            }).ToList();

            var counted = items.Where(x => x.Status != "NA").ToList();
            var done = counted.Count(x => x.Status == "Done");
            var pct = counted.Count == 0 ? 0 : (int)Math.Round(100.0 * done / counted.Count);
            var status = counted.Count > 0 && done == counted.Count ? "Green" : done > 0 ? "InProgress" : "NotStarted";

            if (currentGateKey is null && status != "Green") currentGateKey = g.Key;
            weightedSum += g.Weight * pct;
            totalWeight += g.Weight;

            gateDtos.Add(new GateDto(g.Key, g.Name, g.ExitCriteria, g.Order, g.Weight, status, pct, items));
        }

        var compliance = totalWeight > 0 ? (int)Math.Round(weightedSum / totalWeight) : 0;
        currentGateKey ??= gateDtos.LastOrDefault()?.Key;
        return new NominationGovernanceDto(nominationId, account, tpid, null, currentGateKey, compliance, gateDtos);
    }
}
