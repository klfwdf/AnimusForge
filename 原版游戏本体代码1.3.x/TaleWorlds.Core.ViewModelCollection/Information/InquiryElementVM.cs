using System;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Information
{
	// Token: 0x02000017 RID: 23
	public class InquiryElementVM : ViewModel
	{
		// Token: 0x0600011D RID: 285 RVA: 0x0000460C File Offset: 0x0000280C
		public InquiryElementVM(InquiryElement elementData, TextObject hint, Action<InquiryElementVM, bool> onSelectedStateChanged = null)
		{
			this.Text = elementData.Title;
			this.ImageIdentifier = new GenericImageIdentifierVM(elementData.ImageIdentifier);
			this.InquiryElement = elementData;
			this.IsEnabled = elementData.IsEnabled;
			this.HasVisuals = elementData.ImageIdentifier != null;
			this.Hint = new HintViewModel(hint, null);
			this._onSelectedStateChanged = onSelectedStateChanged;
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x0600011E RID: 286 RVA: 0x00004672 File Offset: 0x00002872
		// (set) Token: 0x0600011F RID: 287 RVA: 0x0000467A File Offset: 0x0000287A
		[DataSourceProperty]
		public bool IsFilteredOut
		{
			get
			{
				return this._isFilteredOut;
			}
			set
			{
				if (this._isFilteredOut != value)
				{
					this._isFilteredOut = value;
					base.OnPropertyChangedWithValue(value, "IsFilteredOut");
				}
			}
		}

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000120 RID: 288 RVA: 0x00004698 File Offset: 0x00002898
		// (set) Token: 0x06000121 RID: 289 RVA: 0x000046A0 File Offset: 0x000028A0
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (this._isSelected != value)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
					Action<InquiryElementVM, bool> onSelectedStateChanged = this._onSelectedStateChanged;
					if (onSelectedStateChanged == null)
					{
						return;
					}
					onSelectedStateChanged(this, value);
				}
			}
		}

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x06000122 RID: 290 RVA: 0x000046D0 File Offset: 0x000028D0
		// (set) Token: 0x06000123 RID: 291 RVA: 0x000046D8 File Offset: 0x000028D8
		[DataSourceProperty]
		public bool HasVisuals
		{
			get
			{
				return this._hasVisuals;
			}
			set
			{
				if (this._hasVisuals != value)
				{
					this._hasVisuals = value;
					base.OnPropertyChangedWithValue(value, "HasVisuals");
				}
			}
		}

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x06000124 RID: 292 RVA: 0x000046F6 File Offset: 0x000028F6
		// (set) Token: 0x06000125 RID: 293 RVA: 0x000046FE File Offset: 0x000028FE
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (this._isEnabled != value)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x06000126 RID: 294 RVA: 0x0000471C File Offset: 0x0000291C
		// (set) Token: 0x06000127 RID: 295 RVA: 0x00004724 File Offset: 0x00002924
		[DataSourceProperty]
		public string Text
		{
			get
			{
				return this._text;
			}
			set
			{
				if (this._text != value)
				{
					this._text = value;
					base.OnPropertyChangedWithValue<string>(value, "Text");
				}
			}
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x06000128 RID: 296 RVA: 0x00004747 File Offset: 0x00002947
		// (set) Token: 0x06000129 RID: 297 RVA: 0x0000474F File Offset: 0x0000294F
		[DataSourceProperty]
		public ImageIdentifierVM ImageIdentifier
		{
			get
			{
				return this._imageIdentifier;
			}
			set
			{
				if (this._imageIdentifier != value)
				{
					this._imageIdentifier = value;
					base.OnPropertyChangedWithValue<ImageIdentifierVM>(value, "ImageIdentifier");
				}
			}
		}

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x0600012A RID: 298 RVA: 0x0000476D File Offset: 0x0000296D
		// (set) Token: 0x0600012B RID: 299 RVA: 0x00004775 File Offset: 0x00002975
		[DataSourceProperty]
		public HintViewModel Hint
		{
			get
			{
				return this._hint;
			}
			set
			{
				if (this._hint != value)
				{
					this._hint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x04000078 RID: 120
		public readonly InquiryElement InquiryElement;

		// Token: 0x04000079 RID: 121
		private readonly Action<InquiryElementVM, bool> _onSelectedStateChanged;

		// Token: 0x0400007A RID: 122
		private bool _isFilteredOut;

		// Token: 0x0400007B RID: 123
		private bool _isSelected;

		// Token: 0x0400007C RID: 124
		private bool _isEnabled;

		// Token: 0x0400007D RID: 125
		private string _text;

		// Token: 0x0400007E RID: 126
		private bool _hasVisuals;

		// Token: 0x0400007F RID: 127
		private ImageIdentifierVM _imageIdentifier;

		// Token: 0x04000080 RID: 128
		private HintViewModel _hint;
	}
}
