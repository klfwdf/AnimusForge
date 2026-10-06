namespace TaleWorlds.Engine.Options;

public struct SelectionData
{
	public bool IsLocalizationId;

	public string Data;

	public SelectionData(bool isLocalizationId, string data)
	{
		IsLocalizationId = isLocalizationId;
		Data = data;
	}
}
