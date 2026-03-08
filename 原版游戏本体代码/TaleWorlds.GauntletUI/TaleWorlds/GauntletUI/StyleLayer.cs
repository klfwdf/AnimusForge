using System;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200001B RID: 27
	public class StyleLayer : IBrushLayerData, IDataSource
	{
		// Token: 0x17000080 RID: 128
		// (get) Token: 0x060001D0 RID: 464 RVA: 0x0000A35C File Offset: 0x0000855C
		// (set) Token: 0x060001D1 RID: 465 RVA: 0x0000A364 File Offset: 0x00008564
		public BrushLayer SourceLayer { get; private set; }

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x060001D2 RID: 466 RVA: 0x0000A36D File Offset: 0x0000856D
		public uint Version
		{
			get
			{
				return this._localVersion + this.SourceLayer.Version;
			}
		}

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x060001D3 RID: 467 RVA: 0x0000A381 File Offset: 0x00008581
		// (set) Token: 0x060001D4 RID: 468 RVA: 0x0000A38E File Offset: 0x0000858E
		[Editor(false)]
		public string Name
		{
			get
			{
				return this.SourceLayer.Name;
			}
			set
			{
			}
		}

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x060001D5 RID: 469 RVA: 0x0000A390 File Offset: 0x00008590
		// (set) Token: 0x060001D6 RID: 470 RVA: 0x0000A3AC File Offset: 0x000085AC
		[Editor(false)]
		public Sprite Sprite
		{
			get
			{
				if (this._isSpriteChanged)
				{
					return this._sprite;
				}
				return this.SourceLayer.Sprite;
			}
			set
			{
				if (this.Sprite != value)
				{
					this._isSpriteChanged = this.SourceLayer.Sprite != value;
					this._sprite = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x060001D7 RID: 471 RVA: 0x0000A3E3 File Offset: 0x000085E3
		// (set) Token: 0x060001D8 RID: 472 RVA: 0x0000A3FF File Offset: 0x000085FF
		[Editor(false)]
		public ImageFit.ImageFitTypes ImageFitType
		{
			get
			{
				if (this._isImageFitTypeChanged)
				{
					return this._imageFitType;
				}
				return this.SourceLayer.ImageFitType;
			}
			set
			{
				if (value != this.ImageFitType)
				{
					this._isImageFitTypeChanged = this.SourceLayer.ImageFitType != value;
					this._imageFitType = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x060001D9 RID: 473 RVA: 0x0000A436 File Offset: 0x00008636
		// (set) Token: 0x060001DA RID: 474 RVA: 0x0000A452 File Offset: 0x00008652
		[Editor(false)]
		public ImageFit.ImageHorizontalAlignments ImageFitHorizontalAlignment
		{
			get
			{
				if (this._isImageFitHorizontalAlignmentChanged)
				{
					return this._imageFitHorizontalAlignment;
				}
				return this.SourceLayer.ImageFitHorizontalAlignment;
			}
			set
			{
				if (value != this.ImageFitHorizontalAlignment)
				{
					this._isImageFitHorizontalAlignmentChanged = this.SourceLayer.ImageFitHorizontalAlignment != value;
					this._imageFitHorizontalAlignment = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x060001DB RID: 475 RVA: 0x0000A489 File Offset: 0x00008689
		// (set) Token: 0x060001DC RID: 476 RVA: 0x0000A4A5 File Offset: 0x000086A5
		[Editor(false)]
		public ImageFit.ImageVerticalAlignments ImageFitVerticalAlignment
		{
			get
			{
				if (this._isImageFitVerticalAlignmentChanged)
				{
					return this._imageFitVerticalAlignment;
				}
				return this.SourceLayer.ImageFitVerticalAlignment;
			}
			set
			{
				if (value != this.ImageFitVerticalAlignment)
				{
					this._isImageFitVerticalAlignmentChanged = this.SourceLayer.ImageFitVerticalAlignment != value;
					this._imageFitVerticalAlignment = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x060001DD RID: 477 RVA: 0x0000A4DC File Offset: 0x000086DC
		// (set) Token: 0x060001DE RID: 478 RVA: 0x0000A4F8 File Offset: 0x000086F8
		[Editor(false)]
		public Color Color
		{
			get
			{
				if (this._isColorChanged)
				{
					return this._color;
				}
				return this.SourceLayer.Color;
			}
			set
			{
				if (this.Color != value)
				{
					this._isColorChanged = this.SourceLayer.Color != value;
					this._color = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x060001DF RID: 479 RVA: 0x0000A534 File Offset: 0x00008734
		// (set) Token: 0x060001E0 RID: 480 RVA: 0x0000A550 File Offset: 0x00008750
		[Editor(false)]
		public float ColorFactor
		{
			get
			{
				if (this._isColorFactorChanged)
				{
					return this._colorFactor;
				}
				return this.SourceLayer.ColorFactor;
			}
			set
			{
				if (this.ColorFactor != value)
				{
					this._isColorFactorChanged = MathF.Abs(this.SourceLayer.ColorFactor - value) > 1E-05f;
					this._colorFactor = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x060001E1 RID: 481 RVA: 0x0000A58F File Offset: 0x0000878F
		// (set) Token: 0x060001E2 RID: 482 RVA: 0x0000A5AB File Offset: 0x000087AB
		[Editor(false)]
		public float AlphaFactor
		{
			get
			{
				if (this._isAlphaFactorChanged)
				{
					return this._alphaFactor;
				}
				return this.SourceLayer.AlphaFactor;
			}
			set
			{
				if (this.AlphaFactor != value)
				{
					this._isAlphaFactorChanged = MathF.Abs(this.SourceLayer.AlphaFactor - value) > 1E-05f;
					this._alphaFactor = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x060001E3 RID: 483 RVA: 0x0000A5EA File Offset: 0x000087EA
		// (set) Token: 0x060001E4 RID: 484 RVA: 0x0000A606 File Offset: 0x00008806
		[Editor(false)]
		public float HueFactor
		{
			get
			{
				if (this._isHueFactorChanged)
				{
					return this._hueFactor;
				}
				return this.SourceLayer.HueFactor;
			}
			set
			{
				if (this.HueFactor != value)
				{
					this._isHueFactorChanged = MathF.Abs(this.SourceLayer.HueFactor - value) > 1E-05f;
					this._hueFactor = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x060001E5 RID: 485 RVA: 0x0000A645 File Offset: 0x00008845
		// (set) Token: 0x060001E6 RID: 486 RVA: 0x0000A661 File Offset: 0x00008861
		[Editor(false)]
		public float SaturationFactor
		{
			get
			{
				if (this._isSaturationFactorChanged)
				{
					return this._saturationFactor;
				}
				return this.SourceLayer.SaturationFactor;
			}
			set
			{
				if (this.SaturationFactor != value)
				{
					this._isSaturationFactorChanged = MathF.Abs(this.SourceLayer.SaturationFactor - value) > 1E-05f;
					this._saturationFactor = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x060001E7 RID: 487 RVA: 0x0000A6A0 File Offset: 0x000088A0
		// (set) Token: 0x060001E8 RID: 488 RVA: 0x0000A6BC File Offset: 0x000088BC
		[Editor(false)]
		public float ValueFactor
		{
			get
			{
				if (this._isValueFactorChanged)
				{
					return this._valueFactor;
				}
				return this.SourceLayer.ValueFactor;
			}
			set
			{
				if (this.ValueFactor != value)
				{
					this._isValueFactorChanged = MathF.Abs(this.SourceLayer.ValueFactor - value) > 1E-05f;
					this._valueFactor = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x060001E9 RID: 489 RVA: 0x0000A6FB File Offset: 0x000088FB
		// (set) Token: 0x060001EA RID: 490 RVA: 0x0000A717 File Offset: 0x00008917
		[Editor(false)]
		public bool IsHidden
		{
			get
			{
				if (this._isIsHiddenChanged)
				{
					return this._isHidden;
				}
				return this.SourceLayer.IsHidden;
			}
			set
			{
				if (this.IsHidden != value)
				{
					this._isIsHiddenChanged = this.SourceLayer.IsHidden != value;
					this._isHidden = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x060001EB RID: 491 RVA: 0x0000A74E File Offset: 0x0000894E
		// (set) Token: 0x060001EC RID: 492 RVA: 0x0000A76A File Offset: 0x0000896A
		[Editor(false)]
		public bool UseOverlayAlphaAsMask
		{
			get
			{
				if (this._isUseOverlayAlphaAsMaskChanged)
				{
					return this._useOverlayAlphaAsMask;
				}
				return this.SourceLayer.UseOverlayAlphaAsMask;
			}
			set
			{
				if (this.UseOverlayAlphaAsMask != value)
				{
					this._isUseOverlayAlphaAsMaskChanged = this.SourceLayer.UseOverlayAlphaAsMask != value;
					this._useOverlayAlphaAsMask = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x060001ED RID: 493 RVA: 0x0000A7A1 File Offset: 0x000089A1
		// (set) Token: 0x060001EE RID: 494 RVA: 0x0000A7BD File Offset: 0x000089BD
		[Editor(false)]
		public float XOffset
		{
			get
			{
				if (this._isXOffsetChanged)
				{
					return this._xOffset;
				}
				return this.SourceLayer.XOffset;
			}
			set
			{
				if (this.XOffset != value)
				{
					this._isXOffsetChanged = MathF.Abs(this.SourceLayer.XOffset - value) > 1E-05f;
					this._xOffset = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x060001EF RID: 495 RVA: 0x0000A7FC File Offset: 0x000089FC
		// (set) Token: 0x060001F0 RID: 496 RVA: 0x0000A818 File Offset: 0x00008A18
		[Editor(false)]
		public float YOffset
		{
			get
			{
				if (this._isYOffsetChanged)
				{
					return this._yOffset;
				}
				return this.SourceLayer.YOffset;
			}
			set
			{
				if (this.YOffset != value)
				{
					this._isYOffsetChanged = MathF.Abs(this.SourceLayer.YOffset - value) > 1E-05f;
					this._yOffset = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x060001F1 RID: 497 RVA: 0x0000A857 File Offset: 0x00008A57
		// (set) Token: 0x060001F2 RID: 498 RVA: 0x0000A873 File Offset: 0x00008A73
		[Editor(false)]
		public float Rotation
		{
			get
			{
				if (this._isRotationChanged)
				{
					return this._rotation;
				}
				return this.SourceLayer.Rotation;
			}
			set
			{
				if (this.Rotation != value)
				{
					this._isRotationChanged = MathF.Abs(this.SourceLayer.Rotation - value) > 1E-05f;
					this._rotation = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x060001F3 RID: 499 RVA: 0x0000A8B2 File Offset: 0x00008AB2
		// (set) Token: 0x060001F4 RID: 500 RVA: 0x0000A8CE File Offset: 0x00008ACE
		[Editor(false)]
		public float ExtendLeft
		{
			get
			{
				if (this._isExtendLeftChanged)
				{
					return this._extendLeft;
				}
				return this.SourceLayer.ExtendLeft;
			}
			set
			{
				if (this.ExtendLeft != value)
				{
					this._isExtendLeftChanged = MathF.Abs(this.SourceLayer.ExtendLeft - value) > 1E-05f;
					this._extendLeft = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x060001F5 RID: 501 RVA: 0x0000A90D File Offset: 0x00008B0D
		// (set) Token: 0x060001F6 RID: 502 RVA: 0x0000A929 File Offset: 0x00008B29
		[Editor(false)]
		public float ExtendRight
		{
			get
			{
				if (this._isExtendRightChanged)
				{
					return this._extendRight;
				}
				return this.SourceLayer.ExtendRight;
			}
			set
			{
				if (this.ExtendRight != value)
				{
					this._isExtendRightChanged = MathF.Abs(this.SourceLayer.ExtendRight - value) > 1E-05f;
					this._extendRight = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x060001F7 RID: 503 RVA: 0x0000A968 File Offset: 0x00008B68
		// (set) Token: 0x060001F8 RID: 504 RVA: 0x0000A984 File Offset: 0x00008B84
		[Editor(false)]
		public float ExtendTop
		{
			get
			{
				if (this._isExtendTopChanged)
				{
					return this._extendTop;
				}
				return this.SourceLayer.ExtendTop;
			}
			set
			{
				if (this.ExtendTop != value)
				{
					this._isExtendTopChanged = MathF.Abs(this.SourceLayer.ExtendTop - value) > 1E-05f;
					this._extendTop = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x060001F9 RID: 505 RVA: 0x0000A9C3 File Offset: 0x00008BC3
		// (set) Token: 0x060001FA RID: 506 RVA: 0x0000A9DF File Offset: 0x00008BDF
		[Editor(false)]
		public float ExtendBottom
		{
			get
			{
				if (this._isExtendBottomChanged)
				{
					return this._extendBottom;
				}
				return this.SourceLayer.ExtendBottom;
			}
			set
			{
				if (this.ExtendBottom != value)
				{
					this._isExtendBottomChanged = MathF.Abs(this.SourceLayer.ExtendBottom - value) > 1E-05f;
					this._extendBottom = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x060001FB RID: 507 RVA: 0x0000AA1E File Offset: 0x00008C1E
		// (set) Token: 0x060001FC RID: 508 RVA: 0x0000AA3A File Offset: 0x00008C3A
		[Editor(false)]
		public float OverridenWidth
		{
			get
			{
				if (this._isOverridenWidthChanged)
				{
					return this._overridenWidth;
				}
				return this.SourceLayer.OverridenWidth;
			}
			set
			{
				if (this.OverridenWidth != value)
				{
					this._isOverridenWidthChanged = MathF.Abs(this.SourceLayer.OverridenWidth - value) > 1E-05f;
					this._overridenWidth = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x060001FD RID: 509 RVA: 0x0000AA79 File Offset: 0x00008C79
		// (set) Token: 0x060001FE RID: 510 RVA: 0x0000AA95 File Offset: 0x00008C95
		[Editor(false)]
		public float OverridenHeight
		{
			get
			{
				if (this._isOverridenHeightChanged)
				{
					return this._overridenHeight;
				}
				return this.SourceLayer.OverridenHeight;
			}
			set
			{
				if (this.OverridenHeight != value)
				{
					this._isOverridenHeightChanged = MathF.Abs(this.SourceLayer.OverridenHeight - value) > 1E-05f;
					this._overridenHeight = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x060001FF RID: 511 RVA: 0x0000AAD4 File Offset: 0x00008CD4
		// (set) Token: 0x06000200 RID: 512 RVA: 0x0000AAF0 File Offset: 0x00008CF0
		[Editor(false)]
		public BrushLayerSizePolicy WidthPolicy
		{
			get
			{
				if (this._isWidthPolicyChanged)
				{
					return this._widthPolicy;
				}
				return this.SourceLayer.WidthPolicy;
			}
			set
			{
				if (this.WidthPolicy != value)
				{
					this._isWidthPolicyChanged = this.SourceLayer.WidthPolicy != value;
					this._widthPolicy = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x06000201 RID: 513 RVA: 0x0000AB27 File Offset: 0x00008D27
		// (set) Token: 0x06000202 RID: 514 RVA: 0x0000AB43 File Offset: 0x00008D43
		public BrushLayerSizePolicy HeightPolicy
		{
			get
			{
				if (this._isHeightPolicyChanged)
				{
					return this._heightPolicy;
				}
				return this.SourceLayer.HeightPolicy;
			}
			set
			{
				if (this.HeightPolicy != value)
				{
					this._isHeightPolicyChanged = this.SourceLayer.HeightPolicy != value;
					this._heightPolicy = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x06000203 RID: 515 RVA: 0x0000AB7A File Offset: 0x00008D7A
		// (set) Token: 0x06000204 RID: 516 RVA: 0x0000AB96 File Offset: 0x00008D96
		[Editor(false)]
		public bool HorizontalFlip
		{
			get
			{
				if (this._isHorizontalFlipChanged)
				{
					return this._horizontalFlip;
				}
				return this.SourceLayer.HorizontalFlip;
			}
			set
			{
				if (this.HorizontalFlip != value)
				{
					this._isHorizontalFlipChanged = this.SourceLayer.HorizontalFlip != value;
					this._horizontalFlip = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x06000205 RID: 517 RVA: 0x0000ABCD File Offset: 0x00008DCD
		// (set) Token: 0x06000206 RID: 518 RVA: 0x0000ABE9 File Offset: 0x00008DE9
		[Editor(false)]
		public bool VerticalFlip
		{
			get
			{
				if (this._isVerticalFlipChanged)
				{
					return this._verticalFlip;
				}
				return this.SourceLayer.VerticalFlip;
			}
			set
			{
				if (this.VerticalFlip != value)
				{
					this._isVerticalFlipChanged = this.SourceLayer.VerticalFlip != value;
					this._verticalFlip = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x06000207 RID: 519 RVA: 0x0000AC20 File Offset: 0x00008E20
		// (set) Token: 0x06000208 RID: 520 RVA: 0x0000AC3C File Offset: 0x00008E3C
		[Editor(false)]
		public BrushOverlayMethod OverlayMethod
		{
			get
			{
				if (this._isOverlayMethodChanged)
				{
					return this._overlayMethod;
				}
				return this.SourceLayer.OverlayMethod;
			}
			set
			{
				if (this.OverlayMethod != value)
				{
					this._isOverlayMethodChanged = this.SourceLayer.OverlayMethod != value;
					this._overlayMethod = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700009D RID: 157
		// (get) Token: 0x06000209 RID: 521 RVA: 0x0000AC73 File Offset: 0x00008E73
		// (set) Token: 0x0600020A RID: 522 RVA: 0x0000AC8F File Offset: 0x00008E8F
		[Editor(false)]
		public Sprite OverlaySprite
		{
			get
			{
				if (this._isOverlaySpriteChanged)
				{
					return this._overlaySprite;
				}
				return this.SourceLayer.OverlaySprite;
			}
			set
			{
				if (this.OverlaySprite != value)
				{
					this._isOverlaySpriteChanged = this.SourceLayer.OverlaySprite != value;
					this._overlaySprite = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x0600020B RID: 523 RVA: 0x0000ACC6 File Offset: 0x00008EC6
		// (set) Token: 0x0600020C RID: 524 RVA: 0x0000ACE2 File Offset: 0x00008EE2
		[Editor(false)]
		public float OverlayXOffset
		{
			get
			{
				if (this._isOverlayXOffsetChanged)
				{
					return this._overlayXOffset;
				}
				return this.SourceLayer.OverlayXOffset;
			}
			set
			{
				if (this.OverlayXOffset != value)
				{
					this._isOverlayXOffsetChanged = MathF.Abs(this.SourceLayer.OverlayXOffset - value) > 1E-05f;
					this._overlayXOffset = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x0600020D RID: 525 RVA: 0x0000AD21 File Offset: 0x00008F21
		// (set) Token: 0x0600020E RID: 526 RVA: 0x0000AD3D File Offset: 0x00008F3D
		[Editor(false)]
		public float OverlayYOffset
		{
			get
			{
				if (this._isOverlayYOffsetChanged)
				{
					return this._overlayYOffset;
				}
				return this.SourceLayer.OverlayYOffset;
			}
			set
			{
				if (this.OverlayYOffset != value)
				{
					this._isOverlayYOffsetChanged = MathF.Abs(this.SourceLayer.OverlayYOffset - value) > 1E-05f;
					this._overlayYOffset = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x0600020F RID: 527 RVA: 0x0000AD7C File Offset: 0x00008F7C
		// (set) Token: 0x06000210 RID: 528 RVA: 0x0000AD98 File Offset: 0x00008F98
		[Editor(false)]
		public bool UseRandomBaseOverlayXOffset
		{
			get
			{
				if (this._isUseRandomBaseOverlayXOffset)
				{
					return this._useRandomBaseOverlayXOffset;
				}
				return this.SourceLayer.UseRandomBaseOverlayXOffset;
			}
			set
			{
				if (this.UseRandomBaseOverlayXOffset != value)
				{
					this._isUseRandomBaseOverlayXOffset = this._useRandomBaseOverlayXOffset != value;
					this._useRandomBaseOverlayXOffset = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x06000211 RID: 529 RVA: 0x0000ADCA File Offset: 0x00008FCA
		// (set) Token: 0x06000212 RID: 530 RVA: 0x0000ADE6 File Offset: 0x00008FE6
		[Editor(false)]
		public bool UseRandomBaseOverlayYOffset
		{
			get
			{
				if (this._isUseRandomBaseOverlayYOffset)
				{
					return this._useRandomBaseOverlayYOffset;
				}
				return this.SourceLayer.UseRandomBaseOverlayYOffset;
			}
			set
			{
				if (this.UseRandomBaseOverlayYOffset != value)
				{
					this._isUseRandomBaseOverlayYOffset = this._useRandomBaseOverlayYOffset != value;
					this._useRandomBaseOverlayYOffset = value;
					this._localVersion += 1U;
				}
			}
		}

		// Token: 0x06000213 RID: 531 RVA: 0x0000AE18 File Offset: 0x00009018
		public StyleLayer(BrushLayer brushLayer)
		{
			this.SourceLayer = brushLayer;
		}

		// Token: 0x06000214 RID: 532 RVA: 0x0000AE27 File Offset: 0x00009027
		public static StyleLayer CreateFrom(StyleLayer source)
		{
			StyleLayer styleLayer = new StyleLayer(source.SourceLayer);
			styleLayer.FillFrom(source);
			return styleLayer;
		}

		// Token: 0x06000215 RID: 533 RVA: 0x0000AE3C File Offset: 0x0000903C
		public void FillFrom(StyleLayer source)
		{
			this.Sprite = source.Sprite;
			this.Color = source.Color;
			this.ColorFactor = source.ColorFactor;
			this.AlphaFactor = source.AlphaFactor;
			this.HueFactor = source.HueFactor;
			this.SaturationFactor = source.SaturationFactor;
			this.ValueFactor = source.ValueFactor;
			this.IsHidden = source.IsHidden;
			this.XOffset = source.XOffset;
			this.YOffset = source.YOffset;
			this.Rotation = source.Rotation;
			this.ExtendLeft = source.ExtendLeft;
			this.ExtendRight = source.ExtendRight;
			this.ExtendTop = source.ExtendTop;
			this.ExtendBottom = source.ExtendBottom;
			this.OverridenWidth = source.OverridenWidth;
			this.OverridenHeight = source.OverridenHeight;
			this.WidthPolicy = source.WidthPolicy;
			this.HeightPolicy = source.HeightPolicy;
			this.HorizontalFlip = source.HorizontalFlip;
			this.VerticalFlip = source.VerticalFlip;
			this.OverlayMethod = source.OverlayMethod;
			this.OverlaySprite = source.OverlaySprite;
			this.OverlayXOffset = source.OverlayXOffset;
			this.OverlayYOffset = source.OverlayYOffset;
			this.UseRandomBaseOverlayXOffset = source.UseRandomBaseOverlayXOffset;
			this.UseRandomBaseOverlayYOffset = source.UseRandomBaseOverlayYOffset;
		}

		// Token: 0x06000216 RID: 534 RVA: 0x0000AF90 File Offset: 0x00009190
		public float GetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
		{
			switch (propertyType)
			{
			case BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor:
				return this.ColorFactor;
			case BrushAnimationProperty.BrushAnimationPropertyType.Color:
			case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
				break;
			case BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor:
				return this.AlphaFactor;
			case BrushAnimationProperty.BrushAnimationPropertyType.HueFactor:
				return this.HueFactor;
			case BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor:
				return this.SaturationFactor;
			case BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor:
				return this.ValueFactor;
			case BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset:
				return this.OverlayXOffset;
			case BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset:
				return this.OverlayYOffset;
			default:
				switch (propertyType)
				{
				case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
					return this.XOffset;
				case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
					return this.YOffset;
				case BrushAnimationProperty.BrushAnimationPropertyType.Rotation:
					return this.Rotation;
				case BrushAnimationProperty.BrushAnimationPropertyType.OverridenWidth:
					return this.OverridenWidth;
				case BrushAnimationProperty.BrushAnimationPropertyType.OverridenHeight:
					return this.OverridenHeight;
				case BrushAnimationProperty.BrushAnimationPropertyType.ExtendLeft:
					return this.ExtendLeft;
				case BrushAnimationProperty.BrushAnimationPropertyType.ExtendRight:
					return this.ExtendRight;
				case BrushAnimationProperty.BrushAnimationPropertyType.ExtendTop:
					return this.ExtendTop;
				case BrushAnimationProperty.BrushAnimationPropertyType.ExtendBottom:
					return this.ExtendBottom;
				}
				break;
			}
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\StyleLayer.cs", "GetValueAsFloat", 940);
			return 0f;
		}

		// Token: 0x06000217 RID: 535 RVA: 0x0000B09E File Offset: 0x0000929E
		public Color GetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
		{
			if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Color)
			{
				return this.Color;
			}
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\StyleLayer.cs", "GetValueAsColor", 954);
			return Color.Black;
		}

		// Token: 0x06000218 RID: 536 RVA: 0x0000B0C9 File Offset: 0x000092C9
		public Sprite GetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
		{
			if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Sprite)
			{
				return this.Sprite;
			}
			if (propertyType != BrushAnimationProperty.BrushAnimationPropertyType.OverlaySprite)
			{
				Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\StyleLayer.cs", "GetValueAsSprite", 971);
				return null;
			}
			return this.OverlaySprite;
		}

		// Token: 0x06000219 RID: 537 RVA: 0x0000B100 File Offset: 0x00009300
		public bool GetIsValueChanged(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
		{
			switch (propertyType)
			{
			case BrushAnimationProperty.BrushAnimationPropertyType.Name:
				return false;
			case BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor:
				return this._isColorFactorChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.Color:
				return this._isSpriteChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor:
				return this._isAlphaFactorChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.HueFactor:
				return this._isHueFactorChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor:
				return this._isSaturationFactorChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor:
				return this._isValueFactorChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset:
				return this._isOverlayXOffsetChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset:
				return this._isOverlayYOffsetChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.Sprite:
				return this._isSpriteChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.IsHidden:
				return this._isIsHiddenChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
				return this._isXOffsetChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
				return this._isYOffsetChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.Rotation:
				return this._isRotationChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.OverridenWidth:
				return this._isOverridenWidthChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.OverridenHeight:
				return this._isOverridenHeightChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.WidthPolicy:
				return this._isWidthPolicyChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.HeightPolicy:
				return this._isHeightPolicyChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.HorizontalFlip:
				return this._isHorizontalFlipChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.VerticalFlip:
				return this._isVerticalFlipChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.OverlayMethod:
				return this._isOverlayMethodChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.OverlaySprite:
				return this._isOverlaySpriteChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.ExtendLeft:
				return this._isExtendLeftChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.ExtendRight:
				return this._isExtendRightChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.ExtendTop:
				return this._isExtendTopChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.ExtendBottom:
				return this._isExtendBottomChanged;
			case BrushAnimationProperty.BrushAnimationPropertyType.UseRandomBaseOverlayXOffset:
				return this._isUseRandomBaseOverlayXOffset;
			case BrushAnimationProperty.BrushAnimationPropertyType.UseRandomBaseOverlayYOffset:
				return this._isUseRandomBaseOverlayYOffset;
			}
			return false;
		}

		// Token: 0x040000D2 RID: 210
		private uint _localVersion;

		// Token: 0x040000D3 RID: 211
		private bool _isImageFitTypeChanged;

		// Token: 0x040000D4 RID: 212
		private bool _isImageFitHorizontalAlignmentChanged;

		// Token: 0x040000D5 RID: 213
		private bool _isImageFitVerticalAlignmentChanged;

		// Token: 0x040000D6 RID: 214
		private bool _isSpriteChanged;

		// Token: 0x040000D7 RID: 215
		private bool _isColorChanged;

		// Token: 0x040000D8 RID: 216
		private bool _isColorFactorChanged;

		// Token: 0x040000D9 RID: 217
		private bool _isAlphaFactorChanged;

		// Token: 0x040000DA RID: 218
		private bool _isHueFactorChanged;

		// Token: 0x040000DB RID: 219
		private bool _isSaturationFactorChanged;

		// Token: 0x040000DC RID: 220
		private bool _isValueFactorChanged;

		// Token: 0x040000DD RID: 221
		private bool _isIsHiddenChanged;

		// Token: 0x040000DE RID: 222
		private bool _isXOffsetChanged;

		// Token: 0x040000DF RID: 223
		private bool _isYOffsetChanged;

		// Token: 0x040000E0 RID: 224
		private bool _isRotationChanged;

		// Token: 0x040000E1 RID: 225
		private bool _isExtendLeftChanged;

		// Token: 0x040000E2 RID: 226
		private bool _isExtendRightChanged;

		// Token: 0x040000E3 RID: 227
		private bool _isExtendTopChanged;

		// Token: 0x040000E4 RID: 228
		private bool _isExtendBottomChanged;

		// Token: 0x040000E5 RID: 229
		private bool _isOverridenWidthChanged;

		// Token: 0x040000E6 RID: 230
		private bool _isOverridenHeightChanged;

		// Token: 0x040000E7 RID: 231
		private bool _isWidthPolicyChanged;

		// Token: 0x040000E8 RID: 232
		private bool _isHeightPolicyChanged;

		// Token: 0x040000E9 RID: 233
		private bool _isHorizontalFlipChanged;

		// Token: 0x040000EA RID: 234
		private bool _isVerticalFlipChanged;

		// Token: 0x040000EB RID: 235
		private bool _isOverlayMethodChanged;

		// Token: 0x040000EC RID: 236
		private bool _isOverlaySpriteChanged;

		// Token: 0x040000ED RID: 237
		private bool _isUseOverlayAlphaAsMaskChanged;

		// Token: 0x040000EE RID: 238
		private bool _isOverlayXOffsetChanged;

		// Token: 0x040000EF RID: 239
		private bool _isOverlayYOffsetChanged;

		// Token: 0x040000F0 RID: 240
		private bool _isUseRandomBaseOverlayXOffset;

		// Token: 0x040000F1 RID: 241
		private bool _isUseRandomBaseOverlayYOffset;

		// Token: 0x040000F2 RID: 242
		private ImageFit.ImageFitTypes _imageFitType;

		// Token: 0x040000F3 RID: 243
		private ImageFit.ImageHorizontalAlignments _imageFitHorizontalAlignment;

		// Token: 0x040000F4 RID: 244
		private ImageFit.ImageVerticalAlignments _imageFitVerticalAlignment;

		// Token: 0x040000F5 RID: 245
		private Sprite _sprite;

		// Token: 0x040000F6 RID: 246
		private Color _color;

		// Token: 0x040000F7 RID: 247
		private float _colorFactor;

		// Token: 0x040000F8 RID: 248
		private float _alphaFactor;

		// Token: 0x040000F9 RID: 249
		private float _hueFactor;

		// Token: 0x040000FA RID: 250
		private float _saturationFactor;

		// Token: 0x040000FB RID: 251
		private float _valueFactor;

		// Token: 0x040000FC RID: 252
		private bool _isHidden;

		// Token: 0x040000FD RID: 253
		private bool _useOverlayAlphaAsMask;

		// Token: 0x040000FE RID: 254
		private float _xOffset;

		// Token: 0x040000FF RID: 255
		private float _yOffset;

		// Token: 0x04000100 RID: 256
		private float _rotation;

		// Token: 0x04000101 RID: 257
		private float _extendLeft;

		// Token: 0x04000102 RID: 258
		private float _extendRight;

		// Token: 0x04000103 RID: 259
		private float _extendTop;

		// Token: 0x04000104 RID: 260
		private float _extendBottom;

		// Token: 0x04000105 RID: 261
		private float _overridenWidth;

		// Token: 0x04000106 RID: 262
		private float _overridenHeight;

		// Token: 0x04000107 RID: 263
		private BrushLayerSizePolicy _widthPolicy;

		// Token: 0x04000108 RID: 264
		private BrushLayerSizePolicy _heightPolicy;

		// Token: 0x04000109 RID: 265
		private bool _horizontalFlip;

		// Token: 0x0400010A RID: 266
		private bool _verticalFlip;

		// Token: 0x0400010B RID: 267
		private BrushOverlayMethod _overlayMethod;

		// Token: 0x0400010C RID: 268
		private Sprite _overlaySprite;

		// Token: 0x0400010D RID: 269
		private float _overlayXOffset;

		// Token: 0x0400010E RID: 270
		private float _overlayYOffset;

		// Token: 0x0400010F RID: 271
		private bool _useRandomBaseOverlayXOffset;

		// Token: 0x04000110 RID: 272
		private bool _useRandomBaseOverlayYOffset;
	}
}
