using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

internal class StringSerializer : IBasicTypeSerializer
{
	void IBasicTypeSerializer.Serialize(IWriter writer, object value)
	{
	}

	object IBasicTypeSerializer.Deserialize(IReader reader)
	{
		return null;
	}

	public int GetSizeInBytes()
	{
		return 0;
	}
}
