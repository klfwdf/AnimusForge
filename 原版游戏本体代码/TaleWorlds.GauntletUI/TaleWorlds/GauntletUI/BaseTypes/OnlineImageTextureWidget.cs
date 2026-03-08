using System;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000061 RID: 97
	public class OnlineImageTextureWidget : TextureWidget
	{
		// Token: 0x170001CF RID: 463
		// (get) Token: 0x06000670 RID: 1648 RVA: 0x0001B992 File Offset: 0x00019B92
		// (set) Token: 0x06000671 RID: 1649 RVA: 0x0001B99A File Offset: 0x00019B9A
		public OnlineImageTextureWidget.ImageSizePolicies ImageSizePolicy { get; set; }

		// Token: 0x06000672 RID: 1650 RVA: 0x0001B9A3 File Offset: 0x00019BA3
		public OnlineImageTextureWidget(UIContext context)
			: base(context)
		{
			base.TextureProviderName = "OnlineImageTextureProvider";
		}

		// Token: 0x06000673 RID: 1651 RVA: 0x0001B9B7 File Offset: 0x00019BB7
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			this.UpdateSizePolicy();
		}

		// Token: 0x06000674 RID: 1652 RVA: 0x0001B9C8 File Offset: 0x00019BC8
		private void UpdateSizePolicy()
		{
			Texture texture = base.Texture;
			bool flag = texture != null && texture.IsValid;
			if (this.ImageSizePolicy == OnlineImageTextureWidget.ImageSizePolicies.OriginalSize)
			{
				if (flag)
				{
					base.WidthSizePolicy = SizePolicy.Fixed;
					base.HeightSizePolicy = SizePolicy.Fixed;
					base.SuggestedWidth = (float)base.Texture.Width;
					base.SuggestedHeight = (float)base.Texture.Height;
					return;
				}
			}
			else
			{
				if (this.ImageSizePolicy == OnlineImageTextureWidget.ImageSizePolicies.Stretch)
				{
					base.WidthSizePolicy = SizePolicy.StretchToParent;
					base.HeightSizePolicy = SizePolicy.StretchToParent;
					return;
				}
				if (this.ImageSizePolicy == OnlineImageTextureWidget.ImageSizePolicies.ScaleToBiggerDimension && flag)
				{
					base.WidthSizePolicy = SizePolicy.Fixed;
					base.HeightSizePolicy = SizePolicy.Fixed;
					float num;
					if (base.Texture.Width > base.Texture.Height)
					{
						num = base.ParentWidget.Size.Y / (float)base.Texture.Height;
						if (num * (float)base.Texture.Width < base.ParentWidget.Size.X)
						{
							num = base.ParentWidget.Size.X / (float)base.Texture.Width;
						}
					}
					else
					{
						num = base.ParentWidget.Size.X / (float)base.Texture.Width;
						if (num * (float)base.Texture.Height < base.ParentWidget.Size.Y)
						{
							num = base.ParentWidget.Size.Y / (float)base.Texture.Height;
						}
					}
					base.ScaledSuggestedWidth = num * (float)base.Texture.Width;
					base.ScaledSuggestedHeight = num * (float)base.Texture.Height;
				}
			}
		}

		// Token: 0x170001D0 RID: 464
		// (get) Token: 0x06000675 RID: 1653 RVA: 0x0001BB63 File Offset: 0x00019D63
		// (set) Token: 0x06000676 RID: 1654 RVA: 0x0001BB6B File Offset: 0x00019D6B
		[Editor(false)]
		public string OnlineImageSourceUrl
		{
			get
			{
				return this._onlineImageSourceUrl;
			}
			set
			{
				if (this._onlineImageSourceUrl != value)
				{
					this._onlineImageSourceUrl = value;
					base.OnPropertyChanged<string>(value, "OnlineImageSourceUrl");
					base.SetTextureProviderProperty("OnlineSourceUrl", value);
					this.RefreshState();
				}
			}
		}

		// Token: 0x04000303 RID: 771
		private string _onlineImageSourceUrl;

		// Token: 0x02000096 RID: 150
		public enum ImageSizePolicies
		{
			// Token: 0x04000492 RID: 1170
			Stretch,
			// Token: 0x04000493 RID: 1171
			OriginalSize,
			// Token: 0x04000494 RID: 1172
			ScaleToBiggerDimension
		}
	}
}
