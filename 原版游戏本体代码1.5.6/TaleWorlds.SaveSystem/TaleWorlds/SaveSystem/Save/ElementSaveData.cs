using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Save;

internal class ElementSaveData : VariableSaveData
{
	public object ElementValue { get; private set; }

	public int ElementIndex { get; private set; }

	public ElementSaveData(ContainerSaveData containerSaveData, object value, int index)
		: base(containerSaveData.Context)
	{
		ElementValue = value;
		ElementIndex = index;
		if (value == null)
		{
			InitializeDataAsNullObject(MemberTypeId.Invalid);
			return;
		}
		TypeDefinitionBase typeDefinition = containerSaveData.Context.DefinitionContext.GetTypeDefinition(value.GetType());
		if (typeDefinition is TypeDefinition { IsClassDefinition: false })
		{
			InitializeDataAsCustomStruct(MemberTypeId.Invalid, index, typeDefinition);
		}
		else
		{
			InitializeData(MemberTypeId.Invalid, value.GetType(), typeDefinition, value);
		}
	}
}
