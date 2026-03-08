using System;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.Core.SaveCompability
{
	// Token: 0x020000E2 RID: 226
	public class CharacterSkillsResolver : IConflictResolver
	{
		// Token: 0x06000B78 RID: 2936 RVA: 0x00024F36 File Offset: 0x00023136
		public bool IsApplicable(ApplicationVersion version)
		{
			return version != ApplicationVersion.Empty && version.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0));
		}

		// Token: 0x06000B79 RID: 2937 RVA: 0x00024F59 File Offset: 0x00023159
		public MemberTypeId GetFieldMemberWithId(MemberTypeId memberTypeId)
		{
			if (memberTypeId == new MemberTypeId(3, 10))
			{
				return new MemberTypeId(TypeDefinitionBase.GetClassLevel(this.GetNewType()), 10);
			}
			return MemberTypeId.Invalid;
		}

		// Token: 0x06000B7A RID: 2938 RVA: 0x00024F83 File Offset: 0x00023183
		public Type GetNewType()
		{
			return typeof(PropertyOwner<SkillObject>);
		}

		// Token: 0x06000B7B RID: 2939 RVA: 0x00024F8F File Offset: 0x0002318F
		public MemberTypeId GetPropertyMemberWithId(MemberTypeId memberTypeId)
		{
			return MemberTypeId.Invalid;
		}
	}
}
