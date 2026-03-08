using System;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000018 RID: 24
	public struct BrushState : IBrushAnimationState, IDataSource
	{
		// Token: 0x0600018C RID: 396 RVA: 0x0000923C File Offset: 0x0000743C
		public void FillFrom(Style style)
		{
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
		}

		// Token: 0x0600018D RID: 397 RVA: 0x000092E8 File Offset: 0x000074E8
		public void LerpFrom(BrushState start, Style end, float ratio)
		{
			this.FontColor = Color.Lerp(start.FontColor, end.FontColor, ratio);
			this.TextGlowColor = Color.Lerp(start.TextGlowColor, end.TextGlowColor, ratio);
			this.TextOutlineColor = Color.Lerp(start.TextOutlineColor, end.TextOutlineColor, ratio);
			this.TextOutlineAmount = Mathf.Lerp(start.TextOutlineAmount, end.TextOutlineAmount, ratio);
			this.TextGlowRadius = Mathf.Lerp(start.TextGlowRadius, end.TextGlowRadius, ratio);
			this.TextBlur = Mathf.Lerp(start.TextBlur, end.TextBlur, ratio);
			this.TextShadowOffset = Mathf.Lerp(start.TextShadowOffset, end.TextShadowOffset, ratio);
			this.TextShadowAngle = Mathf.Lerp(start.TextShadowAngle, end.TextShadowAngle, ratio);
			this.TextColorFactor = Mathf.Lerp(start.TextColorFactor, end.TextColorFactor, ratio);
			this.TextAlphaFactor = Mathf.Lerp(start.TextAlphaFactor, end.TextAlphaFactor, ratio);
			this.TextHueFactor = Mathf.Lerp(start.TextHueFactor, end.TextHueFactor, ratio);
			this.TextSaturationFactor = Mathf.Lerp(start.TextSaturationFactor, end.TextSaturationFactor, ratio);
			this.TextValueFactor = Mathf.Lerp(start.TextValueFactor, end.TextValueFactor, ratio);
		}

		// Token: 0x0600018E RID: 398 RVA: 0x00009430 File Offset: 0x00007630
		void IBrushAnimationState.FillFrom(IDataSource source)
		{
			Style style = (Style)source;
			this.FillFrom(style);
		}

		// Token: 0x0600018F RID: 399 RVA: 0x0000944C File Offset: 0x0000764C
		void IBrushAnimationState.LerpFrom(IBrushAnimationState start, IDataSource end, float ratio)
		{
			BrushState start2 = (BrushState)start;
			Style end2 = (Style)end;
			this.LerpFrom(start2, end2, ratio);
		}

		// Token: 0x06000190 RID: 400 RVA: 0x00009470 File Offset: 0x00007670
		public float GetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
		{
			switch (propertyType)
			{
			case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
				Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsFloat", 102);
				return 0f;
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
			}
			Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsFloat", 106);
			return 0f;
		}

		// Token: 0x06000191 RID: 401 RVA: 0x00009540 File Offset: 0x00007740
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
				Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsColor", 132);
				return Color.Black;
			}
			Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsColor", 135);
			return Color.Black;
		}

		// Token: 0x06000192 RID: 402 RVA: 0x000095E8 File Offset: 0x000077E8
		public Sprite GetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
		{
			if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.FontColor || propertyType - BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor <= 11)
			{
				Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsSprite", 157);
				return null;
			}
			Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "GetValueAsSprite", 161);
			return null;
		}

		// Token: 0x06000193 RID: 403 RVA: 0x00009638 File Offset: 0x00007838
		public void SetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType, float value)
		{
			switch (propertyType)
			{
			case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
			case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
				Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsFloat", 204);
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineAmount:
				this.TextOutlineAmount = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowRadius:
				this.TextGlowRadius = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextBlur:
				this.TextBlur = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowOffset:
				this.TextShadowOffset = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowAngle:
				this.TextShadowAngle = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextColorFactor:
				this.TextColorFactor = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextAlphaFactor:
				this.TextAlphaFactor = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextHueFactor:
				this.TextHueFactor = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextSaturationFactor:
				this.TextSaturationFactor = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextValueFactor:
				this.TextValueFactor = value;
				return;
			}
			Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsFloat", 208);
		}

		// Token: 0x06000194 RID: 404 RVA: 0x00009710 File Offset: 0x00007910
		public void SetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType, in Color value)
		{
			switch (propertyType)
			{
			case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
				this.FontColor = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
				this.TextGlowColor = value;
				return;
			case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
				this.TextOutlineColor = value;
				return;
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
				Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsColor", 237);
				return;
			}
			Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsColor", 240);
		}

		// Token: 0x06000195 RID: 405 RVA: 0x000097C0 File Offset: 0x000079C0
		public void SetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType, Sprite value)
		{
			if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.FontColor || propertyType - BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor <= 11)
			{
				Debug.FailedAssert("Invalid value type for BrushState.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsSprite", 262);
				return;
			}
			Debug.FailedAssert("Invalid BrushState property.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushState.cs", "SetValueAsSprite", 265);
		}

		// Token: 0x06000196 RID: 406 RVA: 0x0000980C File Offset: 0x00007A0C
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

		// Token: 0x06000197 RID: 407 RVA: 0x000098BB File Offset: 0x00007ABB
		void IBrushAnimationState.SetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType, in Color value)
		{
			this.SetValueAsColor(propertyType, value);
		}

		// Token: 0x04000099 RID: 153
		public Color FontColor;

		// Token: 0x0400009A RID: 154
		public Color TextGlowColor;

		// Token: 0x0400009B RID: 155
		public Color TextOutlineColor;

		// Token: 0x0400009C RID: 156
		public float TextOutlineAmount;

		// Token: 0x0400009D RID: 157
		public float TextGlowRadius;

		// Token: 0x0400009E RID: 158
		public float TextBlur;

		// Token: 0x0400009F RID: 159
		public float TextShadowOffset;

		// Token: 0x040000A0 RID: 160
		public float TextShadowAngle;

		// Token: 0x040000A1 RID: 161
		public float TextColorFactor;

		// Token: 0x040000A2 RID: 162
		public float TextAlphaFactor;

		// Token: 0x040000A3 RID: 163
		public float TextHueFactor;

		// Token: 0x040000A4 RID: 164
		public float TextSaturationFactor;

		// Token: 0x040000A5 RID: 165
		public float TextValueFactor;
	}
}
