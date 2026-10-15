using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class VillageAftermathBehavior
{
    private static readonly VillageDispositionDialogueSession HostileDialogue = new VillageDispositionDialogueSession();
    private static readonly Dictionary<int, Agent> HostileDialogueAgents = new Dictionary<int, Agent>();

    private static void ClearHostileDialogue()
    {
        HostileDialogue.End();
        HostileDialogueAgents.Clear();
    }

    // Called by the existing main-thread prompt capture seam; no new background/game-object reads.
    private static string BuildHostileDialoguePrompt(Settlement village, Hero expectedHero,
        CharacterObject expectedCharacter, int agentIndex)
    {
        var catalog = GcczTownPromptResourceProvider.GetCatalog();
        string scene = VillageDispositionPromptProfile.BuildScenePrompt(village?.Name?.ToString(), _hostileVillageMercyApplied,
            _hostileDispositionActive, !_hostileDispositionActive && CanGovern(village), catalog);
        VillageDispositionSpeakerRole role = ResolveHostileSpeakerRole(agentIndex, out Agent agent);
        CharacterObject character = agent?.Character as CharacterObject;
        Hero hero = character?.HeroObject;
        if ((expectedHero != null && expectedHero != hero)
            || (expectedCharacter != null && expectedCharacter != character)) role = VillageDispositionSpeakerRole.Unknown;
        if (role == VillageDispositionSpeakerRole.Unknown)
            return scene + "\n" + VillageDispositionPromptProfile.BuildRoleContext(role, catalog);

        string key = hero != null ? "hero:" + hero.StringId
            : "agent:" + character.StringId + ":" + agent.Index + ":" + RuntimeHelpers.GetHashCode(agent);
        if (!HostileDialogue.TryGet(_activeVillageId, key, out VillageDispositionSpeakerProfile profile))
        {
            string personality = string.Empty;
            string background = string.Empty;
            try
            {
                if (hero != null) MyBehavior.GetNpcPersonaForExternal(hero, out personality, out background);
                else ShoutUtils.TryGetUnnamedNpcPersona(agent, out personality, out background);
            }
            catch (Exception ex)
            {
                Logger.Log("VillageAftermath", "Read speaker persona failed agent=" + agentIndex + " error=" + ex.Message);
            }
            profile = HostileDialogue.GetOrCreate(_activeVillageId, key, hero?.Name?.ToString() ?? character.Name?.ToString(),
                role, personality, background);
            GcczDiagnosticLog.Log("VillagePersona", "captured village=" + _activeVillageId + " agent=" + agentIndex
                + " role=" + role + " hasAfPersona=" + !string.IsNullOrWhiteSpace(personality));
        }
        return scene + "\n" + HostileDialogue.BuildSpeakerPrompt(profile, role, catalog) + "\n" + BuildForeignBudgetContext();
    }

    private static VillageDispositionSpeakerRole ResolveHostileSpeakerRole(int agentIndex, out Agent agent)
    {
        agent = null;
        if (!IsActive() || agentIndex < 0) return VillageDispositionSpeakerRole.Unknown;
        if (!HostileDialogueAgents.TryGetValue(agentIndex, out Agent cached) || !IsLiveHostileSpeaker(cached))
        {
            cached = _activeMission.Agents?.FirstOrDefault(candidate => candidate?.Index == agentIndex && IsLiveHostileSpeaker(candidate));
            if (cached == null)
            {
                HostileDialogueAgents.Remove(agentIndex);
                return VillageDispositionSpeakerRole.Unknown;
            }
            HostileDialogueAgents[agentIndex] = cached;
        }
        agent = cached;
        CharacterObject character = agent.Character as CharacterObject;
        if (character == null) return VillageDispositionSpeakerRole.Unknown;
        Hero hero = character.HeroObject;
        Settlement village = ResolveActiveVillage();
        bool playerMember = hero != null
            ? hero.PartyBelongedTo == MobileParty.MainParty && !hero.IsPrisoner
            : character.IsSoldier && agent.Origin?.BattleCombatant == PartyBase.MainParty;
        bool residentNotable = hero?.IsAlive == true && hero.IsNotable && !hero.IsPrisoner
            && village?.Notables?.Contains(hero) == true
            && (hero.CurrentSettlement == village || hero.StayingInSettlement == village);
        return VillageDispositionSpeakerPolicy.Resolve(true, hero != null, residentNotable, playerMember, !character.IsSoldier);
    }

    private static bool IsLiveHostileSpeaker(Agent agent) => agent != null && agent.IsHuman && agent.IsActive()
        && !agent.IsMainAgent && agent.State != AgentState.Killed && agent.State != AgentState.Unconscious
        && VillageDispositionSceneMissionBehavior.IsAdult(agent.Character as CharacterObject)
        && ReferenceEquals(agent.Mission, _activeMission);
}
