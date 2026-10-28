using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;

public class MenuOverlay : Attribute
{
	public new string TypeId;

	public MenuOverlay(string typeId)
	{
		TypeId = typeId;
	}
}
