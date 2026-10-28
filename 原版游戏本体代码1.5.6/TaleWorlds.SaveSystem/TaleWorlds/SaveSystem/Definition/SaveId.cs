using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

public abstract class SaveId
{
	public abstract string GetStringId();

	public override int GetHashCode()
	{
		return GetStringId().GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return GetStringId() == ((SaveId)obj).GetStringId();
	}

	public abstract void WriteTo(IWriter writer);

	public static SaveId ReadSaveIdFrom(IReader reader)
	{
		byte b = reader.ReadByte();
		SaveId result = null;
		switch (b)
		{
		case 0:
			result = TypeSaveId.ReadFrom(reader);
			break;
		case 1:
			result = GenericSaveId.ReadFrom(reader);
			break;
		case 2:
			result = ContainerSaveId.ReadFrom(reader);
			break;
		}
		return result;
	}

	public abstract int GetSizeInBytes();
}
