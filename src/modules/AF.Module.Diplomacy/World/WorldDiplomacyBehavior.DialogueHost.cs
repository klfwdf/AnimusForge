using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private sealed partial class OrchestrationHost : IWorldDiplomacyDialogueHost
    {
        public bool RulerAlive(string id) => !string.IsNullOrEmpty(id) && Hero.Find(id)?.IsAlive == true;
        public bool IsPlayerRuler(string id) => Hero.MainHero?.StringId == id;
        public string RulerName(string id) => Hero.Find(id)?.Name?.ToString() ?? "统治者";
        public (bool available, bool cycle, bool existing) TreatyState(string receivingId, string joiningId)
        {
            var owner = Campaign.Current?.GetCampaignBehavior<VassalageBehavior>();
            return (owner != null, owner?.WouldCreateVassalageCycleForBridge(receivingId, joiningId, out _) == true,
                owner?.GetAnyVassalageAgreementForBridge(ResolveKingdom(joiningId)) != null);
        }
        public (bool success, bool changed, string reason) ExecuteTreaty(string intent, string receivingId, string joiningId)
        {
            var receiving = ResolveKingdom(receivingId); var joining = ResolveKingdom(joiningId);
            if (receiving == null || joining == null) return (false, false, "treaty_participants_not_available");
            string reason = "treaty_runtime_unavailable";
            bool success;
            bool changed;
            if (intent == "propose_annexation")
            {
                int before = joining.Clans.Count;
                success = Campaign.Current?.GetCampaignBehavior<KingdomAnnexationBehavior>()
                    ?.TryExecuteFormalAnnexation(receiving, joining, out reason) == true;
                changed = success || joining.IsEliminated || joining.Clans.Count != before;
            }
            else
            {
                var type = intent == "propose_tributary" ? AfVassalageType.Tributary
                    : intent == "propose_garrison" ? AfVassalageType.Garrison : AfVassalageType.Vassal;
                success = Campaign.Current?.GetCampaignBehavior<VassalageBehavior>()
                    ?.TryExecuteFormalVassalage(receiving, joining, type, out reason) == true;
                changed = success;
            }
            return (success, changed, reason);
        }
        public MemoryCommitResult CommitFact(string rulerId, string sourceId, string fact, int day,
            string locationId, int hour = -1, string npcName = null, string gameDate = "") =>
            MyBehavior.CommitDiplomacyFactForExternal(Hero.Find(rulerId), sourceId, fact, day, locationId, hour, npcName, gameDate);
        public string PersonalMemory(string rulerId, string topic, string counterpart) =>
            MyBehavior.BuildDiplomacyPersonalMemoryForExternal(Hero.Find(rulerId), topic, counterpart);
    }
}
