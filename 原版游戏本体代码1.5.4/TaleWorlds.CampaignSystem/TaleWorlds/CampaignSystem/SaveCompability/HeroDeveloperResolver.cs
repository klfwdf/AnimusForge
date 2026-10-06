using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.CampaignSystem.SaveCompability;

public class HeroDeveloperResolver : IConflictResolver
{
	public bool IsApplicable(ApplicationVersion version)
	{
		if (version != ApplicationVersion.Empty)
		{
			return version.IsOlderThan(ApplicationVersion.FromString("v1.3.0"));
		}
		return false;
	}

	public MemberTypeId GetFieldMemberWithId(MemberTypeId memberTypeId)
	{
		if (memberTypeId == new MemberTypeId(3, 10))
		{
			return new MemberTypeId(TypeDefinitionBase.GetClassLevel(GetNewType()), 0);
		}
		if (memberTypeId.TypeLevel >= 4)
		{
			return new MemberTypeId(TypeDefinitionBase.GetClassLevel(typeof(HeroDeveloper)), memberTypeId.LocalSaveId);
		}
		return MemberTypeId.Invalid;
	}

	public Type GetNewType()
	{
		return typeof(HeroDeveloper);
	}

	public MemberTypeId GetPropertyMemberWithId(MemberTypeId memberTypeId)
	{
		if (memberTypeId.TypeLevel >= 4)
		{
			return new MemberTypeId(TypeDefinitionBase.GetClassLevel(typeof(HeroDeveloper)), memberTypeId.LocalSaveId);
		}
		return MemberTypeId.Invalid;
	}
}
