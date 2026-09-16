using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using AnimusForge.Illustrator.Core;

namespace AnimusForge.Illustrator.Context
{
    internal static class ConversationEquipmentSnapshot
    {
        // Runs once per request on the main thread; inspect only the player and conversation agents.
        internal static Equipment Capture(Hero hero, bool civilian, out string source, out CharacterAppearanceSnapshot appearance)
        {
            IllustratorRuntime.AssertMainThread();
            appearance = null;
            source = civilian ? "人物便服装备栏（无匹配现场Agent）" : "人物战斗装备栏（无匹配现场Agent）";
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
                source = "匹配身份的现场Agent完整装备（5穿戴槽、5武器槽、坐骑及挽具槽）；文字和离屏参考图共用快照";
                var equipment = CopyAgentEquipment(agent);
                appearance = CharacterAppearanceSnapshot.FromAgent(agent, equipment);
                return equipment;
            }
            Equipment fallback = civilian ? hero?.CivilianEquipment : hero?.BattleEquipment;
            appearance = CharacterAppearanceSnapshot.FromHero(hero, fallback);
            return fallback == null ? null : new Equipment(fallback);
        }

        internal static Equipment CaptureCharacter(CharacterObject character, out string source, out string bodyProperties, out CharacterAppearanceSnapshot appearance)
        {
            IllustratorRuntime.AssertMainThread();
            source = "未找到当前普通NPC的现场装备";
            bodyProperties = null;
            appearance = null;
            if (character == null) return null;
            var participants = Campaign.Current?.ConversationManager?.ConversationAgents;
            if (participants != null)
                for (int i = 0; i < participants.Count; i++)
                {
                    var agent = participants[i] as Agent;
                    if (agent == null || !agent.IsActive() || !ReferenceEquals(agent.Character, character)) continue;
                    if (agent.SpawnEquipment == null) continue;
                    bodyProperties = agent.BodyPropertiesValue.ToString();
                    source = "普通NPC会话Agent完整装备；不使用兵种模板重新随机装备";
                    var equipment = CopyAgentEquipment(agent);
                    appearance = CharacterAppearanceSnapshot.FromAgent(agent, equipment);
                    return equipment;
                }

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
                source = "普通NPC大地图标准会话装备（匹配当前会话变体）";
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
