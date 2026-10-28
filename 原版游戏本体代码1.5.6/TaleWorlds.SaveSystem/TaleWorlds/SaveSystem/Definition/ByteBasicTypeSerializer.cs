using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class ByteBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		writer.WriteByte((byte)value);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadByte();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 1;
	}
}
