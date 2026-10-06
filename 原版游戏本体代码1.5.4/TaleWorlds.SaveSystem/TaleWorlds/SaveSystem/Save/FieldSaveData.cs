using System;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Save;

internal class FieldSaveData : MemberSaveData
{
	public FieldDefinition FieldDefinition { get; private set; }

	public MemberTypeId SaveId { get; private set; }

	public FieldSaveData(ObjectSaveData objectSaveData, FieldDefinition fieldDefinition, MemberTypeId saveId)
		: base(objectSaveData)
	{
		FieldDefinition = fieldDefinition;
		SaveId = saveId;
	}

	public override void Initialize(TypeDefinitionBase typeDefinition)
	{
		object value = FieldDefinition.GetValue(base.ObjectSaveData.Target);
		Type fieldType = FieldDefinition.FieldInfo.FieldType;
		InitializeData(SaveId, fieldType, typeDefinition, value);
	}

	public override void InitializeAsCustomStruct(int structId)
	{
		InitializeDataAsCustomStruct(SaveId, structId, base.TypeDefinition);
	}
}
