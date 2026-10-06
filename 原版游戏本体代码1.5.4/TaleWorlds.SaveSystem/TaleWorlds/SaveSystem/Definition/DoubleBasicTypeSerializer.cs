using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class DoubleBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		writer.WriteDouble((double)value);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadDouble();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 8;
	}
}
