namespace psai.net;

public class ThemeInfo
{
	public int id;

	public ThemeType type;

	public int[] segmentIds;

	public string name;

	public override string ToString()
	{
		return string.Concat(id, ": ", name, " [", type, "]");
	}
}
