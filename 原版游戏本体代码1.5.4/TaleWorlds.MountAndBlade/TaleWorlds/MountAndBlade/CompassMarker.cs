namespace TaleWorlds.MountAndBlade;

public class CompassMarker
{
	public string Id { get; private set; }

	public float Angle { get; private set; }

	public bool IsPrimary { get; private set; }

	public CompassMarker(string id, float angle, bool isPrimary)
	{
		Id = id;
		Angle = angle % 360f;
		IsPrimary = isPrimary;
	}
}
