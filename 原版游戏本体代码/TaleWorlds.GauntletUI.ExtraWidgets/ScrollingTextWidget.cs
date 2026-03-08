using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000010 RID: 16
	public class ScrollingTextWidget : TextWidget
	{
		// Token: 0x17000060 RID: 96
		// (get) Token: 0x060000F3 RID: 243 RVA: 0x00005FCF File Offset: 0x000041CF
		// (set) Token: 0x060000F4 RID: 244 RVA: 0x00005FD7 File Offset: 0x000041D7
		public string ActualText { get; private set; } = string.Empty;

		// Token: 0x060000F5 RID: 245 RVA: 0x00005FE0 File Offset: 0x000041E0
		public ScrollingTextWidget(UIContext context)
			: base(context)
		{
			this.ScrollOnHoverWidget = this;
			this.DefaultTextHorizontalAlignment = base.Brush.TextHorizontalAlignment;
			base.ClipHorizontalContent = true;
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x0000603C File Offset: 0x0000423C
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
				if (!this.IsAutoScrolling)
				{
					this._text.Value = this.ActualText;
					this.UpdateWordWidth();
					this._shouldScroll = this._wordWidth > this.GetMaximumAllowedWidth();
				}
				this._isHovering = true;
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

		// Token: 0x060000F7 RID: 247 RVA: 0x000061F2 File Offset: 0x000043F2
		public override void OnBrushChanged()
		{
			this.DefaultTextHorizontalAlignment = base.Brush.TextHorizontalAlignment;
			this.UpdateScrollable();
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x0000620C File Offset: 0x0000440C
		protected override void SetText(string value)
		{
			base.SetText(value);
			this._text.SkipLineOnContainerExceeded = false;
			this._text.ResizeTextOnOverflow = false;
			this.ActualText = this._text.Value;
			this._currentSize = Vec2.Zero;
			this.ResetScroll();
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000625C File Offset: 0x0000445C
		private void UpdateScrollable()
		{
			this.UpdateWordWidth();
			if (this._wordWidth > this.GetMaximumAllowedWidth())
			{
				this._shouldScroll = this.IsAutoScrolling;
				this._totalScrollAmount = this._wordWidth - this.GetMaximumAllowedWidth();
				base.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Left;
				if (!this.IsAutoScrolling)
				{
					for (int i = this.ActualText.Length; i > 3; i--)
					{
						if (this.GetWordWidth(this.ActualText.Substring(0, i - 3) + "...", 0.25f) * base._scaleToUse <= this.GetMaximumAllowedWidth())
						{
							this._text.Value = this.ActualText.Substring(0, i - 3) + "...";
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

		// Token: 0x060000FA RID: 250 RVA: 0x00006328 File Offset: 0x00004528
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

		// Token: 0x060000FB RID: 251 RVA: 0x00006358 File Offset: 0x00004558
		private void UpdateWordWidth()
		{
			float padding = 0.5f;
			if (base.WidthSizePolicy == SizePolicy.CoverChildren)
			{
				padding = 0f;
			}
			this._wordWidth = this.GetWordWidth(this._text.Value, padding) * base._scaleToUse;
		}

		// Token: 0x060000FC RID: 252 RVA: 0x0000639C File Offset: 0x0000459C
		private float GetWordWidth(string word, float padding)
		{
			float num = padding * 2f;
			for (int i = 0; i < word.Length; i++)
			{
				num += this.GetCharacterWidth(word[i]);
			}
			return num;
		}

		// Token: 0x060000FD RID: 253 RVA: 0x000063D4 File Offset: 0x000045D4
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

		// Token: 0x060000FE RID: 254 RVA: 0x00006480 File Offset: 0x00004680
		private void ResetScroll()
		{
			this._shouldScroll = false;
			this._scrollTimeElapsed = 0f;
			this._currentScrollAmount = 0f;
			this._renderOffset.x = 0f;
			base.Brush.TextHorizontalAlignment = this.DefaultTextHorizontalAlignment;
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x060000FF RID: 255 RVA: 0x000064C0 File Offset: 0x000046C0
		// (set) Token: 0x06000100 RID: 256 RVA: 0x000064C8 File Offset: 0x000046C8
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

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x06000101 RID: 257 RVA: 0x000064E6 File Offset: 0x000046E6
		// (set) Token: 0x06000102 RID: 258 RVA: 0x000064EE File Offset: 0x000046EE
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

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x06000103 RID: 259 RVA: 0x0000650C File Offset: 0x0000470C
		// (set) Token: 0x06000104 RID: 260 RVA: 0x00006514 File Offset: 0x00004714
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

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x06000105 RID: 261 RVA: 0x00006532 File Offset: 0x00004732
		// (set) Token: 0x06000106 RID: 262 RVA: 0x0000653A File Offset: 0x0000473A
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

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x06000107 RID: 263 RVA: 0x00006558 File Offset: 0x00004758
		// (set) Token: 0x06000108 RID: 264 RVA: 0x00006560 File Offset: 0x00004760
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

		// Token: 0x0400006F RID: 111
		private bool _shouldScroll;

		// Token: 0x04000070 RID: 112
		private float _scrollTimeNeeded;

		// Token: 0x04000071 RID: 113
		private float _scrollTimeElapsed;

		// Token: 0x04000072 RID: 114
		private float _totalScrollAmount;

		// Token: 0x04000073 RID: 115
		private float _currentScrollAmount;

		// Token: 0x04000074 RID: 116
		private Vec2 _currentSize;

		// Token: 0x04000076 RID: 118
		private bool _isHovering;

		// Token: 0x04000077 RID: 119
		private float _wordWidth;

		// Token: 0x04000078 RID: 120
		private Widget _scrollOnHoverWidget;

		// Token: 0x04000079 RID: 121
		private bool _isAutoScrolling = true;

		// Token: 0x0400007A RID: 122
		private float _scrollPerTick = 30f;

		// Token: 0x0400007B RID: 123
		private float _inbetweenScrollDuration = 1f;

		// Token: 0x0400007C RID: 124
		private TextHorizontalAlignment _defaultTextHorizontalAlignment;
	}
}
