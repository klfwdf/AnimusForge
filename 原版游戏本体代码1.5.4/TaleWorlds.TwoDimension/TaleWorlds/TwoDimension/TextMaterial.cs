using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public class TextMaterial : Material
{
	public Texture Texture { get; set; }

	public Color Color { get; set; }

	public float SmoothingConstant { get; set; }

	public bool Smooth { get; set; }

	public float ScaleFactor { get; set; }

	public Color GlowColor { get; set; }

	public Color OutlineColor { get; set; }

	public float OutlineAmount { get; set; }

	public float GlowRadius { get; set; }

	public float Blur { get; set; }

	public float ShadowOffset { get; set; }

	public float ShadowAngle { get; set; }

	public float ColorFactor { get; set; }

	public float AlphaFactor { get; set; }

	public float HueFactor { get; set; }

	public float SaturationFactor { get; set; }

	public float ValueFactor { get; set; }

	public TextMaterial()
		: this(null, 0)
	{
	}

	public TextMaterial(Texture texture)
		: this(texture, 0)
	{
	}

	public TextMaterial(Texture texture, int renderOrder)
		: this(texture, renderOrder, blending: true)
	{
	}

	public TextMaterial(Texture texture, int renderOrder, bool blending)
		: base(blending, renderOrder)
	{
		Texture = texture;
		ScaleFactor = 1f;
		SmoothingConstant = 0.47f;
		Smooth = true;
		Color = new Color(1f, 1f, 1f);
		GlowColor = new Color(0f, 0f, 0f);
		OutlineColor = new Color(0f, 0f, 0f);
		OutlineAmount = 0f;
		GlowRadius = 0f;
		Blur = 0f;
		ShadowOffset = 0f;
		ShadowAngle = 0f;
		ColorFactor = 1f;
		AlphaFactor = 1f;
		HueFactor = 0f;
		SaturationFactor = 0f;
		ValueFactor = 0f;
	}

	public void CopyFrom(TextMaterial sourceMaterial)
	{
		Texture = sourceMaterial.Texture;
		Color = sourceMaterial.Color;
		ScaleFactor = sourceMaterial.ScaleFactor;
		SmoothingConstant = sourceMaterial.SmoothingConstant;
		Smooth = sourceMaterial.Smooth;
		GlowColor = sourceMaterial.GlowColor;
		OutlineColor = sourceMaterial.OutlineColor;
		OutlineAmount = sourceMaterial.OutlineAmount;
		GlowRadius = sourceMaterial.GlowRadius;
		Blur = sourceMaterial.Blur;
		ShadowOffset = sourceMaterial.ShadowOffset;
		ShadowAngle = sourceMaterial.ShadowAngle;
		ColorFactor = sourceMaterial.ColorFactor;
		AlphaFactor = sourceMaterial.AlphaFactor;
		HueFactor = sourceMaterial.HueFactor;
		SaturationFactor = sourceMaterial.SaturationFactor;
		ValueFactor = sourceMaterial.ValueFactor;
	}
}
