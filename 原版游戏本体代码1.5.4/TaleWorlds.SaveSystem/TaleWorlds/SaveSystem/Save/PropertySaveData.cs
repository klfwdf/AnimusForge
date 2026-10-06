using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Save;

internal class PropertySaveData : MemberSaveData
{
	public PropertyDefinition PropertyDefinition { get; private set; }

	public MemberTypeId SaveId { get; private set; }

	public PropertySaveData(ObjectSaveData objectSaveData, PropertyDefinition propertyDefinition, MemberTypeId saveId)
		: base(objectSaveData)
	{
		PropertyDefinition = propertyDefinition;
		SaveId = saveId;
	}

	public override void Initialize(TypeDefinitionBase typeDefinition)
	{
		object value = PropertyDefinition.GetValue(base.ObjectSaveData.Target);
		InitializeData(SaveId, PropertyDefinition.PropertyInfo.PropertyType, typeDefinition, value);
	}

	public override void InitializeAsCustomStruct(int structId)
	{
		InitializeDataAsCustomStruct(SaveId, structId, base.TypeDefinition);
	}
}
