namespace TaleWorlds.GauntletUI;

public class AudioProperty
{
	[Editor(false)]
	public string AudioName { get; set; }

	[Editor(false)]
	public bool Delay { get; set; }

	[Editor(false)]
	public float DelaySeconds { get; set; }

	public void FillFrom(AudioProperty audioProperty)
	{
		AudioName = audioProperty.AudioName;
		Delay = audioProperty.Delay;
		DelaySeconds = audioProperty.DelaySeconds;
	}
}
