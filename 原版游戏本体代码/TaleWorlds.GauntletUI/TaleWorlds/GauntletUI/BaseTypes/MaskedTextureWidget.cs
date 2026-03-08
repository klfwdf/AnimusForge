using System;
using System.Numerics;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x0200005F RID: 95
	public class MaskedTextureWidget : TextureWidget
	{
		// Token: 0x170001CA RID: 458
		// (get) Token: 0x0600065F RID: 1631 RVA: 0x0001B573 File Offset: 0x00019773
		// (set) Token: 0x06000660 RID: 1632 RVA: 0x0001B57B File Offset: 0x0001977B
		[Editor(false)]
		public float OverlayTextureScale { get; set; }

		// Token: 0x06000661 RID: 1633 RVA: 0x0001B584 File Offset: 0x00019784
		public MaskedTextureWidget(UIContext context)
			: base(context)
		{
			base.TextureProviderName = "";
			this.OverlayTextureScale = 1f;
		}

		// Token: 0x06000662 RID: 1634 RVA: 0x0001B5A3 File Offset: 0x000197A3
		public override void OnClearTextureProvider()
		{
			this._textureCache = null;
			base.SetTextureProviderProperty("IsReleased", true);
			base.OnClearTextureProvider();
		}

		// Token: 0x06000663 RID: 1635 RVA: 0x0001B5C4 File Offset: 0x000197C4
		protected internal override void OnContextActivated()
		{
			base.OnContextActivated();
			string imageId = this.ImageId;
			this.ImageId = string.Empty;
			this.ImageId = imageId;
		}

		// Token: 0x06000664 RID: 1636 RVA: 0x0001B5F0 File Offset: 0x000197F0
		protected internal override void OnContextDeactivated()
		{
			base.OnContextDeactivated();
			base.SetTextureProviderProperty("IsReleased", true);
		}

		// Token: 0x170001CB RID: 459
		// (get) Token: 0x06000665 RID: 1637 RVA: 0x0001B609 File Offset: 0x00019809
		// (set) Token: 0x06000666 RID: 1638 RVA: 0x0001B614 File Offset: 0x00019814
		[Editor(false)]
		public string ImageId
		{
			get
			{
				return this._imageId;
			}
			set
			{
				if (this._imageId != value)
				{
					if (!string.IsNullOrEmpty(this._imageId))
					{
						base.SetTextureProviderProperty("IsReleased", true);
					}
					this._imageId = value;
					base.OnPropertyChanged<string>(value, "ImageId");
					base.SetTextureProviderProperty("ImageId", value);
					if (!string.IsNullOrEmpty(this._imageId))
					{
						base.SetTextureProviderProperty("IsReleased", false);
					}
				}
			}
		}

		// Token: 0x170001CC RID: 460
		// (get) Token: 0x06000667 RID: 1639 RVA: 0x0001B68A File Offset: 0x0001988A
		// (set) Token: 0x06000668 RID: 1640 RVA: 0x0001B694 File Offset: 0x00019894
		[Editor(false)]
		public string AdditionalArgs
		{
			get
			{
				return this._additionalArgs;
			}
			set
			{
				if (this._additionalArgs != value)
				{
					if (!string.IsNullOrEmpty(this._additionalArgs))
					{
						base.SetTextureProviderProperty("IsReleased", true);
					}
					this._additionalArgs = value;
					base.OnPropertyChanged<string>(value, "AdditionalArgs");
					base.SetTextureProviderProperty("AdditionalArgs", value);
					if (!string.IsNullOrEmpty(this._additionalArgs))
					{
						base.SetTextureProviderProperty("IsReleased", false);
					}
				}
			}
		}

		// Token: 0x170001CD RID: 461
		// (get) Token: 0x06000669 RID: 1641 RVA: 0x0001B70A File Offset: 0x0001990A
		// (set) Token: 0x0600066A RID: 1642 RVA: 0x0001B714 File Offset: 0x00019914
		[Editor(false)]
		public bool IsBig
		{
			get
			{
				return this._isBig;
			}
			set
			{
				if (this._isBig != value)
				{
					base.SetTextureProviderProperty("IsReleased", true);
					base.SetTextureProviderProperty("IsReleased", false);
					this._isBig = value;
					base.OnPropertyChanged(value, "IsBig");
					base.SetTextureProviderProperty("IsBig", value);
				}
			}
		}

		// Token: 0x0600066B RID: 1643 RVA: 0x0001B770 File Offset: 0x00019970
		protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
		{
			this._isRenderRequestedPreviousFrame = true;
			if (base.TextureProvider == null)
			{
				return;
			}
			Texture textureForRender = base.TextureProvider.GetTextureForRender(twoDimensionContext, null);
			if (textureForRender == null || !textureForRender.IsValid)
			{
				return;
			}
			bool flag = false;
			if (textureForRender != this._textureCache)
			{
				base.Brush.DefaultLayer.OverlayMethod = BrushOverlayMethod.CoverWithTexture;
				this._textureCache = textureForRender;
				flag = true;
				base.UpdateBrushRendererInternal(base.EventManager.CachedDt);
			}
			if (this._textureCache != null)
			{
				bool flag2 = base.TextureProviderName == "BannerImageTextureProvider";
				int num = (flag2 ? ((int)(((base.Size.X > base.Size.Y) ? base.Size.Y : base.Size.X) * 2.5f * this.OverlayTextureScale)) : ((int)(((base.Size.X > base.Size.Y) ? base.Size.X : base.Size.Y) * this.OverlayTextureScale)));
				Vector2 overlayOffset = default(Vector2);
				if (flag2)
				{
					float num2 = ((float)num - base.Size.X) * 0.5f - base.Brush.DefaultLayer.OverlayXOffset;
					float num3 = ((float)num - base.Size.Y) * 0.5f - base.Brush.DefaultLayer.OverlayYOffset;
					overlayOffset = new Vector2(num2, num3) * base._inverseScaleToUse;
				}
				if (this._overlaySpriteCache == null || flag || this._overlaySpriteSizeCache != num)
				{
					this._overlaySpriteSizeCache = num;
					this._overlaySpriteCache = new SpriteFromTexture(this._textureCache, this._overlaySpriteSizeCache, this._overlaySpriteSizeCache);
				}
				base.Brush.DefaultLayer.OverlaySprite = this._overlaySpriteCache;
				base.BrushRenderer.Render(drawContext, this.AreaRect, base._scaleToUse, base.Context.ContextAlpha, overlayOffset, default(Vector2));
			}
		}

		// Token: 0x040002FB RID: 763
		private Texture _textureCache;

		// Token: 0x040002FC RID: 764
		private SpriteFromTexture _overlaySpriteCache;

		// Token: 0x040002FD RID: 765
		private int _overlaySpriteSizeCache;

		// Token: 0x040002FE RID: 766
		private string _imageId;

		// Token: 0x040002FF RID: 767
		private string _additionalArgs;

		// Token: 0x04000300 RID: 768
		private bool _isBig;
	}
}
