using System;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x0200011F RID: 287
	public class ClanCardSelectionPopupItemVM : ViewModel
	{
		// Token: 0x170008C2 RID: 2242
		// (get) Token: 0x06001A14 RID: 6676 RVA: 0x000626CE File Offset: 0x000608CE
		public object Identifier { get; }

		// Token: 0x170008C3 RID: 2243
		// (get) Token: 0x06001A15 RID: 6677 RVA: 0x000626D6 File Offset: 0x000608D6
		public TextObject ActionResultText { get; }

		// Token: 0x06001A16 RID: 6678 RVA: 0x000626E0 File Offset: 0x000608E0
		public ClanCardSelectionPopupItemVM(in ClanCardSelectionItemInfo info, Action<ClanCardSelectionPopupItemVM> onSelected)
		{
			this.Identifier = info.Identifier;
			this._onSelected = onSelected;
			this.ActionResultText = info.ActionResult;
			this._titleText = info.Title;
			this._disabledReasonText = info.DisabledReason;
			this._specialActionText = info.SpecialActionText;
			this.DisabledHint = new HintViewModel();
			this.Properties = new MBBindingList<ClanCardSelectionPopupItemPropertyVM>();
			if (info.Properties != null)
			{
				foreach (ClanCardSelectionItemPropertyInfo clanCardSelectionItemPropertyInfo in info.Properties)
				{
					this.Properties.Add(new ClanCardSelectionPopupItemPropertyVM(ref clanCardSelectionItemPropertyInfo));
				}
			}
			this.IsDisabled = info.IsDisabled;
			this.IsSpecialActionItem = info.IsSpecialActionItem;
			this.HasSprite = !string.IsNullOrEmpty(info.SpriteName);
			this.HasImage = info.Image != null;
			this.SpriteType = info.SpriteType.ToString();
			this.SpriteName = info.SpriteName ?? string.Empty;
			this.SpriteLabel = info.SpriteLabel ?? string.Empty;
			this.Image = new GenericImageIdentifierVM(info.Image);
			this.RefreshValues();
		}

		// Token: 0x06001A17 RID: 6679 RVA: 0x00062834 File Offset: 0x00060A34
		public override void RefreshValues()
		{
			base.RefreshValues();
			TextObject titleText = this._titleText;
			this.Title = ((titleText != null) ? titleText.ToString() : null) ?? string.Empty;
			TextObject specialActionText = this._specialActionText;
			this.SpecialAction = ((specialActionText != null) ? specialActionText.ToString() : null) ?? string.Empty;
			this.DisabledHint.HintText = (this.IsDisabled ? this._disabledReasonText : TextObject.GetEmpty());
			this.Properties.ApplyActionOnAllItems(delegate(ClanCardSelectionPopupItemPropertyVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06001A18 RID: 6680 RVA: 0x000628D3 File Offset: 0x00060AD3
		public void ExecuteSelect()
		{
			Action<ClanCardSelectionPopupItemVM> onSelected = this._onSelected;
			if (onSelected == null)
			{
				return;
			}
			onSelected(this);
		}

		// Token: 0x170008C4 RID: 2244
		// (get) Token: 0x06001A19 RID: 6681 RVA: 0x000628E6 File Offset: 0x00060AE6
		// (set) Token: 0x06001A1A RID: 6682 RVA: 0x000628EE File Offset: 0x00060AEE
		[DataSourceProperty]
		public ImageIdentifierVM Image
		{
			get
			{
				return this._image;
			}
			set
			{
				if (value != this._image)
				{
					this._image = value;
					base.OnPropertyChangedWithValue<ImageIdentifierVM>(value, "Image");
				}
			}
		}

		// Token: 0x170008C5 RID: 2245
		// (get) Token: 0x06001A1B RID: 6683 RVA: 0x0006290C File Offset: 0x00060B0C
		// (set) Token: 0x06001A1C RID: 6684 RVA: 0x00062914 File Offset: 0x00060B14
		[DataSourceProperty]
		public MBBindingList<ClanCardSelectionPopupItemPropertyVM> Properties
		{
			get
			{
				return this._properties;
			}
			set
			{
				if (value != this._properties)
				{
					this._properties = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanCardSelectionPopupItemPropertyVM>>(value, "Properties");
				}
			}
		}

		// Token: 0x170008C6 RID: 2246
		// (get) Token: 0x06001A1D RID: 6685 RVA: 0x00062932 File Offset: 0x00060B32
		// (set) Token: 0x06001A1E RID: 6686 RVA: 0x0006293A File Offset: 0x00060B3A
		[DataSourceProperty]
		public HintViewModel DisabledHint
		{
			get
			{
				return this._disabledHint;
			}
			set
			{
				if (value != this._disabledHint)
				{
					this._disabledHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "DisabledHint");
				}
			}
		}

		// Token: 0x170008C7 RID: 2247
		// (get) Token: 0x06001A1F RID: 6687 RVA: 0x00062958 File Offset: 0x00060B58
		// (set) Token: 0x06001A20 RID: 6688 RVA: 0x00062960 File Offset: 0x00060B60
		[DataSourceProperty]
		public string Title
		{
			get
			{
				return this._title;
			}
			set
			{
				if (value != this._title)
				{
					this._title = value;
					base.OnPropertyChangedWithValue<string>(value, "Title");
				}
			}
		}

		// Token: 0x170008C8 RID: 2248
		// (get) Token: 0x06001A21 RID: 6689 RVA: 0x00062983 File Offset: 0x00060B83
		// (set) Token: 0x06001A22 RID: 6690 RVA: 0x0006298B File Offset: 0x00060B8B
		[DataSourceProperty]
		public string SpriteType
		{
			get
			{
				return this._spriteType;
			}
			set
			{
				if (value != this._spriteType)
				{
					this._spriteType = value;
					base.OnPropertyChangedWithValue<string>(value, "SpriteType");
				}
			}
		}

		// Token: 0x170008C9 RID: 2249
		// (get) Token: 0x06001A23 RID: 6691 RVA: 0x000629AE File Offset: 0x00060BAE
		// (set) Token: 0x06001A24 RID: 6692 RVA: 0x000629B6 File Offset: 0x00060BB6
		[DataSourceProperty]
		public string SpriteName
		{
			get
			{
				return this._spriteName;
			}
			set
			{
				if (value != this._spriteName)
				{
					this._spriteName = value;
					base.OnPropertyChangedWithValue<string>(value, "SpriteName");
				}
			}
		}

		// Token: 0x170008CA RID: 2250
		// (get) Token: 0x06001A25 RID: 6693 RVA: 0x000629D9 File Offset: 0x00060BD9
		// (set) Token: 0x06001A26 RID: 6694 RVA: 0x000629E1 File Offset: 0x00060BE1
		[DataSourceProperty]
		public string SpriteLabel
		{
			get
			{
				return this._spriteLabel;
			}
			set
			{
				if (value != this._spriteLabel)
				{
					this._spriteLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "SpriteLabel");
				}
			}
		}

		// Token: 0x170008CB RID: 2251
		// (get) Token: 0x06001A27 RID: 6695 RVA: 0x00062A04 File Offset: 0x00060C04
		// (set) Token: 0x06001A28 RID: 6696 RVA: 0x00062A0C File Offset: 0x00060C0C
		[DataSourceProperty]
		public string SpecialAction
		{
			get
			{
				return this._specialAction;
			}
			set
			{
				if (value != this._specialAction)
				{
					this._specialAction = value;
					base.OnPropertyChangedWithValue<string>(value, "SpecialAction");
				}
			}
		}

		// Token: 0x170008CC RID: 2252
		// (get) Token: 0x06001A29 RID: 6697 RVA: 0x00062A2F File Offset: 0x00060C2F
		// (set) Token: 0x06001A2A RID: 6698 RVA: 0x00062A37 File Offset: 0x00060C37
		[DataSourceProperty]
		public bool HasImage
		{
			get
			{
				return this._hasImage;
			}
			set
			{
				if (value != this._hasImage)
				{
					this._hasImage = value;
					base.OnPropertyChangedWithValue(value, "HasImage");
				}
			}
		}

		// Token: 0x170008CD RID: 2253
		// (get) Token: 0x06001A2B RID: 6699 RVA: 0x00062A55 File Offset: 0x00060C55
		// (set) Token: 0x06001A2C RID: 6700 RVA: 0x00062A5D File Offset: 0x00060C5D
		[DataSourceProperty]
		public bool HasSprite
		{
			get
			{
				return this._hasSprite;
			}
			set
			{
				if (value != this._hasSprite)
				{
					this._hasSprite = value;
					base.OnPropertyChangedWithValue(value, "HasSprite");
				}
			}
		}

		// Token: 0x170008CE RID: 2254
		// (get) Token: 0x06001A2D RID: 6701 RVA: 0x00062A7B File Offset: 0x00060C7B
		// (set) Token: 0x06001A2E RID: 6702 RVA: 0x00062A83 File Offset: 0x00060C83
		[DataSourceProperty]
		public bool IsSpecialActionItem
		{
			get
			{
				return this._isSpecialActionItem;
			}
			set
			{
				if (value != this._isSpecialActionItem)
				{
					this._isSpecialActionItem = value;
					base.OnPropertyChangedWithValue(value, "IsSpecialActionItem");
				}
			}
		}

		// Token: 0x170008CF RID: 2255
		// (get) Token: 0x06001A2F RID: 6703 RVA: 0x00062AA1 File Offset: 0x00060CA1
		// (set) Token: 0x06001A30 RID: 6704 RVA: 0x00062AA9 File Offset: 0x00060CA9
		[DataSourceProperty]
		public bool IsDisabled
		{
			get
			{
				return this._isDisabled;
			}
			set
			{
				if (value != this._isDisabled)
				{
					this._isDisabled = value;
					base.OnPropertyChangedWithValue(value, "IsDisabled");
				}
			}
		}

		// Token: 0x170008D0 RID: 2256
		// (get) Token: 0x06001A31 RID: 6705 RVA: 0x00062AC7 File Offset: 0x00060CC7
		// (set) Token: 0x06001A32 RID: 6706 RVA: 0x00062ACF File Offset: 0x00060CCF
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x04000C12 RID: 3090
		private readonly TextObject _titleText;

		// Token: 0x04000C13 RID: 3091
		private readonly TextObject _disabledReasonText;

		// Token: 0x04000C14 RID: 3092
		private readonly TextObject _specialActionText;

		// Token: 0x04000C15 RID: 3093
		private readonly Action<ClanCardSelectionPopupItemVM> _onSelected;

		// Token: 0x04000C16 RID: 3094
		private ImageIdentifierVM _image;

		// Token: 0x04000C17 RID: 3095
		private MBBindingList<ClanCardSelectionPopupItemPropertyVM> _properties;

		// Token: 0x04000C18 RID: 3096
		private HintViewModel _disabledHint;

		// Token: 0x04000C19 RID: 3097
		private string _title;

		// Token: 0x04000C1A RID: 3098
		private string _spriteType;

		// Token: 0x04000C1B RID: 3099
		private string _spriteName;

		// Token: 0x04000C1C RID: 3100
		private string _spriteLabel;

		// Token: 0x04000C1D RID: 3101
		private string _specialAction;

		// Token: 0x04000C1E RID: 3102
		private bool _hasImage;

		// Token: 0x04000C1F RID: 3103
		private bool _hasSprite;

		// Token: 0x04000C20 RID: 3104
		private bool _isSpecialActionItem;

		// Token: 0x04000C21 RID: 3105
		private bool _isDisabled;

		// Token: 0x04000C22 RID: 3106
		private bool _isSelected;
	}
}
