using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class UintBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		writer.WriteUInt((uint)value);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadUInt();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 4;
	}
}
