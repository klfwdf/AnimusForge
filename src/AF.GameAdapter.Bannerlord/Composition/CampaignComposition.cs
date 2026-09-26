using AFWarStatsTerminal.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace AnimusForge.Refactor.Modules;

/// <summary>
/// Campaign 回调的装配清单；只创建原行为，不接管模块玩法、存档或请求就绪判断。
/// 由唯一框架入口调用，实例交给 CampaignGameStarter；不静态缓存，不跨档复用。
/// 顺序沿用原 SubModule：模型先于行为；不能按目录名重排或并行注册。
/// </summary>
internal static class CampaignComposition
{
    internal static void Register(IGameStarter starterObject)
    {
        if (starterObject is CampaignGameStarter campaignGameStarter)
        {
            CampaignModelComposition.Register(campaignGameStarter);
            campaignGameStarter.AddBehavior(new ModOnboardingBehavior());
            campaignGameStarter.AddBehavior(new MyBehavior());
            campaignGameStarter.AddBehavior(new KingdomStrategicProfileBehavior());
            campaignGameStarter.AddBehavior(new ShoutBehavior());
            campaignGameStarter.AddBehavior(new CourierDeliveryBehavior());
            campaignGameStarter.AddBehavior(new DuelBehavior());
            campaignGameStarter.AddBehavior(new RewardSystemBehavior());
            campaignGameStarter.AddBehavior(new PlayerNotorietyBehavior());
            campaignGameStarter.AddBehavior(new AnimusForgeTerminalBehavior());
            campaignGameStarter.AddBehavior(new AnimusForgeUniqueCosmeticItemBehavior());
            campaignGameStarter.AddBehavior(new CustomPolicyBehavior());
            campaignGameStarter.AddBehavior(new NpcRulerPolicyBehavior());
            campaignGameStarter.AddBehavior(new AnimusForgeWorldEventBehavior());
            campaignGameStarter.AddBehavior(new WorldMessageTimelineMenuBehavior());
            campaignGameStarter.AddBehavior(new RomanceSystemBehavior());
            campaignGameStarter.AddBehavior(new KnowledgeLibraryBehavior());
            campaignGameStarter.AddBehavior(new LordEncounterBehavior());
            campaignGameStarter.AddBehavior(new ProactiveNpcRequestBehavior());
            campaignGameStarter.AddBehavior(new CompanionProactiveChatBehavior());
            campaignGameStarter.AddBehavior(new SceneTauntBehavior());
            campaignGameStarter.AddBehavior(new GcczSettlementCulturePersistenceBehavior());
            campaignGameStarter.AddBehavior(new SiegeAiInterventionBehavior());
            campaignGameStarter.AddBehavior(new VillageAftermathBehavior());
            campaignGameStarter.AddBehavior(new SettlementEntryTroopSelectionBehavior());
            campaignGameStarter.AddBehavior(new NoblePrisonerEscortBehavior());
            campaignGameStarter.AddBehavior(new NoblePrisonerExecutionOrderBehavior());
            campaignGameStarter.AddBehavior(new VoteDealBehavior());
            campaignGameStarter.AddBehavior(new WorldDiplomacyBehavior());
            campaignGameStarter.AddBehavior(new DiplomacyBehavior());
            campaignGameStarter.AddBehavior(new VanillaIssuePromptBehavior());
            campaignGameStarter.AddBehavior(new WorldMapPartyCommandBehavior());
            campaignGameStarter.AddBehavior(new NobleGatheringBehavior());
            campaignGameStarter.AddBehavior(new VassalageBehavior());
            campaignGameStarter.AddBehavior(new NpcTributeVassalageBehavior());
            campaignGameStarter.AddBehavior(new KingdomAnnexationBehavior());
            campaignGameStarter.AddBehavior(new AfWarStatsBehavior());
        }
    }
}
