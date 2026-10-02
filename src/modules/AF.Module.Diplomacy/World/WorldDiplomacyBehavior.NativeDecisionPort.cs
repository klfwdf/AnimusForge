using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private sealed class NativeDecisionPort : IWorldDiplomacyNativeDecisionPort
    {
        private readonly List<(Kingdom host, KingdomDecision decision)> tokens = new List<(Kingdom, KingdomDecision)>();
        private readonly Dictionary<int, (Kingdom proposer, Kingdom target)> parties = new Dictionary<int, (Kingdom, Kingdom)>();
        internal NativeDecisionPort() { }
        internal NativeDecisionPort(Kingdom host, KingdomDecision decision) { tokens.Add((host, decision)); }
        public IEnumerable<IReadOnlyList<int>> Queues()
        {
            if (Campaign.Current == null) yield break;
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (kingdom == null) continue;
                var queue = new List<int>();
                foreach (var decision in kingdom.UnresolvedDecisions.Where(IsNativeDiplomacyDecision).ToList())
                { queue.Add(tokens.Count); tokens.Add((kingdom, decision)); }
                yield return queue;
            }
        }
        public WorldDiplomacyNativeDecisionSnapshot Capture(int token)
        {
            var entry = tokens[token]; Kingdom host = entry.host; KingdomDecision decision = entry.decision;
            if (host == null || decision == null) return null;
            Kingdom target = null; string action = "";
            if (decision is DeclareWarDecision war) { target = war.FactionToDeclareWarOn as Kingdom; action = "declare_war"; }
            else if (decision is MakePeaceKingdomDecision peace) { target = peace.FactionToMakePeaceWith as Kingdom; action = "propose_peace"; }
            else if (decision is StartAllianceDecision alliance) { target = alliance.KingdomToStartAllianceWith; action = "propose_alliance"; }
            else if (decision is TradeAgreementDecision trade) { target = trade.TargetKingdom; action = "propose_trade"; }
            Kingdom proposer = decision.ProposerClan?.Kingdom;
            parties[token] = (proposer ?? host, target);
            return new WorldDiplomacyNativeDecisionSnapshot
            {
                HasHost = true, HasTarget = target != null, HostId = host.StringId, TargetId = target?.StringId,
                ProposerKingdomId = proposer?.StringId, Action = action, SameTargetHost = target == host,
                HostIsPlayer = IsPlayerKingdom(host), HostEliminated = host.IsEliminated,
                TargetEliminated = target?.IsEliminated == true, ProposerEliminated = proposer?.IsEliminated == true
            };
        }
        public string Reason(int token, bool incomingPlayerOffer, string action) =>
            BuildNativeDecisionReason(incomingPlayerOffer ? parties[token].target : parties[token].proposer,
                incomingPlayerOffer ? tokens[token].host : parties[token].target, tokens[token].decision, action);
        public void Remove(int token) => tokens[token].host.RemoveDecision(tokens[token].decision);
        public string Describe(int token) => "kingdom=" + (tokens[token].host?.StringId ?? "") + " type=" + tokens[token].decision.GetType().Name;
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
    }
}
