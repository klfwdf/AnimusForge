using System;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers
{
	// Token: 0x02000021 RID: 33
	public abstract class ImageIdentifierVM : ViewModel
	{
		// Token: 0x17000086 RID: 134
		// (get) Token: 0x0600019D RID: 413 RVA: 0x00005A05 File Offset: 0x00003C05
		// (set) Token: 0x0600019E RID: 414 RVA: 0x00005A10 File Offset: 0x00003C10
		protected ImageIdentifier ImageIdentifier
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
					ImageIdentifier imageIdentifier = this._imageIdentifier;
					this.Id = ((imageIdentifier != null) ? imageIdentifier.Id : null) ?? string.Empty;
					ImageIdentifier imageIdentifier2 = this._imageIdentifier;
					this.AdditionalArgs = ((imageIdentifier2 != null) ? imageIdentifier2.AdditionalArgs : null) ?? string.Empty;
					ImageIdentifier imageIdentifier3 = this._imageIdentifier;
					this.TextureProviderName = ((imageIdentifier3 != null) ? imageIdentifier3.TextureProviderName : null) ?? string.Empty;
				}
			}
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00005A90 File Offset: 0x00003C90
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.ImageIdentifier = null;
		}

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x060001A0 RID: 416 RVA: 0x00005A9F File Offset: 0x00003C9F
		// (set) Token: 0x060001A1 RID: 417 RVA: 0x00005AA7 File Offset: 0x00003CA7
		[DataSourceProperty]
		public string Id
		{
			get
			{
				return this._id;
			}
			set
			{
				if (this._id != value)
				{
					this._id = value;
					base.OnPropertyChangedWithValue<string>(value, "Id");
				}
			}
		}

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x060001A2 RID: 418 RVA: 0x00005ACA File Offset: 0x00003CCA
		// (set) Token: 0x060001A3 RID: 419 RVA: 0x00005AD2 File Offset: 0x00003CD2
		[DataSourceProperty]
		public string AdditionalArgs
		{
			get
			{
				return this._additionalArgs;
			}
			set
			{
				if (value != this._additionalArgs)
				{
					this._additionalArgs = value;
					base.OnPropertyChangedWithValue<string>(value, "AdditionalArgs");
				}
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x060001A4 RID: 420 RVA: 0x00005AF5 File Offset: 0x00003CF5
		// (set) Token: 0x060001A5 RID: 421 RVA: 0x00005AFD File Offset: 0x00003CFD
		[DataSourceProperty]
		public string TextureProviderName
		{
			get
			{
				return this._textureProviderName;
			}
			set
			{
				if (value != this._textureProviderName)
				{
					this._textureProviderName = value;
					base.OnPropertyChangedWithValue<string>(value, "TextureProviderName");
				}
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x060001A6 RID: 422 RVA: 0x00005B20 File Offset: 0x00003D20
		[DataSourceProperty]
		public bool IsEmpty
		{
			get
			{
				return !string.IsNullOrEmpty(this.TextureProviderName) && string.IsNullOrEmpty(this.ImageIdentifier.Id);
			}
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x060001A7 RID: 423 RVA: 0x00005B41 File Offset: 0x00003D41
		[DataSourceProperty]
		public bool IsValid
		{
			get
			{
				return !this.IsEmpty;
			}
		}

		// Token: 0x040000A5 RID: 165
		private ImageIdentifier _imageIdentifier;

		// Token: 0x040000A6 RID: 166
		private string _id;

		// Token: 0x040000A7 RID: 167
		private string _additionalArgs;

		// Token: 0x040000A8 RID: 168
		private string _textureProviderName;
	}
}
