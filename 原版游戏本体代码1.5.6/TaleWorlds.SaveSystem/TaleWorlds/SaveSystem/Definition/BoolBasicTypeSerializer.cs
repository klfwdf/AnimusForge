using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class BoolBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		writer.WriteBool((bool)value);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadBool();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 1;
	}
}
