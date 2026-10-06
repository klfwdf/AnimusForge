using System;

namespace SandBox.ViewModelCollection.Map.Cheat;

public class CheatActionItemVM : CheatItemBaseVM
{
	public readonly GameplayCheatItem Cheat;

	private readonly Action<CheatActionItemVM> _onCheatExecuted;

	public CheatActionItemVM(GameplayCheatItem cheat, Action<CheatActionItemVM> onCheatExecuted)
	{
		_onCheatExecuted = onCheatExecuted;
		Cheat = cheat;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Name = Cheat?.GetName().ToString();
	}

	public override void ExecuteAction()
	{
		Cheat?.ExecuteCheat();
		_onCheatExecuted?.Invoke(this);
	}
}
