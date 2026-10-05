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
        // Preserve explicit clauses. Admission rejects unsupported terms rather than
        // converting a submitted proposal into a different, executable one.
		string payer = port.KingdomId(payerId) ?? payerId;
		string receiver = port.KingdomId(receiverId) ?? receiverId;
        int tribute = ReadExplicitInteger(token, "daily_tribute", 0);
        int duration = ReadExplicitInteger(token, "duration_days",
            port.ResolveDuration("0", tribute > 0));
		string cessionFromId = token["cession_from_kingdom_id"]?.ToString()?.Trim() ?? "";
		string cessionToId = token["cession_to_kingdom_id"]?.ToString()?.Trim() ?? "";
		string cessionFrom = port.KingdomId(cessionFromId) ?? cessionFromId;
		string cessionTo = port.KingdomId(cessionToId) ?? cessionToId;
        string cessionId = token["cession_settlement_id"]?.ToString()?.Trim() ?? "";
		string cession = port.SettlementId(cessionId) ?? cessionId;
		if (string.IsNullOrEmpty(payer) && string.IsNullOrEmpty(receiver)
            && string.IsNullOrEmpty(cessionFrom) && string.IsNullOrEmpty(cessionTo)
            && string.IsNullOrEmpty(cession) && tribute == 0 && duration == 0) return null;
		return new WorldDiplomacyPeaceTerms
		{
			TributePayerKingdomId = payer ?? "",
			TributeReceiverKingdomId = receiver ?? "",
			DailyTribute = tribute,
			DurationDays = duration,
			CessionFromKingdomId = cessionFrom ?? "",
			CessionToKingdomId = cessionTo ?? "",
			CessionSettlementId = cession ?? ""
		};
	}

    private static int ReadExplicitInteger(JObject terms, string key, int defaultValue)
        => terms[key] == null || terms[key].Type == JTokenType.Null ? defaultValue
            : int.TryParse(terms[key].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : -1;

    internal static bool AreOfferedPeaceTermsCurrentlyExecutable(IWorldDiplomacyPeaceAdmissionPort port,
		WorldDiplomacyRoundOffer offer,
		WorldDiplomacyDocument source,
		string proposer,
        string target)
        => TryValidateOfferedPeaceTerms(port, offer, source, proposer, target, out _);

    internal static bool TryValidateOfferedPeaceTerms(IWorldDiplomacyPeaceAdmissionPort port,
        WorldDiplomacyRoundOffer offer, WorldDiplomacyDocument source, string proposer, string target, out string reason)
	{
        reason = "和平原案来源或参与国已失效。";
		if (offer == null || source == null || proposer == null || target == null
			|| proposer == target || !source.IsReadyForPublication
			|| !string.Equals(source.AuthorKingdomId, proposer, StringComparison.OrdinalIgnoreCase)) return false;
		WorldDiplomacyPeaceTerms terms = WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(source, offer.SourceActionId);
		if (source.Actions?.Count > 0 && WorldDiplomacyDocumentFactRules.ResolveDocumentAction(source, offer.SourceActionId) == null) return false;
		if (terms == null) { reason = ""; return true; }
        reason = "贡金金额或期限无法解析，或为负数。";
        if (terms.DailyTribute < 0 || terms.DurationDays < 0) return false;
        bool hasTributeRoles = !string.IsNullOrWhiteSpace(terms.TributePayerKingdomId)
            || !string.IsNullOrWhiteSpace(terms.TributeReceiverKingdomId);

		int promisedTribute = Math.Max(0, terms.DailyTribute);
		int promisedDuration = Math.Max(0, terms.DurationDays);
		if (promisedTribute > 0 || hasTributeRoles)
		{
			string payer = port.KingdomId(terms.TributePayerKingdomId);
			string receiver = port.KingdomId(terms.TributeReceiverKingdomId);
            reason = "贡金支付国和接收国必须是本次交涉的两个不同国家。";
			if (payer == null || receiver == null || payer == receiver
				|| (payer != proposer && payer != target)
				|| (receiver != proposer && receiver != target)) return false;
				// Prosperity-based limits advise AI offers; explicit negotiated amounts
                // are supported by the same native peace effect used by oral diplomacy.
            reason = "约定的贡金期限不受支持：" + promisedDuration + "天；原条款未被改写。";
			if (port.ResolveDuration(
					promisedDuration.ToString(CultureInfo.InvariantCulture),
                    hasTribute: promisedTribute > 0) != promisedDuration) return false;
		}
		else if (promisedDuration != 0)
		{
            reason = "没有贡金的和平原案不能附带贡金支付期限。";
			return false;
		}

		bool hasAnyCession = !string.IsNullOrWhiteSpace(terms.CessionFromKingdomId)
			|| !string.IsNullOrWhiteSpace(terms.CessionToKingdomId)
			|| !string.IsNullOrWhiteSpace(terms.CessionSettlementId);
		if (!hasAnyCession) { reason = ""; return true; }
		string from = port.KingdomId(terms.CessionFromKingdomId);
		string to = port.KingdomId(terms.CessionToKingdomId);
		string settlement = port.SettlementId(terms.CessionSettlementId);
        reason = "割地条款当前不可执行：请核对转出国、接收国、领地归属和允许割让条件；该条款未被删除。";
		if (!IsCessionCurrentlyAllowed(port, from, to, settlement, proposer, target) || !port.HasRuler(to)) return false;
        reason = ""; return true;
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
