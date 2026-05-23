using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.CampaignSystem.SaveCompability
{
	// Token: 0x020000D2 RID: 210
	public class HeroDeveloperResolver : IConflictResolver
	{
		// Token: 0x0600145E RID: 5214 RVA: 0x0005E42B File Offset: 0x0005C62B
		public bool IsApplicable(ApplicationVersion version)
		{
			return version != ApplicationVersion.Empty && version.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0));
		}

		// Token: 0x0600145F RID: 5215 RVA: 0x0005E450 File Offset: 0x0005C650
		public MemberTypeId GetFieldMemberWithId(MemberTypeId memberTypeId)
		{
			if (memberTypeId == new MemberTypeId(3, 10))
			{
				return new MemberTypeId(TypeDefinitionBase.GetClassLevel(this.GetNewType()), 0);
			}
			if (memberTypeId.TypeLevel >= 4)
			{
				return new MemberTypeId(TypeDefinitionBase.GetClassLevel(typeof(HeroDeveloper)), memberTypeId.LocalSaveId);
			}
			return MemberTypeId.Invalid;
		}

		// Token: 0x06001460 RID: 5216 RVA: 0x0005E4A8 File Offset: 0x0005C6A8
		public Type GetNewType()
		{
			return typeof(HeroDeveloper);
		}

		// Token: 0x06001461 RID: 5217 RVA: 0x0005E4B4 File Offset: 0x0005C6B4
		public MemberTypeId GetPropertyMemberWithId(MemberTypeId memberTypeId)
		{
			if (memberTypeId.TypeLevel >= 4)
			{
				return new MemberTypeId(TypeDefinitionBase.GetClassLevel(typeof(HeroDeveloper)), memberTypeId.LocalSaveId);
			}
			return MemberTypeId.Invalid;
		}
	}
}
