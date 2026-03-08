using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000019 RID: 25
	public class Style : IDataSource
	{
		// Token: 0x17000068 RID: 104
		// (get) Token: 0x06000198 RID: 408 RVA: 0x000098C5 File Offset: 0x00007AC5
		// (set) Token: 0x06000199 RID: 409 RVA: 0x000098CD File Offset: 0x00007ACD
		public Style DefaultStyle { get; set; }

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x0600019A RID: 410 RVA: 0x000098D6 File Offset: 0x00007AD6
		// (set) Token: 0x0600019B RID: 411 RVA: 0x000098DE File Offset: 0x00007ADE
		[Editor(false)]
		public string Name { get; set; }

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x0600019C RID: 412 RVA: 0x000098E7 File Offset: 0x00007AE7
		private uint DefaultStyleVersion
		{
			get
			{
				if (this.DefaultStyle == null)
				{
					return 0U;
				}
				return (uint)((long)this.DefaultStyle._localVersion % (long)((ulong)(-1)));
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x0600019D RID: 413 RVA: 0x00009904 File Offset: 0x00007B04
		public long Version
		{
			get
			{
				uint num = 0U;
				for (int i = 0; i < this._layersWithIndex.Count; i++)
				{
					num += this._layersWithIndex[i].Version;
				}
				return (((long)this._localVersion << 32) | (long)((ulong)num)) + (long)((ulong)this.DefaultStyleVersion);
			}
		}

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x0600019E RID: 414 RVA: 0x00009952 File Offset: 0x00007B52
		// (set) Token: 0x0600019F RID: 415 RVA: 0x0000995A File Offset: 0x00007B5A
		[Editor(false)]
		public string AnimationToPlayOnBegin
		{
			get
			{
				return this._animationToPlayOnBegin;
			}
			set
			{
				this._animationToPlayOnBegin = value;
				this.AnimationMode = StyleAnimationMode.Animation;
			}
		}

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x060001A0 RID: 416 RVA: 0x0000996A File Offset: 0x00007B6A
		public int LayerCount
		{
			get
			{
				return this._layers.Count;
			}
		}

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x060001A1 RID: 417 RVA: 0x00009977 File Offset: 0x00007B77
		public StyleLayer DefaultLayer
		{
			get
			{
				return this._layers["Default"];
			}
		}

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x060001A2 RID: 418 RVA: 0x00009989 File Offset: 0x00007B89
		// (set) Token: 0x060001A3 RID: 419 RVA: 0x00009991 File Offset: 0x00007B91
		[Editor(false)]
		public StyleAnimationMode AnimationMode { get; set; }

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x060001A4 RID: 420 RVA: 0x0000999A File Offset: 0x00007B9A
		// (set) Token: 0x060001A5 RID: 421 RVA: 0x000099B6 File Offset: 0x00007BB6
		[Editor(false)]
		public Color FontColor
		{
			get
			{
				if (this._isFontColorChanged)
				{
					return this._fontColor;
				}
				return this.DefaultStyle.FontColor;
			}
			set
			{
				if (this.FontColor != value)
				{
					this._isFontColorChanged = true;
					this._fontColor = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x060001A6 RID: 422 RVA: 0x000099E2 File Offset: 0x00007BE2
		// (set) Token: 0x060001A7 RID: 423 RVA: 0x000099FE File Offset: 0x00007BFE
		[Editor(false)]
		public Color TextGlowColor
		{
			get
			{
				if (this._isTextGlowColorChanged)
				{
					return this._textGlowColor;
				}
				return this.DefaultStyle.TextGlowColor;
			}
			set
			{
				if (this.TextGlowColor != value)
				{
					this._isTextGlowColorChanged = true;
					this._textGlowColor = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x060001A8 RID: 424 RVA: 0x00009A2A File Offset: 0x00007C2A
		// (set) Token: 0x060001A9 RID: 425 RVA: 0x00009A46 File Offset: 0x00007C46
		[Editor(false)]
		public Color TextOutlineColor
		{
			get
			{
				if (this._isTextOutlineColorChanged)
				{
					return this._textOutlineColor;
				}
				return this.DefaultStyle.TextOutlineColor;
			}
			set
			{
				if (this.TextOutlineColor != value)
				{
					this._isTextOutlineColorChanged = true;
					this._textOutlineColor = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x060001AA RID: 426 RVA: 0x00009A72 File Offset: 0x00007C72
		// (set) Token: 0x060001AB RID: 427 RVA: 0x00009A8E File Offset: 0x00007C8E
		[Editor(false)]
		public float TextOutlineAmount
		{
			get
			{
				if (this._isTextOutlineAmountChanged)
				{
					return this._textOutlineAmount;
				}
				return this.DefaultStyle.TextOutlineAmount;
			}
			set
			{
				if (this.TextOutlineAmount != value)
				{
					this._isTextOutlineAmountChanged = true;
					this._textOutlineAmount = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x060001AC RID: 428 RVA: 0x00009AB5 File Offset: 0x00007CB5
		// (set) Token: 0x060001AD RID: 429 RVA: 0x00009AD1 File Offset: 0x00007CD1
		[Editor(false)]
		public float TextGlowRadius
		{
			get
			{
				if (this._isTextGlowRadiusChanged)
				{
					return this._textGlowRadius;
				}
				return this.DefaultStyle.TextGlowRadius;
			}
			set
			{
				if (this.TextGlowRadius != value)
				{
					this._isTextGlowRadiusChanged = true;
					this._textGlowRadius = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x060001AE RID: 430 RVA: 0x00009AF8 File Offset: 0x00007CF8
		// (set) Token: 0x060001AF RID: 431 RVA: 0x00009B14 File Offset: 0x00007D14
		[Editor(false)]
		public float TextBlur
		{
			get
			{
				if (this._isTextBlurChanged)
				{
					return this._textBlur;
				}
				return this.DefaultStyle.TextBlur;
			}
			set
			{
				if (this.TextBlur != value)
				{
					this._isTextBlurChanged = true;
					this._textBlur = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x060001B0 RID: 432 RVA: 0x00009B3B File Offset: 0x00007D3B
		// (set) Token: 0x060001B1 RID: 433 RVA: 0x00009B57 File Offset: 0x00007D57
		[Editor(false)]
		public float TextShadowOffset
		{
			get
			{
				if (this._isTextShadowOffsetChanged)
				{
					return this._textShadowOffset;
				}
				return this.DefaultStyle.TextShadowOffset;
			}
			set
			{
				if (this.TextShadowOffset != value)
				{
					this._isTextShadowOffsetChanged = true;
					this._textShadowOffset = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x060001B2 RID: 434 RVA: 0x00009B7E File Offset: 0x00007D7E
		// (set) Token: 0x060001B3 RID: 435 RVA: 0x00009B9A File Offset: 0x00007D9A
		[Editor(false)]
		public float TextShadowAngle
		{
			get
			{
				if (this._isTextShadowAngleChanged)
				{
					return this._textShadowAngle;
				}
				return this.DefaultStyle.TextShadowAngle;
			}
			set
			{
				if (this.TextShadowAngle != value)
				{
					this._isTextShadowAngleChanged = true;
					this._textShadowAngle = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x060001B4 RID: 436 RVA: 0x00009BC1 File Offset: 0x00007DC1
		// (set) Token: 0x060001B5 RID: 437 RVA: 0x00009BDD File Offset: 0x00007DDD
		[Editor(false)]
		public float TextColorFactor
		{
			get
			{
				if (this._isTextColorFactorChanged)
				{
					return this._textColorFactor;
				}
				return this.DefaultStyle.TextColorFactor;
			}
			set
			{
				if (this.TextColorFactor != value)
				{
					this._isTextColorFactorChanged = true;
					this._textColorFactor = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x060001B6 RID: 438 RVA: 0x00009C04 File Offset: 0x00007E04
		// (set) Token: 0x060001B7 RID: 439 RVA: 0x00009C20 File Offset: 0x00007E20
		[Editor(false)]
		public float TextAlphaFactor
		{
			get
			{
				if (this._isTextAlphaFactorChanged)
				{
					return this._textAlphaFactor;
				}
				return this.DefaultStyle.TextAlphaFactor;
			}
			set
			{
				if (this.TextAlphaFactor != value)
				{
					this._isTextAlphaFactorChanged = true;
					this._textAlphaFactor = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x060001B8 RID: 440 RVA: 0x00009C47 File Offset: 0x00007E47
		// (set) Token: 0x060001B9 RID: 441 RVA: 0x00009C63 File Offset: 0x00007E63
		[Editor(false)]
		public float TextHueFactor
		{
			get
			{
				if (this._isTextHueFactorChanged)
				{
					return this._textHueFactor;
				}
				return this.DefaultStyle.TextHueFactor;
			}
			set
			{
				if (this.TextHueFactor != value)
				{
					this._isTextHueFactorChanged = true;
					this._textHueFactor = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x060001BA RID: 442 RVA: 0x00009C8A File Offset: 0x00007E8A
		// (set) Token: 0x060001BB RID: 443 RVA: 0x00009CA6 File Offset: 0x00007EA6
		[Editor(false)]
		public float TextSaturationFactor
		{
			get
			{
				if (this._isTextSaturationFactorChanged)
				{
					return this._textSaturationFactor;
				}
				return this.DefaultStyle.TextSaturationFactor;
			}
			set
			{
				if (this.TextSaturationFactor != value)
				{
					this._isTextSaturationFactorChanged = true;
					this._textSaturationFactor = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x060001BC RID: 444 RVA: 0x00009CCD File Offset: 0x00007ECD
		// (set) Token: 0x060001BD RID: 445 RVA: 0x00009CE9 File Offset: 0x00007EE9
		[Editor(false)]
		public float TextValueFactor
		{
			get
			{
				if (this._isTextValueFactorChanged)
				{
					return this._textValueFactor;
				}
				return this.DefaultStyle.TextValueFactor;
			}
			set
			{
				if (this.TextValueFactor != value)
				{
					this._isTextValueFactorChanged = true;
					this._textValueFactor = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x060001BE RID: 446 RVA: 0x00009D10 File Offset: 0x00007F10
		// (set) Token: 0x060001BF RID: 447 RVA: 0x00009D2C File Offset: 0x00007F2C
		[Editor(false)]
		public Font Font
		{
			get
			{
				if (this._isFontChanged)
				{
					return this._font;
				}
				return this.DefaultStyle.Font;
			}
			set
			{
				if (this.Font != value)
				{
					this._isFontChanged = true;
					this._font = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x060001C0 RID: 448 RVA: 0x00009D53 File Offset: 0x00007F53
		// (set) Token: 0x060001C1 RID: 449 RVA: 0x00009D6F File Offset: 0x00007F6F
		[Editor(false)]
		public FontStyle FontStyle
		{
			get
			{
				if (this._isFontStyleChanged)
				{
					return this._fontStyle;
				}
				return this.DefaultStyle.FontStyle;
			}
			set
			{
				if (this.FontStyle != value)
				{
					this._isFontStyleChanged = true;
					this._fontStyle = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x060001C2 RID: 450 RVA: 0x00009D96 File Offset: 0x00007F96
		// (set) Token: 0x060001C3 RID: 451 RVA: 0x00009DB2 File Offset: 0x00007FB2
		[Editor(false)]
		public int FontSize
		{
			get
			{
				if (this._isFontSizeChanged)
				{
					return this._fontSize;
				}
				return this.DefaultStyle.FontSize;
			}
			set
			{
				if (this.FontSize != value)
				{
					this._isFontSizeChanged = true;
					this._fontSize = value;
					this._localVersion++;
				}
			}
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x00009DDC File Offset: 0x00007FDC
		public Style(IEnumerable<BrushLayer> layers)
		{
			this.AnimationMode = StyleAnimationMode.BasicTransition;
			this._layers = new Dictionary<string, StyleLayer>();
			this._layersWithIndex = new MBList<StyleLayer>();
			this._fontColor = new Color(0f, 0f, 0f, 1f);
			this._textGlowColor = new Color(0f, 0f, 0f, 1f);
			this._textOutlineColor = new Color(0f, 0f, 0f, 1f);
			this._textOutlineAmount = 0f;
			this._textGlowRadius = 0.2f;
			this._textBlur = 0.8f;
			this._textShadowOffset = 0.5f;
			this._textShadowAngle = 45f;
			this._textColorFactor = 1f;
			this._textAlphaFactor = 1f;
			this._textHueFactor = 0f;
			this._textSaturationFactor = 0f;
			this._textValueFactor = 0f;
			this._fontSize = 30;
			foreach (BrushLayer brushLayer in layers)
			{
				StyleLayer layer = new StyleLayer(brushLayer);
				this.AddLayer(layer);
			}
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x00009F20 File Offset: 0x00008120
		public void FillFrom(Style style)
		{
			this.Name = style.Name;
			this.FontColor = style.FontColor;
			this.TextGlowColor = style.TextGlowColor;
			this.TextOutlineColor = style.TextOutlineColor;
			this.TextOutlineAmount = style.TextOutlineAmount;
			this.TextGlowRadius = style.TextGlowRadius;
			this.TextBlur = style.TextBlur;
			this.TextShadowOffset = style.TextShadowOffset;
			this.TextShadowAngle = style.TextShadowAngle;
			this.TextColorFactor = style.TextColorFactor;
			this.TextAlphaFactor = style.TextAlphaFactor;
			this.TextHueFactor = style.TextHueFactor;
			this.TextSaturationFactor = style.TextSaturationFactor;
			this.TextValueFactor = style.TextValueFactor;
			this.Font = style.Font;
			this.FontStyle = style.FontStyle;
			this.FontSize = style.FontSize;
			this.AnimationToPlayOnBegin = style.AnimationToPlayOnBegin;
			this.AnimationMode = style.AnimationMode;
			foreach (StyleLayer styleLayer in style._layers.Values)
			{
				this._layers[styleLayer.Name].FillFrom(styleLayer);
			}
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x0000A06C File Offset: 0x0000826C
		public void AddLayer(StyleLayer layer)
		{
			this._layers.Add(layer.Name, layer);
			this._layersWithIndex.Add(layer);
			this._localVersion++;
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x0000A09A File Offset: 0x0000829A
		public void RemoveLayer(string layerName)
		{
			this._layersWithIndex.Remove(this._layers[layerName]);
			this._layers.Remove(layerName);
			this._localVersion++;
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x0000A0CF File Offset: 0x000082CF
		public StyleLayer GetLayer(int index)
		{
			return this._layersWithIndex[index];
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x0000A0DD File Offset: 0x000082DD
		public StyleLayer GetLayer(string name)
		{
			if (this._layers.ContainsKey(name))
			{
				return this._layers[name];
			}
			return null;
		}

		// Token: 0x060001CA RID: 458 RVA: 0x0000A0FB File Offset: 0x000082FB
		public StyleLayer[] GetLayers()
		{
			return this._layersWithIndex.ToArray();
		}

		// Token: 0x060001CB RID: 459 RVA: 0x0000A108 File Offset: 0x00008308
		public TextMaterial CreateTextMaterial(TwoDimensionDrawContext drawContext)
		{
			TextMaterial textMaterial = drawContext.CreateTextMaterial();
			textMaterial.Color = this.FontColor;
			textMaterial.GlowColor = this.TextGlowColor;
			textMaterial.OutlineColor = this.TextOutlineColor;
			textMaterial.OutlineAmount = this.TextOutlineAmount;
			textMaterial.GlowRadius = this.TextGlowRadius;
			textMaterial.Blur = this.TextBlur;
			textMaterial.ShadowOffset = this.TextShadowOffset;
			textMaterial.ShadowAngle = this.TextShadowAngle;
			textMaterial.ColorFactor = this.TextColorFactor;
			textMaterial.AlphaFactor = this.TextAlphaFactor;
			textMaterial.HueFactor = this.TextHueFactor;
			textMaterial.SaturationFactor = this.TextSaturationFactor;
			textMaterial.ValueFactor = this.TextValueFactor;
			return textMaterial;
		}

		// Token: 0x060001CC RID: 460 RVA: 0x0000A1B8 File Offset: 0x000083B8
		public float GetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
		{
			switch (propertyType)
			{
			case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineAmount:
				return this.TextOutlineAmount;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowRadius:
				return this.TextGlowRadius;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextBlur:
				return this.TextBlur;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowOffset:
				return this.TextShadowOffset;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowAngle:
				return this.TextShadowAngle;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextColorFactor:
				return this.TextColorFactor;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextAlphaFactor:
				return this.TextAlphaFactor;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextHueFactor:
				return this.TextHueFactor;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextSaturationFactor:
				return this.TextSaturationFactor;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextValueFactor:
				return this.TextValueFactor;
			default:
				Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\Style.cs", "GetValueAsFloat", 615);
				return 0f;
			}
		}

		// Token: 0x060001CD RID: 461 RVA: 0x0000A25C File Offset: 0x0000845C
		public Color GetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
		{
			switch (propertyType)
			{
			case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
				return this.FontColor;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
				return this.TextGlowColor;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
				return this.TextOutlineColor;
			}
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\Style.cs", "GetValueAsColor", 635);
			return Color.Black;
		}

		// Token: 0x060001CE RID: 462 RVA: 0x0000A2BA File Offset: 0x000084BA
		public Sprite GetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
		{
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\Style.cs", "GetValueAsSprite", 643);
			return null;
		}

		// Token: 0x060001CF RID: 463 RVA: 0x0000A2D8 File Offset: 0x000084D8
		public void SetAsDefaultStyle()
		{
			this._isFontColorChanged = true;
			this._isTextGlowColorChanged = true;
			this._isTextOutlineColorChanged = true;
			this._isTextOutlineAmountChanged = true;
			this._isTextGlowRadiusChanged = true;
			this._isTextBlurChanged = true;
			this._isTextShadowOffsetChanged = true;
			this._isTextShadowAngleChanged = true;
			this._isTextColorFactorChanged = true;
			this._isTextAlphaFactorChanged = true;
			this._isTextHueFactorChanged = true;
			this._isTextSaturationFactorChanged = true;
			this._isTextValueFactorChanged = true;
			this._isFontChanged = true;
			this._isFontStyleChanged = true;
			this._isFontSizeChanged = true;
			this.DefaultStyle = null;
		}

		// Token: 0x040000A8 RID: 168
		private int _localVersion;

		// Token: 0x040000A9 RID: 169
		private bool _isFontColorChanged;

		// Token: 0x040000AA RID: 170
		private bool _isTextGlowColorChanged;

		// Token: 0x040000AB RID: 171
		private bool _isTextOutlineColorChanged;

		// Token: 0x040000AC RID: 172
		private bool _isTextOutlineAmountChanged;

		// Token: 0x040000AD RID: 173
		private bool _isTextGlowRadiusChanged;

		// Token: 0x040000AE RID: 174
		private bool _isTextBlurChanged;

		// Token: 0x040000AF RID: 175
		private bool _isTextShadowOffsetChanged;

		// Token: 0x040000B0 RID: 176
		private bool _isTextShadowAngleChanged;

		// Token: 0x040000B1 RID: 177
		private bool _isTextColorFactorChanged;

		// Token: 0x040000B2 RID: 178
		private bool _isTextAlphaFactorChanged;

		// Token: 0x040000B3 RID: 179
		private bool _isTextHueFactorChanged;

		// Token: 0x040000B4 RID: 180
		private bool _isTextSaturationFactorChanged;

		// Token: 0x040000B5 RID: 181
		private bool _isTextValueFactorChanged;

		// Token: 0x040000B6 RID: 182
		private bool _isFontChanged;

		// Token: 0x040000B7 RID: 183
		private bool _isFontStyleChanged;

		// Token: 0x040000B8 RID: 184
		private bool _isFontSizeChanged;

		// Token: 0x040000B9 RID: 185
		private Color _fontColor;

		// Token: 0x040000BA RID: 186
		private Color _textGlowColor;

		// Token: 0x040000BB RID: 187
		private Color _textOutlineColor;

		// Token: 0x040000BC RID: 188
		private float _textOutlineAmount;

		// Token: 0x040000BD RID: 189
		private float _textGlowRadius;

		// Token: 0x040000BE RID: 190
		private float _textBlur;

		// Token: 0x040000BF RID: 191
		private float _textShadowOffset;

		// Token: 0x040000C0 RID: 192
		private float _textShadowAngle;

		// Token: 0x040000C1 RID: 193
		private float _textColorFactor;

		// Token: 0x040000C2 RID: 194
		private float _textAlphaFactor;

		// Token: 0x040000C3 RID: 195
		private float _textHueFactor;

		// Token: 0x040000C4 RID: 196
		private float _textSaturationFactor;

		// Token: 0x040000C5 RID: 197
		private float _textValueFactor;

		// Token: 0x040000C6 RID: 198
		private Font _font;

		// Token: 0x040000C7 RID: 199
		private FontStyle _fontStyle;

		// Token: 0x040000C8 RID: 200
		private int _fontSize;

		// Token: 0x040000C9 RID: 201
		private string _animationToPlayOnBegin;

		// Token: 0x040000CA RID: 202
		private Dictionary<string, StyleLayer> _layers;

		// Token: 0x040000CB RID: 203
		private MBList<StyleLayer> _layersWithIndex;
	}
}
