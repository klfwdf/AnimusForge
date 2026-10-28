using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ViewModelCollection;

namespace SandBox.ViewModelCollection;

public class PerkObjectComparer : IComparer<PerkObject>
{
	public int Compare(PerkObject x, PerkObject y)
	{
		int skillObjectTypeSortIndex = CampaignUIHelper.GetSkillObjectTypeSortIndex(x.Skill);
		int num = CampaignUIHelper.GetSkillObjectTypeSortIndex(y.Skill).CompareTo(skillObjectTypeSortIndex);
		if (num != 0)
		{
			return num;
		}
		return ResolveEquality(x, y);
	}

	private int ResolveEquality(PerkObject x, PerkObject y)
	{
		return x.RequiredSkillValue.CompareTo(y.RequiredSkillValue);
	}
}
