using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public interface IBrushLayerData
{
	string Name { get; set; }

	Sprite Sprite { get; set; }

	Color Color { get; set; }

	float ColorFactor { get; set; }

	float AlphaFactor { get; set; }

	float HueFactor { get; set; }

	float SaturationFactor { get; set; }

	float ValueFactor { get; set; }

	bool IsHidden { get; set; }

	float XOffset { get; set; }

	float YOffset { get; set; }

	float Rotation { get; set; }

	float ExtendLeft { get; set; }

	float ExtendRight { get; set; }

	float ExtendTop { get; set; }

	float ExtendBottom { get; set; }

	float OverridenWidth { get; set; }

	float OverridenHeight { get; set; }

	BrushLayerSizePolicy WidthPolicy { get; set; }

	BrushLayerSizePolicy HeightPolicy { get; set; }

	bool HorizontalFlip { get; set; }

	bool VerticalFlip { get; set; }

	bool UseOverlayAlphaAsMask { get; set; }

	BrushOverlayMethod OverlayMethod { get; set; }

	Sprite OverlaySprite { get; set; }

	float OverlayXOffset { get; set; }

	float OverlayYOffset { get; set; }

	bool UseRandomBaseOverlayXOffset { get; set; }

	bool UseRandomBaseOverlayYOffset { get; set; }

	ImageFit.ImageFitTypes ImageFitType { get; set; }

	ImageFit.ImageHorizontalAlignments ImageFitHorizontalAlignment { get; set; }

	ImageFit.ImageVerticalAlignments ImageFitVerticalAlignment { get; set; }

	float GetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType);

	Color GetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType);

	Sprite GetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType);
}
