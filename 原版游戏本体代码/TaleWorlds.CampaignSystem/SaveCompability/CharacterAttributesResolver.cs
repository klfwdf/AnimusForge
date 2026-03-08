using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.CampaignSystem.SaveCompability
{
	// Token: 0x020000CE RID: 206
	public class CharacterAttributesResolver : IConflictResolver
	{
		// Token: 0x0600144D RID: 5197 RVA: 0x0005E255 File Offset: 0x0005C455
		public bool IsApplicable(ApplicationVersion version)
		{
			return version != ApplicationVersion.Empty && version.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0));
		}

		// Token: 0x0600144E RID: 5198 RVA: 0x0005E278 File Offset: 0x0005C478
		public MemberTypeId GetFieldMemberWithId(MemberTypeId memberTypeId)
		{
			if (memberTypeId == new MemberTypeId(3, 10))
			{
				return new MemberTypeId(TypeDefinitionBase.GetClassLevel(this.GetNewType()), 10);
			}
			return MemberTypeId.Invalid;
		}

		// Token: 0x0600144F RID: 5199 RVA: 0x0005E2A2 File Offset: 0x0005C4A2
		public Type GetNewType()
		{
			return typeof(PropertyOwner<CharacterAttribute>);
		}

		// Token: 0x06001450 RID: 5200 RVA: 0x0005E2AE File Offset: 0x0005C4AE
		public MemberTypeId GetPropertyMemberWithId(MemberTypeId memberTypeId)
		{
			return MemberTypeId.Invalid;
		}
	}
}
