using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Extensions;

public static class Skills
{
	public static MBReadOnlyList<SkillObject> All => Campaign.Current.AllSkills;

	public static SkillObject GetSkill(int i)
	{
		return All[i];
	}
}
