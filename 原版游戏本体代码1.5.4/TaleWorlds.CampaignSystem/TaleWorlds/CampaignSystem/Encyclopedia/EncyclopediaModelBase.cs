using System;

namespace TaleWorlds.CampaignSystem.Encyclopedia;

public abstract class EncyclopediaModelBase : Attribute
{
	public Type[] PageTargetTypes { get; private set; }

	public EncyclopediaModelBase(Type[] pageTargetTypes)
	{
		PageTargetTypes = pageTargetTypes;
	}
}
