using System;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation
{
	// Token: 0x02000155 RID: 341
	public class CharacterCreationReviewStageItemVM : ViewModel
	{
		// Token: 0x06001FFA RID: 8186 RVA: 0x00074D5A File Offset: 0x00072F5A
		public CharacterCreationReviewStageItemVM(BannerImageIdentifierVM imageIdentifier, string title, string text, string description)
			: this(title, text, description)
		{
			this.HasImage = true;
			this.ImageIdentifier = imageIdentifier;
		}

		// Token: 0x06001FFB RID: 8187 RVA: 0x00074D74 File Offset: 0x00072F74
		public CharacterCreationReviewStageItemVM(string title, string text, string description)
		{
			this.Title = title;
			this.Text = text;
			this.Description = description;
		}

		// Token: 0x17000AE3 RID: 2787
		// (get) Token: 0x06001FFC RID: 8188 RVA: 0x00074D91 File Offset: 0x00072F91
		// (set) Token: 0x06001FFD RID: 8189 RVA: 0x00074D99 File Offset: 0x00072F99
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

		// Token: 0x17000AE4 RID: 2788
		// (get) Token: 0x06001FFE RID: 8190 RVA: 0x00074DB7 File Offset: 0x00072FB7
		// (set) Token: 0x06001FFF RID: 8191 RVA: 0x00074DBF File Offset: 0x00072FBF
		[DataSourceProperty]
		public BannerImageIdentifierVM ImageIdentifier
		{
			get
			{
				return this._imageIdentifier;
			}
			set
			{
				if (value != this._imageIdentifier)
				{
					this._imageIdentifier = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "ImageIdentifier");
				}
			}
		}

		// Token: 0x17000AE5 RID: 2789
		// (get) Token: 0x06002000 RID: 8192 RVA: 0x00074DDD File Offset: 0x00072FDD
		// (set) Token: 0x06002001 RID: 8193 RVA: 0x00074DE5 File Offset: 0x00072FE5
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

		// Token: 0x17000AE6 RID: 2790
		// (get) Token: 0x06002002 RID: 8194 RVA: 0x00074E08 File Offset: 0x00073008
		// (set) Token: 0x06002003 RID: 8195 RVA: 0x00074E10 File Offset: 0x00073010
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

		// Token: 0x17000AE7 RID: 2791
		// (get) Token: 0x06002004 RID: 8196 RVA: 0x00074E33 File Offset: 0x00073033
		// (set) Token: 0x06002005 RID: 8197 RVA: 0x00074E3B File Offset: 0x0007303B
		[DataSourceProperty]
		public string Description
		{
			get
			{
				return this._description;
			}
			set
			{
				if (value != this._description)
				{
					this._description = value;
					base.OnPropertyChangedWithValue<string>(value, "Description");
				}
			}
		}

		// Token: 0x04000EE4 RID: 3812
		private bool _hasImage;

		// Token: 0x04000EE5 RID: 3813
		private BannerImageIdentifierVM _imageIdentifier;

		// Token: 0x04000EE6 RID: 3814
		private string _title;

		// Token: 0x04000EE7 RID: 3815
		private string _text;

		// Token: 0x04000EE8 RID: 3816
		private string _description;
	}
}
