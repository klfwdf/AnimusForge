using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class Vec2iBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		Vec2i vec2i = (Vec2i)value;
		writer.WriteFloat(vec2i.Item1);
		writer.WriteFloat(vec2i.Item2);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		int x = reader.ReadInt();
		int y = reader.ReadInt();
		return new Vec2i(x, y);
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 8;
	}
}
