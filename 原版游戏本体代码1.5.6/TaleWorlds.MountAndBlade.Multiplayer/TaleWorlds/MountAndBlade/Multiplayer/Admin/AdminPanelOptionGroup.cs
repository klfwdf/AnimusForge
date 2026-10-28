using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

internal class AdminPanelOptionGroup : IAdminPanelOptionGroup, IAdminPanelTickable
{
	private readonly bool _requiresRestart;

	private readonly string _uniqueId;

	private readonly TextObject _nameTextObj;

	private readonly MBList<IAdminPanelOption> _options;

	private readonly MBList<IAdminPanelAction> _actions;

	private readonly MBList<IAdminPanelTickable> _tickableOptions;

	string IAdminPanelOptionGroup.UniqueId => _uniqueId;

	TextObject IAdminPanelOptionGroup.Name => _nameTextObj;

	MBReadOnlyList<IAdminPanelOption> IAdminPanelOptionGroup.Options => _options;

	MBReadOnlyList<IAdminPanelAction> IAdminPanelOptionGroup.Actions => _actions;

	bool IAdminPanelOptionGroup.RequiresRestart => _requiresRestart;

	public AdminPanelOptionGroup(string uniqueId, TextObject name, bool requiresRestart = false)
	{
		_uniqueId = uniqueId;
		_nameTextObj = name;
		_requiresRestart = requiresRestart;
		_options = new MBList<IAdminPanelOption>();
		_actions = new MBList<IAdminPanelAction>();
		_tickableOptions = new MBList<IAdminPanelTickable>();
	}

	public void AddOption(IAdminPanelOption option)
	{
		_options.Add(option);
		if (option is IAdminPanelTickable item)
		{
			_tickableOptions.Add(item);
		}
	}

	public void AddAction(IAdminPanelAction action)
	{
		_actions.Add(action);
	}

	void IAdminPanelTickable.OnTick(float dt)
	{
		for (int i = 0; i < _tickableOptions.Count; i++)
		{
			_tickableOptions[i].OnTick(dt);
		}
	}

	void IAdminPanelOptionGroup.OnFinalize()
	{
		for (int i = 0; i < _options.Count; i++)
		{
			if (_options[i] is IAdminPanelOptionInternal adminPanelOptionInternal)
			{
				adminPanelOptionInternal.OnFinalize();
			}
		}
		for (int j = 0; j < _actions.Count; j++)
		{
			if (_actions[j] is IAdminPanelActionInternal adminPanelActionInternal)
			{
				adminPanelActionInternal.OnFinalize();
			}
		}
	}
}
