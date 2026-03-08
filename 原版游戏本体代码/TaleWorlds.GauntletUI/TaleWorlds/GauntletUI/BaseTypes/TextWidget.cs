using System;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x0200006C RID: 108
	public class TextWidget : ImageWidget
	{
		// Token: 0x17000214 RID: 532
		// (get) Token: 0x0600075A RID: 1882 RVA: 0x0001F788 File Offset: 0x0001D988
		// (set) Token: 0x0600075B RID: 1883 RVA: 0x0001F790 File Offset: 0x0001D990
		public bool AutoHideIfEmpty
		{
			get
			{
				return this._autoHideIfEmpty;
			}
			set
			{
				if (value != this._autoHideIfEmpty)
				{
					this._autoHideIfEmpty = value;
					if (this._autoHideIfEmpty)
					{
						base.IsVisible = !string.IsNullOrEmpty(this.Text);
					}
				}
			}
		}

		// Token: 0x17000215 RID: 533
		// (get) Token: 0x0600075C RID: 1884 RVA: 0x0001F7BE File Offset: 0x0001D9BE
		// (set) Token: 0x0600075D RID: 1885 RVA: 0x0001F7CB File Offset: 0x0001D9CB
		[Editor(false)]
		public string Text
		{
			get
			{
				return this._text.Value;
			}
			set
			{
				if (this._text.Value != value)
				{
					this.SetText(value);
				}
			}
		}

		// Token: 0x17000216 RID: 534
		// (get) Token: 0x0600075E RID: 1886 RVA: 0x0001F7E8 File Offset: 0x0001D9E8
		// (set) Token: 0x0600075F RID: 1887 RVA: 0x0001F80C File Offset: 0x0001DA0C
		[Editor(false)]
		public int IntText
		{
			get
			{
				int result;
				if (int.TryParse(this._text.Value, out result))
				{
					return result;
				}
				return -1;
			}
			set
			{
				if (this._text.Value != value.ToString())
				{
					this.SetText(value.ToString());
				}
			}
		}

		// Token: 0x17000217 RID: 535
		// (get) Token: 0x06000760 RID: 1888 RVA: 0x0001F834 File Offset: 0x0001DA34
		// (set) Token: 0x06000761 RID: 1889 RVA: 0x0001F85C File Offset: 0x0001DA5C
		[Editor(false)]
		public float FloatText
		{
			get
			{
				float result;
				if (float.TryParse(this._text.Value, out result))
				{
					return result;
				}
				return -1f;
			}
			set
			{
				if (this._text.Value != value.ToString())
				{
					this.SetText(value.ToString());
				}
			}
		}

		// Token: 0x06000762 RID: 1890 RVA: 0x0001F884 File Offset: 0x0001DA84
		public TextWidget(UIContext context)
			: base(context)
		{
			FontFactory fontFactory = context.FontFactory;
			this._text = new Text((int)base.Size.X, (int)base.Size.Y, fontFactory.DefaultFont, new Func<int, Font>(fontFactory.GetUsableFontForCharacter));
			base.LayoutImp = new TextLayout(this._text);
			this._renderOffset = Vec2.Zero;
		}

		// Token: 0x06000763 RID: 1891 RVA: 0x0001F8F8 File Offset: 0x0001DAF8
		protected virtual void SetText(string value)
		{
			base.SetMeasureAndLayoutDirty();
			this._text.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
			this._text.Value = value;
			base.OnPropertyChanged(this.FloatText, "FloatText");
			base.OnPropertyChanged(this.IntText, "IntText");
			base.OnPropertyChanged<string>(this.Text, "Text");
			this.RefreshTextParameters();
			if (this.AutoHideIfEmpty)
			{
				base.IsVisible = !string.IsNullOrEmpty(this.Text);
			}
			this._renderOffset = Vec2.Zero;
		}

		// Token: 0x06000764 RID: 1892 RVA: 0x0001F994 File Offset: 0x0001DB94
		protected void RefreshTextParameters()
		{
			float fontSize = (float)base.ReadOnlyBrush.FontSize * base._scaleToUse;
			this._text.HorizontalAlignment = base.ReadOnlyBrush.TextHorizontalAlignment;
			this._text.VerticalAlignment = base.ReadOnlyBrush.TextVerticalAlignment;
			this._text.FontSize = fontSize;
			this._text.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
			Font font;
			if (base.ReadOnlyBrush.Font != null)
			{
				font = base.ReadOnlyBrush.Font;
			}
			else
			{
				font = base.Context.FontFactory.DefaultFont;
			}
			this._text.Font = base.Context.FontFactory.GetMappedFontForLocalization(font.Name);
		}

		// Token: 0x06000765 RID: 1893 RVA: 0x0001FA58 File Offset: 0x0001DC58
		protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
		{
			base.OnRender(twoDimensionContext, drawContext);
			this.RefreshTextParameters();
			TextMaterial textMaterial = base.BrushRenderer.CreateTextMaterial(drawContext);
			textMaterial.AlphaFactor *= base.Context.ContextAlpha;
			Rectangle2D areaRect = this.AreaRect;
			areaRect.AddVisualOffset(this._renderOffset.X, this._renderOffset.Y);
			drawContext.Draw(this._text, textMaterial, base.ParentWidget.AreaRect, areaRect);
		}

		// Token: 0x17000218 RID: 536
		// (get) Token: 0x06000766 RID: 1894 RVA: 0x0001FAD6 File Offset: 0x0001DCD6
		// (set) Token: 0x06000767 RID: 1895 RVA: 0x0001FADE File Offset: 0x0001DCDE
		public bool CanBreakWords
		{
			get
			{
				return this._canBreakWords;
			}
			set
			{
				if (value != this._canBreakWords)
				{
					this._canBreakWords = value;
					this._text.CanBreakWords = value;
					base.OnPropertyChanged(value, "CanBreakWords");
				}
			}
		}

		// Token: 0x0400036A RID: 874
		protected readonly Text _text;

		// Token: 0x0400036B RID: 875
		private bool _autoHideIfEmpty;

		// Token: 0x0400036C RID: 876
		protected Vec2 _renderOffset;

		// Token: 0x0400036D RID: 877
		private bool _canBreakWords = true;
	}
}
