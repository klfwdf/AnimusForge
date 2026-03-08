using System;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000014 RID: 20
	public interface IBrushAnimationState
	{
		// Token: 0x0600014D RID: 333
		void FillFrom(IDataSource source);

		// Token: 0x0600014E RID: 334
		void LerpFrom(IBrushAnimationState start, IDataSource end, float ratio);

		// Token: 0x0600014F RID: 335
		float GetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType);

		// Token: 0x06000150 RID: 336
		Color GetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType);

		// Token: 0x06000151 RID: 337
		Sprite GetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType);

		// Token: 0x06000152 RID: 338
		void SetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType, float value);

		// Token: 0x06000153 RID: 339
		void SetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType, in Color value);

		// Token: 0x06000154 RID: 340
		void SetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType, Sprite value);
	}
}
