using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class ShortBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		writer.WriteShort((short)value);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadShort();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 2;
	}
}
