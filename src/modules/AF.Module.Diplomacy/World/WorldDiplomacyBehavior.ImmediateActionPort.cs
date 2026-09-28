using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using AnimusForge.Refactor.Domain;

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
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
        public WorldDiplomacyImmediateActionReceipt DeclareWar(string authorId, string targetId, WorldDiplomacyDocument document)
        {
            Kingdom author = WorldDiplomacyBehavior.ResolveKingdom(authorId); Kingdom target = WorldDiplomacyBehavior.ResolveKingdom(targetId);
            bool enforcing = WorldDiplomacyRoundLifecycleRules.IsEnforcingRejectedUltimatum(_owner._storage?.DiplomaticThreats, authorId, targetId);
            if (!_owner.CanDeclareWar(author, target, out string reason, enforcing)) return new(false, "宣战未执行：" + reason);
            Exception error = null;
            try { WorldDiplomacyBehavior.RunDiplomaticAction("world_diplomacy_declare_war", () => DeclareWarAction.ApplyByKingdomDecision(author, target)); }
            catch (Exception ex) { error = ex; }
            string diagnostic = error == null ? null : "declare war action raised after live-state check author=" + authorId + " target=" + targetId + " error=" + error.Message;
            if (FactionManager.IsAtWarAgainstFaction(author, target))
            {
                return new(true, "已宣战", diagnostic);
            }
            return new(false, error == null ? "宣战未执行：游戏状态未发生变化" : "宣战未执行：" + WorldDiplomacyTextRules.Limit(error.Message, 180), diagnostic);
        }
        public WorldDiplomacyImmediateActionReceipt BreakAlliance(string authorId, string targetId, WorldDiplomacyDocument document)
        {
            Kingdom author = WorldDiplomacyBehavior.ResolveKingdom(authorId); Kingdom target = WorldDiplomacyBehavior.ResolveKingdom(targetId);
            IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            if (alliance == null) return new(false, "解盟未执行：同盟系统不可用");
            if (!alliance.IsAllyWithKingdom(author, target)) return new(false, "解盟未执行：双方当前没有同盟");
            Exception error = null;
            try { WorldDiplomacyBehavior.RunDiplomaticAction("world_diplomacy_break_alliance", () => PermanentAllianceGuard.RunAuthorizedBreak("world_diplomacy_break_alliance", author, target, () => alliance.EndAlliance(author, target))); }
            catch (Exception ex) { error = ex; }
            string diagnostic = error == null ? null : "break alliance action raised after live-state check author=" + authorId + " target=" + targetId + " error=" + error.Message;
            return !alliance.IsAllyWithKingdom(author, target)
                ? new(true, "已解除同盟", diagnostic)
                : new(false, error == null ? "解盟未执行：游戏状态未发生变化" : "解盟未执行：" + WorldDiplomacyTextRules.Limit(error.Message, 180), diagnostic);
        }
        public WorldDiplomacyImmediateActionReceipt CancelTrade(string authorId, string targetId, WorldDiplomacyDocument document)
        {
            Kingdom author = WorldDiplomacyBehavior.ResolveKingdom(authorId); Kingdom target = WorldDiplomacyBehavior.ResolveKingdom(targetId);
            ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            if (trade == null) return new(false, "终止贸易未执行：贸易系统不可用");
            if (!BannerlordApiCompat.HasTradeAgreement(trade, author, target)) return new(false, "终止贸易未执行：双方当前没有贸易协定");
            Exception error = null;
            try { WorldDiplomacyBehavior.RunDiplomaticAction("world_diplomacy_cancel_trade", () => trade.EndTradeAgreement(author, target)); }
            catch (Exception ex) { error = ex; }
            string diagnostic = error == null ? null : "cancel trade action raised after live-state check author=" + authorId + " target=" + targetId + " error=" + error.Message;
            return !BannerlordApiCompat.HasTradeAgreement(trade, author, target)
                ? new(true, "已终止贸易协定", diagnostic)
                : new(false, error == null ? "终止贸易未执行：游戏状态未发生变化" : "终止贸易未执行：" + WorldDiplomacyTextRules.Limit(error.Message, 180), diagnostic);
        }
    }
}
