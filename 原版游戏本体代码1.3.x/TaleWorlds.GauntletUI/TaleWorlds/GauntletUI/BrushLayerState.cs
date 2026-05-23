using System;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000015 RID: 21
	public struct BrushLayerState : IBrushAnimationState, IDataSource
	{
		// Token: 0x06000155 RID: 341 RVA: 0x00007174 File Offset: 0x00005374
		public void FillFrom(IBrushLayerData styleLayer)
		{
			this.ColorFactor = styleLayer.ColorFactor;
			this.AlphaFactor = styleLayer.AlphaFactor;
			this.HueFactor = styleLayer.HueFactor;
			this.SaturationFactor = styleLayer.SaturationFactor;
			this.ValueFactor = styleLayer.ValueFactor;
			this.Color = styleLayer.Color;
			this.OverlayXOffset = styleLayer.OverlayXOffset;
			this.OverlayYOffset = styleLayer.OverlayYOffset;
			this.XOffset = styleLayer.XOffset;
			this.YOffset = styleLayer.YOffset;
			this.Rotation = styleLayer.Rotation;
			this.ExtendRight = styleLayer.ExtendRight;
			this.ExtendTop = styleLayer.ExtendTop;
			this.ExtendBottom = styleLayer.ExtendBottom;
			this.ExtendLeft = styleLayer.ExtendLeft;
			this.Sprite = styleLayer.Sprite;
		}

		// Token: 0x06000156 RID: 342 RVA: 0x00007244 File Offset: 0x00005444
		void IBrushAnimationState.FillFrom(IDataSource source)
		{
			StyleLayer styleLayer = (StyleLayer)source;
			this.FillFrom(styleLayer);
		}

		// Token: 0x06000157 RID: 343 RVA: 0x00007260 File Offset: 0x00005460
		void IBrushAnimationState.LerpFrom(IBrushAnimationState start, IDataSource end, float ratio)
		{
			BrushLayerState start2 = (BrushLayerState)start;
			IBrushLayerData end2 = (IBrushLayerData)end;
			this.LerpFrom(start2, end2, ratio);
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00007284 File Offset: 0x00005484
		public void LerpFrom(BrushLayerState start, IBrushLayerData end, float ratio)
		{
			this.ColorFactor = Mathf.Lerp(start.ColorFactor, end.ColorFactor, ratio);
			this.AlphaFactor = Mathf.Lerp(start.AlphaFactor, end.AlphaFactor, ratio);
			this.HueFactor = Mathf.Lerp(start.HueFactor, end.HueFactor, ratio);
			this.SaturationFactor = Mathf.Lerp(start.SaturationFactor, end.SaturationFactor, ratio);
			this.ValueFactor = Mathf.Lerp(start.ValueFactor, end.ValueFactor, ratio);
			this.Color = Color.Lerp(start.Color, end.Color, ratio);
			this.OverlayXOffset = Mathf.Lerp(start.OverlayXOffset, end.OverlayXOffset, ratio);
			this.OverlayYOffset = Mathf.Lerp(start.OverlayYOffset, end.OverlayYOffset, ratio);
			this.XOffset = Mathf.Lerp(start.XOffset, end.XOffset, ratio);
			this.YOffset = Mathf.Lerp(start.YOffset, end.YOffset, ratio);
			this.Rotation = Mathf.Lerp(start.Rotation, end.Rotation, ratio);
			this.ExtendRight = Mathf.Lerp(start.ExtendRight, end.ExtendRight, ratio);
			this.ExtendTop = Mathf.Lerp(start.ExtendTop, end.ExtendTop, ratio);
			this.ExtendBottom = Mathf.Lerp(start.ExtendBottom, end.ExtendBottom, ratio);
			this.ExtendLeft = Mathf.Lerp(start.ExtendLeft, end.ExtendLeft, ratio);
			this.Sprite = ((ratio > 0.9f) ? end.Sprite : start.Sprite);
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00007418 File Offset: 0x00005618
		public void SetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType, float value)
		{
			switch (propertyType)
			{
			case BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor:
				this.ColorFactor = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.Color:
			case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
				break;
			case BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor:
				this.AlphaFactor = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.HueFactor:
				this.HueFactor = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor:
				this.SaturationFactor = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor:
				this.ValueFactor = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset:
				this.OverlayXOffset = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset:
				this.OverlayYOffset = value;
				return;
			default:
				switch (propertyType)
				{
				case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
					this.XOffset = value;
					return;
				case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
					this.YOffset = value;
					return;
				case BrushAnimationProperty.BrushAnimationPropertyType.Rotation:
					this.Rotation = value;
					return;
				default:
					switch (propertyType)
					{
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendLeft:
						this.ExtendLeft = value;
						return;
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendRight:
						this.ExtendRight = value;
						return;
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendTop:
						this.ExtendTop = value;
						return;
					case BrushAnimationProperty.BrushAnimationPropertyType.ExtendBottom:
						this.ExtendBottom = value;
						return;
					}
					break;
				}
				break;
			}
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "SetValueAsFloat", 139);
		}

		// Token: 0x0600015A RID: 346 RVA: 0x0000750A File Offset: 0x0000570A
		public void SetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType, in Color value)
		{
			if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Color)
			{
				this.Color = value;
				return;
			}
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "SetValueAsColor", 152);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00007536 File Offset: 0x00005736
		public void SetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType, Sprite value)
		{
			if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Sprite)
			{
				this.Sprite = value;
				return;
			}
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "SetValueAsSprite", 165);
		}

		// Token: 0x0600015C RID: 348 RVA: 0x00007560 File Offset: 0x00005760
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
				default:
					switch (propertyType)
					{
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
				break;
			}
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "GetValueAsFloat", 203);
			return 0f;
		}

		// Token: 0x0600015D RID: 349 RVA: 0x00007649 File Offset: 0x00005849
		public Color GetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
		{
			if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Color)
			{
				return this.Color;
			}
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "GetValueAsColor", 215);
			return Color.Black;
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00007674 File Offset: 0x00005874
		public Sprite GetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
		{
			if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Sprite)
			{
				return this.Sprite;
			}
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayerState.cs", "GetValueAsSprite", 227);
			return null;
		}

		// Token: 0x0600015F RID: 351 RVA: 0x0000769C File Offset: 0x0000589C
		public static void SetValueAsLerpOfValues(ref BrushLayerState currentState, in BrushAnimationKeyFrame startValue, in BrushAnimationKeyFrame endValue, BrushAnimationProperty.BrushAnimationPropertyType propertyType, float ratio)
		{
			switch (propertyType)
			{
			case BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor:
			case BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor:
			case BrushAnimationProperty.BrushAnimationPropertyType.HueFactor:
			case BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor:
			case BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor:
			case BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset:
			case BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineAmount:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowRadius:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextBlur:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowOffset:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowAngle:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextColorFactor:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextAlphaFactor:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextHueFactor:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextSaturationFactor:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextValueFactor:
			case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
			case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
			case BrushAnimationProperty.BrushAnimationPropertyType.Rotation:
				currentState.SetValueAsFloat(propertyType, MathF.Lerp(startValue.GetValueAsFloat(), endValue.GetValueAsFloat(), ratio, 1E-05f));
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.Color:
			case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
			{
				Color color = Color.Lerp(startValue.GetValueAsColor(), endValue.GetValueAsColor(), ratio);
				currentState.SetValueAsColor(propertyType, color);
				return;
			}
			case BrushAnimationProperty.BrushAnimationPropertyType.Sprite:
				currentState.SetValueAsSprite(propertyType, ((double)ratio > 0.9) ? endValue.GetValueAsSprite() : startValue.GetValueAsSprite());
				break;
			case BrushAnimationProperty.BrushAnimationPropertyType.IsHidden:
				break;
			default:
				return;
			}
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00007781 File Offset: 0x00005981
		void IBrushAnimationState.SetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType, in Color value)
		{
			this.SetValueAsColor(propertyType, value);
		}

		// Token: 0x04000073 RID: 115
		public Color Color;

		// Token: 0x04000074 RID: 116
		public float ColorFactor;

		// Token: 0x04000075 RID: 117
		public float AlphaFactor;

		// Token: 0x04000076 RID: 118
		public float HueFactor;

		// Token: 0x04000077 RID: 119
		public float SaturationFactor;

		// Token: 0x04000078 RID: 120
		public float ValueFactor;

		// Token: 0x04000079 RID: 121
		public float OverlayXOffset;

		// Token: 0x0400007A RID: 122
		public float OverlayYOffset;

		// Token: 0x0400007B RID: 123
		public float XOffset;

		// Token: 0x0400007C RID: 124
		public float YOffset;

		// Token: 0x0400007D RID: 125
		public float Rotation;

		// Token: 0x0400007E RID: 126
		public float ExtendRight;

		// Token: 0x0400007F RID: 127
		public float ExtendTop;

		// Token: 0x04000080 RID: 128
		public float ExtendBottom;

		// Token: 0x04000081 RID: 129
		public float ExtendLeft;

		// Token: 0x04000082 RID: 130
		public Sprite Sprite;
	}
}
