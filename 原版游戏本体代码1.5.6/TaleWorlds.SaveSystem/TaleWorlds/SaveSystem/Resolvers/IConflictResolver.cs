using System;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Resolvers;

public interface IConflictResolver
{
	bool IsApplicable(ApplicationVersion version);

	Type GetNewType();

	MemberTypeId GetFieldMemberWithId(MemberTypeId memberTypeId);

	MemberTypeId GetPropertyMemberWithId(MemberTypeId memberTypeId);
}
