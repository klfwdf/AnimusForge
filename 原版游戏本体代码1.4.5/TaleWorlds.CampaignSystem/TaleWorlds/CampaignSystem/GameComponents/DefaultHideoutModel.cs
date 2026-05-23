using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultHideoutModel : HideoutModel
{
	public override CampaignTime HideoutHiddenDuration => CampaignTime.Days(10f);

	public override int CanAttackHideoutStartTime => CampaignTime.SunSet + 1;

	public override int CanAttackHideoutEndTime => CampaignTime.SunRise;

	public override float GetRogueryXpGainAsGhost()
	{
		return MBRandom.RandomFloatRanged(1000f, 1400f);
	}

	public override float GetRogueryXpGainOnHideoutMissionEnd(bool isSucceeded)
	{
		return isSucceeded ? MBRandom.RandomInt(700, 1000) : MBRandom.RandomInt(225, 400);
	}

	public override float GetSendTroopsSuccessChance(Hideout hideout)
	{
		int skillValue = Hero.MainHero.GetSkillValue(DefaultSkills.Tactics);
		int skillValue2 = Hero.MainHero.GetSkillValue(DefaultSkills.Roguery);
		return 0.3f + (float)(skillValue + skillValue2) / (325f + (float)skillValue + (float)skillValue2);
	}
}
