using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.PolicyTargets;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge.PolicyEffects;

// Invocation-local authorization evidence, never serialized into a policy instance.
internal sealed class PolicyEffectJurisdictionContext
{
	internal string Scope { get; }
	internal string ProposerClanId { get; }
	internal IReadOnlyCollection<string> SourceSettlementIds { get; }
	private readonly Func<string, PolicyEffectJurisdictionFief> _resolveFief;
	private readonly Dictionary<string, PolicyEffectJurisdictionFief> _fiefs
		= new Dictionary<string, PolicyEffectJurisdictionFief>(StringComparer.OrdinalIgnoreCase);

	internal PolicyEffectJurisdictionContext(string scope, string proposerClanId,
		IEnumerable<string> sourceSettlementIds,
		Func<string, PolicyEffectJurisdictionFief> resolveFief = null)
	{
		Scope = (scope ?? string.Empty).Trim();
		ProposerClanId = (proposerClanId ?? string.Empty).Trim();
		SourceSettlementIds = PolicyEffectTargetJurisdiction.NormalizeIds(sourceSettlementIds);
		_resolveFief = resolveFief ?? ResolveLiveFief;
	}

	internal PolicyEffectJurisdictionFief ResolveFief(string id)
	{
		id = (id ?? string.Empty).Trim();
		if (!_fiefs.TryGetValue(id, out PolicyEffectJurisdictionFief fief))
		{
			fief = _resolveFief(id);
			_fiefs.Add(id, fief);
		}
		return fief;
	}

	internal static PolicyEffectJurisdictionContext FromSnapshot(string scope, string proposerClanId,
		IEnumerable<string> sourceSettlementIds, PolicyTargetWorldSnapshot snapshot)
	{
		return new PolicyEffectJurisdictionContext(scope, proposerClanId, sourceSettlementIds,
			id => snapshot?.JurisdictionFiefs != null && snapshot.JurisdictionFiefs.TryGetValue(id, out var fief) ? fief : null);
	}

	private static PolicyEffectJurisdictionFief ResolveLiveFief(string id)
	{
		return CaptureFief(Settlement.Find(id));
	}

	internal static PolicyEffectJurisdictionFief CaptureFief(Settlement settlement)
	{
		if (settlement == null || (!settlement.IsTown && !settlement.IsCastle)
			|| settlement.OwnerClan == null || settlement.OwnerClan.IsEliminated)
		{
			return null;
		}
		var leader = settlement.OwnerClan.Leader;
		return new PolicyEffectJurisdictionFief
		{
			Id = settlement.StringId,
			OwnerClanId = settlement.OwnerClan.StringId,
			OwnerKingdomId = settlement.OwnerClan.Kingdom?.StringId ?? string.Empty,
			OwnerLeaderId = leader?.IsActive == true ? leader.StringId : string.Empty,
			VillageIds = (settlement.BoundVillages ?? Enumerable.Empty<Village>())
				.Where(village => village?.Settlement != null)
				.Select(village => village.Settlement.StringId).ToArray()
		};
	}
}

internal sealed class PolicyEffectJurisdictionFief
{
	internal string Id { get; set; }
	internal string OwnerClanId { get; set; }
	internal string OwnerKingdomId { get; set; }
	internal string OwnerLeaderId { get; set; }
	internal IReadOnlyCollection<string> VillageIds { get; set; } = Array.Empty<string>();
}
