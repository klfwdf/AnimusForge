using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x0200000F RID: 15
	public class ScrollingRichTextWidget : RichTextWidget
	{
		// Token: 0x1700005A RID: 90
		// (get) Token: 0x060000DD RID: 221 RVA: 0x000059A9 File Offset: 0x00003BA9
		// (set) Token: 0x060000DE RID: 222 RVA: 0x000059B1 File Offset: 0x00003BB1
		public string ActualText { get; private set; } = string.Empty;

		// Token: 0x060000DF RID: 223 RVA: 0x000059BC File Offset: 0x00003BBC
		public ScrollingRichTextWidget(UIContext context)
			: base(context)
		{
			this.ScrollOnHoverWidget = this;
			this.DefaultTextHorizontalAlignment = base.Brush.TextHorizontalAlignment;
			base.ClipContents = true;
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x00005A18 File Offset: 0x00003C18
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (base.Size != this._currentSize)
			{
				this._currentSize = base.Size;
				this.UpdateScrollable();
			}
			if (this._shouldScroll)
			{
				this._scrollTimeElapsed += dt;
				if (this._scrollTimeElapsed < this.InbetweenScrollDuration)
				{
					this._currentScrollAmount = 0f;
				}
				else if (this._scrollTimeElapsed >= this.InbetweenScrollDuration && this._currentScrollAmount < this._totalScrollAmount)
				{
					this._currentScrollAmount += dt * this.ScrollPerTick;
				}
				else if (this._currentScrollAmount >= this._totalScrollAmount)
				{
					if (this._scrollTimeNeeded.ApproximatelyEqualsTo(0f, 1E-05f))
					{
						this._scrollTimeNeeded = this._scrollTimeElapsed;
					}
					if (this._scrollTimeElapsed < this._scrollTimeNeeded + this.InbetweenScrollDuration)
					{
						this._currentScrollAmount = this._totalScrollAmount;
					}
					else
					{
						this._scrollTimeNeeded = 0f;
						this._scrollTimeElapsed = 0f;
					}
				}
			}
			if (base.EventManager.HoveredView == this.ScrollOnHoverWidget && !this._isHovering)
			{
				this._isHovering = true;
				if (!this.IsAutoScrolling)
				{
					base.Text = this.ActualText;
					this.UpdateWordWidth();
					this._shouldScroll = this._wordWidth > this.GetMaximumAllowedWidth();
				}
			}
			else if (base.EventManager.HoveredView != this.ScrollOnHoverWidget && this._isHovering)
			{
				if (!this.IsAutoScrolling)
				{
					this.ResetScroll();
				}
				this._isHovering = false;
				this.UpdateScrollable();
			}
			this._renderOffset.x = -this._currentScrollAmount;
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x00005BC9 File Offset: 0x00003DC9
		public override void OnBrushChanged()
		{
			base.OnBrushChanged();
			this.DefaultTextHorizontalAlignment = base.Brush.TextHorizontalAlignment;
			this.UpdateScrollable();
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x00005BE8 File Offset: 0x00003DE8
		protected override void SetText(string value)
		{
			base.SetText(value);
			this._richText.SkipLineOnContainerExceeded = false;
			this.ActualText = this._richText.Value;
			this._currentSize = Vec2.Zero;
			this.ResetScroll();
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00005C20 File Offset: 0x00003E20
		private void UpdateScrollable()
		{
			this.UpdateWordWidth();
			if (this._wordWidth > this.GetMaximumAllowedWidth())
			{
				this._shouldScroll = this.IsAutoScrolling;
				this._totalScrollAmount = this._wordWidth - this.GetMaximumAllowedWidth();
				base.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Left;
				if (!this.IsAutoScrolling && !this._isHovering)
				{
					bool flag = false;
					for (int i = this.ActualText.Length; i > 3; i--)
					{
						if (this.ActualText[i - 1] == '>')
						{
							flag = true;
						}
						else if (this.ActualText[i - 1] == '<')
						{
							flag = false;
						}
						if (!flag && this.GetWordWidth(this.ActualText.Substring(0, i - 3) + "...", 0.25f) * base._scaleToUse <= this.GetMaximumAllowedWidth())
						{
							this._richText.Value = this.ActualText.Substring(0, i - 3) + "...";
							return;
						}
					}
					return;
				}
			}
			else
			{
				this.ResetScroll();
			}
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00005D2F File Offset: 0x00003F2F
		private float GetMaximumAllowedWidth()
		{
			if (base.WidthSizePolicy != SizePolicy.CoverChildren)
			{
				return base.Size.X;
			}
			if (base.ScaledMaxWidth == 0f)
			{
				return 2.1474836E+09f;
			}
			return base.ScaledMaxWidth;
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x00005D60 File Offset: 0x00003F60
		private void UpdateWordWidth()
		{
			float padding = 0.5f;
			if (base.WidthSizePolicy == SizePolicy.CoverChildren)
			{
				padding = 0f;
			}
			this._wordWidth = this.GetWordWidth(this._richText.Value, padding) * base._scaleToUse;
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00005DA4 File Offset: 0x00003FA4
		private float GetWordWidth(string word, float padding)
		{
			float num = padding * 2f;
			for (int i = 0; i < word.Length; i++)
			{
				num += this.GetCharacterWidth(word[i]);
			}
			return num;
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00005DDC File Offset: 0x00003FDC
		private float GetCharacterWidth(char character)
		{
			FontFactory fontFactory = base.Context.FontFactory;
			Brush brush = base.Brush;
			string englishFontName;
			if (brush == null)
			{
				englishFontName = null;
			}
			else
			{
				Font font = brush.Font;
				englishFontName = ((font != null) ? font.Name : null);
			}
			Font mappedFontForLocalization = fontFactory.GetMappedFontForLocalization(englishFontName);
			float result;
			if (!mappedFontForLocalization.Characters.ContainsKey((int)character))
			{
				Font font2 = base.Context.FontFactory.GetUsableFontForCharacter((int)character) ?? mappedFontForLocalization;
				float num = (float)base.Brush.FontSize / (float)font2.Size;
				result = font2.GetCharacterWidth(character, 0.5f) * num;
			}
			else
			{
				float num = (float)base.Brush.FontSize / (float)mappedFontForLocalization.Size;
				result = mappedFontForLocalization.GetCharacterWidth(character, 0.5f) * num;
			}
			return result;
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00005E88 File Offset: 0x00004088
		private void ResetScroll()
		{
			this._shouldScroll = false;
			this._scrollTimeElapsed = 0f;
			this._currentScrollAmount = 0f;
			base.Brush.TextHorizontalAlignment = this.DefaultTextHorizontalAlignment;
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x060000E9 RID: 233 RVA: 0x00005EB8 File Offset: 0x000040B8
		// (set) Token: 0x060000EA RID: 234 RVA: 0x00005EC0 File Offset: 0x000040C0
		[Editor(false)]
		public Widget ScrollOnHoverWidget
		{
			get
			{
				return this._scrollOnHoverWidget;
			}
			set
			{
				if (value != this._scrollOnHoverWidget)
				{
					this._scrollOnHoverWidget = value;
					base.OnPropertyChanged<Widget>(value, "ScrollOnHoverWidget");
				}
			}
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x060000EB RID: 235 RVA: 0x00005EDE File Offset: 0x000040DE
		// (set) Token: 0x060000EC RID: 236 RVA: 0x00005EE6 File Offset: 0x000040E6
		[Editor(false)]
		public bool IsAutoScrolling
		{
			get
			{
				return this._isAutoScrolling;
			}
			set
			{
				if (value != this._isAutoScrolling)
				{
					this._isAutoScrolling = value;
					base.OnPropertyChanged(value, "IsAutoScrolling");
				}
			}
		}

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x060000ED RID: 237 RVA: 0x00005F04 File Offset: 0x00004104
		// (set) Token: 0x060000EE RID: 238 RVA: 0x00005F0C File Offset: 0x0000410C
		[Editor(false)]
		public float ScrollPerTick
		{
			get
			{
				return this._scrollPerTick;
			}
			set
			{
				if (value != this._scrollPerTick)
				{
					this._scrollPerTick = value;
					base.OnPropertyChanged(value, "ScrollPerTick");
				}
			}
		}

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x060000EF RID: 239 RVA: 0x00005F2A File Offset: 0x0000412A
		// (set) Token: 0x060000F0 RID: 240 RVA: 0x00005F32 File Offset: 0x00004132
		[Editor(false)]
		public float InbetweenScrollDuration
		{
			get
			{
				return this._inbetweenScrollDuration;
			}
			set
			{
				if (value != this._inbetweenScrollDuration)
				{
					this._inbetweenScrollDuration = value;
					base.OnPropertyChanged(value, "InbetweenScrollDuration");
				}
			}
		}

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x060000F1 RID: 241 RVA: 0x00005F50 File Offset: 0x00004150
		// (set) Token: 0x060000F2 RID: 242 RVA: 0x00005F58 File Offset: 0x00004158
		[Editor(false)]
		public TextHorizontalAlignment DefaultTextHorizontalAlignment
		{
			get
			{
				return this._defaultTextHorizontalAlignment;
			}
			set
			{
				if (value != this._defaultTextHorizontalAlignment)
				{
					this._defaultTextHorizontalAlignment = value;
					switch (value)
					{
					case TextHorizontalAlignment.Left:
						base.OnPropertyChanged<string>("Left", "DefaultTextHorizontalAlignment");
						return;
					case TextHorizontalAlignment.Right:
						base.OnPropertyChanged<string>("Right", "DefaultTextHorizontalAlignment");
						return;
					case TextHorizontalAlignment.Center:
						base.OnPropertyChanged<string>("Center", "DefaultTextHorizontalAlignment");
						return;
					case TextHorizontalAlignment.Justify:
						base.OnPropertyChanged<string>("Justify", "DefaultTextHorizontalAlignment");
						break;
					default:
						return;
					}
				}
			}
		}

		// Token: 0x04000061 RID: 97
		private bool _shouldScroll;

		// Token: 0x04000062 RID: 98
		private float _scrollTimeNeeded;

		// Token: 0x04000063 RID: 99
		private float _scrollTimeElapsed;

		// Token: 0x04000064 RID: 100
		private float _totalScrollAmount;

		// Token: 0x04000065 RID: 101
		private float _currentScrollAmount;

		// Token: 0x04000066 RID: 102
		private Vec2 _currentSize;

		// Token: 0x04000068 RID: 104
		private bool _isHovering;

		// Token: 0x04000069 RID: 105
		private float _wordWidth;

		// Token: 0x0400006A RID: 106
		private Widget _scrollOnHoverWidget;

		// Token: 0x0400006B RID: 107
		private bool _isAutoScrolling = true;

		// Token: 0x0400006C RID: 108
		private float _scrollPerTick = 30f;

		// Token: 0x0400006D RID: 109
		private float _inbetweenScrollDuration = 1f;

		// Token: 0x0400006E RID: 110
		private TextHorizontalAlignment _defaultTextHorizontalAlignment;
	}
}
