using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

public interface IBasicTypeSerializer
{
	void Serialize(IWriter writer, object value);

	object Deserialize(IReader reader);

	int GetSizeInBytes();
}
