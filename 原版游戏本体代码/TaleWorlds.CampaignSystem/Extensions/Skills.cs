using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Extensions
{
	// Token: 0x02000169 RID: 361
	public static class Skills
	{
		// Token: 0x170006E3 RID: 1763
		// (get) Token: 0x06001AF4 RID: 6900 RVA: 0x0008AC21 File Offset: 0x00088E21
		public static MBReadOnlyList<SkillObject> All
		{
			get
			{
				return Campaign.Current.AllSkills;
			}
		}

		// Token: 0x06001AF5 RID: 6901 RVA: 0x0008AC2D File Offset: 0x00088E2D
		public static SkillObject GetSkill(int i)
		{
			return Skills.All[i];
		}
	}
}
