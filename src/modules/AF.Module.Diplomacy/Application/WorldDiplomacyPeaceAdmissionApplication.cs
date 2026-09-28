using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;
namespace AnimusForge;
internal readonly struct WorldDiplomacyCessionCandidate
{
    internal readonly string Id, OwnerId;
    internal readonly bool SameCulture, IsTown, IsFort, UnderSiege;
    internal WorldDiplomacyCessionCandidate(string id, string ownerId, bool sameCulture, bool isTown, bool isFort, bool underSiege)
    { Id = id; OwnerId = ownerId; SameCulture = sameCulture; IsTown = isTown; IsFort = isFort; UnderSiege = underSiege; }
}
internal interface IWorldDiplomacyPeaceAdmissionPort
{
    string KingdomId(string id);
    bool AtWar(string first, string second);
    string SettlementId(string id);
    string SettlementOwner(string id);
    bool HasRuler(string id);
    float CessionScore(string first, string second, string from);
    IEnumerable<WorldDiplomacyCessionCandidate> LostSettlements(string originalOwner, string currentOwner);
    IEnumerable<WorldDiplomacyCessionCandidate> OwnedSettlements(string owner, string receiver);
    int FiefCount(string id);
    float CastleThreshold { get; }
    float TownThreshold { get; }
    int MaxCandidates { get; }
    int ClampTribute(string payer, int amount);
    int ResolveDuration(string token, bool hasTribute);
}
internal static class WorldDiplomacyPeaceAdmissionApplication
{

    internal static WorldDiplomacyPeaceTerms ParseAndValidatePeaceTerms(IWorldDiplomacyPeaceAdmissionPort port, JObject json, string author, string target)
	{
		if (json == null || author == null || target == null || !port.AtWar(author, target)) return null;
		if (json.SelectToken("peace_terms") is not JObject token) return null;
		string payerId = token["tribute_payer_kingdom_id"]?.ToString()?.Trim() ?? "";
		string receiverId = token["tribute_receiver_kingdom_id"]?.ToString()?.Trim() ?? "";
		string payer = port.KingdomId(payerId);
		string receiver = port.KingdomId(receiverId);
		if ((payer != author && payer != target) || (receiver != author && receiver != target) || payer == receiver)
		{
			payer = null;
			receiver = null;
		}
		int.TryParse(token["daily_tribute"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int tribute);
		int.TryParse(token["duration_days"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int duration);
		string cessionFromId = token["cession_from_kingdom_id"]?.ToString()?.Trim() ?? "";
		string cessionToId = token["cession_to_kingdom_id"]?.ToString()?.Trim() ?? "";
		string cessionFrom = port.KingdomId(cessionFromId);
		string cessionTo = port.KingdomId(cessionToId);
		string cession = port.SettlementId(token["cession_settlement_id"]?.ToString());
		if (!IsCessionCurrentlyAllowed(port, cessionFrom, cessionTo, cession, author, target))
		{
			cessionFrom = null;
			cessionTo = null;
			cession = null;
		}
		if (payer == null && cession == null && tribute <= 0) return null;
		return new WorldDiplomacyPeaceTerms
		{
			TributePayerKingdomId = payer ?? "",
			TributeReceiverKingdomId = receiver ?? "",
			DailyTribute = payer == null ? 0 : port.ClampTribute(payer, Math.Max(0, tribute)),
			DurationDays = port.ResolveDuration(duration.ToString(CultureInfo.InvariantCulture), payer != null && tribute > 0),
			CessionFromKingdomId = cessionFrom ?? "",
			CessionToKingdomId = cessionTo ?? "",
			CessionSettlementId = cession ?? ""
		};
	}

    internal static bool AreOfferedPeaceTermsCurrentlyExecutable(IWorldDiplomacyPeaceAdmissionPort port,
		WorldDiplomacyRoundOffer offer,
		WorldDiplomacyDocument source,
		string proposer,
		string target)
	{
		if (offer == null || source == null || proposer == null || target == null
			|| proposer == target || !source.IsReadyForPublication
			|| !string.Equals(source.AuthorKingdomId, proposer, StringComparison.OrdinalIgnoreCase)) return false;
		WorldDiplomacyPeaceTerms terms = WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(source, offer.SourceActionId);
		if (source.Actions?.Count > 0 && WorldDiplomacyDocumentFactRules.ResolveDocumentAction(source, offer.SourceActionId) == null) return false;
		if (terms == null) return true;

		int promisedTribute = Math.Max(0, terms.DailyTribute);
		int promisedDuration = Math.Max(0, terms.DurationDays);
		if (promisedTribute > 0)
		{
			string payer = port.KingdomId(terms.TributePayerKingdomId);
			string receiver = port.KingdomId(terms.TributeReceiverKingdomId);
			if (payer == null || receiver == null || payer == receiver
				|| (payer != proposer && payer != target)
				|| (receiver != proposer && receiver != target)
				|| port.ClampTribute(payer, promisedTribute) != promisedTribute
				|| port.ResolveDuration(
					promisedDuration.ToString(CultureInfo.InvariantCulture),
					hasTribute: true) != promisedDuration) return false;
		}
		else if (promisedDuration != 0)
		{
			return false;
		}

		bool hasAnyCession = !string.IsNullOrWhiteSpace(terms.CessionFromKingdomId)
			|| !string.IsNullOrWhiteSpace(terms.CessionToKingdomId)
			|| !string.IsNullOrWhiteSpace(terms.CessionSettlementId);
		if (!hasAnyCession) return true;
		string from = port.KingdomId(terms.CessionFromKingdomId);
		string to = port.KingdomId(terms.CessionToKingdomId);
		string settlement = port.SettlementId(terms.CessionSettlementId);
		return from != null && to != null && settlement != null && from != to
			&& (from == proposer || from == target)
			&& (to == proposer || to == target)
			&& port.SettlementOwner(settlement) == from
			&& port.HasRuler(to);
	}

    internal static bool IsCessionCurrentlyAllowed(IWorldDiplomacyPeaceAdmissionPort port, string from, string to, string settlement, string first, string second)
	{
		if (from == null || to == null || settlement == null || from == to || (from != first && from != second) || (to != first && to != second) || port.SettlementOwner(settlement) != from) return false;
        float score = port.CessionScore(first, second, from);
        return BuildCessionCandidates(port, from, to, score).Contains(settlement);
	}

    internal static List<string> BuildCessionCandidates(IWorldDiplomacyPeaceAdmissionPort port, string cedingKingdom, string receivingKingdom, float cessionScore)
    {
        if (cedingKingdom == null || receivingKingdom == null || cessionScore < port.CastleThreshold) return new List<string>();
        List<WorldDiplomacyCessionCandidate> priority = port.LostSettlements(receivingKingdom, cedingKingdom).ToList();
        IEnumerable<WorldDiplomacyCessionCandidate> owned = port.OwnedSettlements(cedingKingdom, receivingKingdom).Where(x => x.IsFort);
        return priority.Concat(owned.Where(x => x.SameCulture)).Concat(owned)
            .Where(x => x.Id != null && x.OwnerId == cedingKingdom && !x.UnderSiege
                && (!x.IsTown || cessionScore >= port.TownThreshold) && port.FiefCount(cedingKingdom) > 1)
            .Select(x => x.Id).Distinct().Take(port.MaxCandidates).ToList();
    }
}
