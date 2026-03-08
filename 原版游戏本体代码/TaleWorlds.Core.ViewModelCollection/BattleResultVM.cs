using System;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection
{
	// Token: 0x02000002 RID: 2
	public class BattleResultVM : ViewModel
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		public BattleResultVM(string text, Func<List<TooltipProperty>> propertyFunc, CharacterCode deadHeroCode = null)
		{
			this.Text = text;
			this.Hint = new BasicTooltipViewModel(propertyFunc);
			if (deadHeroCode != null)
			{
				this.DeadLordPortrait = new CharacterImageIdentifierVM(deadHeroCode);
				this.DeadLordClanBanner = new BannerImageIdentifierVM(deadHeroCode.Banner, true);
				return;
			}
			this.DeadLordPortrait = null;
			this.DeadLordClanBanner = null;
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000002 RID: 2 RVA: 0x0000209E File Offset: 0x0000029E
		// (set) Token: 0x06000003 RID: 3 RVA: 0x000020A6 File Offset: 0x000002A6
		[DataSourceProperty]
		public string Text
		{
			get
			{
				return this._text;
			}
			set
			{
				if (value != this._text)
				{
					this._text = value;
					base.OnPropertyChangedWithValue<string>(value, "Text");
				}
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000004 RID: 4 RVA: 0x000020C9 File Offset: 0x000002C9
		// (set) Token: 0x06000005 RID: 5 RVA: 0x000020D1 File Offset: 0x000002D1
		[DataSourceProperty]
		public BasicTooltipViewModel Hint
		{
			get
			{
				return this._hint;
			}
			set
			{
				if (value != this._hint)
				{
					this._hint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000006 RID: 6 RVA: 0x000020EF File Offset: 0x000002EF
		// (set) Token: 0x06000007 RID: 7 RVA: 0x000020F7 File Offset: 0x000002F7
		[DataSourceProperty]
		public CharacterImageIdentifierVM DeadLordPortrait
		{
			get
			{
				return this._deadLordPortrait;
			}
			set
			{
				if (value != this._deadLordPortrait)
				{
					this._deadLordPortrait = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "DeadLordPortrait");
				}
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000008 RID: 8 RVA: 0x00002115 File Offset: 0x00000315
		// (set) Token: 0x06000009 RID: 9 RVA: 0x0000211D File Offset: 0x0000031D
		[DataSourceProperty]
		public BannerImageIdentifierVM DeadLordClanBanner
		{
			get
			{
				return this._deadLordClanBanner;
			}
			set
			{
				if (value != this._deadLordClanBanner)
				{
					this._deadLordClanBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "DeadLordClanBanner");
				}
			}
		}

		// Token: 0x04000001 RID: 1
		private string _text;

		// Token: 0x04000002 RID: 2
		private BasicTooltipViewModel _hint;

		// Token: 0x04000003 RID: 3
		private CharacterImageIdentifierVM _deadLordPortrait;

		// Token: 0x04000004 RID: 4
		private BannerImageIdentifierVM _deadLordClanBanner;
	}
}
