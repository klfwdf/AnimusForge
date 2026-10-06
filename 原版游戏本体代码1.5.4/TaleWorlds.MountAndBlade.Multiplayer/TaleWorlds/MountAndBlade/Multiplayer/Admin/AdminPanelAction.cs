using System;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

internal class AdminPanelAction : IAdminPanelActionInternal, IAdminPanelAction
{
	private readonly string _uniqueId;

	private TextObject _nameTextObj;

	private TextObject _descriptionTextObj;

	private Action _onActionExecuted;

	public string UniqueId => _uniqueId;

	public string Name => _nameTextObj?.ToString() ?? string.Empty;

	public string Description => _descriptionTextObj?.ToString() ?? string.Empty;

	public AdminPanelAction(string uniqueId)
	{
		_uniqueId = uniqueId;
	}

	public virtual void OnFinalize()
	{
	}

	void IAdminPanelAction.OnActionExecuted()
	{
		_onActionExecuted?.Invoke();
	}

	public virtual bool GetIsAvailable()
	{
		return true;
	}

	public virtual bool GetIsDisabled(out string reason)
	{
		reason = string.Empty;
		return false;
	}

	public AdminPanelAction BuildName(TextObject name)
	{
		_nameTextObj = name;
		return this;
	}

	public AdminPanelAction BuildDescription(TextObject description)
	{
		_descriptionTextObj = description;
		return this;
	}

	public AdminPanelAction BuildOnActionExecutedCallback(Action onActionExecuted)
	{
		_onActionExecuted = onActionExecuted;
		return this;
	}
}
