using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class LongBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		writer.WriteLong((long)value);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadLong();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 8;
	}
}
