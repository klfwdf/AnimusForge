using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x0200006B RID: 107
	public class TextureWidget : ImageWidget
	{
		// Token: 0x1700020F RID: 527
		// (get) Token: 0x06000745 RID: 1861 RVA: 0x0001F21E File Offset: 0x0001D41E
		// (set) Token: 0x06000746 RID: 1862 RVA: 0x0001F226 File Offset: 0x0001D426
		public Widget LoadingIconWidget { get; set; }

		// Token: 0x17000210 RID: 528
		// (get) Token: 0x06000747 RID: 1863 RVA: 0x0001F22F File Offset: 0x0001D42F
		// (set) Token: 0x06000748 RID: 1864 RVA: 0x0001F237 File Offset: 0x0001D437
		public TextureProvider TextureProvider { get; private set; }

		// Token: 0x17000211 RID: 529
		// (get) Token: 0x06000749 RID: 1865 RVA: 0x0001F240 File Offset: 0x0001D440
		// (set) Token: 0x0600074A RID: 1866 RVA: 0x0001F248 File Offset: 0x0001D448
		public bool SetForClearNextFrame { get; protected set; }

		// Token: 0x17000212 RID: 530
		// (get) Token: 0x0600074B RID: 1867 RVA: 0x0001F251 File Offset: 0x0001D451
		// (set) Token: 0x0600074C RID: 1868 RVA: 0x0001F259 File Offset: 0x0001D459
		[Editor(false)]
		public string TextureProviderName
		{
			get
			{
				return this._textureProviderName;
			}
			set
			{
				if (this._textureProviderName != value)
				{
					this._textureProviderName = value;
					base.OnPropertyChanged<string>(value, "TextureProviderName");
				}
			}
		}

		// Token: 0x17000213 RID: 531
		// (get) Token: 0x0600074D RID: 1869 RVA: 0x0001F27C File Offset: 0x0001D47C
		// (set) Token: 0x0600074E RID: 1870 RVA: 0x0001F284 File Offset: 0x0001D484
		public Texture Texture
		{
			get
			{
				return this._texture;
			}
			protected set
			{
				if (value != this._texture)
				{
					this._texture = value;
					this.OnTextureUpdated();
				}
			}
		}

		// Token: 0x0600074F RID: 1871 RVA: 0x0001F29C File Offset: 0x0001D49C
		public TextureWidget(UIContext context)
			: base(context)
		{
			this.TextureProviderName = "ResourceTextureProvider";
			this.TextureProvider = null;
			this._textureProviderProperties = new Dictionary<string, object>();
			this.SetTextureProviderProperty("SourceInfo", base.Context.Name ?? this.ToString());
		}

		// Token: 0x06000750 RID: 1872 RVA: 0x0001F2ED File Offset: 0x0001D4ED
		public virtual void OnClearTextureProvider()
		{
			TextureProvider textureProvider = this.TextureProvider;
			if (textureProvider != null)
			{
				textureProvider.Clear(true);
			}
			this.TextureProvider = null;
			this.SetForClearNextFrame = true;
			this._lastWidth = 0f;
			this._lastHeight = 0f;
		}

		// Token: 0x06000751 RID: 1873 RVA: 0x0001F325 File Offset: 0x0001D525
		protected override void OnDisconnectedFromRoot()
		{
			base.OnDisconnectedFromRoot();
			this.OnClearTextureProvider();
		}

		// Token: 0x06000752 RID: 1874 RVA: 0x0001F334 File Offset: 0x0001D534
		private void SetTextureProviderProperties()
		{
			if (this.TextureProvider != null)
			{
				foreach (KeyValuePair<string, object> keyValuePair in this._textureProviderProperties)
				{
					this.TextureProvider.SetProperty(keyValuePair.Key, keyValuePair.Value);
				}
			}
		}

		// Token: 0x06000753 RID: 1875 RVA: 0x0001F3A4 File Offset: 0x0001D5A4
		protected void SetTextureProviderProperty(string name, object value)
		{
			this._textureProviderProperties[name] = value;
			if (this.TextureProvider != null)
			{
				this.TextureProvider.SetProperty(name, value);
			}
			this.Texture = null;
		}

		// Token: 0x06000754 RID: 1876 RVA: 0x0001F3CF File Offset: 0x0001D5CF
		protected object GetTextureProviderProperty(string propertyName)
		{
			TextureProvider textureProvider = this.TextureProvider;
			if (textureProvider == null)
			{
				return null;
			}
			return textureProvider.GetProperty(propertyName);
		}

		// Token: 0x06000755 RID: 1877 RVA: 0x0001F3E4 File Offset: 0x0001D5E4
		protected TObject? GetTextureProviderProperty<TObject>(string propertyName) where TObject : struct
		{
			TextureProvider textureProvider = this.TextureProvider;
			object obj;
			if ((obj = ((textureProvider != null) ? textureProvider.GetProperty(propertyName) : null)) is TObject)
			{
				TObject value = (TObject)((object)obj);
				return new TObject?(value);
			}
			return null;
		}

		// Token: 0x06000756 RID: 1878 RVA: 0x0001F424 File Offset: 0x0001D624
		protected void UpdateTextureWidget()
		{
			if (this._isRenderRequestedPreviousFrame && base.IsRecursivelyVisible())
			{
				if (this.TextureProvider != null)
				{
					if (this._lastWidth != base.Size.X || this._lastHeight != base.Size.Y || this._isTargetSizeDirty)
					{
						int width = MathF.Round(base.Size.X);
						int height = MathF.Round(base.Size.Y);
						this.TextureProvider.SetTargetSize(width, height);
						this._lastWidth = base.Size.X;
						this._lastHeight = base.Size.Y;
						this._isTargetSizeDirty = false;
						return;
					}
				}
				else if (!string.IsNullOrEmpty(this.TextureProviderName))
				{
					this.TextureProvider = TextureProviderFactory.CreateInstance(this.TextureProviderName);
					this.SetTextureProviderProperties();
					this.SetForClearNextFrame = false;
					this._isTargetSizeDirty = true;
				}
			}
		}

		// Token: 0x06000757 RID: 1879 RVA: 0x0001F510 File Offset: 0x0001D710
		protected virtual void OnTextureUpdated()
		{
			TextureWidget.<>c__DisplayClass33_0 CS$<>8__locals1 = new TextureWidget.<>c__DisplayClass33_0();
			TextureWidget.<>c__DisplayClass33_0 CS$<>8__locals2 = CS$<>8__locals1;
			Texture texture = this.Texture;
			CS$<>8__locals2.isTextureValid = texture != null && texture.IsValid;
			if (this.LoadingIconWidget != null)
			{
				this.LoadingIconWidget.IsVisible = !CS$<>8__locals1.isTextureValid;
				this.LoadingIconWidget.ApplyActionToAllChildrenRecursive(delegate(Widget w)
				{
					w.IsVisible = !CS$<>8__locals1.isTextureValid;
				});
			}
		}

		// Token: 0x06000758 RID: 1880 RVA: 0x0001F56E File Offset: 0x0001D76E
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			this.UpdateTextureWidget();
			if (this._isRenderRequestedPreviousFrame)
			{
				TextureProvider textureProvider = this.TextureProvider;
				if (textureProvider != null)
				{
					textureProvider.Tick(dt);
				}
			}
			this._isRenderRequestedPreviousFrame = false;
		}

		// Token: 0x06000759 RID: 1881 RVA: 0x0001F5A0 File Offset: 0x0001D7A0
		protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
		{
			this._isRenderRequestedPreviousFrame = base.IsRecursivelyVisible();
			if (this.TextureProvider == null)
			{
				return;
			}
			this.Texture = this.TextureProvider.GetTextureForRender(twoDimensionContext, null);
			Texture texture = this.Texture;
			if (texture == null || !texture.IsValid)
			{
				return;
			}
			SimpleMaterial simpleMaterial = drawContext.CreateSimpleMaterial();
			StyleLayer[] layers = base.ReadOnlyBrush.GetStyleOrDefault(base.CurrentState).GetLayers();
			simpleMaterial.OverlayEnabled = false;
			simpleMaterial.CircularMaskingEnabled = false;
			simpleMaterial.Texture = this.Texture;
			simpleMaterial.NinePatchParameters = SpriteNinePatchParameters.Empty;
			if (layers != null && layers.Length != 0)
			{
				StyleLayer styleLayer = layers[0];
				simpleMaterial.AlphaFactor = styleLayer.AlphaFactor * base.ReadOnlyBrush.GlobalAlphaFactor * base.Context.ContextAlpha;
				simpleMaterial.ColorFactor = styleLayer.ColorFactor * base.ReadOnlyBrush.GlobalColorFactor;
				simpleMaterial.HueFactor = styleLayer.HueFactor;
				simpleMaterial.SaturationFactor = styleLayer.SaturationFactor;
				simpleMaterial.ValueFactor = styleLayer.ValueFactor;
				simpleMaterial.Color = styleLayer.Color * base.ReadOnlyBrush.GlobalColor;
			}
			else
			{
				simpleMaterial.AlphaFactor = base.ReadOnlyBrush.GlobalAlphaFactor * base.Context.ContextAlpha;
				simpleMaterial.ColorFactor = base.ReadOnlyBrush.GlobalColorFactor;
				simpleMaterial.HueFactor = 0f;
				simpleMaterial.SaturationFactor = 0f;
				simpleMaterial.ValueFactor = 0f;
				simpleMaterial.Color = Color.White * base.ReadOnlyBrush.GlobalColor;
			}
			ImageDrawObject imageDrawObject = ImageDrawObject.Create(this.AreaRect, Vec2.Zero, Vec2.One);
			imageDrawObject.Scale = base._scaleToUse;
			if (drawContext.CircularMaskEnabled)
			{
				simpleMaterial.CircularMaskingEnabled = true;
				simpleMaterial.CircularMaskingCenter = drawContext.CircularMaskCenter;
				simpleMaterial.CircularMaskingRadius = drawContext.CircularMaskRadius;
				simpleMaterial.CircularMaskingSmoothingRadius = drawContext.CircularMaskSmoothingRadius;
			}
			drawContext.Draw(simpleMaterial, imageDrawObject);
		}

		// Token: 0x04000363 RID: 867
		private string _textureProviderName;

		// Token: 0x04000364 RID: 868
		private Texture _texture;

		// Token: 0x04000365 RID: 869
		private float _lastWidth;

		// Token: 0x04000366 RID: 870
		private float _lastHeight;

		// Token: 0x04000367 RID: 871
		protected bool _isTargetSizeDirty;

		// Token: 0x04000368 RID: 872
		private Dictionary<string, object> _textureProviderProperties;

		// Token: 0x04000369 RID: 873
		protected bool _isRenderRequestedPreviousFrame;
	}
}
