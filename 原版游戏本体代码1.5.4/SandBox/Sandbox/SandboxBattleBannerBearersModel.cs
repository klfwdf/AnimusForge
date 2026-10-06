using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace SandBox;

public class SandboxBattleBannerBearersModel : BattleBannerBearersModel
{
	private static readonly int[] BannerBearerPriorityPerTier = new int[7] { 0, 1, 3, 5, 6, 4, 2 };

	public override int GetMinimumFormationTroopCountToBearBanners()
	{
		return 2;
	}

	public override float GetBannerInteractionDistance(Agent interactingAgent)
	{
		if (!interactingAgent.HasMount)
		{
			return 1.5f;
		}
		return 3f;
	}

	public override bool CanBannerBearerProvideEffectToFormation(Agent agent, Formation formation)
	{
		if (agent.Formation != formation)
		{
			if (agent.IsPlayerControlled)
			{
				return agent.Team == formation.Team;
			}
			return false;
		}
		return true;
	}

	public override bool CanAgentPickUpAnyBanner(Agent agent)
	{
		if (agent.IsHuman && agent.Banner == null && agent.CanBeAssignedForScriptedMovement() && (agent.CommonAIComponent == null || !agent.CommonAIComponent.IsPanicked))
		{
			if (agent.HumanAIComponent != null)
			{
				return !agent.HumanAIComponent.IsInImportantCombatAction();
			}
			return true;
		}
		return false;
	}

	public override bool CanAgentBecomeBannerBearer(Agent agent)
	{
		if (agent.IsHuman && !agent.IsMainAgent && !agent.IsHero && agent.IsAIControlled)
		{
			return agent.Character is CharacterObject;
		}
		return false;
	}

	public override int GetAgentBannerBearingPriority(Agent agent)
	{
		if (!CanAgentBecomeBannerBearer(agent))
		{
			return 0;
		}
		if (agent.Formation != null)
		{
			bool calculateHasSignificantNumberOfMounted = agent.Formation.CalculateHasSignificantNumberOfMounted;
			if ((calculateHasSignificantNumberOfMounted && !agent.HasMount) || (!calculateHasSignificantNumberOfMounted && agent.HasMount))
			{
				return 0;
			}
		}
		if (agent.Banner != null)
		{
			return int.MaxValue;
		}
		int num = 0;
		if (agent.Character is CharacterObject characterObject)
		{
			int num2 = Math.Min(characterObject.Tier, BannerBearerPriorityPerTier.Length - 1);
			num += BannerBearerPriorityPerTier[num2];
		}
		return num;
	}

	public override bool CanFormationDeployBannerBearers(Formation formation)
	{
		BannerBearerLogic bannerBearerLogic = base.BannerBearerLogic;
		if (bannerBearerLogic == null || formation.CountOfUnits < GetMinimumFormationTroopCountToBearBanners() || bannerBearerLogic.GetFormationBanner(formation) == null)
		{
			return false;
		}
		return formation.UnitsWithoutLooseDetachedOnes.Count((IFormationUnit unit) => unit is Agent agent && CanAgentBecomeBannerBearer(agent)) > 0;
	}

	public override int GetDesiredNumberOfBannerBearersForFormation(Formation formation)
	{
		if (!CanFormationDeployBannerBearers(formation))
		{
			return 0;
		}
		return 1;
	}

	public override ItemObject GetBannerBearerReplacementWeapon(BasicCharacterObject agentCharacter)
	{
		if (agentCharacter is CharacterObject characterObject && agentCharacter.Culture is CultureObject cultureObject && !cultureObject.BannerBearerReplacementWeapons.IsEmpty())
		{
			List<(int, ItemObject)> list = new List<(int, ItemObject)>();
			int minTierDifference = int.MaxValue;
			foreach (ItemObject bannerBearerReplacementWeapon in cultureObject.BannerBearerReplacementWeapons)
			{
				int num = TaleWorlds.Library.MathF.Abs((int)(bannerBearerReplacementWeapon.Tier + 1 - characterObject.Tier));
				if (num < minTierDifference)
				{
					minTierDifference = num;
				}
				list.Add((num, bannerBearerReplacementWeapon));
			}
			return list.Where<(int, ItemObject)>(((int TierDifference, ItemObject Weapon) tuple) => tuple.TierDifference == minTierDifference).GetRandomElementInefficiently().Item2;
		}
		return null;
	}
}
