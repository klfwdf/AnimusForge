using System;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay
{
	// Token: 0x020000B8 RID: 184
	public class GameMenuOverlayActionVM : StringItemWithEnabledAndHintVM
	{
		// Token: 0x06001224 RID: 4644 RVA: 0x000491A4 File Offset: 0x000473A4
		public GameMenuOverlayActionVM(Action<object> onExecute, string item, bool isEnabled, object identifier, TextObject hint = null)
			: base(onExecute, item, isEnabled, identifier, hint)
		{
		}

		// Token: 0x170005F2 RID: 1522
		// (get) Token: 0x06001225 RID: 4645 RVA: 0x000491B3 File Offset: 0x000473B3
		// (set) Token: 0x06001226 RID: 4646 RVA: 0x000491BB File Offset: 0x000473BB
		[DataSourceProperty]
		public bool IsHiglightEnabled
		{
			get
			{
				return this._isHiglightEnabled;
			}
			set
			{
				if (value != this._isHiglightEnabled)
				{
					this._isHiglightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsHiglightEnabled");
				}
			}
		}

		// Token: 0x0400084B RID: 2123
		private bool _isHiglightEnabled;
	}
}
