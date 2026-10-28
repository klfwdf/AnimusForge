using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Save;

internal abstract class MemberSaveData : VariableSaveData
{
	public ObjectSaveData ObjectSaveData { get; private set; }

	protected MemberSaveData(ObjectSaveData objectSaveData)
		: base(objectSaveData.Context)
	{
		ObjectSaveData = objectSaveData;
	}

	public abstract void Initialize(TypeDefinitionBase typeDefinition);

	public abstract void InitializeAsCustomStruct(int structId);
}
