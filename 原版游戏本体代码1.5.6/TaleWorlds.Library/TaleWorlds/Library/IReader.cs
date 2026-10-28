namespace TaleWorlds.Library;

public interface IReader
{
	ISerializableObject ReadSerializableObject();

	int ReadInt();

	short ReadShort();

	string ReadString();

	Color ReadColor();

	bool ReadBool();

	float ReadFloat();

	uint ReadUInt();

	ulong ReadULong();

	long ReadLong();

	byte ReadByte();

	byte[] ReadBytes(int length);

	Vec2 ReadVec2();

	Vec3 ReadVec3();

	Vec3i ReadVec3Int();

	sbyte ReadSByte();

	ushort ReadUShort();

	double ReadDouble();
}
