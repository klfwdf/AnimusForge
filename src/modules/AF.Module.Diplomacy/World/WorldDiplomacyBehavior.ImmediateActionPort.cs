using AnimusForge;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Adapters;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private sealed class ImmediateActionPort : IWorldDiplomacyImmediateActionPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal ImmediateActionPort(WorldDiplomacyBehavior owner) => _owner = owner;
        public WorldDiplomacyStorage Storage => _owner._storage;
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public bool CanAiAuthor(string authorId, out string reason) => WorldDiplomacyBehavior.CanAiAuthorDiplomaticDocument(WorldDiplomacyBehavior.ResolveKingdom(authorId), out reason);
        public bool IsPlayerDiplomacy(WorldDiplomacyDocument document) => _owner.Orchestration.IsPlayerDiplomacyDocument(document);
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
        public WorldDiplomacyImmediateActionReceipt DeclareWar(string authorId, string targetId, WorldDiplomacyDocument document)
        {
            Kingdom author = WorldDiplomacyBehavior.ResolveKingdom(authorId); Kingdom target = WorldDiplomacyBehavior.ResolveKingdom(targetId);
            bool enforcing = WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum(_owner._storage?.DiplomaticThreats, authorId, targetId);
            bool playerDiplomacy = document?.IsPlayerAuthored == true || IsPlayerDiplomacy(document);
            if (author == null || target == null || author == target || author.IsEliminated || target.IsEliminated)
                return new(false, DiplomacyMiscPromptTexts.Get("Immediate_DeclareWarInvalidTarget", "War declaration not executed: invalid kingdom target"));
            if (!playerDiplomacy && !_owner.CanDeclareWar(author, target, out string reason, enforcing)) return new(false, DiplomacyMiscPromptTexts.Get("Immediate_DeclareWarNotExecutedPrefix", "War declaration not executed: ") + reason);
            return Measure(() => RunDiplomaticAction("world_diplomacy_declare_war", () => {
                    // The same explicit pair scope also permits the native alliance-end event.
                    if (playerDiplomacy) PermanentAllianceGuard.RunAuthorizedBreak("world_diplomacy_declare_war", author, target,
                        () => DeclareWarAction.ApplyByKingdomDecision(author, target));
                    else DeclareWarAction.ApplyByKingdomDecision(author, target);
                }),
                () => FactionManager.IsAtWarAgainstFaction(author, target), DiplomacyMiscPromptTexts.Get("Immediate_DeclareWarAction", "declare war"), DiplomacyMiscPromptTexts.Get("Immediate_DeclaredWarSuccess", "war declared"));
        }
        public WorldDiplomacyImmediateActionReceipt BreakAlliance(string authorId, string targetId, WorldDiplomacyDocument document)
        {
            Kingdom author = WorldDiplomacyBehavior.ResolveKingdom(authorId); Kingdom target = WorldDiplomacyBehavior.ResolveKingdom(targetId);
            IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            if (alliance == null) return new(false, DiplomacyMiscPromptTexts.Get("Immediate_BreakAllianceSystemUnavailable", "Alliance break not executed: alliance system is unavailable"));
            try { if (!alliance.IsAllyWithKingdom(author, target)) return new(false, DiplomacyMiscPromptTexts.Get("Immediate_BreakAllianceNotAllied", "Alliance break not executed: factions are not currently allied")); }
            catch (Exception ex) { return new(false, DiplomacyMiscPromptTexts.Get("Immediate_BreakAllianceStatusUnconfirmed", "Alliance break status cannot be confirmed"), ex.Message, false); }
            return Measure(() => RunDiplomaticAction("world_diplomacy_break_alliance", () => PermanentAllianceGuard.RunAuthorizedBreak("world_diplomacy_break_alliance", author, target, () => alliance.EndAlliance(author, target))),
                () => !alliance.IsAllyWithKingdom(author, target), DiplomacyMiscPromptTexts.Get("Immediate_BreakAllianceAction", "break alliance"), DiplomacyMiscPromptTexts.Get("Immediate_BrokenAllianceSuccess", "alliance broken"));
        }
        public WorldDiplomacyImmediateActionReceipt CancelTrade(string authorId, string targetId, WorldDiplomacyDocument document)
        {
            Kingdom author = WorldDiplomacyBehavior.ResolveKingdom(authorId); Kingdom target = WorldDiplomacyBehavior.ResolveKingdom(targetId);
            ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            if (trade == null) return new(false, DiplomacyMiscPromptTexts.Get("Immediate_CancelTradeSystemUnavailable", "Cancel trade not executed: trade system is unavailable"));
            if (!BannerlordApiCompat.TryGetTradeAgreementState(trade, author, target, out bool active)) return new(false, DiplomacyMiscPromptTexts.Get("Immediate_TradeAgreementStatusUnconfirmed", "Trade agreement status cannot be confirmed"), null, false);
            if (!active) return new(false, DiplomacyMiscPromptTexts.Get("Immediate_CancelTradeNotTrading", "Cancel trade not executed: factions do not currently have a trade agreement"));
            return Measure(() => RunDiplomaticAction("world_diplomacy_cancel_trade", () => trade.EndTradeAgreement(author, target)),
                () => BannerlordApiCompat.TryGetTradeAgreementState(trade, author, target, out bool after)
                    ? !after : throw new InvalidOperationException(DiplomacyMiscPromptTexts.Get("Immediate_TradeAgreementStatusReadFailed", "Trade agreement status cannot be read")), DiplomacyMiscPromptTexts.Get("Immediate_CancelTradeAction", "cancel trade"), DiplomacyMiscPromptTexts.Get("Immediate_CancelledTradeSuccess", "trade agreement cancelled"));
        }
        private static WorldDiplomacyImmediateActionReceipt Measure(Action action, Func<bool> confirm, string label, string success)
        {
            var receipt = DiplomacyEffectReadback.Execute(action, confirm);
            string message = !receipt.IsKnown ? string.Format(DiplomacyMiscPromptTexts.Get("Immediate_ActionOutcomeUnconfirmedFormat", "{0} executed but status cannot be confirmed"), label)
                : receipt.Applied ? success : string.Format(DiplomacyMiscPromptTexts.Get("Immediate_ActionNotExecutedNoStateChangeFormat", "{0} not executed: game state did not change"), label);
            return new WorldDiplomacyImmediateActionReceipt(receipt.Applied, message, receipt.Diagnostic, receipt.IsKnown);
        }
    }
}
