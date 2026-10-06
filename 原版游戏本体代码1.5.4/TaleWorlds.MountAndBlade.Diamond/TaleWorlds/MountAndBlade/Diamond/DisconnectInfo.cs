namespace TaleWorlds.MountAndBlade.Diamond;

public class DisconnectInfo
{
	public DisconnectType Type { get; set; }

	public DisconnectInfo()
	{
		Type = DisconnectType.Unknown;
	}
}
