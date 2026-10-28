using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

public class ContainerSaveId : SaveId
{
	private readonly string _stringId;

	public ContainerType ContainerType { get; set; }

	public SaveId KeyId { get; set; }

	public SaveId ValueId { get; set; }

	public ContainerSaveId(ContainerType containerType, SaveId elementId)
	{
		ContainerType = containerType;
		KeyId = elementId;
		_stringId = CalculateStringId();
	}

	public ContainerSaveId(ContainerType containerType, SaveId keyId, SaveId valueId)
	{
		ContainerType = containerType;
		KeyId = keyId;
		ValueId = valueId;
		_stringId = CalculateStringId();
	}

	private string CalculateStringId()
	{
		string text = "";
		if (ContainerType == ContainerType.Dictionary)
		{
			string stringId = KeyId.GetStringId();
			string stringId2 = ValueId.GetStringId();
			return "C(" + (int)ContainerType + ")-(" + stringId + "," + stringId2 + ")";
		}
		string stringId3 = KeyId.GetStringId();
		return "C(" + (int)ContainerType + ")-(" + stringId3 + ")";
	}

	public override string GetStringId()
	{
		return _stringId;
	}

	public override void WriteTo(IWriter writer)
	{
		writer.WriteByte(2);
		writer.WriteByte((byte)ContainerType);
		KeyId.WriteTo(writer);
		if (ContainerType == ContainerType.Dictionary)
		{
			ValueId.WriteTo(writer);
		}
	}

	public static ContainerSaveId ReadFrom(IReader reader)
	{
		ContainerType containerType = (ContainerType)reader.ReadByte();
		int num = ((containerType != ContainerType.Dictionary) ? 1 : 2);
		List<SaveId> list = new List<SaveId>();
		for (int i = 0; i < num; i++)
		{
			SaveId item = null;
			switch (reader.ReadByte())
			{
			case 0:
				item = TypeSaveId.ReadFrom(reader);
				break;
			case 1:
				item = GenericSaveId.ReadFrom(reader);
				break;
			case 2:
				item = ReadFrom(reader);
				break;
			}
			list.Add(item);
		}
		SaveId keyId = list[0];
		SaveId valueId = ((list.Count > 1) ? list[1] : null);
		return new ContainerSaveId(containerType, keyId, valueId);
	}

	public override int GetSizeInBytes()
	{
		int num = 2 + KeyId.GetSizeInBytes();
		if (ContainerType == ContainerType.Dictionary)
		{
			num += ValueId.GetSizeInBytes();
		}
		return num;
	}
}
