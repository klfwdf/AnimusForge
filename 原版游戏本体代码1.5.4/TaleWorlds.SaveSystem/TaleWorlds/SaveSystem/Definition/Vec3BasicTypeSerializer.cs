using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class Vec3BasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		Vec3 vec = (Vec3)value;
		writer.WriteVec3(vec);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadVec3();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 16;
	}
}
