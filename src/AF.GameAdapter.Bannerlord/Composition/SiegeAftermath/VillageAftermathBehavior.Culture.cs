using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class VillageAftermathBehavior
{
    private static int _cultureSpeaker = -1;
    private static int _culturePreparedTurn;
    private static int _cultureQuestionTurn;
    private static string _cultureTarget = "";
    private static VillageCultureChangeMode _cultureMethod;
    private static bool _cultureUiOpen;
    private Dictionary<string, int> _purgeUntil = new Dictionary<string, int>();

    private static string CultureRuleContext(bool postprocess = true)
    {
        if (_cultureTarget.Length == 0)
            return VP("文化改造仅能与随行英雄讨论；先打开方式和文化选择，尚未施行。选完后必须由同一英雄自然询问村名、方式和目标文化，请玩家再次明确确认，不要使用固定台词。", "Cultural reform is discussed only with an accompanying hero. First select method and culture; nothing has been enacted. The same hero must then naturally ask the player to explicitly confirm the village, method and culture. Do not use scripted dialogue.");
        string stage = !postprocess ? (_cultureQuestionTurn == 0
            ? VP("请选定的同伴先自然询问玩家是否确认这个方案；本轮只是询问，尚未执行。", "The selected companion must naturally ask whether the player confirms this proposal. This reply is only a question, not execution.")
            : VP("同伴已询问。只有玩家下一轮明确确认当前方案才可施行；否则取消并重新选择。", "The companion has asked. Enact only after the player's next explicit confirmation of this exact proposal; otherwise cancel and restart selection."))
            : _cultureQuestionTurn == 0
            ? VP("先由选定的同伴自然询问方案，询问后只输出[VILLAGE_ACTION:核对文化方案]，此轮禁止执行改造。", "The selected companion must first naturally ask about this proposal, then emit only [VILLAGE_ACTION:核对文化方案]. Do not enact reform in that reply.")
            : VP("同伴已经询问。只有玩家下一轮明确确认当前方案，才输出[VILLAGE_ACTION:文化改造]；否则取消方案。", "The companion has asked. Emit [VILLAGE_ACTION:文化改造] only if the player's next reply explicitly confirms this proposal; otherwise cancel it.");
        return stage + "\n" + VP("【待确认，不是既成事实】村庄=", "[Pending confirmation, NOT an accomplished fact] Village=") + ResolveActiveVillage()?.Name
            + "; method=" + (_cultureMethod == VillageCultureChangeMode.PurgeColonization ? VP("屠村迁殖", "Purge and resettlement") : VP("移民融合", "Migrant integration"))
            + "; target=" + (Game.Current?.ObjectManager?.GetObject<CultureObject>(_cultureTarget)?.Name?.ToString() ?? _cultureTarget)
            + VP("；费用10000。含糊、拒绝、换话题都不得执行并须重新发起；未完成前不要声称已扣费或改造。", "; cost=10000. Ambiguity, refusal or changing topics cancels it; restart selection. Do not claim payment or conversion before execution.");
    }

    private static bool HandleCultureConversation(int index)
    {
        var village = ResolveActiveVillage(); var owner = EconomyOwner;
        if (!CanGovern(village) || owner == null || ResolveHostileSpeakerRole(index, out _) != VillageDispositionSpeakerRole.PlayerCompanion) return false;
        if (_cultureUiOpen || _cultureInquiryPending || owner._gradualCultureTargetByVillageId.ContainsKey(village.StringId)) return false;
        if (_cultureTarget.Length > 0)
        {
            if (!VillageGovernanceRules.CanConfirmCulture(_cultureSpeaker, index, _villageReplySequence, _cultureQuestionTurn)) return false;
            var target = Game.Current?.ObjectManager?.GetObject<CultureObject>(_cultureTarget);
            var mode = _cultureMethod;
            ClearCultureProposal();
            if (target == null || Hero.MainHero.Gold < VillageGovernanceRules.CultureCost) return false;
            if (mode == VillageCultureChangeMode.PurgeColonization)
            {
                if (!ApplyVillageCultureNow(village, target, true, false, "village_purge_completed")) return false;
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, VillageGovernanceRules.CultureCost, true);
                ApplyForeignEconomicAction(Spec(VillageDispositionActionKind.MassacreAndSeize), index);
                owner._purgeUntil[village.StringId] = CurrentDay + VillageGovernanceRules.CultureDays;
                ApplyCulturePolitics(village);
                RecordCultureCompletion(village, target);
            }
            else
            {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, VillageGovernanceRules.CultureCost, true);
                owner._gradualCultureTargetByVillageId[village.StringId] = target.StringId;
                owner._gradualCultureFinishDayByVillageId[village.StringId] = CurrentDay
                    + VillageGovernanceRules.CultureDuration(false, target == Hero.MainHero.Culture);
                ApplyCulturePolitics(village);
                InformationManager.DisplayMessage(new InformationMessage(VUi("移民融合已开始；完成前兵种和生产照旧。", "Migrant integration has begun; troops and production remain unchanged until completion.")));
            }
            Departure.MarkDisposition();
            return true;
        }
        _cultureSpeaker = index; _cultureInquiryPending = true;
        return true;
    }

    private static void ClearCultureProposal()
    { _cultureTarget = ""; _cultureSpeaker = -1; _culturePreparedTurn = 0; _cultureQuestionTurn = 0; _cultureUiOpen = false; _cultureInquiryPending = false; }

    private static void OpenGovernanceCultureInquiry()
    {
        var mission = _activeMission; var village = ResolveActiveVillage();
        if (!CanGovern(village) || !IsActive()) { ClearCultureProposal(); return; }
        _cultureUiOpen = true;
        bool Valid() => ReferenceEquals(mission, _activeMission) && IsActive() && ResolveActiveVillage() == village && CanGovern(village);
        var methods = new List<InquiryElement>
        {
            new InquiryElement(VillageCultureChangeMode.PurgeColonization, VUi("屠村迁殖", "Purge and resettlement"), null, true,
                VUi("立即改造，继承屠村后果；84天内生产与成长为20%。费用10000。", "Immediate conversion with massacre consequences; production and growth at 20% for 84 days. Cost: 10000.")),
            new InquiryElement(VillageCultureChangeMode.GradualEducation, VUi("移民融合", "Migrant integration"), null, true,
                VUi("84天完成，与玩家同文化则42天；无生产惩罚。费用10000。", "Completes in 84 days, or 42 for the player's culture. No production penalty. Cost: 10000."))
        };
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(VUi("文化改造方式", "Cultural reform method"),
            VUi("选择只是准备，还需回到同伴处口头确认。", "Selection only prepares a proposal. Return to the same companion and explicitly confirm."), methods, true, 1, 1,
            VUi("下一步", "Next"), VUi("取消", "Cancel"), selected =>
            {
                if (!Valid()) return;
                if (selected.Count != 1) { ClearCultureProposal(); return; }
                _cultureMethod = (VillageCultureChangeMode)selected[0].Identifier;
                var cultures = Game.Current.ObjectManager.GetObjectTypeList<CultureObject>()
                    .Where(c => c != null && !string.IsNullOrEmpty(c.StringId) && c != village.Culture)
                    .Select(c => new InquiryElement(c.StringId, c.Name.ToString(), null)).ToList();
                if (cultures.Count == 0) { ClearCultureProposal(); return; }
                MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(VUi("选择文化", "Choose culture"),
                    VUi("包含已加载模组中的文化。选定不会直接执行。", "Includes cultures from loaded mods. Selecting one does not enact the reform."), cultures, true, 1, 1,
                    VUi("准备方案", "Prepare proposal"), VUi("取消", "Cancel"), choices =>
                    {
                        if (!Valid()) return;
                        if (choices.Count != 1) { ClearCultureProposal(); return; }
                        _cultureTarget = (string)choices[0].Identifier; _culturePreparedTurn = _villageReplySequence; _cultureQuestionTurn = 0; _cultureUiOpen = false;
                        InformationManager.DisplayMessage(new InformationMessage(VUi("方案已准备。请与刚才的同伴核对方案，再明确确认；尚未扣费或改造。", "Proposal ready. Check it with the same companion, then explicitly confirm. No fee or conversion yet.")));
                    }, _ => { if (ReferenceEquals(mission, _activeMission)) ClearCultureProposal(); }, "", true), true);
            }, _ => { if (ReferenceEquals(mission, _activeMission)) ClearCultureProposal(); }), true);
    }

    private static void ApplyCulturePolitics(Settlement village)
    {
        Hero ruler = Clan.PlayerClan?.Kingdom?.Leader;
        if (ruler == Hero.MainHero) ApplyVillagePoliticalPenalty(village.Village.Bound?.OwnerClan?.Leader, -20);
        else ApplyVillagePoliticalPenalty(ruler, -10);
    }

    private static void ApplyVillagePoliticalPenalty(Hero hero, int requested)
    {
        if (hero == null || !hero.IsAlive || hero == Hero.MainHero) return;
        bool merciful = hero.GetTraitLevel(DefaultTraits.Mercy) > 0;
        int delta = ForeignPolitics.AdditionalPenalty(hero.StringId, requested, merciful);
        if (delta == 0) return;
        ForeignPolitics.CommitPenalty(hero.StringId, requested, merciful);
        ChangeRelationAction.ApplyPlayerRelation(hero, delta, true, true);
    }

    private static void RecordCultureCompletion(Settlement village, CultureObject culture)
    {
        string text = Hero.MainHero.Name + VP("在", " completed cultural reform in ") + village.Name
            + VP("完成文化改造，目标文化为", "; target culture: ") + culture.Name;
        string key = "gccz.village.culture." + village.StringId + "." + CurrentDay + "." + culture.StringId;
        Hero owner = village.Village.Bound?.OwnerClan?.Leader;
        Hero ruler = Clan.PlayerClan?.Kingdom?.Leader;
        if (owner != null && owner != Hero.MainHero) MyBehavior.AppendExternalPlayerFact(owner, text);
        MyBehavior.RecordPlayerActionForExternal(text, key, "gccz_village_culture_completed", true, targetHero: owner, settlement: village);
        if (owner == Hero.MainHero && ruler != null && ruler != Hero.MainHero)
            { MyBehavior.AppendExternalPlayerFact(ruler, text);
            MyBehavior.RecordPlayerActionForExternal(text, key + ".ruler", "gccz_village_culture_completed", true, targetHero: ruler, settlement: village); }
        InformationManager.DisplayMessage(new InformationMessage(VUi("村庄文化改造完成：", "Village cultural reform completed: ") + village.Name + " → " + culture.Name));
    }
}
