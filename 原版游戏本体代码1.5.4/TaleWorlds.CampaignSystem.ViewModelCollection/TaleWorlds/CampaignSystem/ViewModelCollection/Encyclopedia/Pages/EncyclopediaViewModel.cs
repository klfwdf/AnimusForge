using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

public class EncyclopediaViewModel : Attribute
{
	public Type PageTargetType { get; private set; }

	public EncyclopediaViewModel(Type pageTargetType)
	{
		PageTargetType = pageTargetType;
	}
}
