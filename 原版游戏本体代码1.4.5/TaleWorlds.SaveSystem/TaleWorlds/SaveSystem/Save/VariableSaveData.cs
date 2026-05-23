using System;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.SaveSystem.Save;

internal abstract class VariableSaveData
{
	public ISaveContext Context { get; private set; }

	public SavedMemberType MemberType { get; private set; }

	public object Value { get; private set; }

	public MemberTypeId MemberSaveId { get; private set; }

	public TypeDefinitionBase TypeDefinition { get; private set; }

	protected VariableSaveData(ISaveContext context)
	{
		Context = context;
	}

	protected void InitializeDataAsNullObject(MemberTypeId memberSaveId)
	{
		MemberSaveId = memberSaveId;
		MemberType = SavedMemberType.Object;
		Value = -1;
	}

	protected void InitializeDataAsCustomStruct(MemberTypeId memberSaveId, int structId, TypeDefinitionBase typeDefinition)
	{
		MemberSaveId = memberSaveId;
		MemberType = SavedMemberType.CustomStruct;
		Value = structId;
		TypeDefinition = typeDefinition;
	}

	protected void InitializeData(MemberTypeId memberSaveId, Type memberType, TypeDefinitionBase definition, object data)
	{
		MemberSaveId = memberSaveId;
		TypeDefinition = definition;
		TypeDefinition typeDefinition = TypeDefinition as TypeDefinition;
		if (TypeDefinition is ContainerDefinition)
		{
			int num = -1;
			if (data != null)
			{
				num = Context.GetContainerId(data);
			}
			MemberType = SavedMemberType.Container;
			Value = num;
		}
		else if (typeof(string) == memberType)
		{
			MemberType = SavedMemberType.String;
			Value = data;
		}
		else if ((typeDefinition != null && typeDefinition.IsClassDefinition) || TypeDefinition is InterfaceDefinition || (TypeDefinition == null && memberType.IsInterface))
		{
			int num2 = -1;
			if (data != null)
			{
				num2 = Context.GetObjectId(data);
			}
			MemberType = SavedMemberType.Object;
			Value = num2;
		}
		else if (TypeDefinition is EnumDefinition)
		{
			MemberType = SavedMemberType.Enum;
			Value = data;
		}
		else if (TypeDefinition is BasicTypeDefinition)
		{
			MemberType = SavedMemberType.BasicType;
			Value = data;
		}
		else
		{
			MemberType = SavedMemberType.CustomStruct;
			Value = data;
		}
		if (TypeDefinition == null && !memberType.IsInterface)
		{
			string message = $"Cant find definition for: {memberType.Name}. Save id: {MemberSaveId}";
			Debug.Print(message, 0, Debug.DebugColor.Red);
			Debug.FailedAssert(message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\Save\\VariableSaveData.cs", "InitializeData", 98);
		}
	}

	public void SaveTo(IWriter writer)
	{
		writer.WriteByte((byte)MemberType);
		writer.WriteByte(MemberSaveId.TypeLevel);
		writer.WriteShort(MemberSaveId.LocalSaveId);
		if (MemberType == SavedMemberType.Object)
		{
			writer.WriteInt((int)Value);
		}
		else if (MemberType == SavedMemberType.Container)
		{
			writer.WriteInt((int)Value);
		}
		else if (MemberType == SavedMemberType.String)
		{
			int stringId = Context.GetStringId((string)Value);
			writer.WriteInt(stringId);
		}
		else if (MemberType == SavedMemberType.Enum)
		{
			TypeDefinition.SaveId.WriteTo(writer);
			writer.WriteString(Value.ToString());
		}
		else if (MemberType == SavedMemberType.BasicType)
		{
			TypeDefinition.SaveId.WriteTo(writer);
			if (Context.DefinitionContext.TryGetTypeDefinition(TypeDefinition.SaveId) == null)
			{
				Debug.FailedAssert("Basic type definition cant be found: " + TypeDefinition.SaveId.GetStringId(), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\Save\\VariableSaveData.cs", "SaveTo", 132);
			}
			((BasicTypeDefinition)TypeDefinition).Serializer.Serialize(writer, Value);
		}
		else if (MemberType == SavedMemberType.CustomStruct)
		{
			writer.WriteInt((int)Value);
		}
	}

	public int GetDataSize()
	{
		int num = 4;
		if (MemberType == SavedMemberType.Object || MemberType == SavedMemberType.Container || MemberType == SavedMemberType.String || MemberType == SavedMemberType.CustomStruct)
		{
			num += 4;
		}
		else if (MemberType == SavedMemberType.Enum)
		{
			num += TypeDefinition.SaveId.GetSizeInBytes() + SaveContext.GetStringSizeInBytes(Value.ToString());
		}
		else if (MemberType == SavedMemberType.BasicType)
		{
			num += TypeDefinition.SaveId.GetSizeInBytes();
			BasicTypeDefinition basicTypeDefinition = (BasicTypeDefinition)TypeDefinition;
			num += basicTypeDefinition.Serializer.GetSizeInBytes();
		}
		return num;
	}
}
