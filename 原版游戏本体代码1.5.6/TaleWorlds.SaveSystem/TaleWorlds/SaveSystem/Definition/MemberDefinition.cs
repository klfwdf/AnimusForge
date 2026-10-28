using System;
using System.Reflection;

namespace TaleWorlds.SaveSystem.Definition;

public abstract class MemberDefinition
{
	public MemberTypeId Id { get; private set; }

	public MemberInfo MemberInfo { get; private set; }

	protected MemberDefinition(MemberInfo memberInfo, MemberTypeId id)
	{
		MemberInfo = memberInfo;
		Id = id;
	}

	public abstract Type GetMemberType();

	public abstract object GetValue(object target);
}
