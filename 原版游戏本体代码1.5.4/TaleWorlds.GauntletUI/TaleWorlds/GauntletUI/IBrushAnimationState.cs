using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public interface IBrushAnimationState
{
	void FillFrom(IDataSource source);

	void LerpFrom(IBrushAnimationState start, IDataSource end, float ratio);

	float GetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType);

	Color GetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType);

	Sprite GetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType);

	void SetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType, float value);

	void SetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType, in Color value);

	void SetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType, Sprite value);
}
