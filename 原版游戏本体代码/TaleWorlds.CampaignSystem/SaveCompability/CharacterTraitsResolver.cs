using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.CampaignSystem.SaveCompability
{
	// Token: 0x020000D0 RID: 208
	public class CharacterTraitsResolver : IConflictResolver
	{
		// Token: 0x06001457 RID: 5207 RVA: 0x0005E325 File Offset: 0x0005C525
		public bool IsApplicable(ApplicationVersion version)
		{
			return version != ApplicationVersion.Empty && version.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0));
		}

		// Token: 0x06001458 RID: 5208 RVA: 0x0005E348 File Offset: 0x0005C548
		public MemberTypeId GetFieldMemberWithId(MemberTypeId memberTypeId)
		{
			if (memberTypeId == new MemberTypeId(3, 10))
			{
				return new MemberTypeId(TypeDefinitionBase.GetClassLevel(this.GetNewType()), 10);
			}
			return MemberTypeId.Invalid;
		}

		// Token: 0x06001459 RID: 5209 RVA: 0x0005E372 File Offset: 0x0005C572
		public Type GetNewType()
		{
			return typeof(PropertyOwner<TraitObject>);
		}

		// Token: 0x0600145A RID: 5210 RVA: 0x0005E37E File Offset: 0x0005C57E
		public MemberTypeId GetPropertyMemberWithId(MemberTypeId memberTypeId)
		{
			return MemberTypeId.Invalid;
		}
	}
}
