using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class Mat2BasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		Mat2 mat = (Mat2)value;
		writer.WriteVec2(mat.s);
		writer.WriteVec2(mat.f);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		Vec2 vec = reader.ReadVec2();
		Vec2 vec2 = reader.ReadVec2();
		return new Mat2(vec.x, vec.y, vec2.x, vec2.y);
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 16;
	}
}
