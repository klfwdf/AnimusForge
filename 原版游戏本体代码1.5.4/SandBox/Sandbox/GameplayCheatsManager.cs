using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox;

public static class GameplayCheatsManager
{
	public static IEnumerable<GameplayCheatBase> GetMapCheatList()
	{
		yield return new Add1000GoldCheat();
		yield return new Add100InfluenceCheat();
		yield return new Add100RenownCheat();
		yield return new AddCraftingMaterialsCheat();
		yield return new BoostSkillCheatGroup();
		if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsFortification)
		{
			yield return new CompleteBuildingProjectCheat();
		}
		yield return new FillCraftingStaminaCheat();
		yield return new Give5TroopsToPlayerCheat();
		yield return new Give10GrainCheat();
		yield return new Give10WarhorsesCheat();
		yield return new HealPlayerPartyCheat();
		yield return new UnlockAllCraftingRecipesCheat();
		yield return new UnlockFogOfWarCheat();
	}

	public static IEnumerable<GameplayCheatBase> GetMissionCheatList()
	{
		Mission current = Mission.Current;
		if (current != null && current.Mode == MissionMode.Battle)
		{
			yield return new WoundAllEnemiesCheat();
		}
		yield return new HealMainHeroCheat();
	}
}
