using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState;

public class CraftingState : TaleWorlds.Core.GameState
{
	private ICraftingStateHandler _handler;

	public override bool IsMenuState => true;

	public Crafting CraftingLogic { get; private set; }

	public ICraftingStateHandler Handler
	{
		get
		{
			return _handler;
		}
		set
		{
			_handler = value;
		}
	}

	public void InitializeLogic(Crafting newCraftingLogic, bool isReplacingWeaponClass = false)
	{
		CraftingLogic = newCraftingLogic;
		if (_handler != null)
		{
			if (isReplacingWeaponClass)
			{
				_handler.OnCraftingLogicRefreshed();
			}
			else
			{
				_handler.OnCraftingLogicInitialized();
			}
		}
	}
}
