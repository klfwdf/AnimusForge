using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.GameState;

public class TutorialState : TaleWorlds.Core.GameState
{
	private MBObjectManager _objectManager = MBObjectManager.Instance;

	public MenuContext MenuContext = MBObjectManager.Instance.CreateObject<MenuContext>();

	public override bool IsMenuState => true;

	protected override void OnActivate()
	{
		base.OnActivate();
		MenuContext.Refresh();
	}

	protected override void OnFinalize()
	{
		MenuContext.Destroy();
		_objectManager.UnregisterObject(MenuContext);
		MenuContext = null;
		base.OnFinalize();
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		MenuContext.OnTick(dt);
	}
}
