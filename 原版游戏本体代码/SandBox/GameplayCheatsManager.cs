using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox
{
	// Token: 0x0200000B RID: 11
	public static class GameplayCheatsManager
	{
		// Token: 0x06000022 RID: 34 RVA: 0x00003820 File Offset: 0x00001A20
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
			yield break;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00003829 File Offset: 0x00001A29
		public static IEnumerable<GameplayCheatBase> GetMissionCheatList()
		{
			Mission mission = Mission.Current;
			if (mission != null && mission.Mode == MissionMode.Battle)
			{
				yield return new WoundAllEnemiesCheat();
			}
			yield return new HealMainHeroCheat();
			yield break;
		}
	}
}
