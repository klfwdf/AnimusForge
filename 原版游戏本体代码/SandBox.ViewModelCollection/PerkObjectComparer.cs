using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ViewModelCollection;

namespace SandBox.ViewModelCollection
{
	// Token: 0x02000008 RID: 8
	public class PerkObjectComparer : IComparer<PerkObject>
	{
		// Token: 0x06000050 RID: 80 RVA: 0x00004008 File Offset: 0x00002208
		public int Compare(PerkObject x, PerkObject y)
		{
			int skillObjectTypeSortIndex = CampaignUIHelper.GetSkillObjectTypeSortIndex(x.Skill);
			int num = CampaignUIHelper.GetSkillObjectTypeSortIndex(y.Skill).CompareTo(skillObjectTypeSortIndex);
			if (num != 0)
			{
				return num;
			}
			return this.ResolveEquality(x, y);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00004044 File Offset: 0x00002244
		private int ResolveEquality(PerkObject x, PerkObject y)
		{
			return x.RequiredSkillValue.CompareTo(y.RequiredSkillValue);
		}
	}
}
