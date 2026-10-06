using System;

namespace TaleWorlds.CampaignSystem;

[Flags]
public enum CharacterRestrictionFlags : uint
{
	None = 0u,
	NotTransferableInPartyScreen = 1u,
	CanNotGoInHideout = 2u
}
