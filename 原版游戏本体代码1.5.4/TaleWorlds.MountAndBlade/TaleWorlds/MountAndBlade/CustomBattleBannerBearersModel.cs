using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class CustomBattleBannerBearersModel : BattleBannerBearersModel
{
	private static readonly int[] BannerBearerPriorityPerTier = new int[7] { 0, 1, 3, 5, 6, 4, 2 };

	private static List<ItemObject> ReplacementWeapons = null;

	private static DefaultBattleMissionAgentSpawnLogic _missionSpawnLogic;

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
		if (_missionSpawnLogic == null)
		{
			_missionSpawnLogic = Mission.Current.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
		}
		if (_missionSpawnLogic != null)
		{
			Team team = agent.Formation?.Team;
			if (team != null)
			{
				BasicCharacterObject generalCharacterOfSide = _missionSpawnLogic.GetGeneralCharacterOfSide(team.Side);
				if (agent.IsHuman && !agent.IsMainAgent && !agent.IsHero && agent.IsAIControlled)
				{
					return agent.Character != generalCharacterOfSide;
				}
				return false;
			}
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
		int num = Math.Min(agent.Character.Level / 4 + 1, BannerBearerPriorityPerTier.Length - 1);
		return BannerBearerPriorityPerTier[num];
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
		if (ReplacementWeapons == null)
		{
			ReplacementWeapons = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(delegate(ItemObject item)
			{
				if (item.PrimaryWeapon != null)
				{
					WeaponComponentData primaryWeapon = item.PrimaryWeapon;
					return primaryWeapon.WeaponClass == WeaponClass.OneHandedSword;
				}
				return false;
			}).ToList();
		}
		if (ReplacementWeapons.IsEmpty())
		{
			return null;
		}
		IEnumerable<ItemObject> enumerable = ReplacementWeapons.Where((ItemObject item) => item.Culture != null && item.Culture == agentCharacter.Culture);
		List<(int, ItemObject)> list = new List<(int, ItemObject)>();
		int minTierDifference = int.MaxValue;
		foreach (ItemObject item in enumerable)
		{
			int a = TaleWorlds.Library.MathF.Ceiling(((float)agentCharacter.Level - 5f) / 5f);
			a = TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Max(a, 0), 7);
			int num = TaleWorlds.Library.MathF.Abs((int)(item.Tier - a));
			if (num < minTierDifference)
			{
				minTierDifference = num;
			}
			list.Add((num, item));
		}
		return list.Where<(int, ItemObject)>(((int TierDifference, ItemObject Weapon) tuple) => tuple.TierDifference == minTierDifference).GetRandomElementInefficiently().Item2;
	}
}
