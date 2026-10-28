using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class MatrixFrameBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		MatrixFrame matrixFrame = (MatrixFrame)value;
		writer.WriteVec3(matrixFrame.origin);
		writer.WriteVec3(matrixFrame.rotation.s);
		writer.WriteVec3(matrixFrame.rotation.f);
		writer.WriteVec3(matrixFrame.rotation.u);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		Vec3 o = reader.ReadVec3();
		Vec3 f = reader.ReadVec3();
		return new MatrixFrame(new Mat3(reader.ReadVec3(), in f, reader.ReadVec3()), in o);
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 48;
	}
}
