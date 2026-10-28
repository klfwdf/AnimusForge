namespace TaleWorlds.Core;

public class SaddleComponent : ItemComponent
{
	public SaddleComponent(SaddleComponent saddleComponent)
	{
	}

	public override ItemComponent GetCopy()
	{
		return new SaddleComponent(this);
	}
}
