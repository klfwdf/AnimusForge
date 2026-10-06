namespace TaleWorlds.Library;

public interface ISerializableObject
{
	void DeserializeFrom(IReader reader);

	void SerializeTo(IWriter writer);
}
