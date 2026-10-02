using AnimusForge.Refactor.Domain;
namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal struct CampaignSource : IWorldDiplomacyCampaignSource
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal CampaignSource(WorldDiplomacyBehavior owner) { _owner = owner; }
        public bool IsEnabled => IsWorldDiplomacyEnabled();
        public int CurrentDay => WorldDiplomacyBehavior.CurrentDay();
        public bool DisabledStateApplied { get => _owner._runtime.DisabledStateApplied; set => _owner._runtime.DisabledStateApplied = value; }
        public bool NativeQueueSanitized { get => _owner._runtime.NativeQueueSanitized; set => _owner._runtime.NativeQueueSanitized = value; }
        public int LastSchedulerDay { get => _owner._runtime.LastSchedulerDay; set => _owner._runtime.LastSchedulerDay = value; }
        public void RemoveQueuedNativeDiplomacyDecisions() => _owner.RemoveQueuedNativeDiplomacyDecisions();
        public void ClearDailyCaches()
        {
            _owner._warSituationCache.Clear();
            _owner._realmRelationProfileCache.Clear();
            _owner._courtSettlementCache.Clear();
            _owner._kingdomBorderCache.Clear();
            _owner._kingdomBorderCacheDay = -1;
        }
        public void ResetDailyGenerationBudget() => WorldDiplomacyRoundLifecycleRules.ResetDailyGenerationBudget(
            ref _owner._runtime.AiDocumentsStartedDay, ref _owner._runtime.AiDocumentsStartedToday, CurrentDay);
    }
}
