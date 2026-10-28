using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

public class TypeSaveId : SaveId
{
	private readonly string _stringId;

	public int Id { get; private set; }

	public TypeSaveId(int id)
	{
		Id = id;
		_stringId = Id.ToString();
	}

	public override string GetStringId()
	{
		return _stringId;
	}

	public override void WriteTo(IWriter writer)
	{
		writer.WriteByte(0);
		writer.WriteInt(Id);
	}

	public static TypeSaveId ReadFrom(IReader reader)
	{
		return new TypeSaveId(reader.ReadInt());
	}

	public override int GetSizeInBytes()
	{
		return 5;
	}
}
