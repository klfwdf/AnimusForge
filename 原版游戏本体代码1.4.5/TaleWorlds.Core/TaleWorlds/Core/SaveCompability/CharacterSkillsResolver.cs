using System;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.Core.SaveCompability;

public class CharacterSkillsResolver : IConflictResolver
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
			return new MemberTypeId(TypeDefinitionBase.GetClassLevel(GetNewType()), 10);
		}
		return MemberTypeId.Invalid;
	}

	public Type GetNewType()
	{
		return typeof(PropertyOwner<SkillObject>);
	}

	public MemberTypeId GetPropertyMemberWithId(MemberTypeId memberTypeId)
	{
		return MemberTypeId.Invalid;
	}
}
