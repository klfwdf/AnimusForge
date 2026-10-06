using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class Mat3BasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		Mat3 mat = (Mat3)value;
		writer.WriteVec3(mat.s);
		writer.WriteVec3(mat.f);
		writer.WriteVec3(mat.u);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return new Mat3(reader.ReadVec3(), reader.ReadVec3(), reader.ReadVec3());
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 48;
	}
}
