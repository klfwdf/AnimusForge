using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class Vec2BasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		Vec2 vec = (Vec2)value;
		writer.WriteVec2(vec);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadVec2();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 8;
	}
}
