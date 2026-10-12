using AnimusForge;
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
                return new(false, DiplomacyMiscPromptTexts.Get("Offer_MakePeaceNotAtWar", "Peace negotiation not executed: factions are not currently at war"));
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
            if (!receipt.PeaceApplied) return new(false, receipt.PeaceKnown ? DiplomacyMiscPromptTexts.Get("Offer_PeaceNotExecutedActionNotEffective", "Peace negotiation not executed: peace action did not take effect") : DiplomacyMiscPromptTexts.Get("Offer_PeaceResultUnconfirmed", "Peace negotiation outcome cannot be confirmed"), known: receipt.PeaceKnown);
            string message = DiplomacyMiscPromptTexts.Get("Offer_BothSidesPeace", "Both sides have made peace");
            if (receipt.Complete && receipt.ActualDailyTribute > 0)
                message += string.Format(DiplomacyMiscPromptTexts.Get("Offer_TributeClauseFulfillmentFormat", "; {0} pays {2} denars daily to {1} for {3} days"), KingdomName(payer), KingdomName(receiver), receipt.ActualDailyTribute.ToString(CultureInfo.InvariantCulture), receipt.ActualDurationDays.ToString(CultureInfo.InvariantCulture));
                    
                    
            if (!receipt.Complete) message += DiplomacyMiscPromptTexts.Get("Offer_TributeClauseIncompleteOrUnconfirmed", "; tribute terms not fully fulfilled or unconfirmed");
            if (!string.IsNullOrEmpty(receipt.Diagnostic)) WorldDiplomacyBehavior.Log("peace receipt: " + receipt.Diagnostic);
            return new(true, message, receipt.Complete);
        }

        public WorldDiplomacyOfferActionReceipt ExecuteAlliance(string proposerId, string targetId)
        {
            Kingdom proposer = ResolveKingdom(proposerId);
            Kingdom target = ResolveKingdom(targetId);
            if (FactionManager.IsAtWarAgainstFaction(proposer, target)) return new(false, DiplomacyMiscPromptTexts.Get("Offer_AllianceStillAtWar", "Alliance not executed: factions are still at war"));
            IAllianceCampaignBehavior alliance = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
            if (alliance == null) return new(false, DiplomacyMiscPromptTexts.Get("Offer_AllianceSystemUnavailable", "Alliance not executed: alliance system is unavailable"));
            if (alliance.IsAllyWithKingdom(proposer, target)) return new(false, DiplomacyMiscPromptTexts.Get("Offer_AllianceAlreadyAllied", "Alliance not executed: factions are already allied"));
            DiplomacyEffectReadback result = DiplomacyEffectReadback.Execute(
                () => WorldDiplomacyBehavior.RunDiplomaticAction("world_diplomacy_alliance", () => alliance.StartAlliance(proposer, target)),
                () => alliance.IsAllyWithKingdom(proposer, target));
            return result.Applied
                ? new(true, DiplomacyMiscPromptTexts.Get("Offer_BothSidesAlliance", "Both sides have formed an alliance"))
                : new(false, result.IsKnown ? DiplomacyMiscPromptTexts.Get("Offer_AllianceNoStateChange", "Alliance not executed: game state did not change") : DiplomacyMiscPromptTexts.Get("Offer_AllianceResultUnconfirmed", "Alliance outcome cannot be confirmed"), known: result.IsKnown);
        }

        public WorldDiplomacyOfferActionReceipt ExecuteTrade(string proposerId, string targetId)
            => ExecuteTrade(proposerId, targetId, 0);

        public WorldDiplomacyOfferActionReceipt ExecuteTrade(string proposerId, string targetId, int durationDays)
        {
            Kingdom proposer = ResolveKingdom(proposerId);
            Kingdom target = ResolveKingdom(targetId);
            if (FactionManager.IsAtWarAgainstFaction(proposer, target)) return new(false, DiplomacyMiscPromptTexts.Get("Offer_TradeStillAtWar", "Trade agreement not executed: factions are still at war"));
            ITradeAgreementsCampaignBehavior trade = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
            if (trade == null) return new(false, DiplomacyMiscPromptTexts.Get("Offer_TradeSystemUnavailable", "Trade agreement not executed: trade system is unavailable"));
            if (!BannerlordApiCompat.TryGetTradeAgreementState(trade, proposer, target, out bool trading))
                return new(false, DiplomacyMiscPromptTexts.Get("Offer_TradeStatusUnconfirmed", "Trade agreement not executed: current status cannot be confirmed"), known: false);
            if (trading) return new(false, DiplomacyMiscPromptTexts.Get("Offer_TradeAlreadyTrading", "Trade agreement not executed: factions already have a trade agreement"));
            CampaignTime duration = durationDays > 0 ? CampaignTime.Days(durationDays)
                : Campaign.Current.Models.TradeAgreementModel.GetTradeAgreementDurationInYears(proposer, target);
            DiplomacyEffectReadback result = DiplomacyEffectReadback.Execute(
                () => WorldDiplomacyBehavior.RunDiplomaticAction("world_diplomacy_trade", () => trade.MakeTradeAgreement(proposer, target, duration)),
                () => BannerlordApiCompat.TryGetTradeAgreementState(trade, proposer, target, out bool active)
                    ? active : throw new InvalidOperationException("trade readback unavailable"));
            return result.Applied
                ? new(true, DiplomacyMiscPromptTexts.Get("Offer_BothSidesTrade", "Both sides have concluded a trade agreement"))
                : new(false, result.IsKnown ? DiplomacyMiscPromptTexts.Get("Offer_TradeNoStateChange", "Trade agreement not executed: game state did not change") : DiplomacyMiscPromptTexts.Get("Offer_TradeResultUnconfirmed", "Trade agreement outcome cannot be confirmed"), known: result.IsKnown);
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
