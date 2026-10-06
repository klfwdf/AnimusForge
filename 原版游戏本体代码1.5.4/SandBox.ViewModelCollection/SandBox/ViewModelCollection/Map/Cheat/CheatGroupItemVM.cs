using System;

namespace SandBox.ViewModelCollection.Map.Cheat;

public class CheatGroupItemVM : CheatItemBaseVM
{
	public readonly GameplayCheatGroup CheatGroup;

	private readonly Action<CheatGroupItemVM> _onSelectCheatGroup;

	public CheatGroupItemVM(GameplayCheatGroup cheatGroup, Action<CheatGroupItemVM> onSelectCheatGroup)
	{
		CheatGroup = cheatGroup;
		_onSelectCheatGroup = onSelectCheatGroup;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Name = CheatGroup.GetName()?.ToString();
	}

	public override void ExecuteAction()
	{
		_onSelectCheatGroup?.Invoke(this);
	}
}
