using TaleWorlds.Library;

namespace TaleWorlds.Diamond.ClientApplication;

public abstract class DiamondClientApplicationObject
{
	private DiamondClientApplication _application;

	public DiamondClientApplication Application => _application;

	public ApplicationVersion ApplicationVersion => Application.ApplicationVersion;

	protected DiamondClientApplicationObject(DiamondClientApplication application)
	{
		_application = application;
	}
}
