using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SandBox
{
	// Token: 0x02000010 RID: 16
	public class BoostSkillCheatGroup : GameplayCheatGroup
	{
		// Token: 0x06000030 RID: 48 RVA: 0x00003900 File Offset: 0x00001B00
		public override IEnumerable<GameplayCheatBase> GetCheats()
		{
			foreach (SkillObject skillToBoost in Skills.All)
			{
				yield return new BoostSkillCheatGroup.BoostSkillCheeat(skillToBoost);
			}
			List<SkillObject>.Enumerator enumerator = default(List<SkillObject>.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003909 File Offset: 0x00001B09
		public override TextObject GetName()
		{
			return new TextObject("{=SFn4UFd4}Boost Skill", null);
		}

		// Token: 0x0200010D RID: 269
		public class BoostSkillCheeat : GameplayCheatItem
		{
			// Token: 0x06000D4C RID: 3404 RVA: 0x000609AF File Offset: 0x0005EBAF
			public BoostSkillCheeat(SkillObject skillToBoost)
			{
				this._skillToBoost = skillToBoost;
			}

			// Token: 0x06000D4D RID: 3405 RVA: 0x000609C0 File Offset: 0x0005EBC0
			public override void ExecuteCheat()
			{
				int num = 50;
				if (Hero.MainHero.GetSkillValue(this._skillToBoost) + num > 330)
				{
					num = 330 - Hero.MainHero.GetSkillValue(this._skillToBoost);
				}
				Hero.MainHero.HeroDeveloper.ChangeSkillLevel(this._skillToBoost, num, false);
			}

			// Token: 0x06000D4E RID: 3406 RVA: 0x00060A17 File Offset: 0x0005EC17
			public override TextObject GetName()
			{
				return this._skillToBoost.GetName();
			}

			// Token: 0x040005BA RID: 1466
			private readonly SkillObject _skillToBoost;
		}
	}
}
