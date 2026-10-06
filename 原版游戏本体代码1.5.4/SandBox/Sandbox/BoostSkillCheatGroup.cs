using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SandBox;

public class BoostSkillCheatGroup : GameplayCheatGroup
{
	public class BoostSkillCheeat : GameplayCheatItem
	{
		private readonly SkillObject _skillToBoost;

		public BoostSkillCheeat(SkillObject skillToBoost)
		{
			_skillToBoost = skillToBoost;
		}

		public override void ExecuteCheat()
		{
			int num = 50;
			if (Hero.MainHero.GetSkillValue(_skillToBoost) + num > 330)
			{
				num = 330 - Hero.MainHero.GetSkillValue(_skillToBoost);
			}
			Hero.MainHero.HeroDeveloper.ChangeSkillLevel(_skillToBoost, num, shouldNotify: false);
		}

		public override TextObject GetName()
		{
			return _skillToBoost.GetName();
		}
	}

	public override IEnumerable<GameplayCheatBase> GetCheats()
	{
		foreach (SkillObject item in Skills.All)
		{
			yield return new BoostSkillCheeat(item);
		}
	}

	public override TextObject GetName()
	{
		return new TextObject("{=SFn4UFd4}Boost Skill");
	}
}
