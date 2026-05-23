using System.Collections.Generic;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public readonly struct ClanCardSelectionItemInfo
{
	public readonly object Identifier;

	public readonly TextObject Title;

	public readonly ImageIdentifier Image;

	public readonly CardSelectionItemSpriteType SpriteType;

	public readonly string SpriteName;

	public readonly string SpriteLabel;

	public readonly IEnumerable<ClanCardSelectionItemPropertyInfo> Properties;

	public readonly bool IsSpecialActionItem;

	public readonly TextObject SpecialActionText;

	public readonly bool IsDisabled;

	public readonly TextObject DisabledReason;

	public readonly TextObject ActionResult;

	public ClanCardSelectionItemInfo(object identifier, TextObject title, ImageIdentifier image, CardSelectionItemSpriteType spriteType, string spriteName, string spriteLabel, IEnumerable<ClanCardSelectionItemPropertyInfo> properties, bool isDisabled, TextObject disabledReason, TextObject actionResult)
	{
		bool isDisabled2 = isDisabled;
		Identifier = identifier;
		Title = title;
		Image = image;
		SpriteType = spriteType;
		SpriteName = spriteName;
		SpriteLabel = spriteLabel;
		Properties = properties;
		IsSpecialActionItem = false;
		SpecialActionText = null;
		IsDisabled = isDisabled2;
		DisabledReason = disabledReason;
		ActionResult = actionResult;
	}

	public ClanCardSelectionItemInfo(TextObject specialActionText, bool isDisabled, TextObject disabledReason, TextObject actionResult)
	{
		bool isDisabled2 = isDisabled;
		Identifier = null;
		Title = null;
		Image = null;
		SpriteType = CardSelectionItemSpriteType.None;
		SpriteName = null;
		SpriteLabel = null;
		Properties = null;
		IsSpecialActionItem = true;
		SpecialActionText = specialActionText;
		IsDisabled = isDisabled2;
		DisabledReason = disabledReason;
		ActionResult = actionResult;
	}
}
