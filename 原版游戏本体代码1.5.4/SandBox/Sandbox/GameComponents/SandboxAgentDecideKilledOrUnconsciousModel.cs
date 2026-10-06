using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace SandBox.GameComponents;

public class SandboxAgentDecideKilledOrUnconsciousModel : AgentDecideKilledOrUnconsciousModel
{
	public override float GetAgentStateProbability(Agent affectorAgent, Agent effectedAgent, DamageTypes damageType, WeaponFlags weaponFlags, out float useSurgeryProbability)
	{
		useSurgeryProbability = 1f;
		if (effectedAgent.IsHuman)
		{
			CharacterObject characterObject = (CharacterObject)effectedAgent.Character;
			if (Campaign.Current != null)
			{
				if (characterObject.IsHero && !characterObject.HeroObject.CanDie(KillCharacterAction.KillCharacterActionDetail.DiedInBattle))
				{
					return 0f;
				}
				PartyBase party = effectedAgent.GetComponent<CampaignAgentComponent>()?.OwnerParty;
				if (affectorAgent != null && affectorAgent.IsHuman)
				{
					PartyBase enemyParty = affectorAgent.GetComponent<CampaignAgentComponent>()?.OwnerParty;
					return 1f - Campaign.Current.Models.PartyHealingModel.GetSurvivalChance(party, characterObject, damageType, weaponFlags.HasAnyFlag(WeaponFlags.CanKillEvenIfBlunt), enemyParty);
				}
				return 1f - Campaign.Current.Models.PartyHealingModel.GetSurvivalChance(party, characterObject, damageType, weaponFlags.HasAnyFlag(WeaponFlags.CanKillEvenIfBlunt));
			}
		}
		return 1f;
	}
}
