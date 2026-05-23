using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class GenericSaveId : SaveId
{
	private readonly string _stringId;

	public SaveId BaseId { get; set; }

	public SaveId[] GenericTypeIDs { get; set; }

	public GenericSaveId(TypeSaveId baseId, SaveId[] saveIds)
	{
		BaseId = baseId;
		GenericTypeIDs = saveIds;
		_stringId = CalculateStringId();
	}

	private string CalculateStringId()
	{
		string text = "";
		for (int i = 0; i < GenericTypeIDs.Length; i++)
		{
			if (i != 0)
			{
				text += ",";
			}
			SaveId saveId = GenericTypeIDs[i];
			text += saveId.GetStringId();
		}
		return "G(" + BaseId.GetStringId() + ")-(" + text + ")";
	}

	public override string GetStringId()
	{
		return _stringId;
	}

	public override void WriteTo(IWriter writer)
	{
		writer.WriteByte(1);
		BaseId.WriteTo(writer);
		writer.WriteByte((byte)GenericTypeIDs.Length);
		for (int i = 0; i < GenericTypeIDs.Length; i++)
		{
			GenericTypeIDs[i].WriteTo(writer);
		}
	}

	public static GenericSaveId ReadFrom(IReader reader)
	{
		reader.ReadByte();
		TypeSaveId baseId = TypeSaveId.ReadFrom(reader);
		byte b = reader.ReadByte();
		List<SaveId> list = new List<SaveId>();
		for (int i = 0; i < b; i++)
		{
			SaveId item = null;
			switch (reader.ReadByte())
			{
			case 0:
				item = TypeSaveId.ReadFrom(reader);
				break;
			case 1:
				item = ReadFrom(reader);
				break;
			case 2:
				item = ContainerSaveId.ReadFrom(reader);
				break;
			}
			list.Add(item);
		}
		return new GenericSaveId(baseId, list.ToArray());
	}

	public override int GetSizeInBytes()
	{
		int num = 2 + BaseId.GetSizeInBytes();
		for (int i = 0; i < GenericTypeIDs.Length; i++)
		{
			SaveId saveId = GenericTypeIDs[i];
			num += saveId.GetSizeInBytes();
		}
		return num;
	}
}
