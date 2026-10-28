using System;
using System.Reflection;

namespace TaleWorlds.SaveSystem.Definition;

public class FieldDefinition : MemberDefinition
{
	public FieldInfo FieldInfo { get; private set; }

	public SaveableFieldAttribute SaveableFieldAttribute { get; private set; }

	public GetFieldValueDelegate GetFieldValueMethod { get; private set; }

	public FieldDefinition(FieldInfo fieldInfo, MemberTypeId id)
		: base(fieldInfo, id)
	{
		FieldInfo = fieldInfo;
		SaveableFieldAttribute = fieldInfo.GetCustomAttribute<SaveableFieldAttribute>();
	}

	public override Type GetMemberType()
	{
		return FieldInfo.FieldType;
	}

	public override object GetValue(object target)
	{
		if (GetFieldValueMethod != null)
		{
			return GetFieldValueMethod(target);
		}
		return FieldInfo.GetValue(target);
	}

	public void InitializeForAutoGeneration(GetFieldValueDelegate getFieldValueMethod)
	{
		GetFieldValueMethod = getFieldValueMethod;
	}
}
