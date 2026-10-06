using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class IntBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		writer.WriteInt((int)value);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadInt();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 4;
	}
}
