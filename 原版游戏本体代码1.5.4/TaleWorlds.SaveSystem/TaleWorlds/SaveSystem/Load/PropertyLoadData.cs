using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Load;

internal class PropertyLoadData : MemberLoadData
{
	public PropertyLoadData(ObjectLoadData objectLoadData, IReader reader)
		: base(objectLoadData, reader)
	{
	}

	public void FillObject()
	{
		PropertyDefinition propertyDefinitionWithId;
		if (base.ObjectLoadData.TypeDefinition != null && (propertyDefinitionWithId = base.ObjectLoadData.TypeDefinition.GetPropertyDefinitionWithId(GetMemberTypeId())) != null)
		{
			MethodInfo setMethod = propertyDefinitionWithId.SetMethod;
			object target = base.ObjectLoadData.Target;
			object data = GetDataToUse();
			if (data == null || propertyDefinitionWithId.PropertyInfo.PropertyType.IsInstanceOfType(data) || LoadContext.TryConvertType(data.GetType(), propertyDefinitionWithId.PropertyInfo.PropertyType, ref data))
			{
				setMethod.Invoke(target, new object[1] { data });
			}
		}
	}

	private MemberTypeId GetMemberTypeId()
	{
		MemberTypeId memberTypeId = base.MemberSaveId;
		base.Context.DefinitionContext.GetConflictedPropertyMemberTypeId(base.ObjectLoadData.TypeDefinition, ref memberTypeId);
		return memberTypeId;
	}
}
