using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class UlongBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		writer.WriteULong((ulong)value);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadULong();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 8;
	}
}
