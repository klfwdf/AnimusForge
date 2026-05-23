using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class BannerBuilderState : GameState
{
	public override bool IsMenuState => true;

	public string DefaultBannerKey { get; }

	public BannerBuilderState()
	{
	}

	public BannerBuilderState(string defaultBannerKey)
	{
		DefaultBannerKey = defaultBannerKey;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
	}
}
