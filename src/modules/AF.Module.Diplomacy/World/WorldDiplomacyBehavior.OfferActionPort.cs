using System;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Adapters;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private sealed class OfferActionPort : IWorldDiplomacyOfferActionPort, IWorldDiplomacyTimedTradePort
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal OfferActionPort(WorldDiplomacyBehavior owner) => _owner = owner;

        public WorldDiplomacyStorage Storage => _owner._storage;
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public WorldDiplomacyRound ResolveRound(string id) => _owner.ResolveRound(id);
        public WorldDiplomacyDocument ResolveDocument(string id) => _owner.ResolveDocument(id);
        public WorldDiplomacyCessionReceipt ApplyCession(string proposerId, string targetId, WorldDiplomacyPeaceTerms terms)
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
            return PeaceReceipt(DiplomacyPeaceTermsService.ApplyPeace(payer, receiver,
                Math.Max(0, terms?.DailyTribute ?? 0), Math.Max(0, terms?.DurationDays ?? 0),
                "world_diplomacy_make_peace"), payer, receiver);
        }

        public WorldDiplomacyOfferActionReceipt ReadPeace(string proposerId, string targetId, WorldDiplomacyPeaceTerms terms)
        {
            Kingdom payer = ResolveKingdom(terms?.TributePayerKingdomId) ?? ResolveKingdom(proposerId);
            Kingdom receiver = ResolveKingdom(terms?.TributeReceiverKingdomId) ?? ResolveKingdom(targetId);
            return PeaceReceipt(DiplomacyPeaceTermsService.ConfirmPeace(payer, receiver,
                Math.Max(0, terms?.DailyTribute ?? 0), Math.Max(0, terms?.DurationDays ?? 0),
                "world_diplomacy_make_peace"), payer, receiver);
        }

        private static WorldDiplomacyOfferActionReceipt PeaceReceipt(DiplomacyPeaceEffectReceipt receipt, Kingdom payer, Kingdom receiver)
        {
            if (!receipt.PeaceApplied) return new(false, receipt.PeaceKnown ? "议和未执行：和平动作未生效" : "议和结果无法确认", known: receipt.PeaceKnown);
            string message = "双方已达成和平";
            if (receipt.Complete && receipt.ActualDailyTribute > 0)
                message += "；" + KingdomName(payer) + "每日向" + KingdomName(receiver) + "支付"
                    + receipt.ActualDailyTribute.ToString(CultureInfo.InvariantCulture) + "第纳尔，共"
                    + receipt.ActualDurationDays.ToString(CultureInfo.InvariantCulture) + "天";
            if (!receipt.Complete) message += "；贡金条款未完整履行或无法确认";
            if (!string.IsNullOrEmpty(receipt.Diagnostic)) WorldDiplomacyBehavior.Log("peace receipt: " + receipt.Diagnostic);
            return new(true, message, receipt.Complete);
        }

        public WorldDiplomacyOfferActionReceipt ExecuteAlliance(string proposerId, string targetId)
        {
            Kingdom proposer = ResolveKingdom(proposerId);
            Kingdom target = ResolveKingdom(targetId);
            if (FactionManager.IsAtWarAgainstFaction(proposer, target)) return new(false, "结盟未执行：双方仍处于战争状态");
            IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            if (alliance == null) return new(false, "结盟未执行：同盟系统不可用");
            if (alliance.IsAllyWithKingdom(proposer, target)) return new(false, "结盟未执行：双方已经结盟");
            DiplomacyEffectReadback result = DiplomacyEffectReadback.Execute(
                () => WorldDiplomacyBehavior.RunDiplomaticAction("world_diplomacy_alliance", () => alliance.StartAlliance(proposer, target)),
                () => alliance.IsAllyWithKingdom(proposer, target));
            return result.Applied
                ? new(true, "双方已缔结同盟")
                : new(false, result.IsKnown ? "结盟未执行：游戏状态未发生变化" : "结盟结果无法确认", known: result.IsKnown);
        }

        public WorldDiplomacyOfferActionReceipt ExecuteTrade(string proposerId, string targetId)
            => ExecuteTrade(proposerId, targetId, 0);

        public WorldDiplomacyOfferActionReceipt ExecuteTrade(string proposerId, string targetId, int durationDays)
        {
            Kingdom proposer = ResolveKingdom(proposerId);
            Kingdom target = ResolveKingdom(targetId);
            if (FactionManager.IsAtWarAgainstFaction(proposer, target)) return new(false, "贸易协定未执行：双方仍处于战争状态");
            ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            if (trade == null) return new(false, "贸易协定未执行：贸易系统不可用");
            if (!BannerlordApiCompat.TryGetTradeAgreementState(trade, proposer, target, out bool trading))
                return new(false, "贸易协定未执行：当前状态无法确认", known: false);
            if (trading) return new(false, "贸易协定未执行：双方已经有贸易协定");
            CampaignTime duration = durationDays > 0 ? CampaignTime.Days(durationDays)
                : Campaign.Current.Models.TradeAgreementModel.GetTradeAgreementDurationInYears(proposer, target);
            DiplomacyEffectReadback result = DiplomacyEffectReadback.Execute(
                () => WorldDiplomacyBehavior.RunDiplomaticAction("world_diplomacy_trade", () => trade.MakeTradeAgreement(proposer, target, duration)),
                () => BannerlordApiCompat.TryGetTradeAgreementState(trade, proposer, target, out bool active)
                    ? active : throw new InvalidOperationException("trade readback unavailable"));
            return result.Applied
                ? new(true, "双方已缔结贸易协定")
                : new(false, result.IsKnown ? "贸易协定未执行：游戏状态未发生变化" : "贸易协定结果无法确认", known: result.IsKnown);
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
