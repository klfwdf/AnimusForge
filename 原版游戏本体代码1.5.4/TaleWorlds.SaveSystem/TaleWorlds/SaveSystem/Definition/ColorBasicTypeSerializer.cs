using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class ColorBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		Color value2 = (Color)value;
		writer.WriteColor(value2);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadColor();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 16;
	}
}
