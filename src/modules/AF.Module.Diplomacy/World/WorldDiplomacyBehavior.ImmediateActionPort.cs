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
                return new(false, "宣战未执行：王国目标无效");
            if (!playerDiplomacy && !_owner.CanDeclareWar(author, target, out string reason, enforcing)) return new(false, "宣战未执行：" + reason);
            return Measure(() => RunDiplomaticAction("world_diplomacy_declare_war", () => {
                    // The same explicit pair scope also permits the native alliance-end event.
                    if (playerDiplomacy) PermanentAllianceGuard.RunAuthorizedBreak("world_diplomacy_declare_war", author, target,
                        () => DeclareWarAction.ApplyByKingdomDecision(author, target));
                    else DeclareWarAction.ApplyByKingdomDecision(author, target);
                }),
                () => FactionManager.IsAtWarAgainstFaction(author, target), "宣战", "已宣战");
        }
        public WorldDiplomacyImmediateActionReceipt BreakAlliance(string authorId, string targetId, WorldDiplomacyDocument document)
        {
            Kingdom author = WorldDiplomacyBehavior.ResolveKingdom(authorId); Kingdom target = WorldDiplomacyBehavior.ResolveKingdom(targetId);
            IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            if (alliance == null) return new(false, "解盟未执行：同盟系统不可用");
            try { if (!alliance.IsAllyWithKingdom(author, target)) return new(false, "解盟未执行：双方当前没有同盟"); }
            catch (Exception ex) { return new(false, "解盟状态无法确认", ex.Message, false); }
            return Measure(() => RunDiplomaticAction("world_diplomacy_break_alliance", () => PermanentAllianceGuard.RunAuthorizedBreak("world_diplomacy_break_alliance", author, target, () => alliance.EndAlliance(author, target))),
                () => !alliance.IsAllyWithKingdom(author, target), "解盟", "已解除同盟");
        }
        public WorldDiplomacyImmediateActionReceipt CancelTrade(string authorId, string targetId, WorldDiplomacyDocument document)
        {
            Kingdom author = WorldDiplomacyBehavior.ResolveKingdom(authorId); Kingdom target = WorldDiplomacyBehavior.ResolveKingdom(targetId);
            ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            if (trade == null) return new(false, "终止贸易未执行：贸易系统不可用");
            if (!BannerlordApiCompat.TryGetTradeAgreementState(trade, author, target, out bool active)) return new(false, "贸易协定状态无法确认", null, false);
            if (!active) return new(false, "终止贸易未执行：双方当前没有贸易协定");
            return Measure(() => RunDiplomaticAction("world_diplomacy_cancel_trade", () => trade.EndTradeAgreement(author, target)),
                () => BannerlordApiCompat.TryGetTradeAgreementState(trade, author, target, out bool after)
                    ? !after : throw new InvalidOperationException("贸易协定状态无法读取"), "终止贸易", "已终止贸易协定");
        }
        private static WorldDiplomacyImmediateActionReceipt Measure(Action action, Func<bool> confirm, string label, string success)
        {
            var receipt = DiplomacyEffectReadback.Execute(action, confirm);
            string message = !receipt.IsKnown ? label + "执行后状态无法确认"
                : receipt.Applied ? success : label + "未执行：游戏状态未发生变化";
            return new WorldDiplomacyImmediateActionReceipt(receipt.Applied, message, receipt.Diagnostic, receipt.IsKnown);
        }
    }
}
