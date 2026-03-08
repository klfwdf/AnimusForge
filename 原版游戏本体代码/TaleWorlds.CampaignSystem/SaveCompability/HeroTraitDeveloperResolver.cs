using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.CampaignSystem.SaveCompability
{
	// Token: 0x020000D3 RID: 211
	public class HeroTraitDeveloperResolver : IConflictResolver
	{
		// Token: 0x06001463 RID: 5219 RVA: 0x0005E4E7 File Offset: 0x0005C6E7
		public bool IsApplicable(ApplicationVersion version)
		{
			return version != ApplicationVersion.Empty && version.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0));
		}

		// Token: 0x06001464 RID: 5220 RVA: 0x0005E50A File Offset: 0x0005C70A
		public MemberTypeId GetFieldMemberWithId(MemberTypeId memberTypeId)
		{
			if (memberTypeId == new MemberTypeId(3, 10))
			{
				return new MemberTypeId(TypeDefinitionBase.GetClassLevel(this.GetNewType()), 10);
			}
			return MemberTypeId.Invalid;
		}

		// Token: 0x06001465 RID: 5221 RVA: 0x0005E534 File Offset: 0x0005C734
		public Type GetNewType()
		{
			return typeof(PropertyOwner<PropertyObject>);
		}

		// Token: 0x06001466 RID: 5222 RVA: 0x0005E540 File Offset: 0x0005C740
		public MemberTypeId GetPropertyMemberWithId(MemberTypeId memberTypeId)
		{
			return MemberTypeId.Invalid;
		}
	}
}
