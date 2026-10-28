using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class SbyteBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		writer.WriteSByte((sbyte)value);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadSByte();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 1;
	}
}
