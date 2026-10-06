namespace TaleWorlds.TwoDimension;

public class TwoDimensionContextObject
{
	public TwoDimensionContext Context { get; private set; }

	protected TwoDimensionContextObject(TwoDimensionContext context)
	{
		Context = context;
	}
}
