using System;
using System.Collections.Generic;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x0200011E RID: 286
	public readonly struct ClanCardSelectionItemInfo
	{
		// Token: 0x06001A12 RID: 6674 RVA: 0x000625DC File Offset: 0x000607DC
		public ClanCardSelectionItemInfo(object identifier, TextObject title, ImageIdentifier image, CardSelectionItemSpriteType spriteType, string spriteName, string spriteLabel, IEnumerable<ClanCardSelectionItemPropertyInfo> properties, bool isDisabled, TextObject disabledReason, TextObject actionResult)
		{
			this.Identifier = identifier;
			this.Title = title;
			this.Image = image;
			this.SpriteType = spriteType;
			this.SpriteName = spriteName;
			this.SpriteLabel = spriteLabel;
			this.Properties = properties;
			this.IsSpecialActionItem = false;
			this.SpecialActionText = null;
			this.IsDisabled = isDisabled;
			this.DisabledReason = disabledReason;
			this.ActionResult = actionResult;
		}

		// Token: 0x06001A13 RID: 6675 RVA: 0x00062664 File Offset: 0x00060864
		public ClanCardSelectionItemInfo(TextObject specialActionText, bool isDisabled, TextObject disabledReason, TextObject actionResult)
		{
			this.Identifier = null;
			this.Title = null;
			this.Image = null;
			this.SpriteType = CardSelectionItemSpriteType.None;
			this.SpriteName = null;
			this.SpriteLabel = null;
			this.Properties = null;
			this.IsSpecialActionItem = true;
			this.SpecialActionText = specialActionText;
			this.IsDisabled = isDisabled;
			this.DisabledReason = disabledReason;
			this.ActionResult = actionResult;
		}

		// Token: 0x04000C04 RID: 3076
		public readonly object Identifier;

		// Token: 0x04000C05 RID: 3077
		public readonly TextObject Title;

		// Token: 0x04000C06 RID: 3078
		public readonly ImageIdentifier Image;

		// Token: 0x04000C07 RID: 3079
		public readonly CardSelectionItemSpriteType SpriteType;

		// Token: 0x04000C08 RID: 3080
		public readonly string SpriteName;

		// Token: 0x04000C09 RID: 3081
		public readonly string SpriteLabel;

		// Token: 0x04000C0A RID: 3082
		public readonly IEnumerable<ClanCardSelectionItemPropertyInfo> Properties;

		// Token: 0x04000C0B RID: 3083
		public readonly bool IsSpecialActionItem;

		// Token: 0x04000C0C RID: 3084
		public readonly TextObject SpecialActionText;

		// Token: 0x04000C0D RID: 3085
		public readonly bool IsDisabled;

		// Token: 0x04000C0E RID: 3086
		public readonly TextObject DisabledReason;

		// Token: 0x04000C0F RID: 3087
		public readonly TextObject ActionResult;
	}
}
