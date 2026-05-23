using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.ClassLoadout;

public class MultiplayerClassLoadoutItemTabListPanel : ListPanel
{
	private bool _isInitialized;

	public event Action OnInitialized;

	public MultiplayerClassLoadoutItemTabListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_isInitialized)
		{
			_isInitialized = true;
			this.OnInitialized?.Invoke();
		}
	}
}
