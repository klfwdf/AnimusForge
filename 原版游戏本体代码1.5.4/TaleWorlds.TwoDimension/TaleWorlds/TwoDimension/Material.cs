namespace TaleWorlds.TwoDimension;

public abstract class Material
{
	public bool Blending { get; private set; }

	public int RenderOrder { get; private set; }

	protected Material(bool blending, int renderOrder)
	{
		Blending = blending;
		RenderOrder = renderOrder;
	}
}
