using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class Vec3iBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		Vec3i vec = (Vec3i)value;
		writer.WriteVec3Int(vec);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return reader.ReadVec3Int();
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 12;
	}
}
