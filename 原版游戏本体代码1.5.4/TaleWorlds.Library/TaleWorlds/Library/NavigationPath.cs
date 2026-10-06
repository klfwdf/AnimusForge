using System.Runtime.Serialization;
using System.Security.Permissions;

namespace TaleWorlds.Library;

public class NavigationPath : ISerializable
{
	private const int PathSize = 128;

	public Vec2[] PathPoints { get; private set; }

	[CachedData]
	public int Size { get; set; }

	public Vec2 this[int i] => PathPoints[i];

	public NavigationPath()
	{
		PathPoints = new Vec2[128];
	}

	protected NavigationPath(SerializationInfo info, StreamingContext context)
	{
		PathPoints = new Vec2[128];
		Size = info.GetInt32("s");
		for (int i = 0; i < Size; i++)
		{
			float single = info.GetSingle("x" + i);
			float single2 = info.GetSingle("y" + i);
			PathPoints[i] = new Vec2(single, single2);
		}
	}

	[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("s", Size);
		for (int i = 0; i < Size; i++)
		{
			info.AddValue("x" + i, PathPoints[i].x);
			info.AddValue("y" + i, PathPoints[i].y);
		}
	}

	public void OverridePathPointAtIndex(int index, in Vec2 newValue)
	{
		PathPoints[index] = newValue;
	}
}
