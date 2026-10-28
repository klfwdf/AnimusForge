namespace TaleWorlds.Engine.Options;

public struct SelectionData(bool isLocalizationId, string data)
{
	public bool IsLocalizationId = isLocalizationId;

	public string Data = data;
}
