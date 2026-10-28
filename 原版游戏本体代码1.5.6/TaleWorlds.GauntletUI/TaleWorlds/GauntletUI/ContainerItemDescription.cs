namespace TaleWorlds.GauntletUI;

public class ContainerItemDescription
{
	public string WidgetId { get; set; }

	public int WidgetIndex { get; set; }

	public float WidthStretchRatio { get; set; }

	public float HeightStretchRatio { get; set; }

	public ContainerItemDescription()
	{
		WidgetId = "";
		WidgetIndex = -1;
		WidthStretchRatio = 1f;
		HeightStretchRatio = 1f;
	}
}
