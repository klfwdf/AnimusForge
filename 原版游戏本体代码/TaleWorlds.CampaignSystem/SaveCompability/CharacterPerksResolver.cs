using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.CampaignSystem.SaveCompability
{
	// Token: 0x020000CF RID: 207
	public class CharacterPerksResolver : IConflictResolver
	{
		// Token: 0x06001452 RID: 5202 RVA: 0x0005E2BD File Offset: 0x0005C4BD
		public bool IsApplicable(ApplicationVersion version)
		{
			return version != ApplicationVersion.Empty && version.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0));
		}

		// Token: 0x06001453 RID: 5203 RVA: 0x0005E2E0 File Offset: 0x0005C4E0
		public MemberTypeId GetFieldMemberWithId(MemberTypeId memberTypeId)
		{
			if (memberTypeId == new MemberTypeId(3, 10))
			{
				return new MemberTypeId(TypeDefinitionBase.GetClassLevel(this.GetNewType()), 10);
			}
			return MemberTypeId.Invalid;
		}

		// Token: 0x06001454 RID: 5204 RVA: 0x0005E30A File Offset: 0x0005C50A
		public Type GetNewType()
		{
			return typeof(PropertyOwner<PerkObject>);
		}

		// Token: 0x06001455 RID: 5205 RVA: 0x0005E316 File Offset: 0x0005C516
		public MemberTypeId GetPropertyMemberWithId(MemberTypeId memberTypeId)
		{
			return MemberTypeId.Invalid;
		}
	}
}
