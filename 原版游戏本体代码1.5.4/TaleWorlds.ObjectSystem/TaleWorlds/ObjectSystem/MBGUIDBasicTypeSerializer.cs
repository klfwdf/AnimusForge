using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;

namespace TaleWorlds.ObjectSystem;

internal class MBGUIDBasicTypeSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
		writer.WriteUInt(((MBGUID)value).InternalValue);
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return new MBGUID(reader.ReadUInt());
	}

	int IBasicTypeSerializer.GetSizeInBytes()
	{
		return 4;
	}
}
