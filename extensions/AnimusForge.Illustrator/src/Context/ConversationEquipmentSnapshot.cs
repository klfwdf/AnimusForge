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
            Equipment fallback = character.Equipment ?? character.FirstBattleEquipment;
            if (fallback != null)
            {
                source = "普通NPC标准会话装备（无现场Agent）";
                var visible = CharacterAppearanceSnapshot.VisibleEquipment(fallback);
                bodyProperties = character.GetBodyProperties(visible, -1).ToString();
                appearance = CharacterAppearanceSnapshot.FromCharacter(character, fallback);
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
