using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using AnimusForge.Illustrator.Core;

namespace AnimusForge.Illustrator.Context
{
    // Managed evidence captured once on the game thread; never retain a native Agent in diagnostics.
    internal sealed class ConversationNpcAgeSnapshot
    {
        internal float? RawAge { get; }
        internal int? AgentIndex { get; }
        internal string Source { get; }
        internal int Age => RawAge.HasValue && !float.IsNaN(RawAge.Value) &&
            !float.IsInfinity(RawAge.Value) && RawAge.Value > 0 && RawAge.Value < int.MaxValue
                ? (int)RawAge.Value : 0;

        internal ConversationNpcAgeSnapshot(float? age, int? agentIndex, string source)
        { RawAge = age; AgentIndex = agentIndex; Source = source; }
    }

    internal static class ConversationEquipmentSnapshot
    {
        // Runs once per request on the main thread; inspect only the player and conversation agents.
        internal static Equipment Capture(Hero hero, bool civilian, out string source, out CharacterAppearanceSnapshot appearance)
        {
            IllustratorRuntime.AssertMainThread();
            appearance = null;
            source = civilian ? IllustratorText.Get("EquipmentCivilianSource") : IllustratorText.Get("EquipmentBattleSource");
            Agent agent = Mission.Current?.MainAgent;
            if (!Matches(agent, hero))
            {
                agent = null;
                var participants = Campaign.Current?.ConversationManager?.ConversationAgents;
                if (participants != null)
                    for (int i = 0; i < participants.Count; i++)
                    {
                        var candidate = participants[i] as Agent;
                        if (Matches(candidate, hero)) { agent = candidate; break; }
                    }
            }
            if (agent?.SpawnEquipment != null)
            {
                source = IllustratorText.Get("EquipmentMatchedAgentSource");
                var equipment = CopyAgentEquipment(agent);
                appearance = CharacterAppearanceSnapshot.FromAgent(agent, equipment);
                return equipment;
            }
            Equipment fallback = civilian ? hero?.CivilianEquipment : hero?.BattleEquipment;
            appearance = CharacterAppearanceSnapshot.FromHero(hero, fallback);
            return fallback == null ? null : new Equipment(fallback);
        }

        internal static Equipment CaptureCharacter(CharacterObject character, out string source, out string bodyProperties, out CharacterAppearanceSnapshot appearance,
            out ConversationNpcAgeSnapshot ageSnapshot)
        {
            IllustratorRuntime.AssertMainThread();
            source = IllustratorText.Get("EquipmentNpcMissingSource");
            bodyProperties = null;
            appearance = null;
            ageSnapshot = new ConversationNpcAgeSnapshot(null, null, "unconfirmed_no_current_agent");
            if (character == null) return null;
            // CharacterObject identifies a troop template, not an individual. Only the exact
            // one-to-one participant may provide this NPC's age, body and equipment.
            var agent = Campaign.Current?.ConversationManager?.OneToOneConversationAgent as Agent;
            if (agent != null && agent.IsActive() && ReferenceEquals(agent.Character, character))
            {
                var body = agent.BodyPropertiesValue;
                bodyProperties = body.ToString();
                ageSnapshot = new ConversationNpcAgeSnapshot(body.Age, agent.Index, "current_conversation_agent_body");
                if (agent.SpawnEquipment == null)
                {
                    source = IllustratorText.Get("EquipmentNpcSnapshotMissingSource");
                    return null;
                }
                source = IllustratorText.Get("EquipmentNpcInstanceSource");
                var equipment = CopyAgentEquipment(agent);
                appearance = CharacterAppearanceSnapshot.FromAgent(agent, equipment, body);
                return equipment;
            }
            // A live mission without an exact participant is not a map-conversation fallback.
            if (Mission.Current != null) return null;

            // 原版大地图会面算法：按部队成员种子精确选定当前屏幕正在展示的装备变体与面容
            var party = TaleWorlds.CampaignSystem.Party.MobileParty.ConversationParty?.Party ?? TaleWorlds.CampaignSystem.Encounters.PlayerEncounter.EncounteredParty;
            int seed = -1;
            try
            {
                if (party != null)
                {
                    seed = Helpers.CharacterHelper.GetPartyMemberFaceSeed(party, character, 0);
                }
            }
            catch { }
            if (seed == -1)
            {
                try { seed = character.GetDefaultFaceSeed(0); } catch { seed = 0; }
            }

            Equipment fallback = null;
            try
            {
                var battleEquipments = character.BattleEquipments;
                int count = battleEquipments != null ? System.Linq.Enumerable.Count(battleEquipments) : 0;
                if (count > 0)
                {
                    int variantIndex = Math.Abs(character.GetDefaultFaceSeed(0)) % count;
                    fallback = System.Linq.Enumerable.ElementAt(battleEquipments, variantIndex);
                }
            }
            catch { }

            if (fallback == null)
            {
                fallback = character.Equipment ?? character.FirstBattleEquipment;
            }

            if (fallback != null)
            {
                source = IllustratorText.Get("EquipmentNpcMapSource");
                var visible = CharacterAppearanceSnapshot.VisibleEquipment(fallback);
                BodyProperties bp = character.GetBodyProperties(visible, seed);
                bodyProperties = bp.ToString();
                appearance = CharacterAppearanceSnapshot.FromCharacter(character, fallback, seed);
                return new Equipment(fallback);
            }
            return null;
        }

        private static Equipment CopyAgentEquipment(Agent agent)
        {
            var snapshot = new Equipment(agent.SpawnEquipment);
            if (agent.Equipment != null)
                for (EquipmentIndex slot = EquipmentIndex.Weapon0; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
                {
                    var weapon = agent.Equipment[slot];
                    var original = snapshot[slot];
                    snapshot[slot] = new EquipmentElement(weapon.Item, weapon.ItemModifier, weapon.Item == original.Item ? original.CosmeticItem : null);
                }
            return snapshot;
        }

        private static bool Matches(Agent agent, Hero hero)
        {
            return hero != null && agent != null && agent.IsActive() &&
                ReferenceEquals(agent.Character, hero.CharacterObject);
        }
    }
}
