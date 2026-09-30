using System;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private sealed class OfferActionPort : IWorldDiplomacyOfferActionPort
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal OfferActionPort(WorldDiplomacyBehavior owner) => _owner = owner;

        public WorldDiplomacyStorage Storage => _owner._storage;
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public WorldDiplomacyRound ResolveRound(string id) => _owner.ResolveRound(id);
        public WorldDiplomacyDocument ResolveDocument(string id) => _owner.ResolveDocument(id);
        public string ApplyCession(string proposerId, string targetId, WorldDiplomacyPeaceTerms terms)
            => _owner.TryApplyValidatedCession(terms, ResolveKingdom(proposerId), ResolveKingdom(targetId));

        public bool ResolveParties(WorldDiplomacyRoundOffer offer)
            => WorldDiplomacyBehavior.ResolveKingdom(offer?.ProposerKingdomId) != null
                && WorldDiplomacyBehavior.ResolveKingdom(offer?.TargetKingdomId) != null;

        public WorldDiplomacyOfferActionReceipt ExecutePeace(string proposerId, string targetId, WorldDiplomacyPeaceTerms terms)
        {
            Kingdom proposer = ResolveKingdom(proposerId);
            Kingdom target = ResolveKingdom(targetId);
            if (!FactionManager.IsAtWarAgainstFaction(proposer, target))
                return new(false, "议和未执行：双方当前没有战争");
            Kingdom payer = WorldDiplomacyBehavior.ResolveKingdom(terms?.TributePayerKingdomId) ?? proposer;
            Kingdom receiver = WorldDiplomacyBehavior.ResolveKingdom(terms?.TributeReceiverKingdomId) ?? target;
            if (payer == receiver || (payer != proposer && payer != target) || (receiver != proposer && receiver != target))
            { payer = proposer; receiver = target; }
            if (!DiplomacyPeaceTermsService.TryApplyPeace(payer, receiver, Math.Max(0, terms?.DailyTribute ?? 0), Math.Max(0, terms?.DurationDays ?? 0),
                "world_diplomacy_make_peace", out int appliedTribute, out int appliedDays, out string failureReason))
                return new(false, "议和未执行：" + failureReason);
            if (FactionManager.IsAtWarAgainstFaction(proposer, target))
                return new(false, "议和未执行：游戏状态未发生变化");
            return new(true, "双方已达成和平"
                + (appliedTribute > 0 ? "；" + WorldDiplomacyBehavior.KingdomName(payer) + "每日向" + WorldDiplomacyBehavior.KingdomName(receiver) + "支付" + appliedTribute.ToString(CultureInfo.InvariantCulture) + "第纳尔，共" + appliedDays.ToString(CultureInfo.InvariantCulture) + "天" : "")
                );
        }

        public WorldDiplomacyOfferActionReceipt ExecuteAlliance(string proposerId, string targetId)
        {
            Kingdom proposer = ResolveKingdom(proposerId);
            Kingdom target = ResolveKingdom(targetId);
            if (FactionManager.IsAtWarAgainstFaction(proposer, target)) return new(false, "结盟未执行：双方仍处于战争状态");
            IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            if (alliance == null) return new(false, "结盟未执行：同盟系统不可用");
            if (alliance.IsAllyWithKingdom(proposer, target)) return new(false, "结盟未执行：双方已经结盟");
            WorldDiplomacyBehavior.RunDiplomaticAction("world_diplomacy_alliance", () => alliance.StartAlliance(proposer, target));
            return alliance.IsAllyWithKingdom(proposer, target)
                ? new(true, "双方已缔结同盟")
                : new(false, "结盟未执行：游戏状态未发生变化");
        }

        public WorldDiplomacyOfferActionReceipt ExecuteTrade(string proposerId, string targetId)
        {
            Kingdom proposer = ResolveKingdom(proposerId);
            Kingdom target = ResolveKingdom(targetId);
            if (FactionManager.IsAtWarAgainstFaction(proposer, target)) return new(false, "贸易协定未执行：双方仍处于战争状态");
            ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            if (trade == null) return new(false, "贸易协定未执行：贸易系统不可用");
            if (BannerlordApiCompat.HasTradeAgreement(trade, proposer, target)) return new(false, "贸易协定未执行：双方已经有贸易协定");
            CampaignTime duration = Campaign.Current.Models.TradeAgreementModel.GetTradeAgreementDurationInYears(proposer, target);
            WorldDiplomacyBehavior.RunDiplomaticAction("world_diplomacy_trade", () => trade.MakeTradeAgreement(proposer, target, duration));
            return BannerlordApiCompat.HasTradeAgreement(trade, proposer, target)
                ? new(true, "双方已缔结贸易协定")
                : new(false, "贸易协定未执行：游戏状态未发生变化");
        }

        public bool HasTakenEffect(string intent, string proposerId, string targetId)
        {
            Kingdom proposer = WorldDiplomacyBehavior.ResolveKingdom(proposerId);
            Kingdom target = WorldDiplomacyBehavior.ResolveKingdom(targetId);
            if (proposer == null || target == null) return false;
            return WorldDiplomacyIntentVocabulary.NormalizeIntent(intent) switch
            {
                "propose_peace" => proposer != null && target != null && !FactionManager.IsAtWarAgainstFaction(proposer, target),
                "propose_alliance" => Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>()?.IsAllyWithKingdom(proposer, target) == true,
                "propose_trade" => Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>() is ITradeAgreementsCampaignBehavior trade
                    && BannerlordApiCompat.HasTradeAgreement(trade, proposer, target),
                _ => false
            };
        }

        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
    }
}
