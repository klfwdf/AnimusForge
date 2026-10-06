using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class QuaternionBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		Quaternion quaternion = (Quaternion)value;
		writer.WriteFloat(quaternion.X);
		writer.WriteFloat(quaternion.Y);
		writer.WriteFloat(quaternion.Z);
		writer.WriteFloat(quaternion.W);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		float x = reader.ReadFloat();
		float y = reader.ReadFloat();
		float z = reader.ReadFloat();
		float w = reader.ReadFloat();
		return new Quaternion(x, y, z, w);
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 16;
	}
}
